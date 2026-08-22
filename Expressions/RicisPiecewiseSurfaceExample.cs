using System.Linq.Expressions;
using Ricis.Core.Logging;
using Ricis.Core.Phases;
using Ricis.Core.Resources;

namespace Ricis.Core.Expressions;

/// <summary>
/// Typed stage marker for the RICIS piecewise surface example.
/// </summary>
public sealed class RicisPiecewiseSurfaceStage
{
    private RicisPiecewiseSurfaceStage()
    {
    }
}

/// <summary>
/// Builds the deferred two-variable nullable surface
/// <c>(x,y) ↦ x·y</c> on the declared shaded region and <c>null</c> outside it.
/// The domain is structural: no delegate is compiled by this factory.
/// </summary>
public static class RicisPiecewiseSurfaceExample
{
    /// <summary>
    /// Builds the piecewise expression. A null optional log preserves the silent
    /// legacy construction path; a non-null log receives every construction step.
    /// </summary>
    public static Expression<Func<double, double, double?>> Build(
        ILog<RicisPiecewiseSurfaceStage> log = null,
        ICollection<RicisPhaseTraceStep> trace = null)
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var zero = Expression.Constant(0.0);
        var five = Expression.Constant(5.0);
        var one = Expression.Constant(1.0);
        log?.Info(
            "RICIS_PIECEWISE_START",
            RicisLegacyTextResources.Get("runtime.legacy.0eedc4da0112"),
            new Dictionary<string, string>
            {
                ["resultType"] = typeof(double?).FullName!,
                ["domain"] = "0 <= x <= 5; 1 <= y <= 5; y > x^2 / 5",
                ["value"] = "x * y",
                ["outside"] = "null",
            });
        var lowerY = Expression.GreaterThanOrEqual(y, one);
        var upperY = Expression.LessThanOrEqual(y, five);
        var lowerX = Expression.GreaterThanOrEqual(x, zero);
        var upperX = Expression.LessThanOrEqual(x, five);
        var xStrip = Expression.AndAlso(lowerX, upperX);
        RecordStep(log, trace, "RICIS_PIECEWISE_X_STRIP", RicisLegacyTextResources.Get("runtime.legacy.8bc9759cbfa9"), lowerX, xStrip, "eq2");
        var yStrip = Expression.AndAlso(lowerY, upperY);
        RecordStep(log, trace, "RICIS_PIECEWISE_Y_STRIP", RicisLegacyTextResources.Get("runtime.legacy.f823031d46d9"), lowerY, yStrip, "g");
        var parabola = Expression.Divide(Expression.Multiply(x, x), five);
        var aboveParabola = Expression.GreaterThan(y, parabola);
        RecordStep(log, trace, "RICIS_PIECEWISE_PARABOLA", RicisLegacyTextResources.Get("runtime.legacy.3df784a1f3c2"), y, aboveParabola, "b");
        var domain = Expression.AndAlso(xStrip, Expression.AndAlso(yStrip, aboveParabola));
        RecordStep(log, trace, "RICIS_PIECEWISE_DOMAIN", RicisLegacyTextResources.Get("runtime.legacy.0bcfcb312c66"), xStrip, domain, "SP2 / domain intersection");
        var product = Expression.Convert(Expression.Multiply(x, y), typeof(double?));
        RecordStep(log, trace, "RICIS_PIECEWISE_VALUE", RicisLegacyTextResources.Get("runtime.legacy.73ece1fbd2d6"), Expression.Multiply(x, y), product, "A1 / value branch");
        var nullValue = Expression.Constant(null, typeof(double?));
        var body = Expression.Condition(domain, product, nullValue);
        RecordStep(log, trace, "RICIS_PIECEWISE_CONDITIONAL", RicisLegacyTextResources.Get("runtime.legacy.c7e636d1d89c"), product, body, "A4 / conditional branch");
        var function = Expression.Lambda<Func<double, double, double?>>(body, x, y);

        log?.Info(
            "RICIS_PIECEWISE_COMPLETE",
            RicisLegacyTextResources.Get("runtime.legacy.24cfdda0a889"),
            new Dictionary<string, string> { ["expression"] = function.ToString() });

        return function;
    }

    private static void RecordStep(
        ILog<RicisPiecewiseSurfaceStage> log,
        ICollection<RicisPhaseTraceStep> trace,
        string eventCode,
        string message,
        Expression before,
        Expression after,
        string ruleFamily)
    {
        trace?.Add(new RicisPhaseTraceStep(message, ruleFamily, before, after, wasSkipped: false));
        log?.For<RicisPiecewiseSurfaceStage>().Trace(
            eventCode,
            message,
            before.ToString(),
            after.ToString(),
            new Dictionary<string, string>
            {
                ["ruleFamily"] = ruleFamily,
                ["changed"] = (!before.AreEqual(after)).ToString(),
            });
    }

    /// <summary>Renders the canonical snapshot into the requested report format.</summary>
    public static string RenderLog(
        ILog<RicisPiecewiseSurfaceStage> log,
        RicisProofLogFormat format)
    {
        ArgumentNullException.ThrowIfNull(log);
        return RicisProofLogReportRenderer.Render(log.Snapshot(), format);
    }
}
