using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Logging;
using Ricis.Core.Phases;

internal static class RicisGenericScalarPolicySuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("GNP01: generic Simplify returns typed INumber identity", GenericSimplifyReturnsTypedIdentity),
        ("GNP02: generic SimplifyWithTrace records normative phases", GenericSimplifyWithTraceRecordsPhases),
        ("GNP03: generic SimplifyWithLog publishes typed audit", GenericSimplifyWithLogPublishesAudit),
        ("GNP04: generic SimplifyWithTraceAndLog preserves both journals", GenericSimplifyWithTraceAndLogPreservesBothJournals),
        ("GNP05: generic unsigned subtraction does not fabricate unary negate", GenericUnsignedSubtractionPreservesTree),
    ];

    private static void GenericSimplifyReturnsTypedIdentity()
    {
        var value = Expression.Parameter(typeof(BigInteger), "x");
        var source = Expression.Lambda<Func<BigInteger, BigInteger>>(Expression.Divide(value, value), value);
        var result = RicisPhasePipeline.Simplify(source);

        Require(result.Body is ConstantExpression { Value: BigInteger one } && one == BigInteger.One && result.Body.Type == typeof(BigInteger),
            "Generic Simplify обязан вернуть exact T.One без регистрации или concrete-type fallback.");
    }

    private static void GenericSimplifyWithTraceRecordsPhases()
    {
        var value = Expression.Parameter(typeof(BigInteger), "x");
        var source = Expression.Lambda<Func<BigInteger, BigInteger>>(Expression.Divide(value, value), value);
        var trace = new List<RicisPhaseTraceStep>();
        var result = RicisPhasePipeline.SimplifyWithTrace(source, trace);

        Require(result.Body is ConstantExpression { Value: BigInteger one } && one == BigInteger.One &&
                trace.Count >= 8 && trace.Any(step => step.PhaseName.Contains("тождество", StringComparison.Ordinal)),
            "Generic SimplifyWithTrace обязан сохранить typed L1 result и trace каждой нормативной фазы.");
    }

    private static void GenericSimplifyWithLogPublishesAudit()
    {
        var value = Expression.Parameter(typeof(BigInteger), "x");
        var source = Expression.Lambda<Func<BigInteger, BigInteger>>(Expression.Divide(value, value), value);
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        var result = RicisPhasePipeline.SimplifyWithLog<BigInteger, RicisProofOrchestrationStage>(source, log);

        Require(result.Body is ConstantExpression { Value: BigInteger one } && one == BigInteger.One &&
                log.Snapshot().Any(entry => entry.EventCode == "RICIS_PIPELINE_START") &&
                log.Snapshot().Any(entry => entry.EventCode == "RICIS_PHASE_TRACE") &&
                log.Snapshot().Any(entry => entry.EventCode == "RICIS_PIPELINE_COMPLETE"),
            "Generic SimplifyWithLog обязан публиковать root и phase audit в canonical typed journal.");
    }

    private static void GenericSimplifyWithTraceAndLogPreservesBothJournals()
    {
        var value = Expression.Parameter(typeof(BigInteger), "x");
        var source = Expression.Lambda<Func<BigInteger, BigInteger>>(Expression.Divide(value, value), value);
        var trace = new List<RicisPhaseTraceStep>();
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        var result = RicisPhasePipeline.SimplifyWithTraceAndLog<BigInteger, RicisProofOrchestrationStage>(source, trace, log);

        Require(result.Body is ConstantExpression { Value: BigInteger one } && one == BigInteger.One &&
                trace.Count >= 8 && log.Snapshot().Count >= 10,
            "Generic combined overload обязан сохранить typed result и оба независимых audit output.");
    }

    private static void GenericUnsignedSubtractionPreservesTree()
    {
        var value = Expression.Parameter(typeof(uint), "x");
        var source = Expression.Lambda<Func<uint, uint>>(
            Expression.Subtract(Expression.Constant(0u), value),
            value);
        var result = RicisPhasePipeline.Simplify(source);

        Require(result.Body is BinaryExpression { NodeType: ExpressionType.Subtract },
            "Unsigned generic route не должен заменять 0-x на неподдерживаемое unary negation.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
