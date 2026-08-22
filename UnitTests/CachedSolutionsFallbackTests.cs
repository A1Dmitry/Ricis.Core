using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.CachedSolutions;
using Ricis.Core.Phases;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class CachedSolutionsFallbackTests
{
    [TestMethod]
    public void CachedSolutionIsOnlyAHypothesisUntilDeepRicisMatches()
    {
        var index = DefaultCachedSolutions.CreateIndex();
        var query = index.Solutions.Single(solution => solution.Id == "difference-of-squares").Formula;

        var proposal = index.ResolveWithoutAgent(
            query,
            expression => (LambdaExpression)RicisPhasePipeline.Simplify(expression));

        Assert.IsNotNull(proposal);
        Assert.AreEqual("difference-of-squares", proposal!.Solution.Id);
        Assert.AreEqual(1d, proposal.Similarity, 1e-12);
        Assert.IsTrue(proposal.RicisValidated, proposal.ValidationMessage);
        Assert.IsTrue(proposal.RicisCandidate.Body.ToString() == proposal.Solution.RicisExpectation.Body.ToString(),
            "Validated fallback must expose the deeper RICIS III result.");
    }

    [TestMethod]
    public void CachedSolutionDoesNotOverrideAConflictingDeepRicisResult()
    {
        var index = DefaultCachedSolutions.CreateIndex();
        var query = index.Solutions.Single(solution => solution.Id == "difference-of-squares").Formula;

        var proposal = index.ResolveWithoutAgent(
            query,
            expression => Expression.Lambda<Func<double, double>>(
                Expression.Constant(999d), expression.Parameters[0]));

        Assert.IsNotNull(proposal);
        Assert.IsFalse(proposal!.RicisValidated);
        StringAssert.Contains(proposal.ValidationMessage, "differs");
    }

    [TestMethod]
    public void CachedSolutionsReturnNoHypothesisWhenNoTestShapeMatches()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var query = Expression.Lambda<Func<double, double>>(
            Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x), x);
        var index = DefaultCachedSolutions.CreateIndex();

        var proposal = index.ResolveWithoutAgent(
            query,
            expression => (LambdaExpression)RicisPhasePipeline.Simplify(expression));

        Assert.IsNull(proposal);
    }
}
