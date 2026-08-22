using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Core.CachedSolutions;

/// <summary>
/// Test-backed seed catalog. Entries are copied from formulas that already
/// have explicit classical and RICIS expectations in the regression suites.
/// </summary>
public static class DefaultCachedSolutions
{
    /// <summary>Creates the default index from verified Reduce regression formulas.</summary>
    public static CachedSolutionIndex CreateIndex()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var x2 = Expression.Power(x, Expression.Constant(2d));
        var x4 = Expression.Power(x2, Expression.Constant(2d));
        var five = Expression.Constant(5d);
        var two = Expression.Constant(2d);

        var removable = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Subtract(x2, Expression.Constant(25d)), Expression.Subtract(x, five)), x);
        var removableExpected = Expression.Lambda<Func<double, double>>(
            Expression.Add(x, five), x);

        var nested = Expression.Lambda<Func<double, double>>(
            Expression.Divide(
                Expression.Divide(Expression.Subtract(x4, Expression.Constant(16d)), Expression.Subtract(x2, Expression.Constant(4d))),
                Expression.Divide(Expression.Add(x2, Expression.Constant(4d)), Expression.Add(x, two))), x);
        var nestedExpected = Expression.Lambda<Func<double, double>>(Expression.Add(x, two), x);

        var pole = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(1d), Expression.Subtract(x2, Expression.Constant(4d))), x);
        var poleExpected = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(1d), Expression.Subtract(x2, Expression.Constant(4d))), x);

        return new CachedSolutionIndex([
            new("difference-of-squares", removable, removableExpected, removableExpected,
                "UnitTests/ComplexAlgebraicReduceTests.cs", "Removable difference-of-squares cancellation.",
                "Разность квадратов: устранимое сокращение", "https://fizmatschool.ru/textbooks/alg-8/sokr-i-alg-drob/",
                "Разложение x²−25=(x−5)(x+5) позволяет сократить общий множитель x−5 при сохранении ограничения x≠5.",
                "x => (x + 5)", "x => (x + 5)", "confirmed",
                ["algebra", "factorization", "removable-singularity"], -3, 1, 0),
            new("nested-difference-of-squares", nested, nestedExpected, nestedExpected,
                "UnitTests/OlympiadNestedReductionTests.cs", "Nested quotient with repeated factor cancellation.",
                "Вложенная олимпиадная дробь", "https://openstax.org/books/college-algebra-2e/pages/7-4-simplify-complex-rational-expressions",
                "Два последовательных шага: разность четвёртых степеней, обращение внутреннего делителя и сокращение повторных множителей.",
                "x => (x + 2)", "x => (x + 2)", "confirmed",
                ["olympiad", "nested-fraction", "difference-of-squares"], 0, 2, 1),
            new("quadratic-pole", pole, poleExpected, poleExpected,
                "RegressionTests/KnownRicisLimitsSuite.cs", "Preserve both singular roots instead of cancelling the pole.",
                "Квадратичный знаменатель и два полюса", "https://openstax.org/books/calculus-volume-1/pages/2-2-the-limit-of-a-function",
                "Знаменатель (x−2)(x+2) обращается в ноль в двух корнях. Классическая запись скрывает структуру полюсов, а RICIS сохраняет оба ключа.",
                "x => 1 / (x² − 4)", "∞₁ at {x=-2, x=2}", "confirmed",
                ["singularity", "pole", "quadratic"], 3, 0, -1)
        ]);
    }
}
