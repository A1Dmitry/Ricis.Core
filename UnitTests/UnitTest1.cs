using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class LogicalSimplifierUnitTests
{
    [TestMethod]
    [TestCategory("PublicApi")]
    public void Apply_ReducesSafeBooleanIdentity()
    {
        var flag = Expression.Parameter(typeof(bool), "flag");
        var source = Expression.AndAlso(Expression.Constant(true), flag);

        var actual = LogicalSimplifier.Apply(source);

        Assert.IsTrue(ReferenceEquals(flag, actual),
            "Public logical facade must safely reduce true && flag to the original flag node.");
    }

    [TestMethod]
    [TestCategory("SafetyBoundary")]
    public void Apply_PreservesImpureShortCircuitExpression()
    {
        var source = Expression.AndAlso(
            Expression.Call(typeof(LogicalSimplifierUnitTests).GetMethod(
                nameof(SideEffectPredicate),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!),
            Expression.Constant(false));

        var actual = LogicalSimplifier.Apply(source);

        Assert.IsTrue(ReferenceEquals(source, actual),
            "Public logical facade must not rewrite impureCall() && false because that would alter evaluation semantics.");
    }

    [TestMethod]
    [TestCategory("SafetyBoundary")]
    public void Apply_PreservesNonBooleanExpression()
    {
        var source = Expression.Add(Expression.Constant(1.0), Expression.Constant(2.0));

        var actual = LogicalSimplifier.Apply(source);

        Assert.IsTrue(ReferenceEquals(source, actual),
            "Public logical facade must leave expressions outside the Boolean domain unchanged.");
    }

    [TestMethod]
    [TestCategory("PublicApi")]
    public void Apply_RejectsNullExpression()
    {
        Assert.ThrowsException<ArgumentNullException>(() => LogicalSimplifier.Apply(null!));
    }

    private static bool SideEffectPredicate() => true;
}
