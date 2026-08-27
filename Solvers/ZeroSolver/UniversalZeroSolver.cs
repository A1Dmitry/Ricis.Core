using System.Linq.Expressions;
using Ricis.Core.Polynomial;
using Ricis.Core.Solvers;

namespace Ricis.Core.Solvers.ZeroSolver;

/// <summary>
///     Универсальный точный решатель уравнения expr = 0
///     Возвращает список точных корней в виде Rational (если возможно)
///     или double с высокой точностью для трансцендентных случаев (π, π/2 и т.д.)
/// </summary>
public static class UniversalZeroSolver
{
    /// <summary>
    ///     Находит все точные конечные корни уравнения expr = 0 для заданного параметра
    /// </summary>
    public static List<Root> FindExactRoots(this Expression expr, ParameterExpression param)
    {
        var roots = new List<Root>();

        // 1. Полиномы любой степени — приоритет №1 (самые точные)
        var polyRoots = PolynomialZeroSolver.FindRoots(expr, param);
        if (polyRoots.Count > 0)
        {
            roots.AddRange(polyRoots);
            return roots.Distinct().ToList(); // может быть несколько стратегий
        }

        // 2. Тригонометрические функции (sin, cos, tan)
        var trigRoots = expr.FindTrigonometricRoots(param);
        if (trigRoots.Count > 0)
        {
            roots.AddRange(trigRoots);
        }

        // 3. Логарифм: log(g(x)) = 0 ⇒ g(x) = 1.
        var logRoots = LogSolver.FindRoots(expr, param);
        if (logRoots.Count > 0)
        {
            roots.AddRange(logRoots);
        }

        // 4. Экспонента: exp(expr) = 1 ⇒ expr = 0 (если встретим exp(...) - 1)
        var expRoots = ExponentialZeroSolver.FindRoots(expr, param);
        if (expRoots.Count > 0)
        {
            roots.AddRange(expRoots);
        }

        // Удаляем дубликаты
        return roots.Distinct().ToList();
    }
}