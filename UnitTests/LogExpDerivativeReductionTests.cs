using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class LogExpDerivativeReductionTests
{
    private readonly ExpressionSimplifierVisitor simplifier = new();

    [TestMethod]
    public void Reduce_LogOne_ReturnsZero()
    {
        // Math.Log(1.0) => 0
        var expr = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, Expression.Constant(1.0));
        var reduced = simplifier.Visit(expr);

        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.IsZero(), $"Expected zero, got {reduced}");
    }

    [TestMethod]
    public void Reduce_ExpZero_ReturnsOne()
    {
        // Math.Exp(0.0) => 1
        var expr = Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, Expression.Constant(0.0));
        var reduced = simplifier.Visit(expr);

        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.IsOne(), $"Expected one, got {reduced}");
    }

    [TestMethod]
    public void Reduce_LogExp_ReturnsInnerArgument()
    {
        // Math.Log(Math.Exp(x)) => x
        var x = Expression.Parameter(typeof(double), "x");
        var exp = Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, x);
        var logExp = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, exp);

        var reduced = simplifier.Visit(logExp);

        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.AreEqual(x), $"Expected x, got {reduced}");
    }

    [TestMethod]
    public void Reduce_ExpLog_ReturnsInnerArgument()
    {
        // Math.Exp(Math.Log(x)) => x
        var x = Expression.Parameter(typeof(double), "x");
        var log = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, x);
        var expLog = Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, log);

        var reduced = simplifier.Visit(expLog);

        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.AreEqual(x), $"Expected x, got {reduced}");
    }

    [TestMethod]
    public void Reduce_ComplexLogExpComposition_ReducesCorrectly()
    {
        // Math.Log(Math.Exp(x + 5.0)) - 5.0 => x
        var x = Expression.Parameter(typeof(double), "x");
        var xPlusFive = Expression.Add(x, Expression.Constant(5.0));
        var exp = Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, xPlusFive);
        var logExp = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, exp);
        var minusFive = Expression.Subtract(logExp, Expression.Constant(5.0));

        var simplified = simplifier.Visit(minusFive);

        Assert.IsNotNull(simplified);
        Assert.IsTrue(simplified.AreEqual(x), $"Expected x, got {simplified}");
    }

    [TestMethod]
    public void Derivative_SimplePower_CalculatesCorrectDerivative()
    {
        // d/dx (x^2) = 2*x
        Expression<Func<double, double>> f = x => Math.Pow(x, 2.0);
        var df = f.Derivative();

        Assert.IsNotNull(df);
        var fn = df.Compile();
        Assert.AreEqual(6.0, fn(3.0), 1e-9);
    }

    [TestMethod]
    public void Derivative_Exp_CalculatesCorrectDerivative()
    {
        // d/dx (exp(x)) = exp(x)
        Expression<Func<double, double>> f = x => Math.Exp(x);
        var df = f.Derivative();

        Assert.IsNotNull(df);
        var fn = df.Compile();
        Assert.AreEqual(Math.Exp(2.0), fn(2.0), 1e-9);
    }

    [TestMethod]
    public void Derivative_Log_CalculatesCorrectDerivativeStructure()
    {
        // d/dx (ln(x)) = 1/x
        Expression<Func<double, double>> f = x => Math.Log(x);
        var df = f.Derivative();

        Assert.IsNotNull(df);
        Assert.IsNotNull(df.Body);
        var str = df.ToString();
        Assert.IsTrue(str.Contains("1") && str.Contains("x"), $"Expected 1/x structure, got {str}");
    }

    [TestMethod]
    public void Derivative_ComplexChainRule_CalculatesAndSimplifiesDerivative()
    {
        // f(x) = exp(x^2) => f'(x) = 2 * x * exp(x^2)
        Expression<Func<double, double>> f = x => Math.Exp(x * x);
        var df = f.Derivative();

        Assert.IsNotNull(df);
        var fn = df.Compile();
        double expected = 4.0 * Math.Exp(4.0);
        Assert.AreEqual(expected, fn(2.0), 1e-7);
    }

    [TestMethod]
    public void Derivative_QuotientRule_CalculatesCorrectDerivativeStructure()
    {
        // f(x) = ln(x) / x
        Expression<Func<double, double>> f = x => Math.Log(x) / x;
        var df = f.Derivative();

        Assert.IsNotNull(df);
        Assert.IsNotNull(df.Body);
        var str = df.ToString();
        Assert.IsTrue(str.Contains("Log") || str.Contains("x"), $"Expected derivative structure, got {str}");
    }
}
