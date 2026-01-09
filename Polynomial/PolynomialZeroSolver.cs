using Ricis.Core.Extensions;
using Ricis.Core.Rationals;
using System.Linq.Expressions;

namespace Ricis.Core.Polynomial;

public static class PolynomialZeroSolver
{
    public static List<Root> FindRoots(this Expression expr, ParameterExpression param)
    {
        // СТАРЫЙ КОД (полиномы - БЕЗ ИЗМЕНЕНИЙ)
        var collector = new PolynomialCoefficientCollector(param);
        collector.Visit(expr);

        if (collector.IsPolynomial && collector.Coefficients.Count > 0 &&
            !collector.Coefficients.All(c => c.Value.IsZero))
        {
            var degree = collector.Coefficients.Keys.Max();
            if (degree > 0)
            {
                var possibleRationals = RationalRootTheorem.GetPossibleRoots(collector.Coefficients);
                var roots = new List<Root>();

                foreach (var candidate in possibleRationals)
                {
                    if (expr.TryEvaluate(param.Name, candidate, out var result) && result.IsZero)
                    {
                        roots.Add(new Root(param, candidate));
                    }
                }
                if (roots.Any())
                {
                    return roots;
                }
            }
        }

        // *** НОВОЕ: Трансцендентные L25 ***
        return FindNumericalRoots(expr, param);
    }

    public static List<Root> FindNumericalRoots(this Expression expr, ParameterExpression param)
    {
        var roots = new List<Root>();
        const double step = 0.05;  // Точнее
        var paramName = param.Name;

        for (double x = -10; x < 10; x += step)
        {
            // ✅ Evaluate() уже работает!
            double fx = expr.Evaluate(param, x);
            double fx1 = expr.Evaluate(param, x + step);

            if (fx * fx1 < 0)
            {
                var root = expr.Bisection(param, x, x + step, 1e-10);
                if (root.HasValue)
                {
                    roots.Add(new Root(param, root.Value));
                }
            }
        }
        return roots;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="expr"></param>
    /// <param name="param"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tol"></param>
    /// <returns></returns>
    public static double? Bisection(this Expression expr, ParameterExpression param, double a, double b, double tol = 1e-6)
    {
        var fa = expr.Evaluate(param, a);
        var fb = expr.Evaluate(param, b);

        if (fa * fb >= 0)
        {
            return null;
        }

        while (Math.Abs(b - a) > tol)
        {
            var c = (a + b) / 2;
            var fc = expr.Evaluate(param, c);

            if (Math.Abs(fc) < tol)
            {
                return c;
            }

            if (fa * fc < 0) { b = c; }
            else { a = c; fa = fc; }
        }
        return (a + b) / 2;
    }

    public static double[] FindRootsInRange(this Expression expr, ParameterExpression x,
        double a, double b, int steps = 20)
    {
        var roots = new List<double>();
        var h = (b - a) / steps;

        for (var i = 0; i < steps; i++)
        {
            var x1 = a + i * h;
            var x2 = a + (i + 1) * h;

            var f1 = expr.Evaluate(x, x1);
            var f2 = expr.Evaluate(x, x2);

            if (f1 * f2 < 0) // ЗНАК МЕНЯЕТСЯ
            {
                var root = Bisection(expr, x, x1, x2);
                if (root.HasValue)
                {
                    roots.Add(root.Value);
                }
            }
        }
        return roots.Distinct().OrderBy(Math.Abs).ToArray();
    }

}