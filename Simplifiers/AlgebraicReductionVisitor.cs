using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Polynomial;
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

        // SP2 / L1: identical subtrees → 1
        if (left.AreEqual(right))
        {
            return RicisType.InfinityOne;
        }

        var cancelled = TryCancelCommonFactor(left, right);
        if (cancelled is not null)
        {
            return Visit(cancelled);
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
    /// SP2 cancellation for a single common deferred factor. This covers ratios
    /// such as F/(F·G) → 1/G and (F·G)/F → G before polynomial division runs.
    /// </summary>
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

    private static Expression OneOf(Type type) => type switch
    {
        _ when type == typeof(double) => Expression.Constant(1.0),
        _ when type == typeof(float) => Expression.Constant(1.0f),
        _ when type == typeof(decimal) => Expression.Constant(1m),
        _ when type == typeof(long) => Expression.Constant(1L),
        _ => Expression.Constant(1, type),
    };

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
