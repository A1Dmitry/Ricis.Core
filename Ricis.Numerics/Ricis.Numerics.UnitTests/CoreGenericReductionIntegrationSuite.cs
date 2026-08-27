using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Numerics.Factorization;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class CoreGenericReductionIntegrationSuite
{
    [TestMethod("CORE-INT2048-01: Int2048 generic identity reduces through Core without registration")]
    public void Int2048GenericIdentityReducesThroughCoreWithoutRegistration()
    {
        var value = Expression.Parameter(typeof(Int2048), "x");
        var source = Expression.Lambda<Func<Int2048, Int2048>>(Expression.Divide(value, value), value);
        var derived = RicisPhasePipeline.Simplify(source);

        Assert.IsInstanceOfType<ConstantExpression>(derived.Body);
        var constant = (ConstantExpression)derived.Body;
        Assert.AreEqual(typeof(Int2048), constant.Type);
        Assert.AreEqual(Int2048.One, (Int2048)constant.Value!);
    }

    [TestMethod("CORE-INT2048-02: Int2048 generic O1 bridge preserves fixed-width payload type")]
    public void Int2048GenericO1BridgePreservesFixedWidthPayloadType()
    {
        var value = Expression.Parameter(typeof(Int2048), "x");
        var source = Expression.Lambda<Func<Int2048, Int2048>>(
            Expression.Multiply(value, Expression.Constant(Int2048.Zero)),
            value);
        var derived = RicisPhasePipeline.Simplify(source);

        Assert.IsInstanceOfType<ZeroInfinityExpression>(derived.Body);
        Assert.AreEqual(typeof(Int2048), derived.Body.Type);
    }

    [TestMethod("CORE-INT2048-03: generic trace preserves exact fixed-width lambda type")]
    public void Int2048GenericTracePreservesExactLambdaType()
    {
        var value = Expression.Parameter(typeof(Int2048), "x");
        var source = Expression.Lambda<Func<Int2048, Int2048>>(Expression.Divide(value, value), value);
        var trace = new List<RicisPhaseTraceStep>();
        var derived = RicisPhasePipeline.SimplifyWithTrace(source, trace);

        Assert.AreEqual(typeof(Func<Int2048, Int2048>), derived.Type);
        Assert.IsTrue(trace.Count >= 8);
        Assert.IsInstanceOfType<ConstantExpression>(derived.Body);
    }

    [TestMethod("CORE-ULONG2048-01: unsigned domain is intentionally not presented as Core INumber")]
    public void ULong2048UnsignedDomainIsNotPresentedAsCoreINumber()
    {
        Assert.IsFalse(typeof(ULong2048).GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType &&
            interfaceType.GetGenericTypeDefinition() == typeof(System.Numerics.INumber<>)));
        Assert.AreEqual(ULong2048.Zero, new Semiprime<ULong2048>(new ULong2048(59), new ULong2048(101)).P - new ULong2048(59));
    }
}
