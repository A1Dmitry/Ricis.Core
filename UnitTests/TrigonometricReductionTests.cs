using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;
using Ricis.Core.Solvers;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class TrigonometricReductionTests
{
    [TestMethod]
    public void PolarTransitionCollapsesExactConstantAngle()
    {
        var sin = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!,
            Expression.Constant(Math.PI / 6d));

        var reduced = new PolarTrigVisitor().Visit(sin);

        Assert.IsInstanceOfType<ConstantExpression>(reduced);
        Assert.AreEqual(0.5d, (double)((ConstantExpression)reduced).Value!, 1e-12);
    }

    [TestMethod]
    public void PolarTransitionRepresentsTangentPoleAsIndexedInfinity()
    {
        var tan = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!,
            Expression.Constant(Math.PI / 2d));

        var reduced = new PolarTrigVisitor().Visit(tan);

        Assert.IsInstanceOfType<InfinityExpression>(reduced);
        Assert.AreEqual(1d, ((InfinityExpression)reduced!).NumeratorAsDouble(), 1e-12);
    }

    [TestMethod]
    public void TrigSolverCertifiesRootsWithoutTreatingTangentPolesAsRoots()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var tan = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!, x);
        var denominator = Expression.Subtract(Expression.Constant(1d), tan);

        var solved = TrigSolver.Solve(tan);

        Assert.IsTrue(solved.HasValue);
        var root = solved.Value;
        Assert.AreEqual(0d, Math.Tan(root.Item2), 1e-12);
        Assert.IsTrue(Math.Abs(Math.Cos(root.Item2)) > 1e-6,
            $"TrigSolver returned a tangent pole instead of a root: {root.Item2:R}");
    }

    [TestMethod]
    public void PipelineUsesTrigSolverForIndexedTangentPoles()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var tan = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!, x);
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(1d), Expression.Subtract(Expression.Constant(1d), tan)), x);

        var reduced = RicisPhasePipeline.Simplify(source);

        Assert.IsTrue(reduced.Body is InfinityExpression infinity && infinity.Roots.Count > 0,
            $"Expected indexed tangent poles, got: {reduced}");
    }
}

internal static class InfinityTestExtensions
{
    public static double NumeratorAsDouble(this InfinityExpression infinity)
    {
        return ((ConstantExpression)infinity.Numerator).Value is double value
            ? value
            : double.NaN;
    }
}
