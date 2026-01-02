using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Polynomial;

namespace Ricis.Core.Simplifiers;

public static class AlgebraicSimplifier
{
    public static Expression CleanFirst(Expression expr)
    {
        // Больше не нужно конвертировать Pow, так как Evaluator теперь его понимает
        return new AlgebraicReductionVisitor().Visit(expr);
    }

    public static Expression ApplyPostRicis(Expression expr)
    {
        return new AlgebraicReductionVisitor().Visit(expr);
    }

    private class AlgebraicReductionVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            if (node.NodeType == ExpressionType.Divide)
            {
                if (left.AreEqual(right))
                {
                    return RicisType.InfinityOne;
                }

                var parameter = FindSingleParameter(node);
                if (parameter != null)
                {
                    // 1. Ищем сингулярность 0/0
                    var singularity = DetectInfinityZero(left, right, parameter);
                    if (singularity != null)
                    {
                        return singularity;
                    }

                    // 2. Деление
                    var divided = PolynomialLongDivision.TryDivide(left, right, parameter);
                    if (divided != null)
                    {
                        return Visit(divided);
                    }
                }
            }

            if (left == node.Left && right == node.Right)
            {
                return node;
            }

            return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        private static InfinityExpression DetectInfinityZero(Expression numerator, Expression denominator, ParameterExpression param)
        {
            // ШАГ 1: Находим корни знаменателя через Solver
            // (Solver внутри использует ExactEvaluator, который теперь поддерживает Pow)
            var roots = denominator.FindPolynomialRoots(param);

            if (roots == null || roots.Count == 0)
            {
                return null;
            }

            // ШАГ 2: Проверяем числитель в этих точках
            var valuesOnly = roots.Where(root => root.RationalValue.HasValue);
            foreach (var root in valuesOnly)
            {
                // Используем ExactEvaluator напрямую для проверки числителя
                // Это самый надежный способ (DRY)
                if (!numerator.TryEvaluate(param.Name, root.RationalValue.Value, out var result)) continue;
                if (!result.IsZero) continue;
                // 0/0 DETECTED -> ∞_0
                var indexZero = RicisType.InfinityZero;
                return new InfinityExpression(indexZero, param, root.DoubleValue);
            }

            return null;
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
}