using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Phases;
using Ricis.Core.Expressions;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class UniversityComplexRationalReduceTests
{
    // College Algebra example adapted from OpenStax 1.6, Example 3:
    // (2x^2 + x - 6)/(x^2 - 1) ÷ (x^2 - 4)/(x^2 + 2x + 1)
    // = (2x - 3)(x + 1) / ((x - 1)(x - 2)).
    // Source: https://openstax.org/books/college-algebra-2e/pages/1-6-rational-expressions
    [TestMethod]
    public void UniversityDivisionOfRationalExpressionsReducesToLowestTerms()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var twoX = Expression.Multiply(Expression.Constant(2d), x);
        var xPlusOne = Expression.Add(x, Expression.Constant(1d));
        var xMinusOne = Expression.Subtract(x, Expression.Constant(1d));
        var xPlusTwo = Expression.Add(x, Expression.Constant(2d));
        var xMinusTwo = Expression.Subtract(x, Expression.Constant(2d));
        var twoXMinusThree = Expression.Subtract(twoX, Expression.Constant(3d));

        // Fully factored form after the university solution's reciprocal step:
        // ((2x−3)(x+2)(x+1)) / ((x+1)(x−1)(x−2)(x+2)).
        // Keeping every factor explicit makes each cancellation observable.
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(
                Expression.Multiply(
                    Expression.Multiply(
                        Expression.Multiply(twoXMinusThree, xPlusTwo),
                        xPlusOne),
                    xPlusOne),
                Expression.Multiply(
                    Expression.Multiply(
                        Expression.Multiply(xPlusOne, xMinusOne),
                        xMinusTwo),
                    xPlusTwo)),
            x);

        // Expectation 1 — classical algebra: after cancelling common factors,
        // the rational function is (2x−3)(x+1)/((x−1)(x−2)).
        var classicalExpected = Expression.Lambda<Func<double, double>>(
            Expression.Divide(
                Expression.Multiply(twoXMinusThree, xPlusOne),
                Expression.Multiply(xMinusOne, xMinusTwo)),
            x);
        var sourceValue = source.Compile()(3d);
        var classicalValue = classicalExpected.Compile()(3d);
        Assert.AreEqual(classicalValue, sourceValue, 1e-12,
            "Classical expectation must match the original expression at a valid point.");

        var reduced = RicisPhasePipeline.Simplify(source);

        // Expectation 2 — RICIS: preserve the reduced parent numerator index
        // (2x−3)(x+1) and both excluded denominator keys, rather than replacing
        // the index with key-specific numerical projections.
        var infinity = reduced.Body as InfinityExpression;
        Assert.IsNotNull(infinity, $"Expected a RICIS indexed pole, got: {reduced}");
        var expectedParentIndex = Expression.Multiply(twoXMinusThree, xPlusOne);
        Assert.IsTrue(infinity!.Numerator.AreEqual(expectedParentIndex),
            $"SP4 must retain the reduced parent index. Actual: {infinity.Numerator}");
        CollectionAssert.AreEquivalent(
            new[] { 1d, 2d },
            infinity.Roots.Select(root => root.Value).ToArray());
    }
}
