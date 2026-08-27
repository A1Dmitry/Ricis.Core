using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class OlympiadNestedReductionTests
{
    // Олимпиадный университетский тип задачи: сложная вложенная дробь.
    // Адаптация стандартного университетского приёма рационализации:
    // (x^4 - 16)/(x^2 - 4) ÷ ((x^2 + 4)/(x + 2)) = x + 2.
    // Учебный контекст: https://yufeizhao.com/olympiad/
    [TestMethod]
    public void OlympiadNestedDifferenceOfSquaresReducesClassicallyAndInRicis()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var x2 = Expression.Power(x, Expression.Constant(2d));
        var x4 = Expression.Power(x2, Expression.Constant(2d));
        var xPlusTwo = Expression.Add(x, Expression.Constant(2d));
        var xMinusTwo = Expression.Subtract(x, Expression.Constant(2d));

        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(
                Expression.Divide(
                    Expression.Subtract(x4, Expression.Constant(16d)),
                    Expression.Subtract(x2, Expression.Constant(4d))),
                Expression.Divide(
                    Expression.Add(x2, Expression.Constant(4d)),
                    xPlusTwo)),
            x);

        // Classical expectation on the ordinary domain, for example at x=3.
        var classicalExpected = Expression.Lambda<Func<double, double>>(xPlusTwo, x);
        Assert.AreEqual(classicalExpected.Compile()(3d), source.Compile()(3d), 1e-12);

        // RICIS expectation: structural reduction reaches x+2.
        var reduced = RicisPhasePipeline.Simplify(source);
        Assert.IsTrue(reduced.Body.AreEqual(classicalExpected.Body),
            $"Olympiad expression was not reduced to x+2. Actual: {reduced}");
    }
}
