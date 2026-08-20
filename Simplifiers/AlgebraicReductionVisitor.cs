using Ricis.Core.Execution;
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
    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType != ExpressionType.Divide)
        {
            // Safe ordinary structural algebra. RICIS extension payloads are
            // deliberately excluded so indexed zero/infinity semantics remain
            // available to the later O(1) and A phases.
            if ((node.Method is null || NumericConstants.IsIntrinsicNumeric(node.Type)) &&
                left is not RicisExpression && right is not RicisExpression)
            {
                if (node.NodeType == ExpressionType.Add)
                {
                    if (left.IsZero()) return right;
                    if (right.IsZero()) return left;
                }

                if (node.NodeType == ExpressionType.Subtract)
                {
                    if (right.IsZero()) return left;
                    if (left.IsZero()) return Expression.Negate(right);
                    if (left.AreEqual(right)) return NumericConstants.ZeroOf(left.Type);
                }
            }

            // Safe structural units. Deliberately do not reduce F·0 here:
            // the later O(1) bridge must retain the parent index 0_F.
            if (node.NodeType == ExpressionType.Multiply &&
                (node.Method is null || NumericConstants.IsIntrinsicNumeric(node.Type)))
            {
                if (left.IsOne()) return right;
                if (right.IsOne()) return left;
            }

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

        // Structural units are simplified before any singularity analysis.
        if (right.IsOne())
        {
            return left;
        }

        // SP2: (F/A) / (G/A) → F/G. This must run before any generic
        // nested-ratio normalisation and before a bridge can create ∞_F.
        if (left is BinaryExpression { NodeType: ExpressionType.Divide } leftRatio &&
            right is BinaryExpression { NodeType: ExpressionType.Divide } rightRatio &&
            leftRatio.Right.AreEqual(rightRatio.Right))
        {
            return Visit(Expression.Divide(leftRatio.Left, rightRatio.Left));
        }

        // SP2 normalisation of nested ratios: F/(G/H) → (F·H)/G.
        // It exposes ordinary factors for later structural cancellation.
        var nestedRatio = TryNormalizeNestedRatio(left, right);
        if (nestedRatio is not null)
        {
            return Visit(nestedRatio);
        }

        // SP2 / L1: identical subtrees → 1
        if (left.AreEqual(right))
        {
            return NumericConstants.OneOf(left.Type);
        }

        // SP2 adjacent powers: F^n / F^(n−1) → F. The base and
        // exponents remain deferred structural expressions; no evaluation of F
        // or classical domain substitution is performed.
        var adjacentPowers = TryReduceAdjacentPowers(left, right);
        if (adjacentPowers is not null)
        {
            return Visit(adjacentPowers);
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

        // Root certification and polynomial long division are currently a
        // double-domain facility. Generic INumber expressions still receive
        // all structural SP2 reductions above, but must not be coerced into a
        // Func<double,double> merely to inspect a denominator.
        if (node.Type != typeof(double))
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

        var isPolynomial = IsSafePolynomialDenominator(right, parameter);

        // Pure RICIS: do NOT invent ∞ from numerical 0/0 here.
        // If long division cancels the common factor, return the quotient (SP2).
        // If not, leave Divide intact for Phase 2 (A4: F/G by identity).
        //
        // Phase 1 must never compile a caller-provided expression merely to
        // inspect its denominator. Polynomial collection is structural only;
        // non-polynomial trees remain deferred for the later RICIS phases.
        if (!isPolynomial)
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
    /// Clears one explicit nested denominator without numerical evaluation.
    /// F/(G/H) becomes (F·H)/G; F/(A±B/C) becomes (F·C)/(A·C±B).
    /// </summary>
    private static Expression TryNormalizeNestedRatio(Expression numerator, Expression denominator)
    {
        if (denominator is BinaryExpression { NodeType: ExpressionType.Divide } ratio)
        {
            return Expression.Divide(Expression.Multiply(numerator, ratio.Right), ratio.Left);
        }

        if (denominator is not BinaryExpression { NodeType: ExpressionType.Add or ExpressionType.Subtract } sum ||
            sum.Right is not BinaryExpression { NodeType: ExpressionType.Divide } rightRatio)
        {
            return null;
        }

        var commonDenominator = rightRatio.Right;
        var clearedLeft = Expression.Multiply(sum.Left, commonDenominator);
        var clearedDenominator = sum.NodeType == ExpressionType.Add
            ? Expression.Add(clearedLeft, rightRatio.Left)
            : Expression.Subtract(clearedLeft, rightRatio.Left);
        return Expression.Divide(Expression.Multiply(numerator, commonDenominator), clearedDenominator);
    }

    /// <summary>
    /// Applies the RICIS exponent-difference identity a^N/a^(N−X) → a^X
    /// for structurally identical deferred bases. The exponent subtraction is
    /// preserved structurally; constant integral exponents remain supported.
    /// </summary>
    private static Expression TryReduceAdjacentPowers(Expression numerator, Expression denominator)
    {
        if (numerator is not BinaryExpression { NodeType: ExpressionType.Power, Left: var numeratorBase, Right: var numeratorExponent } ||
            denominator is not BinaryExpression { NodeType: ExpressionType.Power, Left: var denominatorBase, Right: var denominatorExponent } ||
            !numeratorBase.AreEqual(denominatorBase))
        {
            return null;
        }

        if (denominatorExponent is BinaryExpression
            {
                NodeType: ExpressionType.Subtract,
                Left: var commonExponent,
                Right: var difference
            } && commonExponent.AreEqual(numeratorExponent))
        {
            return TryGetIntegralConstant(difference, out var differenceValue) && differenceValue == 1
                ? numeratorBase
                : Expression.Power(numeratorBase, difference);
        }

        if (TryGetIntegralConstant(numeratorExponent, out var n) &&
            TryGetIntegralConstant(denominatorExponent, out var predecessor) &&
            n >= 1 &&
            predecessor < n)
        {
            var exponentDifference = n - predecessor;
            if (exponentDifference == 1)
            {
                return numeratorBase;
            }

            var typedDifference = denominatorExponent.Type == typeof(double)
                ? Expression.Constant((double)exponentDifference)
                : Expression.Constant(exponentDifference, denominatorExponent.Type);
            return Expression.Power(numeratorBase, typedDifference);
        }

        return null;
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

        if (n.Type != predecessor.Type ||
            predecessor is not BinaryExpression { NodeType: ExpressionType.Subtract, Left: var predecessorArgument, Right: var decrement } ||
            !predecessorArgument.AreEqual(n) ||
            !IsOneOrStaticOne(decrement, n.Type))
        {
            return null;
        }

        return n;
    }

    private static bool IsOneOrStaticOne(Expression expression, Type scalarType)
    {
        if (expression.IsOne())
        {
            return true;
        }

        return expression is MemberExpression { Expression: null, Member: var member } &&
               member.Name == "One" &&
               member.DeclaringType == scalarType;
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

        if (numeratorFactors.Count < 2 && denominatorFactors.Count < 2)
        {
            return null;
        }

        var remainingNumerator = numeratorFactors.ToList();
        var remainingDenominator = denominatorFactors.ToList();
        var cancelledCount = 0;

        // SP2 is cancellation of the common multiset, not a requirement that
        // the denominator be wholly contained in the numerator. The uncancelled
        // tail remains an exact deferred quotient.
        for (var denominatorIndex = remainingDenominator.Count - 1; denominatorIndex >= 0; denominatorIndex--)
        {
            var matchIndex = remainingNumerator.FindIndex(factor =>
                factor.AreEqual(remainingDenominator[denominatorIndex]));
            if (matchIndex < 0)
            {
                continue;
            }

            remainingNumerator.RemoveAt(matchIndex);
            remainingDenominator.RemoveAt(denominatorIndex);
            cancelledCount++;
        }

        if (cancelledCount == 0)
        {
            return null;
        }

        var reducedNumerator = BuildProduct(remainingNumerator, numerator.Type);
        var reducedDenominator = BuildProduct(remainingDenominator, denominator.Type);
        return reducedDenominator.IsOne()
            ? reducedNumerator
            : Expression.Divide(reducedNumerator, reducedDenominator);
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

    private static bool IsSafePolynomialDenominator(Expression denominator, ParameterExpression param)
    {
        if (!NumericalEvaluationSafety.IsSafeDoubleExpression(denominator))
        {
            return false;
        }

        var collector = new PolynomialCoefficientCollector(param);
        collector.Visit(denominator);
        return collector.IsPolynomial;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var obj = Visit(node.Object);
        var args = node.Arguments.Select(Visit).ToArray();
        if (obj == node.Object && args.SequenceEqual(node.Arguments))
        {
            return node;
        }
        return Expression.Call(obj, node.Method, args);
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);
        if (node.NodeType == ExpressionType.Negate &&
            operand is UnaryExpression { NodeType: ExpressionType.Negate } innerNegate &&
            innerNegate.Operand is not RicisExpression)
        {
            return innerNegate.Operand;
        }

        return operand == node.Operand
            ? node
            : Expression.MakeUnary(node.NodeType, operand, node.Type, node.Method);
    }

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => node;

    private static ParameterExpression FindSingleParameter(Expression expr)
    {
        var finder = new ParameterVisitor();
        finder.Visit(expr);
        return finder.FoundParameter;
    }
}
