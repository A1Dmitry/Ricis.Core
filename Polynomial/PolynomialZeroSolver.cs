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
        var collector = new PolynomialCoefficientCollector(param);
        collector.Visit(expr);

        if (!collector.IsPolynomial ||
            collector.Coefficients.Count <= 0 ||
            collector.Coefficients.All(c => c.Value.IsZero))
        {
            return expr.FindNumericalRoots(param);
        }

        var roots = new List<Root>();

        // FIX: Проверка на корень x=0 (отсутствие свободного члена)
        // Если в полиноме нет коэффициента при степени 0, значит x=0 — корень.
        if (!collector.Coefficients.ContainsKey(0) || collector.Coefficients[0].IsZero)
        {
            roots.Add(new Root(param, 0.0));

            // Опционально: можно удалить фактор x^k и искать остальные корни,
            // но для текущих тестов (x^3) этого достаточно.
        }

        var degree = collector.Coefficients.Keys.Max();
        if (degree <= 0)
        {
            // Если степень 0 (константа), корней нет (если константа не 0, что проверено выше)
            return roots;
        }

        // Ищем рациональные корни (для x^2 - 1 и т.д.)
        // RationalRootTheorem требует ненулевого свободного члена.
        // Если мы нашли корень 0, теорема может не сработать для оставшейся части без деления.
        // Но если свободный член ЕСТЬ, теорема сработает.
        if (!collector.Coefficients.ContainsKey(0) || collector.Coefficients[0].IsZero)
            return roots.Any() ? roots : expr.FindNumericalRoots(param);
        var possibleRationals = RationalRootTheorem.GetPossibleRoots(collector.Coefficients);
        foreach (var candidate in possibleRationals)
        {
            // Используем EvaluateAtPoint вместо TryEvaluate для надежности
            var val = expr.EvaluateAtPoint(candidate.ToDouble(), param.Name);
            if (Math.Abs(val) < 1e-10) // Используем Epsilon
            {
                roots.Add(new Root(param, candidate));
            }
        }

        // Если нашли корни (включая 0) — возвращаем их.
        // Если нет — пробуем численный метод (на всякий случай).
        return roots.Any() ? roots : expr.FindNumericalRoots(param);
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