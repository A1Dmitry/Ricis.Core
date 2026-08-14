using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Polynomial;
using Ricis.Core.SpecialFunctions;
using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// RICIS Phase 1 — SP2 Clean First.
/// Algebraic cancellation BEFORE singularity axioms.
/// NO limits. NO L'Hôpital.
///
///   X/X → 1
///   polynomial long division when possible
///   remaining 0_F/0_G is NOT collapsed numerically — left for A4 (F/G)
/// </summary>
public class AlgebraicReductionVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType != ExpressionType.Divide)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        // RICIS only overrides the built-in arithmetic algebra. A custom
        // operator may have non-classical side effects or semantics, so it
        // remains an untouched classical expression.
        if (node.Method is not null && !NumericConstants.IsIntrinsicNumeric(node.Type))
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        // SP2: (F/A) / (G/A) → F/G. This must run before the limit bridge
        // can replace either inner ratio by 0_F or ∞_F.
        if (left is BinaryExpression { NodeType: ExpressionType.Divide } leftRatio &&
            right is BinaryExpression { NodeType: ExpressionType.Divide } rightRatio &&
            leftRatio.Right.AreEqual(rightRatio.Right))
        {
            return Visit(Expression.Divide(leftRatio.Left, rightRatio.Left));
        }

        // SP2 / L1: identical subtrees → 1
        if (left.AreEqual(right))
        {
            return NumericConstants.OneOf(left.Type);
        }

        // SP2 factorization: (A²−B²)/(A−B) → A+B. This exact structural
        // identity is evaluated before generic factor cancellation and before
        // polynomial division, so string-parsed powers receive the same result.
        var differenceOfSquares = TryReduceDifferenceOfSquares(left, right);
        if (differenceOfSquares is not null)
        {
            return Visit(differenceOfSquares);
        }

        // SP2 extension for factorials: n! / (n-1)! → n. The rule is
        // applied structurally, before a delegate can materialize n!.
        var factorialRatio = TryReduceAdjacentFactorials(left, right);
        if (factorialRatio is not null)
        {
            return Visit(factorialRatio);
        }

        var associativeCancellation = TryCancelAssociativeFactors(left, right);
        if (associativeCancellation is not null)
        {
            return Visit(associativeCancellation);
        }

        var cancelled = TryCancelCommonFactor(left, right);
        if (cancelled is not null)
        {
            return Visit(cancelled);
        }

        // A constant denominator is a usable deferred ratio, not a request to
        // rewrite F/C as a floating-point coefficient. Preserve Divide(F, C).
        if (right is ConstantExpression)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        var parameter = FindSingleParameter(node);
        if (parameter == null)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        var cache = AnalyzeDenominator(right, parameter);

        // Pure RICIS: do NOT invent ∞ from numerical 0/0 here.
        // If long division cancels the common factor, return the quotient (SP2).
        // If not, leave Divide intact for Phase 2 (A4: F/G by identity).

        if (!cache.isPolynomial)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        var divided = left.TryDivide(right, parameter);
        if (divided != null)
        {
            return Visit(divided);
        }

        return left == node.Left && right == node.Right
            ? node
            : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    /// <summary>
    /// Exact structural factorization of a difference of squares. The
    /// denominator supplies the two deferred factors, so a constant square
    /// such as 25 is accepted as the normalised square of its factor 5.
    /// </summary>
    private static Expression TryReduceDifferenceOfSquares(Expression numerator, Expression denominator)
    {
        if (numerator is not BinaryExpression { NodeType: ExpressionType.Subtract } difference ||
            denominator is not BinaryExpression denominatorBinary)
        {
            return null;
        }

        if (denominatorBinary.NodeType == ExpressionType.Subtract &&
            IsSquareOf(difference.Left, denominatorBinary.Left) &&
            IsSquareOf(difference.Right, denominatorBinary.Right))
        {
            return Expression.Add(denominatorBinary.Left, denominatorBinary.Right);
        }

        if (denominatorBinary.NodeType == ExpressionType.Add &&
            IsSquareOf(difference.Left, denominatorBinary.Left) &&
            IsSquareOf(difference.Right, denominatorBinary.Right))
        {
            return Expression.Subtract(denominatorBinary.Left, denominatorBinary.Right);
        }

        return null;
    }

    private static bool IsSquareOf(Expression square, Expression factor)
    {
        if (square is BinaryExpression { NodeType: ExpressionType.Multiply } multiplication &&
            multiplication.Left.AreEqual(factor) && multiplication.Right.AreEqual(factor))
        {
            return true;
        }

        if (square is BinaryExpression { NodeType: ExpressionType.Power } power &&
            power.Left.AreEqual(factor) && TryGetIntegralConstant(power.Right, out var exponent) && exponent == 2)
        {
            return true;
        }

        return TryGetFiniteDouble(square, out var squareValue) &&
               TryGetFiniteDouble(factor, out var factorValue) &&
               squareValue == factorValue * factorValue;
    }

    private static bool TryGetIntegralConstant(Expression expression, out int value)
    {
        value = 0;
        if (!TryGetFiniteDouble(expression, out var number) || number != Math.Truncate(number) ||
            number < int.MinValue || number > int.MaxValue)
        {
            return false;
        }

        value = (int)number;
        return true;
    }

    private static bool TryGetFiniteDouble(Expression expression, out double value)
    {
        value = 0.0;
        if (expression is not ConstantExpression constant || constant.Value is null)
        {
            return false;
        }

        try
        {
            value = Convert.ToDouble(constant.Value, System.Globalization.CultureInfo.InvariantCulture);
            return double.IsFinite(value);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// SP2 cancellation for a single common deferred factor. This covers ratios
    /// such as F/(F·G) → 1/G and (F·G)/F → G before polynomial division runs.
    /// </summary>
    private static Expression TryReduceAdjacentFactorials(Expression numerator, Expression denominator)
    {
        if (numerator is not MethodCallExpression { Method: var numeratorMethod, Arguments.Count: 1 } numeratorFactorial ||
            denominator is not MethodCallExpression { Method: var denominatorMethod, Arguments.Count: 1 } denominatorFactorial ||
            numeratorMethod != typeof(Factorial).GetMethod(nameof(Factorial.Of)) ||
            denominatorMethod != typeof(Factorial).GetMethod(nameof(Factorial.Of)))
        {
            return null;
        }

        var n = numeratorFactorial.Arguments[0];
        var predecessor = denominatorFactorial.Arguments[0];

        // Concrete BigInteger inputs preserve the familiar notation 10!/9!
        // while retaining exact arithmetic; no calculation of either factorial
        // occurs in the RICIS phase.
        if (n is ConstantExpression { Value: System.Numerics.BigInteger nValue } &&
            predecessor is ConstantExpression { Value: System.Numerics.BigInteger predecessorValue })
        {
            return nValue >= System.Numerics.BigInteger.One && predecessorValue == nValue - System.Numerics.BigInteger.One
                ? n
                : null;
        }

        if (n.Type != predecessor.Type)
        {
            return null;
        }

        var expectedPredecessor = Expression.Subtract(n, NumericConstants.OneOf(n.Type));
        return predecessor.AreEqual(expectedPredecessor) ? n : null;
    }

    /// <summary>
    /// SP2 for arbitrary parenthesization of a product. The operation first
    /// flattens only built-in multiplication trees, then removes matching
    /// factors as a multiset. Thus (a·a·a·a·a)/(a·a·a·a) → a regardless of
    /// the binary-tree association used by the expression builder.
    /// </summary>
    private static Expression TryCancelAssociativeFactors(Expression numerator, Expression denominator)
    {
        var numeratorFactors = FlattenBuiltInMultiplication(numerator);
        var denominatorFactors = FlattenBuiltInMultiplication(denominator);

        if (numeratorFactors.Count < 2 || denominatorFactors.Count < 2)
        {
            return null;
        }

        var remainingNumerator = numeratorFactors.ToList();
        var cancelledCount = 0;
        foreach (var denominatorFactor in denominatorFactors)
        {
            var matchIndex = remainingNumerator.FindIndex(factor => factor.AreEqual(denominatorFactor));
            if (matchIndex < 0)
            {
                return null;
            }

            remainingNumerator.RemoveAt(matchIndex);
            cancelledCount++;
        }

        if (cancelledCount == 0)
        {
            return null;
        }

        return BuildProduct(remainingNumerator, numerator.Type);
    }

    private static List<Expression> FlattenBuiltInMultiplication(Expression expression)
    {
        var factors = new List<Expression>();
        CollectBuiltInMultiplicationFactors(expression, factors);
        return factors;
    }

    private static void CollectBuiltInMultiplicationFactors(Expression expression, List<Expression> factors)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply } product &&
            (product.Method is null || NumericConstants.IsIntrinsicNumeric(product.Type)))
        {
            CollectBuiltInMultiplicationFactors(product.Left, factors);
            CollectBuiltInMultiplicationFactors(product.Right, factors);
            return;
        }

        factors.Add(expression);
    }

    private static Expression BuildProduct(IReadOnlyList<Expression> factors, Type scalarType)
    {
        return factors.Count switch
        {
            0 => NumericConstants.OneOf(scalarType),
            1 => factors[0],
            _ => factors.Aggregate(Expression.Multiply),
        };
    }

    private static Expression TryCancelCommonFactor(Expression numerator, Expression denominator)
    {
        if (denominator is BinaryExpression { NodeType: ExpressionType.Multiply } denProduct)
        {
            if (numerator.AreEqual(denProduct.Left))
            {
                return Expression.Divide(OneOf(numerator.Type), denProduct.Right);
            }

            if (numerator.AreEqual(denProduct.Right))
            {
                return Expression.Divide(OneOf(numerator.Type), denProduct.Left);
            }
        }

        if (numerator is BinaryExpression { NodeType: ExpressionType.Multiply } numProduct)
        {
            if (denominator.AreEqual(numProduct.Left))
            {
                return numProduct.Right;
            }

            if (denominator.AreEqual(numProduct.Right))
            {
                return numProduct.Left;
            }
        }

        return null;
    }

    private static Expression OneOf(Type type) => NumericConstants.OneOf(type);

    private static (List<Root> roots, bool isPolynomial) AnalyzeDenominator(Expression denominator, ParameterExpression param)
    {
        var collector = new PolynomialCoefficientCollector(param);
        collector.Visit(denominator);

        var roots = collector.IsPolynomial
            ? denominator.FindRoots(param)
            : denominator.FindNumericalRoots(param);

        return (roots, collector.IsPolynomial);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var obj = Visit(node.Object);
        var args = node.Arguments.Select(Visit);
        if (obj == node.Object && args.SequenceEqual(node.Arguments))
        {
            return node;
        }
        return Expression.Call(obj, node.Method, args);
    }

    protected override Expression VisitExtension(Expression node) => node;

    private static ParameterExpression FindSingleParameter(Expression expr)
    {
        var finder = new ParameterVisitor();
        finder.Visit(expr);
        return finder.FoundParameter;
    }
}
