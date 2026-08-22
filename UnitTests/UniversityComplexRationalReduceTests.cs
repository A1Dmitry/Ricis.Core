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
                    Expression.Multiply(twoXMinusThree, xPlusTwo),
                    xPlusOne),
                Expression.Multiply(
                    Expression.Multiply(
                        Expression.Multiply(xPlusOne, xMinusOne),
                        xMinusTwo),
                    xPlusTwo)),
            x);

        var reduced = RicisPhasePipeline.Simplify(source);

        // RICIS preserves the two excluded denominator keys instead of silently
        // erasing them after cancellation: ∞_{−1} at x=1 and ∞_{1} at x=2.
        var keyed = reduced.Body as KeyedInfinityExpression;
        Assert.IsNotNull(keyed, $"Expected keyed RICIS poles, got: {reduced}");
        Assert.AreEqual(2, keyed!.Branches.Count);
        CollectionAssert.AreEquivalent(
            new[] { -1d, 1d },
            keyed.Branches.Select(branch => (double)((ConstantExpression)branch.Numerator).Value!).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { 1d, 2d },
            keyed.Roots.Select(root => root.Value).ToArray());
    }
}
