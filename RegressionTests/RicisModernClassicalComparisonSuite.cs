using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;

internal static class RicisModernClassicalComparisonSuite
{
    private const double Tolerance = 1e-10;

    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("CHK01: Непрерывный сахар совпадает с классическими piecewise-функциями", ContinuousSugarMatchesClassical),
        ("CHK02: Комплексные expression-компоненты совпадают с System.Numerics.Complex", ComplexFunctionsMatchClassical),
        ("CHK03: CompoundInterest совпадает с прямой формулой Math.Pow", CompoundInterestMatchesClassical),
        ("CHK04: Аналитический сахар совпадает с Math.* на контрольных точках", AnalyticSugarMatchesClassical),
        ("CHK05: Производная аналитического Pow совпадает с классической формулой", AnalyticDerivativeMatchesClassical),
    ];

    private static void ContinuousSugarMatchesClassical()
    {
        Expression<Func<double, double>> first = x => (x * x) - 3.0;
        Expression<Func<double, double>> second = y => y + 2.0;
        var absolute = first.Abs().Compile();
        var minimum = first.Min(second).Compile();
        var maximum = first.Max(second).Compile();
        var clamped = first.Clamp(-1.0, 2.0).Compile();
        var positive = first.PositivePart().Compile();
        var negative = first.NegativePart().Compile();
        var distance = first.Distance(second).Compile();

        foreach (var point in new[] { -2.0, -0.5, 0.0, 1.5, 3.0 })
        {
            var f = (point * point) - 3.0;
            var g = point + 2.0;
            AssertClose(absolute(point), Math.Abs(f), $"Abs при x={point}");
            AssertClose(minimum(point), Math.Min(f, g), $"Min при x={point}");
            AssertClose(maximum(point), Math.Max(f, g), $"Max при x={point}");
            AssertClose(clamped(point), Math.Clamp(f, -1.0, 2.0), $"Clamp при x={point}");
            AssertClose(positive(point), Math.Max(f, 0.0), $"PositivePart при x={point}");
            AssertClose(negative(point), Math.Min(f, 0.0), $"NegativePart при x={point}");
            AssertClose(distance(point), Math.Abs(f - g), $"Distance при x={point}");
        }
    }

    private static void ComplexFunctionsMatchClassical()
    {
        Expression<Func<double, double>> real = x => x + 1.0;
        Expression<Func<double, double>> imaginary = _ => 2.0;
        Expression<Func<double, double>> otherReal = y => y - 1.0;
        Expression<Func<double, double>> otherImaginary = _ => 3.0;
        var first = real.AsComplex(imaginary);
        var second = otherReal.AsComplex(otherImaginary);
        var product = first.Multiply(second);
        var conjugate = first.Conjugate();
        var squaredNorm = first.SquaredNorm().Compile();
        var norm = first.Norm().Compile();
        var productReal = product.Re().Compile();
        var productImaginary = product.Im().Compile();
        var conjugateImaginary = conjugate.Im().Compile();

        foreach (var point in new[] { -1.0, 0.0, 2.0 })
        {
            var z = new Complex(point + 1.0, 2.0);
            var w = new Complex(point - 1.0, 3.0);
            var expectedProduct = z * w;
            AssertClose(productReal(point), expectedProduct.Real, $"Re(z·w) при x={point}");
            AssertClose(productImaginary(point), expectedProduct.Imaginary, $"Im(z·w) при x={point}");
            AssertClose(conjugateImaginary(point), Complex.Conjugate(z).Imaginary, $"Im(conj(z)) при x={point}");
            AssertClose(squaredNorm(point), z.Real * z.Real + z.Imaginary * z.Imaginary, $"|z|² при x={point}");
            AssertClose(norm(point), z.Magnitude, $"|z| при x={point}");
        }
    }

    private static void CompoundInterestMatchesClassical()
    {
        Expression<Func<double, double>> principal = x => 1000.0 * x;
        Expression<Func<double, double>> rate = y => 2.5 * y;
        Expression<Func<double, double>> periods = z => z;
        var fixedPeriods = principal.CompoundInterest(rate, 3).Compile();
        var deferredPeriods = principal.CompoundInterest(rate, periods).Compile();

        foreach (var point in new[] { 1.0, 2.0, 3.0 })
        {
            var s = 1000.0 * point;
            var r = 2.5 * point;
            AssertClose(fixedPeriods(point), s * Math.Pow(1.0 + (r / 100.0), 3.0),
                $"CompoundInterest n=3 при x={point}");
            AssertClose(deferredPeriods(point), s * Math.Pow(1.0 + (r / 100.0), point),
                $"CompoundInterest n=x при x={point}");
        }
    }

    private static void AnalyticSugarMatchesClassical()
    {
        Expression<Func<double, double>> shifted = x => x + 2.0;
        Expression<Func<double, double>> exponent = y => (y / 2.0) + 1.0;
        var sin = shifted.Sin().Compile();
        var cos = shifted.Cos().Compile();
        var tan = shifted.Tan().Compile();
        var sinh = shifted.Sinh().Compile();
        var cosh = shifted.Cosh().Compile();
        var tanh = shifted.Tanh().Compile();
        var exp = shifted.Exp().Compile();
        var log = shifted.Log().Compile();
        var log10 = shifted.Log10().Compile();
        var sqrt = shifted.Sqrt().Compile();
        var constantPower = shifted.Pow(3.0).Compile();
        var deferredPower = shifted.Pow(exponent).Compile();

        foreach (var point in new[] { 0.0, 0.5, 1.0, 2.0 })
        {
            var f = point + 2.0;
            var p = (point / 2.0) + 1.0;
            AssertClose(sin(point), Math.Sin(f), $"Sin при x={point}");
            AssertClose(cos(point), Math.Cos(f), $"Cos при x={point}");
            AssertClose(tan(point), Math.Tan(f), $"Tan при x={point}");
            AssertClose(sinh(point), Math.Sinh(f), $"Sinh при x={point}");
            AssertClose(cosh(point), Math.Cosh(f), $"Cosh при x={point}");
            AssertClose(tanh(point), Math.Tanh(f), $"Tanh при x={point}");
            AssertClose(exp(point), Math.Exp(f), $"Exp при x={point}");
            AssertClose(log(point), Math.Log(f), $"Log при x={point}");
            AssertClose(log10(point), Math.Log10(f), $"Log10 при x={point}");
            AssertClose(sqrt(point), Math.Sqrt(f), $"Sqrt при x={point}");
            AssertClose(constantPower(point), Math.Pow(f, 3.0), $"Pow(F,3) при x={point}");
            AssertClose(deferredPower(point), Math.Pow(f, p), $"Pow(F,G) при x={point}");
        }
    }

    private static void AnalyticDerivativeMatchesClassical()
    {
        Expression<Func<double, double>> shifted = x => x + 1.0;
        var derivative = shifted.Pow(3.0).DxDt().Compile();

        foreach (var point in new[] { -0.5, 0.0, 1.0, 2.0 })
        {
            AssertClose(derivative(point), 3.0 * Math.Pow(point + 1.0, 2.0),
                $"d((x+1)^3)/dx при x={point}");
        }
    }

    private static void AssertClose(double actual, double expected, string context) =>
        RegressionAssertions.AssertClose(
            actual,
            expected,
            Tolerance,
            () => $"{context}: RICIS={actual:G17}, классика={expected:G17}.");
}
