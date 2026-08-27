using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Logging;
using Ricis.Core.Phases;

internal static class RicisPiecewiseSurfaceSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PWS01: piecewise surface returns x*y inside and null outside", PiecewiseSurfaceEvaluatesDomain),
        ("PWS02: piecewise surface logs domain and both branches into Tex", PiecewiseSurfaceLogsIntoTex),
        ("PWS03: null optional log preserves expression construction", NullLogPreservesLegacyConstruction),
    ];

    private static void PiecewiseSurfaceEvaluatesDomain()
    {
        var function = RicisPiecewiseSurfaceExample.Build();
        var evaluate = function.Compile();

        Require(evaluate(2.0, 2.0) == 4.0, "Точка (2,2) внутри области должна вернуть x*y=4.");
        Require(evaluate(0.0, 1.0) == 0.0, "Граничная точка (0,1) должна быть включена по нестрогим eq2/g.");
        Require(evaluate(6.0, 2.0) is null, "Точка x=6 вне eq2 должна вернуть null.");
        Require(evaluate(2.0, 0.5) is null, "Точка y=0.5 вне g должна вернуть null.");
        Require(evaluate(4.0, 3.0) is null, "Точка на параболе y=x²/5 не проходит строгое b и должна вернуть null.");
    }

    private static void PiecewiseSurfaceLogsIntoTex()
    {
        var log = new RicisProofLog<RicisPiecewiseSurfaceStage>();
        var trace = new List<RicisPhaseTraceStep>();
        var function = RicisPiecewiseSurfaceExample.Build(log, trace);
        var entries = log.Snapshot();
        var tex = RicisPiecewiseSurfaceExample.RenderLog(log, RicisProofLogFormat.Latex);
        var evaluate = function.Compile();

        Require(evaluate(2.0, 2.0) == 4.0 && evaluate(4.0, 3.0) is null &&
                entries.Select(entry => entry.EventCode).SequenceEqual(
                [
                    "RICIS_PIECEWISE_START",
                    "RICIS_PIECEWISE_X_STRIP",
                    "RICIS_PIECEWISE_Y_STRIP",
                    "RICIS_PIECEWISE_PARABOLA",
                    "RICIS_PIECEWISE_DOMAIN",
                    "RICIS_PIECEWISE_VALUE",
                    "RICIS_PIECEWISE_CONDITIONAL",
                    "RICIS_PIECEWISE_COMPLETE",
                ]) &&
                trace.Count == 6 &&
                trace.All(step => step.Changed && step.BeforeNodeToRoot.Count > 0 && step.AfterNodeToRoot.Count > 0) &&
                trace[0].Before.ToString().Contains("x >= 0", StringComparison.Ordinal) &&
                trace[0].After.ToString().Contains("x <= 5", StringComparison.Ordinal) &&
                trace[2].After.ToString().Contains("x * x", StringComparison.Ordinal) &&
                trace[5].After.ToString().Contains("IIF", StringComparison.Ordinal) &&
                entries.Single(entry => entry.EventCode == "RICIS_PIECEWISE_DOMAIN").BeforeExpression?.Contains("x >= 0", StringComparison.Ordinal) == true &&
                tex.Contains("RICIS\\_PIECEWISE\\_DOMAIN", StringComparison.Ordinal) &&
                tex.Contains("RICIS\\_PIECEWISE\\_VALUE", StringComparison.Ordinal) &&
                tex.Contains("RICIS\\_PIECEWISE\\_CONDITIONAL", StringComparison.Ordinal) &&
                tex.Contains("0 <= x <= 5", StringComparison.Ordinal) &&
                tex.Contains("x\\textasciicircum{}2 / 5", StringComparison.Ordinal) &&
                tex.Contains("Ветви объединяются: внутри x*y, вне области null", StringComparison.Ordinal),
            "Tex report обязан содержать область, value branch, null branch и ordered typed journal.");
    }

    private static void NullLogPreservesLegacyConstruction()
    {
        var legacy = RicisPiecewiseSurfaceExample.Build();
        var explicitNull = RicisPiecewiseSurfaceExample.Build(null);
        Require(legacy.ToString() == explicitNull.ToString(),
            "Явный null log не должен изменять построенное expression tree.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
