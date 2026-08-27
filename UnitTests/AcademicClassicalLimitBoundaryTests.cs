using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class AcademicClassicalLimitBoundaryTests
{
    // Академический пример из Calculus Volume 1, OpenStax, section 2.2:
    // lim(x→0) sin(1/x) does not exist because the function oscillates
    // between -1 and 1 arbitrarily close to zero.
    // Source: https://openstax.org/books/calculus-volume-1/pages/2-2-the-limit-of-a-function
    [TestMethod]
    public void OscillatoryLimitIsNotClassicallySolvableAndRemainsDeferredInRicis()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var body = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!,
            Expression.Divide(Expression.Constant(1d), x));
        var source = Expression.Lambda<Func<double, double>>(body, x);

        // Classical expectation: two valid sequences approach zero but yield
        // different values, so no single real limit exists.
        var positiveSequence = 1d / (Math.PI / 2d + 2d * Math.PI * 1000d);
        var negativeSequence = 1d / (3d * Math.PI / 2d + 2d * Math.PI * 1000d);
        Assert.AreEqual(1d, source.Compile()(positiveSequence), 1e-12);
        Assert.AreEqual(-1d, source.Compile()(negativeSequence), 1e-12);

        // RICIS expectation: do not invent a classical limit. Instead, retain
        // the oscillatory Sin node and index the singular argument 1/x.
        var reduced = RicisPhasePipeline.Simplify(source);
        Assert.IsTrue(
            reduced.Body is MethodCallExpression
            {
                Method.Name: nameof(Math.Sin),
                Arguments: [InfinityExpression]
            },
            $"Oscillatory academic expression must remain indexed and deferred. Actual: {reduced}");
    }
}
