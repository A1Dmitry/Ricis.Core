using System.Linq.Expressions;
using Ricis.Core.Logging;

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
        ILog<RicisPiecewiseSurfaceStage> log = null)
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var zero = Expression.Constant(0.0);
        var five = Expression.Constant(5.0);
        var one = Expression.Constant(1.0);
        var lowerY = Expression.GreaterThanOrEqual(y, one);
        var upperY = Expression.LessThanOrEqual(y, five);
        var lowerX = Expression.GreaterThanOrEqual(x, zero);
        var upperX = Expression.LessThanOrEqual(x, five);
        var parabola = Expression.Divide(Expression.Multiply(x, x), five);
        var aboveParabola = Expression.GreaterThan(y, parabola);
        var domain = Expression.AndAlso(
            Expression.AndAlso(lowerX, upperX),
            Expression.AndAlso(
                Expression.AndAlso(lowerY, upperY),
                aboveParabola));
        var product = Expression.Convert(Expression.Multiply(x, y), typeof(double?));
        var nullValue = Expression.Constant(null, typeof(double?));
        var body = Expression.Condition(domain, product, nullValue);
        var function = Expression.Lambda<Func<double, double, double?>>(body, x, y);

        log?.Info(
            "RICIS_PIECEWISE_START",
            "Построение двухпеременной nullable RICIS-поверхности; domain: 0 <= x <= 5, 1 <= y <= 5, y > x^2 / 5; value: x*y; outside: null.",
            new Dictionary<string, string>
            {
                ["resultType"] = typeof(double?).FullName!,
                ["domain"] = "0 <= x <= 5; 1 <= y <= 5; y > x^2 / 5",
                ["value"] = "x * y",
                ["outside"] = "null",
            });
        log?.For<RicisPiecewiseSurfaceStage>().Trace(
            "RICIS_PIECEWISE_DOMAIN",
            "Область задана пересечением eq2: 0 <= x <= 5, g: 1 <= y <= 5 и b: y > x^2 / 5.",
            domain.ToString(),
            domain.ToString(),
            new Dictionary<string, string>
            {
                ["eq2"] = "5 >= x >= 0",
                ["g"] = "1 <= y <= 5",
                ["b"] = "y > x^2 / 5",
            });
        log?.For<RicisPiecewiseSurfaceStage>().Trace(
            "RICIS_PIECEWISE_VALUE_BRANCH",
            "Внутри области выбирается значение a(x,y) = x*y.",
            body.ToString(),
            product.ToString(),
            new Dictionary<string, string> { ["branch"] = "domain=true" });
        log?.For<RicisPiecewiseSurfaceStage>().Trace(
            "RICIS_PIECEWISE_NULL_BRANCH",
            "Вне области результатом является null.",
            body.ToString(),
            nullValue.ToString(),
            new Dictionary<string, string> { ["branch"] = "domain=false" });
        log?.Info(
            "RICIS_PIECEWISE_COMPLETE",
            "Nullable кусочная поверхность построена без исполнения expression tree.",
            new Dictionary<string, string> { ["expression"] = function.ToString() });

        return function;
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
