using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Applies finite structural algebraic identities that are needed after
/// multivariate coordinate substitution. It never compiles expressions and
/// never evaluates limits or numerical samples.
/// </summary>
public sealed class RicisMultivariateAlgebraicVisitor<T> : ExpressionVisitor
    where T : INumber<T>
{
    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => node;

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        return node.NodeType switch
        {
            ExpressionType.Add => ReduceAdd(node, left, right),
            ExpressionType.Subtract => ReduceSubtract(node, left, right),
            ExpressionType.Multiply => ReduceMultiply(node, left, right),
            _ => Rebuild(node, left, right)
        };
    }

    private static Expression ReduceAdd(BinaryExpression source, Expression left, Expression right)
    {
        if (left.IsZero()) return right;
        if (right.IsZero()) return left;
        if (left.AreEqual(right)) return Expression.Multiply(Expression.Constant(T.CreateChecked(2), typeof(T)), left);
        if (IsNegationOf(left, right) || IsNegationOf(right, left)) return Expression.Constant(T.Zero, typeof(T));

        // (A−B)+B → A is the dual composition rule to (A+B)−B → A.
        if (left is BinaryExpression { NodeType: ExpressionType.Subtract } subtraction &&
            subtraction.Right.AreEqual(right)) return subtraction.Left;

        // (A+B)+(C−B) is intentionally not conflated; only exact structural inverses reduce.
        return Rebuild(source, left, right);
    }

    private static Expression ReduceSubtract(BinaryExpression source, Expression left, Expression right)
    {
        if (right.IsZero()) return left;
        if (left.AreEqual(right)) return Expression.Constant(T.Zero, typeof(T));

        // (A+B)−B → A and (A−B)−B are deliberately not conflated.
        if (left is BinaryExpression { NodeType: ExpressionType.Add } addition &&
            addition.Right.AreEqual(right)) return addition.Left;
        if (left is BinaryExpression { NodeType: ExpressionType.Subtract } subtraction &&
            subtraction.Right.AreEqual(right)) return subtraction.Left;

        // (A+B)−(C+B) → A−C and (A−B)−(C−B) → A−C.
        if (TryRemoveCommonRight(left, right, ExpressionType.Add, out var addDifference) ||
            TryRemoveCommonRight(left, right, ExpressionType.Subtract, out addDifference))
            return addDifference;

        return Rebuild(source, left, right);
    }

    private static Expression ReduceMultiply(BinaryExpression source, Expression left, Expression right)
    {
        if (left.IsOne()) return right;
        if (right.IsOne()) return left;
        if (left.IsZero() || right.IsZero()) return Expression.Constant(T.Zero, typeof(T));
        return Rebuild(source, left, right);
    }

    private static bool TryRemoveCommonRight(
        Expression left,
        Expression right,
        ExpressionType operation,
        out Expression result)
    {
        if (left is BinaryExpression leftBinary && right is BinaryExpression rightBinary &&
            leftBinary.NodeType == operation && rightBinary.NodeType == operation &&
            leftBinary.Right.AreEqual(rightBinary.Right))
        {
            result = Expression.Subtract(leftBinary.Left, rightBinary.Left);
            return true;
        }

        result = null;
        return false;
    }

    private static bool IsNegationOf(Expression candidate, Expression source) =>
        candidate is UnaryExpression { NodeType: ExpressionType.Negate } unary && unary.Operand.AreEqual(source);

    private static Expression Rebuild(BinaryExpression source, Expression left, Expression right) =>
        left == source.Left && right == source.Right
            ? source
            : Expression.MakeBinary(source.NodeType, left, right, source.IsLiftedToNull, source.Method);
}
