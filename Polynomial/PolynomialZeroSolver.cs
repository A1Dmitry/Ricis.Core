using Ricis.Core.Extensions;
using Ricis.Core.Rationals;
using System.Linq.Expressions;

namespace Ricis.Core.Polynomial;

public static class PolynomialZeroSolver
{
    /// <summary>
    /// Поиск корней
    /// </summary>
    /// <param name="expr"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static List<Root> FindRoots(this Expression expr, ParameterExpression param)
    {
        // СТАРЫЙ КОД (полиномы - БЕЗ ИЗМЕНЕНИЙ)
        var collector = new PolynomialCoefficientCollector(param);
        collector.Visit(expr);

        if (!collector.IsPolynomial || 
            collector.Coefficients.Count <= 0 ||
            collector.Coefficients.All(c => c.Value.IsZero))
        {
            return expr.FindNumericalRoots(param);
        }

        var degree = collector.Coefficients.Keys.Max();
        if (degree <= 0)
        {
            return expr.FindNumericalRoots(param);
        }

        var possibleRationals = RationalRootTheorem.GetPossibleRoots(collector.Coefficients);
        var roots = new List<Root>();

        foreach (var candidate in possibleRationals)
        {
            if (expr.TryEvaluate(param.Name, candidate, out var result) && result.IsZero)
            {
                roots.Add(new Root(param, candidate));
            }
        }

        return roots.Any() 
            ? roots 
            : expr.FindNumericalRoots(param);
    }

    /// <summary>
    /// Подбор параметров
    /// </summary>
    /// <param name="expr"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static List<Root> FindNumericalRoots(this Expression expr, ParameterExpression param)
    {
        var roots = new List<Root>();
        const double step = 0.05;  // Точнее
        var paramName = param.Name;
        var prepare = expr.Prepare(param);
        var compiled = prepare.Compile();
        for (double x = -10; x < 10; x += step)
        {
            // ✅ Evaluate() уже работает!
            var fx = compiled(x);
            var fx1 = compiled(x + step);

            if (!(fx * fx1 < 0))
            {
                continue;
            }

            var root = expr.Bisection(param, x, x + step);
            if (root.HasValue)
            {
                roots.Add(new Root(param, root.Value));
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
        var compiled = expr.Prepare(param).Compile();
        var fa = compiled(a);
        var fb = compiled(b);

        if (fa * fb >= 0)
        {
            return null;
        }

        while (Math.Abs(b - a) > tol)
        {
            var c = (a + b) / 2;
            var fc = compiled(c);

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
        var compiled = expr.Prepare(x).Compile();
        for (var i = 0; i < steps; i++)
        {
            var x1 = a + i * h;
            var x2 = a + (i + 1) * h;

            var f1 = compiled(x1);
            var f2 = compiled(x2);

            if (f1 * f2 < 0) // ЗНАК МЕНЯЕТСЯ
            {
                var root = expr.Bisection(x, x1, x2);
                if (root.HasValue)
                {
                    roots.Add(root.Value);
                }
            }
        }
        return roots.Distinct().OrderBy(Math.Abs).ToArray();
    }

}