using System.Linq.Expressions;

namespace Ricis.Core.Solvers.ZeroSolver
{
    /// <summary>
    /// Общие вспомогательные методы для zero‑solvers:
    /// - поиск первого параметра в выражении,
    /// - нормализация корня +0.0,
    /// - адаптер: из FindRoots(Func(expr, param) -> ICollection{Root}) получить (ParameterExpression,double)?.
    /// </summary>
    public static class ZeroSolverUtils
    {
        public static ParameterExpression FindFirstParameter(this Expression expr)
        {
            var pf = new ParamFinder();
            pf.Visit(expr);
            return pf.Parameter;
        }

        public static double NormalizeZero(double v) => v == 0.0 ? 0.0 : v;

        /// <summary>
        /// Вызвать реализацию FindRoots, передав найденный параметр; возвращает первый root как (param,double)?
        /// Упрощает повторяющийся код в солверах.
        /// </summary>
        public static (ParameterExpression, double)? FindFirstRootFromFindRoots(Func<Expression, ParameterExpression, ICollection<Root>> findRootsFunc, Expression expr)
        {
            if (expr == null || findRootsFunc == null)
            {
                return null;
            }

            var param = expr.FindFirstParameter();
            if (param == null)
            {
                return null;
            }

            var roots = findRootsFunc(expr, param);
            if (roots == null || roots.Count == 0)
            {
                return null;
            }

            var first = roots.First();
            return (first.Parameter, NormalizeZero(first.DoubleValue));
        }

        private class ParamFinder : ExpressionVisitor
        {
            public ParameterExpression Parameter { get; private set; }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (Parameter == null)
                {
                    Parameter = node;
                }

                return base.VisitParameter(node);
            }
        }
    }
}