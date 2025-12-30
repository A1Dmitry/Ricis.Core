using Ricis.Core;
using Ricis.Core.ZeroSolver;
using System.Linq.Expressions;

public static class RicisTransformPhase
{
    public static Expression Apply(Expression expr)
    {
        return new RicisTransformVisitor().Visit(expr);
    }

    private class RicisTransformVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Divide)
            {
                return SimplifyDivision(node.Left, node.Right);
            }
            return base.VisitBinary(node);
        }

        private Expression SimplifyDivision(Expression numerator, Expression denominator)
        {
            var singularities = new List<InfinityExpression>();

            // 1. Полиномиальные корни
            var polyRoots = SingularitySolver.SolveRoot(denominator);
            foreach (var root in polyRoots)
            {
                AddSingularityIfValid(numerator, denominator, root.Item1, root.Item2, singularities);
            }

            // 2. Тригонометрические корни
            var trigRoot = TrigSolver.Solve(denominator);
            if (trigRoot.HasValue)
            {
                var (param, value) = trigRoot.Value;
                AddSingularityIfValid(numerator, denominator, param, value, singularities);
            }

            if (singularities.Count == 0)
                return Expression.Divide(numerator, denominator);

            return singularities.Count == 1
                ? singularities[0]
                : singularities[0];
        }

        private void AddSingularityIfValid(
            Expression numerator,
            Expression denominator,
            ParameterExpression param,
            double value,
            List<InfinityExpression> singularities)
        {
            double numAtRoot = EvaluateAtPoint(numerator, param.Name, value);

            InfinityExpression infinity;
            if (numAtRoot == 0.0) // 0/0 DETECTED
            {
                // --- FIX: Индекс 0 для состояния 0/0 ---
                var indexZero = RicisType.InfinityZero;
                infinity = new InfinityExpression(indexZero, param, value);
            }
            else
            {
                // Полюс C/0 -> Индекс C (числитель)
                infinity = new InfinityExpression(numerator, param, value);
            }

            singularities.Add(infinity);
        }

        private static double EvaluateAtPoint(Expression expr, string paramName, double value)
        {
            try
            {
                var visitor = new SubstitutionVisitor(paramName, value);
                var substituted = visitor.Visit(expr);
                var lambda = Expression.Lambda<Func<double>>(Expression.Convert(substituted, typeof(double)));
                return lambda.Compile()();
            }
            catch
            {
                return 1.0;
            }
        }
    }
}