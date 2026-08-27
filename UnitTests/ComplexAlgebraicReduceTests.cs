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
        var x = Expression.Parameter(typeof(double), "x");
        var x2 = Expression.Power(x, Expression.Constant(2d));
        var x3 = Expression.Power(x, Expression.Constant(3d));

        var differenceOfSquares = Expression.Divide(
            Expression.Subtract(x2, Expression.Constant(25d)),
            Expression.Subtract(x, Expression.Constant(5d)));
        var differenceOfCubes = Expression.Divide(
            Expression.Subtract(x3, Expression.Constant(8d)),
            Expression.Subtract(x, Expression.Constant(2d)));
        var quadraticFactor = Expression.Add(
            Expression.Add(x2, Expression.Multiply(Expression.Constant(2d), x)),
            Expression.Constant(4d));
        var expectedFactors = Expression.Multiply(
            Expression.Add(x, Expression.Constant(5d)),
            quadraticFactor);
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(
                Expression.Multiply(differenceOfSquares, differenceOfCubes),
                expectedFactors),
            x);

        var reduced = RicisPhasePipeline.Simplify(source);

        Assert.IsInstanceOfType<ConstantExpression>(reduced.Body);
        Assert.AreEqual(1d, ((ConstantExpression)reduced.Body).Value);
    }
}
