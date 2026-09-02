using System.Linq.Expressions;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.SpecialFunctions;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class ClassicalImpossibleRicisCasesTests
{
    [TestMethod]
    public void SharedZeroDenominator_ClassicalNaN_RicisExactReduction()
    {
        // Classical: (x/0) / (5/0) at x=4 => (4/0)/(5/0) = Infinity/Infinity = NaN.
        var x = Expression.Parameter(typeof(double), "x");
        var zero = Expression.Constant(0.0);
        var source = Expression.Divide(
            Expression.Divide(x, zero),
            Expression.Divide(Expression.Constant(5.0), zero));

        var nativeFn = Expression.Lambda<Func<double, double>>(source, x).Compile();
        double classicalResult = nativeFn(4.0);
        Assert.IsTrue(double.IsNaN(classicalResult), $"Classical evaluation must produce NaN, got {classicalResult}");

        // RICIS III: SP2 cancels common zero denominator => x / 5
        var ricisFn = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x));
        Assert.IsNotNull(ricisFn);

        var expectedBody = Expression.Divide(x, Expression.Constant(5.0));
        Assert.IsTrue(ricisFn.Body.AreEqual(expectedBody), $"Expected RICIS reduction x/5, got {ricisFn.Body}");

        var compiledRicis = ricisFn.Compile();
        Assert.AreEqual(0.8, compiledRicis(4.0), 1e-12);
    }

    [TestMethod]
    public void IdenticalTrigRatio_ClassicalNaNAtZero_RicisExactOne()
    {
        // Classical: sin(x) / sin(x) at x=0 => sin(0)/sin(0) = 0/0 = NaN.
        var x = Expression.Parameter(typeof(double), "x");
        var sin = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);
        var source = Expression.Divide(sin, sin);

        var nativeFn = Expression.Lambda<Func<double, double>>(source, x).Compile();
        double classicalResult = nativeFn(0.0);
        Assert.IsTrue(double.IsNaN(classicalResult), $"Classical evaluation must produce NaN, got {classicalResult}");

        // RICIS III: ID-01 / L1 normalizes F/F -> 1
        var ricisFn = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x));
        Assert.IsTrue(ricisFn.Body.IsOne(), $"Expected exact constant 1, got {ricisFn.Body}");
        Assert.AreEqual(1.0, ricisFn.Compile()(0.0), 1e-12);
    }

    [TestMethod]
    public void LargeFactorialRatio_ClassicalOverflowNaN_RicisExactValue()
    {
        // Classical: 200! / 199! using BigInteger factorial method.
        // For double or standard computation, 200! exceeds Double.MaxValue (~1.79e308), so classical double math gives Infinity/Infinity = NaN.
        var n = Expression.Constant(new BigInteger(200));
        var predecessor = Expression.Constant(new BigInteger(199));

        var factMethod = typeof(Factorial).GetMethod(nameof(Factorial.Of))!;
        var source = Expression.Divide(
            Expression.Call(factMethod, n),
            Expression.Call(factMethod, predecessor));

        // RICIS III: SP2 adjacent factorials n! / (n-1)! -> n
        var reduced = RicisPhasePipeline.Simplify(source);
        Assert.IsNotNull(reduced);
        Assert.IsTrue(reduced.AreEqual(n), $"Expected 200, got {reduced}");
    }

    [TestMethod]
    public void RemovableCubicSingularity_ClassicalNaNAtRoot_RicisExactPolynomial()
    {
        // Classical: (x^3 - 8) / (x - 2) at x=2 => (8 - 8)/(2 - 2) = 0/0 = NaN.
        var x = Expression.Parameter(typeof(double), "x");
        var xCubed = Expression.Multiply(Expression.Multiply(x, x), x);
        var source = Expression.Divide(
            Expression.Subtract(xCubed, Expression.Constant(8.0)),
            Expression.Subtract(x, Expression.Constant(2.0)));

        var nativeFn = Expression.Lambda<Func<double, double>>(source, x).Compile();
        double classicalResult = nativeFn(2.0);
        Assert.IsTrue(double.IsNaN(classicalResult), $"Classical evaluation must produce NaN at x=2, got {classicalResult}");

        // RICIS III: Polynomial division in SP2 reduces (x^3-8)/(x-2) -> x^2 + 2x + 4
        var ricisFn = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x));
        Assert.IsNotNull(ricisFn);

        double ricisResultAt2 = ricisFn.Compile()(2.0);
        Assert.AreEqual(12.0, ricisResultAt2, 1e-12, "RICIS III evaluates the removable singularity to exact 12");
    }

    [TestMethod]
    public void PolarTangentAtHalfPi_ClassicalPrecisionErrorOrInfinity_RicisIndexedInfinity()
    {
        // Classical: Math.Tan(Math.PI / 2) returns ~1.633123935319537E+16 due to floating point representation of pi/2.
        double point = Math.PI / 2.0;
        double classicalResult = Math.Tan(point);
        Assert.IsTrue(classicalResult > 1e15, "Classical floating point Math.Tan(pi/2) gives huge finite float error");

        // RICIS III: Polar Converter recognizes rational circle sector 1/4 circle (90 deg) and yields exact indexed pole infinity
        var call = Expression.Call(typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!, Expression.Constant(point));
        var ricisReduced = PolarConverter.CollapseConstantTrig(call);

        Assert.IsInstanceOfType<InfinityExpression>(ricisReduced);
        var infinity = (InfinityExpression)ricisReduced;
        Assert.AreEqual(1.0, infinity.NumeratorAsDouble(), 1e-12);
    }

    [TestMethod]
    public void RationalDecomposition_100Over3_ReturnsExact33AndOneThird()
    {
        // Classical C# 100 / 3 in int loses remainder -> 33.
        // Classical double 100.0 / 3.0 loses exactness -> 33.333333333333336.
        // RICIS Rational: 100/3 decomposes into exact whole = 33 and remainder = 1/3, formatted as "33 + 1/3".
        var rational = new Ricis.Core.Rationals.Rational(100, 3);
        var (whole, remainder) = rational.DecomposeMixed();

        Assert.AreEqual(new BigInteger(33), whole);
        Assert.AreEqual(new Ricis.Core.Rationals.Rational(1, 3), remainder);
        Assert.AreEqual("33 + 1/3", rational.ToMixedString());

        var negativeRational = new Ricis.Core.Rationals.Rational(-100, 3);
        Assert.AreEqual("-33 - 1/3", negativeRational.ToMixedString());
    }

    [TestMethod]
    public void EpsilonDisplacementAnalysis_CubicRemovableSingularity_MatchesRicisAndLimit()
    {
        // Evaluating (x^3 - 8) / (x - 2) with x = 2 + epsilon (for epsilon != 0):
        // Numerator: (2 + eps)^3 - 8 = 12*eps + 6*eps^2 + eps^3 = eps * (12 + 6*eps + eps^2)
        // Denominator: (2 + eps) - 2 = eps
        // Ratio: eps * (12 + 6*eps + eps^2) / eps = 12 + 6*eps + eps^2.
        // As eps -> 0, the ratio equals 12.
        double[] epsilons = [1e-1, 1e-3, 1e-6, 1e-9, 1e-12];
        foreach (var eps in epsilons)
        {
            double xVal = 2.0 + eps;
            double classicalRatio = (Math.Pow(xVal, 3.0) - 8.0) / (xVal - 2.0);
            double epsilonExpansion = 12.0 + (6.0 * eps) + (eps * eps);

            Assert.AreEqual(epsilonExpansion, classicalRatio, 1e-7, $"Epsilon ratio mismatch at eps={eps}");
        }

        // RICIS III (SP2) reduces (x^3 - 8)/(x - 2) -> x^2 + 2x + 4, which at x = 2 is exactly 12.
        var x = Expression.Parameter(typeof(double), "x");
        var xCubed = Expression.Multiply(Expression.Multiply(x, x), x);
        var source = Expression.Divide(
            Expression.Subtract(xCubed, Expression.Constant(8.0)),
            Expression.Subtract(x, Expression.Constant(2.0)));

        var ricisReduced = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x));
        double ricisAtTwo = ricisReduced.Compile()(2.0);

        Assert.AreEqual(12.0, ricisAtTwo, 1e-12, "RICIS III exact value at x=2 equals the epsilon limit 12");
    }
}
