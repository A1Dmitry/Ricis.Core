using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class LazyInfinityReduceTests
{
    [TestMethod]
    public void ReducePreservesAllRootsWhenIndexIsShared()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var infinity = InfinityExpression.CreateLazy(
            Expression.Constant(1d),
            [(x, -2d), (x, 2d)]);

        var reduced = infinity.Reduce();

        var pole = reduced as PoleInfinityExpression;
        Assert.IsNotNull(pole);
        Assert.AreEqual(1d, ((ConstantExpression)pole!.Numerator).Value);
        Assert.AreEqual(2, pole.Roots.Count);
        CollectionAssert.AreEqual(new[] { -2d, 2d }, pole.Roots.Select(root => root.Value).ToArray());
    }

    [TestMethod]
    public void ReduceCreatesKeyedBranchesWhenIndexChangesByRoot()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var sin = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!,
            Expression.Multiply(Expression.Constant(2d), x));
        var infinity = InfinityExpression.CreateLazy(
            sin,
            [(x, Math.PI / 4d), (x, 3d * Math.PI / 4d)]);

        var reduced = infinity.Reduce();

        var keyed = reduced as KeyedInfinityExpression;
        Assert.IsNotNull(keyed);
        Assert.AreEqual(2, keyed!.Branches.Count);
        CollectionAssert.AreEquivalent(
            new[] { 1d, -1d },
            keyed.Branches.Select(branch => (double)((ConstantExpression)branch.Numerator).Value!).ToArray());
        Assert.AreEqual(2, keyed.Roots.Count);
    }
}

