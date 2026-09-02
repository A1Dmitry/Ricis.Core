using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class ComplexExpressionReductionTests
{
    private readonly ExpressionSimplifierVisitor simplifier = new();
    private readonly PolarTrigVisitor polarVisitor = new();

    [TestMethod]
    public void Reduce_PythagoreanTrigIdentity_ReducesToOne()
    {
        // sin^2(x) + cos^2(x) => 1
        var x = Expression.Parameter(typeof(double), "x");
        var sin = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);
        var cos = Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!, x);
        var sinSq = Expression.Power(sin, Expression.Constant(2.0));
        var cosSq = Expression.Power(cos, Expression.Constant(2.0));
        var pythagorean = Expression.Add(sinSq, cosSq);

        var reduced = simplifier.Visit(pythagorean);

        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.IsOne(), $"Expected 1, got {reduced}");
    }

    [TestMethod]
    public void Reduce_SinNegation_ReducesToNegatedSin()
    {
        // sin(-x) => -sin(x)
        var x = Expression.Parameter(typeof(double), "x");
        var negX = Expression.Negate(x);
        var sinNegX = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, negX);

        var reduced = simplifier.Visit(sinNegX);

        Assert.IsNotNull(reduced);
        var expected = Expression.Negate(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x));
        Assert.IsTrue(reduced.AreEqual(expected), $"Expected -sin(x), got {reduced}");
    }

    [TestMethod]
    public void Reduce_CosNegation_ReducesToCos()
    {
        // cos(-x) => cos(x)
        var x = Expression.Parameter(typeof(double), "x");
        var negX = Expression.Negate(x);
        var cosNegX = Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!, negX);

        var reduced = simplifier.Visit(cosNegX);

        Assert.IsNotNull(reduced);
        var expected = Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!, x);
        Assert.IsTrue(reduced.AreEqual(expected), $"Expected cos(x), got {reduced}");
    }

    [TestMethod]
    public void Reduce_TanCos_ReducesToSin()
    {
        // tan(x) * cos(x) => sin(x)
        var x = Expression.Parameter(typeof(double), "x");
        var tan = Expression.Call(typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!, x);
        var cos = Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!, x);
        var tanCos = Expression.Multiply(tan, cos);

        var reduced = simplifier.Visit(tanCos);

        Assert.IsNotNull(reduced);
        var expected = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);
        Assert.IsTrue(reduced.AreEqual(expected), $"Expected sin(x), got {reduced}");
    }

    [TestMethod]
    public void Reduce_LogPower_ReducesToExponentTimesLog()
    {
        // ln(x^3) => 3 * ln(x)
        var x = Expression.Parameter(typeof(double), "x");
        var xCubed = Expression.Power(x, Expression.Constant(3.0));
        var logPower = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, xCubed);

        var reduced = simplifier.Visit(logPower);

        Assert.IsNotNull(reduced);
        var expected = Expression.Multiply(
            Expression.Constant(3.0),
            Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, x));
        Assert.IsTrue(reduced.AreEqual(expected), $"Expected 3*ln(x), got {reduced}");
    }

    [TestMethod]
    public void Reduce_ComplexTrigLogAlgebraCombination_ReducesCleanly()
    {
        // (sin^2(x) + cos^2(x)) * exp(ln(y + 2)) - (y + 2) => 0
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");

        var sin = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);
        var cos = Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!, x);
        var sinSqPlusCosSq = Expression.Add(Expression.Power(sin, Expression.Constant(2.0)), Expression.Power(cos, Expression.Constant(2.0)));

        var yPlusTwo = Expression.Add(y, Expression.Constant(2.0));
        var logYPlusTwo = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, yPlusTwo);
        var expLog = Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, logYPlusTwo);

        var product = Expression.Multiply(sinSqPlusCosSq, expLog);
        var combination = Expression.Subtract(product, yPlusTwo);

        var reduced = simplifier.Visit(combination);

        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.IsZero(), $"Expected 0, got {reduced}");
    }
}
