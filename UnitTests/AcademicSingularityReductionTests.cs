using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class AcademicSingularityReductionTests
{
    // Academic calculus pattern: 1/x^2 has an infinite singular behavior at 0.
    // OpenStax discusses 1/x and vertical asymptotes in Calculus Volume 1, 2.2.
    // Source: https://openstax.org/books/calculus-volume-1/pages/2-2-the-limit-of-a-function
    [TestMethod]
    public void DoublePoleBecomesIndexedInfinityAtZero()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(1d), Expression.Power(x, Expression.Constant(2d))), x);

        var reduced = RicisPhasePipeline.Simplify(source);

        Assert.IsTrue(reduced.Body is InfinityExpression infinity &&
                      infinity.Roots.Count == 1 &&
                      infinity.Roots[0].Value == 0d,
            $"Expected an indexed double pole at x=0. Actual: {reduced}");
    }

    // Academic analysis pattern: exp(1/x) has an essential singularity at 0;
    // no finite classical value can be substituted there. RICIS must retain the
    // outer exponential and expose the indexed singular argument.
    [TestMethod]
    public void EssentialSingularityRemainsNestedAroundIndexedInfinity()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var reciprocal = Expression.Divide(Expression.Constant(1d), x);
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, reciprocal), x);

        var reduced = RicisPhasePipeline.Simplify(source);

        Assert.IsTrue(
            reduced.Body is MethodCallExpression
            {
                Method.Name: nameof(Math.Exp),
                Arguments: [InfinityExpression]
            },
            $"Expected Exp around indexed infinity. Actual: {reduced}");
    }

    // Academic boundary pattern: ln(x) is defined only for x>0 and diverges
    // toward -infinity as x approaches 0 from the right. There is no classical
    // real value at the singular boundary, so the symbolic log remains deferred.
    [TestMethod]
    public void LogarithmicDomainBoundaryIsNotFalselyCollapsed()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var body = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, x);
        var source = Expression.Lambda<Func<double, double>>(body, x);

        var reduced = RicisPhasePipeline.Simplify(source);

        Assert.IsTrue(reduced.Body.AreEqual(body),
            $"Logarithmic boundary must remain deferred. Actual: {reduced}");
        Assert.IsTrue(source.Compile()(1e-300) < -600d,
            "The classical right-hand behavior must be recognized as divergent toward -infinity.");
    }
}
