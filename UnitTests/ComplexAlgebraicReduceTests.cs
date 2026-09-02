using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Phases;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class ComplexAlgebraicReduceTests
{
    // Учебный принцип: разложить числитель и знаменатель на множители,
    // затем сократить общие множители. См. https://fizmatschool.ru/textbooks/alg-8/sokr-i-alg-drob/
    //
    // ((x^2 - 25)/(x - 5) * (x^3 - 8)/(x - 2)) /
    // ((x + 5) * (x^2 + 2x + 4))  ==>  1
    // при x != 5, x != 2, x != -5 и x^2 + 2x + 4 != 0.
    [TestMethod]
    public void ComplexSchoolFractionReducesToOne()
    {
        Expression<Func<double, double>> source = x =>
            (((x * x - 25.0) / (x - 5.0)) * ((x * x * x - 8.0) / (x - 2.0))) /
            ((x + 5.0) * (x * x + 2.0 * x + 4.0));

        var reduced = RicisPhasePipeline.Simplify(source);

        Assert.IsInstanceOfType<ConstantExpression>(reduced.Body);
        Assert.AreEqual(1d, ((ConstantExpression)reduced.Body).Value);
    }
}
