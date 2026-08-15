using System.Linq.Expressions;
using System.Reflection;
using Ricis.Core.Expressions;

namespace Ricis.Core.Execution;

/// <summary>
/// Executes ordinary double-based expression trees in strict reference mode.
/// In contrast to IEEE-754 double division, a zero denominator is an explicit
/// DivideByZeroException. This mode is intended for immutable source
/// expressions; RICIS-derived symbolic expressions are simplified first.
/// </summary>
public static class StrictExpressionExecutor
{
    /// <summary>
    /// Executes <c>Compile</c> for the RICIS expression model.
    /// </summary>
    public static Func<double, double> Compile(Expression<Func<double, double>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var guardedBody = new StrictDivisionVisitor().Visit(expression.Body);
        return Expression.Lambda<Func<double, double>>(guardedBody, expression.Parameters).Compile();
    }

    private static double RequireNonZero(double denominator)
    {
        if (denominator == 0.0)
        {
            throw new DivideByZeroException("Reference expression contains a zero denominator.");
        }

        return denominator;
    }

    private sealed class StrictDivisionVisitor : ExpressionVisitor
    {
        private static readonly MethodInfo RequireNonZeroMethod =
            typeof(StrictExpressionExecutor).GetMethod(nameof(RequireNonZero), BindingFlags.NonPublic | BindingFlags.Static)!;

        protected override Expression VisitExtension(Expression node)
        {
            if (node is ZeroInfinityExpression)
            {
                // Compile boundary: 0_F is intentionally projected to native zero
                // only after all symbolic RICIS phases have completed.
                return node;
            }

            throw new InvalidOperationException(
                $"Нельзя строго скомпилировать нерешённый RICIS-узел {node.GetType().Name}. Сначала примените нормативный pipeline.");
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            if (node.NodeType == ExpressionType.Divide && right.Type == typeof(double))
            {
                return Expression.Divide(left, Expression.Call(RequireNonZeroMethod, right));
            }

            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }
    }
}
