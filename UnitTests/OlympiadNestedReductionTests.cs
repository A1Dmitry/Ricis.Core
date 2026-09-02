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
        Expression<Func<double, double>> source = x =>
            ((x * x * x * x - 16.0) / (x * x - 4.0)) /
            ((x * x + 4.0) / (x + 2.0));

        Expression<Func<double, double>> classicalExpected = x => x + 2.0;
        Assert.AreEqual(classicalExpected.Compile()(3d), source.Compile()(3d), 1e-12);

        // RICIS expectation: structural reduction reaches x+2.
        var reduced = RicisPhasePipeline.Simplify(source);
        double val = reduced.Compile()(3d);
        Assert.AreEqual(5d, val, 1e-12, $"Olympiad expression evaluated at 3 must equal 5. Actual: {reduced}");
    }
}
