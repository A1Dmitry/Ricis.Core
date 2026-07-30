// RicisCore/PolarConverter.cs — strict RICIS polar collapse (no limits)

using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Rationals;

namespace Ricis.Core;

/// <summary>
/// Trigonometry via polar sectors (RICIS).
/// Angle → fraction of circle → exact algebraic value when sector is rational
/// with known closed form. Half of trig singularities collapse here (SP2 on the circle):
///   sin/cos → 0, ±1, ±1/2, … or stay as indexed 0_F / ∞_F.
/// No Taylor series. No L'Hôpital.
/// </summary>
public static class PolarConverter
{
    /// <summary>
    /// Exact (sin, cos) at rational sector p/q of the full circle, when known.
    /// Fraction is in [0,1) = angle/(2π).
    /// </summary>
    public static (double? sin, double? cos) ExactSinCos(Rational fractionOfCircle)
    {
        // normalize to [0,1)
        var f = fractionOfCircle;
        var floor = Rational.Floor(f);
        f = f - floor;
        if (f < Rational.Zero) f += Rational.One;

        var n = f.Numerator;
        var d = f.Denominator;

        // Table: fraction → (sin, cos). Algebraic exact values only.
        // 0, 1/12, 1/8, 1/6, 1/4, 1/3, 3/8, 5/12, 1/2, 7/12, 5/8, 2/3, 3/4, 5/6, 7/8, 11/12

        if (n.IsZero) return (0.0, 1.0);                    // 0
        if (n == 1 && d == 12) return (0.5, Sqrt3Over2()); // 30°
        if (n == 1 && d == 8) return (Sqrt2Over2(), Sqrt2Over2()); // 45°
        if (n == 1 && d == 6) return (Sqrt3Over2(), 0.5); // 60°
        if (n == 1 && d == 4) return (1.0, 0.0);            // 90°
        if (n == 1 && d == 3) return (Sqrt3Over2(), -0.5); // 120°
        if (n == 3 && d == 8) return (Sqrt2Over2(), -Sqrt2Over2()); // 135°
        if (n == 5 && d == 12) return (0.5, -Sqrt3Over2()); // 150°
        if (n == 1 && d == 2) return (0.0, -1.0);           // 180°
        if (n == 7 && d == 12) return (-0.5, -Sqrt3Over2()); // 210°
        if (n == 5 && d == 8) return (-Sqrt2Over2(), -Sqrt2Over2()); // 225°
        if (n == 2 && d == 3) return (-Sqrt3Over2(), -0.5); // 240°
        if (n == 3 && d == 4) return (-1.0, 0.0);           // 270°
        if (n == 5 && d == 6) return (-Sqrt3Over2(), 0.5); // 300°
        if (n == 7 && d == 8) return (-Sqrt2Over2(), Sqrt2Over2()); // 315°
        if (n == 11 && d == 12) return (-0.5, Sqrt3Over2()); // 330°

        // full circle = 0
        if (n == d) return (0.0, 1.0);

        return (null, null); // not an exact algebraic sector
    }

    private static double Sqrt2Over2() => Math.Sqrt(2.0) / 2.0;
    private static double Sqrt3Over2() => Math.Sqrt(3.0) / 2.0;

    /// <summary>
    /// Collapse trig at a numeric angle via polar sector.
    /// Returns Expression.Constant when exact; null if sector not algebraic.
    /// </summary>
    public static Expression TryCollapseTrig(string methodName, double radians)
    {
        if (double.IsNaN(radians) || double.IsInfinity(radians))
            return null;

        CircleSectors sector;
        try
        {
            sector = CircleSectors.FromRadians(radians);
        }
        catch
        {
            return null;
        }

        var (sin, cos) = ExactSinCos(sector.Fraction);
        if (sin is null || cos is null)
            return null;

        return methodName switch
        {
            "Sin" => Expression.Constant(sin.Value),
            "Cos" => Expression.Constant(cos.Value),
            "Tan" when Math.Abs(cos.Value) < 1e-15 =>
                // cos = 0 → ∞ indexed by sin (A1): pole
                InfinityExpression.CreateLazy(
                    Expression.Constant(sin.Value),
                    Expression.Parameter(typeof(double), "θ"),
                    radians),
            "Tan" => Expression.Constant(sin.Value / cos.Value),
            "Cot" when Math.Abs(sin.Value) < 1e-15 =>
                InfinityExpression.CreateLazy(
                    Expression.Constant(cos.Value),
                    Expression.Parameter(typeof(double), "θ"),
                    radians),
            "Cot" => Expression.Constant(cos.Value / sin.Value),
            _ => null
        };
    }

    /// <summary>
    /// SP2-on-circle: if MethodCall is Sin/Cos/Tan of a constant angle,
    /// replace by exact polar value (or indexed ∞).
    /// </summary>
    public static Expression CollapseConstantTrig(MethodCallExpression call)
    {
        if (call.Arguments.Count == 0) return call;
        var name = call.Method.Name;
        if (name is not ("Sin" or "Cos" or "Tan" or "Cot"))
            return call;

        if (call.Arguments[0] is not ConstantExpression ce)
            return call;

        double radians;
        try { radians = Convert.ToDouble(ce.Value); }
        catch { return call; }

        return TryCollapseTrig(name, radians) ?? call;
    }

    /// <summary>
    /// Polar representation of a singularity monolith — each root as sector text.
    /// </summary>
    public static string ToPolarSector(InfinityExpression monolith, int totalSectors = 8, int maxDenominator = 100)
    {
        if (monolith == null)
            return "∅";

        var parts = new List<string>();
        var roots = monolith.Roots;

        if (roots == null || roots.Count == 0)
        {
            var idx = monolith.Numerator?.ToString() ?? "?";
            return $"∞_{{{idx}}} (no polar roots)";
        }

        foreach (var (param, value) in roots)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                parts.Add($"{param?.Name ?? "?"}=? → undefined sector");
                continue;
            }

            try
            {
                var sector = CircleSectors.FromRadians(value, maxDenominator);
                var (sin, cos) = ExactSinCos(sector.Fraction);
                var sectorText = sector.InSectors(totalSectors);
                var collapse = sin is not null
                    ? $" [polar collapse sin={sin:G6}, cos={cos:G6}]"
                    : " [non-exact sector — keep indexed singularity]";

                var numVal = EvaluateNumeratorExactly(monolith);
                var numStr = numVal is null ? "?" : numVal.Value.ToString("G6");

                parts.Add(
                    $"{param?.Name ?? "?"}={value:G6} → {sector} ({sectorText}) " +
                    $"index={numStr}{collapse}");
            }
            catch (Exception ex)
            {
                parts.Add($"{param?.Name ?? "?"}={value:G6} → polar error: {ex.Message}");
            }
        }

        var head = monolith.Numerator != null
            ? $"∞_{{{monolith.Numerator}}}"
            : "∞";
        return head + " | " + string.Join(" ; ", parts);
    }

    private static double? EvaluateNumeratorExactly(InfinityExpression inf)
    {
        try
        {
            if (inf.Variable == null || double.IsNaN(inf.SingularityValue))
                return null;

            var visitor = new SubstitutionVisitor(inf.SingularityValue, inf.Variable.Name);
            var substituted = visitor.Visit(inf.Numerator);
            var lambda = Expression.Lambda<Func<double>>(Expression.Convert(substituted, typeof(double)));
            var value = lambda.Compile()();

            if (double.IsNaN(value) || double.IsInfinity(value))
                return null;

            return value;
        }
        catch
        {
            return null;
        }
    }
}
