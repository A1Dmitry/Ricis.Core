using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class SymbolicSingularityIndexTests
{
    [TestMethod]
    public void AddSingularityIfValidPreservesParentExpressionAndCertifiedKey()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var numerator = Expression.Add(x, Expression.Constant(3d));
        var singularities = new List<InfinityExpression>();

        numerator.AddSingularityIfValid(x, 5d, singularities);

        Assert.AreEqual(1, singularities.Count);
        Assert.IsInstanceOfType<PoleInfinityExpression>(singularities[0]);
        Assert.IsTrue(singularities[0].Numerator.AreEqual(numerator));
        Assert.AreEqual(typeof(double), singularities[0].Type);
        Assert.AreEqual(1, singularities[0].Roots.Count);
        Assert.AreSame(x, singularities[0].Roots[0].Param);
        Assert.AreEqual(5d, singularities[0].Roots[0].Value);
    }

    [TestMethod]
    public void AddSingularityIfValidDoesNotNumericallyEvaluateTheParentExpression()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var numerator = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!,
            x);
        var singularities = new List<InfinityExpression>();

        numerator.AddSingularityIfValid(x, -1d, singularities);

        Assert.AreEqual(1, singularities.Count,
            "A1 retains the parent numerator identity; a numerical projection at the certified key cannot erase it.");
        Assert.IsTrue(singularities[0].Numerator.AreEqual(numerator));
        Assert.AreEqual(-1d, singularities[0].Roots.Single().Value);
    }
}
