using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class AcademicForumStructuralReductionTests
{
    // Adapted from a Math StackExchange discussion about simplifying a messy
    // symmetric expression with parameters p,q,a,b,N:
    // https://math.stackexchange.com/questions/3806463/experienced-mathematicians-simplifying-messy-algebra
    [TestMethod]
    public void ForumMessySymmetricIdentityHasClassicalAndRicisExpectations()
    {
        var p = Expression.Parameter(typeof(double), "p");
        var q = Expression.Parameter(typeof(double), "q");
        var a = Expression.Parameter(typeof(double), "a");
        var b = Expression.Parameter(typeof(double), "b");
        var n = Expression.Parameter(typeof(double), "N");
        var one = Expression.Constant(1d);
        Func<Expression, Expression, BinaryExpression> plus = Expression.Add;
        Func<Expression, Expression, BinaryExpression> minus = Expression.Subtract;
        Func<Expression, Expression, BinaryExpression> times = Expression.Multiply;
        Func<Expression, Expression, BinaryExpression> divide = Expression.Divide;

        var nMinusOne = minus(n, one);
        var nPlusOne = plus(n, one);
        var pa = times(p, a);
        var qb = times(q, b);
        var lhs = plus(
            plus(
                divide(
                    times(pa, plus(
                        divide(times(p, minus(a, one)), nMinusOne),
                        divide(times(q, plus(b, one)), nPlusOne))),
                    n),
                times(
                    p,
                    times(minus(one, divide(a, n)),
                        plus(divide(pa, nMinusOne), divide(qb, nPlusOne))))),
            plus(
                divide(
                    times(qb, plus(
                        divide(times(p, plus(a, one)), nPlusOne),
                        divide(times(q, minus(b, one)), nMinusOne))),
                    n),
                times(
                    q,
                    times(minus(one, divide(b, n)),
                        plus(divide(pa, nPlusOne), divide(qb, nMinusOne))))));

        // The compact classical target mirrors the forum identity's intended
        // factor grouping; the numerical check uses a legal point N != 0.
        var classicalExpected = divide(times(plus(p, q), plus(times(p, a), times(q, b))), n);
        var source = Expression.Lambda<Func<double, double, double, double, double, double>>(
            lhs, p, q, a, b, n);
        var expected = Expression.Lambda<Func<double, double, double, double, double, double>>(
            classicalExpected, p, q, a, b, n);
        Assert.AreEqual(expected.Compile()(2d, 3d, 4d, 5d, 7d), source.Compile()(2d, 3d, 4d, 5d, 7d), 1e-12);

        // RICIS III expectation: unsupported multivariate grouping is not
        // guessed; the original structure remains explicit and executable.
        var reduced = (LambdaExpression)RicisPhasePipeline.Simplify(source);
        Assert.IsTrue(reduced.Body.AreEqual(lhs),
            $"RICIS must preserve this unsupported multivariate structure. Actual: {reduced}");
    }

    // Adapted from a Math StackExchange discussion of algebraic extensions:
    // https://math.stackexchange.com/questions/95829/software-for-algebraic-simplifying-expressions
    [TestMethod]
    public void ForumAlgebraicExtensionHasClassicalAndRicisExpectations()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var z = Expression.Parameter(typeof(double), "z");
        var one = Expression.Constant(1d);
        var five = Expression.Constant(5d);
        var numerator = Expression.Multiply(x, Expression.Power(z, Expression.Constant(3d)));
        var denominator = Expression.Multiply(
            Expression.Add(one, x),
            Expression.Power(z, Expression.Constant(2d)));
        var sourceBody = Expression.Divide(numerator, denominator);
        var source = Expression.Lambda<Func<double, double, double>>(sourceBody, x, z);

        // Classical expectation: without declaring z^5 = 5x+1, no valid
        // polynomial cancellation is available; at a legal point the value is
        // exactly the original quotient.
        var classicalExpected = Expression.Lambda<Func<double, double, double>>(
            Expression.Divide(numerator, denominator), x, z);
        Assert.AreEqual(classicalExpected.Compile()(2d, 3d), source.Compile()(2d, 3d), 1e-12);

        // RICIS III expectation: preserve the algebraic-extension relation as
        // deferred structure instead of inventing a rational simplification.
        var reduced = (LambdaExpression)RicisPhasePipeline.Simplify(source);
        Assert.IsTrue(reduced.Body.AreEqual(sourceBody),
            $"RICIS must defer unsupported algebraic-extension reduction. Actual: {reduced}");
    }
}
