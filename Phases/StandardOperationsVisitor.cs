using System.Linq.Expressions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

public class StandardOperationsVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // 1 * x → x
        if (node.NodeType == ExpressionType.Multiply)
        {
            if (IsConstantOne(left))
            {
                return right;
            }

            if (IsConstantOne(right))
            {
                return left;
            }
        }

        // 0 + x → x
        if (node.NodeType == ExpressionType.Add)
        {
            if (IsConstantZero(left))
            {
                return right;
            }

            if (IsConstantZero(right))
            {
                return left;
            }
        }

        // x + 0 → x, x * 1 → x (уже покрыто выше)

        if (left == node.Left && right == node.Right)
        {
            return node;
        }

        return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    protected override Expression VisitExtension(Expression node)
    {
        // Не трогаем RICIS-узлы: ∞_F, Monolith, Bridged
        return node;
    }

    private static bool IsConstantOne(Expression expr)
    {
        return expr is ConstantExpression c &&
               c.Value is double d &&
               Math.Abs(d - 1.0) < double.Epsilon;
    }

    private static bool IsConstantZero(Expression expr)
    {
        return expr is ConstantExpression c &&
               c.Value is double d &&
               Math.Abs(d) < double.Epsilon;
    }
}