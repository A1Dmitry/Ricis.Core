using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;
using System.Linq.Expressions;

namespace Ricis.Core.Phases;

public class StandardOperationsVisitor : ExpressionVisitor, IExpressionVisitor
{
    // КРИТИЧНО: Переопределяем VisitExtension, иначе ExpressionVisitor упадет 
    // или не сможет обработать InfinityExpression при рекурсивном обходе.
    protected override Expression VisitExtension(Expression node)
    {
        if (node is InfinityExpression)
        {
            return node;
        }
        return base.VisitExtension(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // --- RICIS ALGEBRA: Операции над сингулярностями (∞ + ∞, ∞ * ∞) ---
        if (left is InfinityExpression infLeft && right is InfinityExpression infRight)
        {
            // Проверяем, что это сингулярности в одной и той же точке
            if (AreRootsCompatible(infLeft, infRight))
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Add:
                        // A7: ∞_A + ∞_B = ∞_{A+B}
                        return MergeSingularities(infLeft, infRight, ExpressionType.Add);

                    case ExpressionType.Subtract:
                        // ∞_A - ∞_B = ∞_{A-B}
                        return MergeSingularities(infLeft, infRight, ExpressionType.Subtract);

                    case ExpressionType.Multiply:
                        // ∞_A * ∞_B = ∞_{A*B}
                        return MergeSingularities(infLeft, infRight, ExpressionType.Multiply);

                    case ExpressionType.Divide:
                        // A5: ∞_A / ∞_B = A / B
                        // Сингулярности сокращаются, остаются индексы.
                        // Возвращаем обычное деление индексов.
                        return Expression.Divide(infLeft.Numerator, infRight.Numerator);
                }
            }
            // Если корни разные -> это Монолит (Tuple), оставляем BinaryExpression
        }

        // --- RICIS ALGEBRA: Скаляр и Сингулярность (C * ∞) ---
        if (node.NodeType == ExpressionType.Multiply)
        {
            // C * ∞_A = ∞_{C*A}
            if (left is InfinityExpression infL && IsScalar(right))
            {
                var newNum = Expression.Multiply(infL.Numerator, right);
                return InfinityExpression.CreateLazy(newNum, infL.Roots);
            }
            // ∞_A * C = ∞_{A*C}
            if (right is InfinityExpression infR && IsScalar(left))
            {
                var newNum = Expression.Multiply(left, infR.Numerator);
                return InfinityExpression.CreateLazy(newNum, infR.Roots);
            }
        }

        // ∞_A / C = ∞_{A/C}
        if (node.NodeType == ExpressionType.Divide)
        {
            if (left is InfinityExpression infDiv && IsScalar(right))
            {
                var newNum = Expression.Divide(infDiv.Numerator, right);
                return InfinityExpression.CreateLazy(newNum, infDiv.Roots);
            }
        }

        // --- Стандартные упрощения (используем ваши Extensions) ---
        if (node.NodeType == ExpressionType.Multiply)
        {
            if (left.IsOne())
            {
                return right;
            }

            if (right.IsOne())
            {
                return left;
            }

            if (left.IsZero())
            {
                return left; // 0 * x = 0
            }

            if (right.IsZero())
            {
                return right;
            }
        }

        if (node.NodeType == ExpressionType.Add)
        {
            if (left.IsZero())
            {
                return right;
            }

            if (right.IsZero())
            {
                return left;
            }
        }

        // Если ничего не изменилось, возвращаем узел
        if (left == node.Left && right == node.Right)
        {
            return node;
        }

        return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    // Хелпер для слияния двух сингулярностей
    private Expression MergeSingularities(InfinityExpression a, InfinityExpression b, ExpressionType op)
    {
        // Создаем новое выражение для индекса: IndexA (op) IndexB
        var newNumerator = Expression.MakeBinary(op, a.Numerator, b.Numerator);

        // Возвращаем новую сингулярность с объединенным индексом
        // Берем корни от 'a', так как они совместимы
        return InfinityExpression.CreateLazy(newNumerator, a.Roots);
    }

    private bool AreRootsCompatible(InfinityExpression a, InfinityExpression b)
    {
        if (a.Roots.Count == 0 || b.Roots.Count == 0)
        {
            return false;
        }

        // Сравниваем первую точку сингулярности
        var rootA = a.Roots[0];
        var rootB = b.Roots[0];

        return rootA.Param == rootB.Param &&
               Math.Abs(rootA.Value - rootB.Value) < 1e-9;
    }

    private static bool IsScalar(Expression expr)
    {
        return !(expr is InfinityExpression);
    }
}