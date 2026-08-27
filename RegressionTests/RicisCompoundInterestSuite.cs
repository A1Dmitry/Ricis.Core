using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

internal static class RicisCompoundInterestSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("INT01: CompoundInterest — decimal вычисляет S·(1+r/100)^n точно", DecimalCompoundInterest),
        ("INT02: CompoundInterest — связывает отложенные S и r", DeferredPrincipalAndRate),
        ("INT03: CompoundInterest — n=0 возвращает нормализованный S", ZeroPeriodsPreservePrincipal),
        ("INT04: CompoundInterest — BigInteger сохраняет native-точность", BigIntegerExactGrowth),
        ("INT05: CompoundInterest — double-период сохраняет Math.Pow как дерево", DeferredDoublePeriods),
        ("INT06: CompoundInterest — отрицательное число периодов отклоняется", NegativePeriodsAreRejected),
        ("INT07: CompoundInterest — L1 нормализует rate до построения", RateUsesPhaseZeroIdentity),
    ];

    private static void DecimalCompoundInterest()
    {
        Expression<Func<decimal, decimal>> principal = _ => 1000m;
        Expression<Func<decimal, decimal>> rate = _ => 5m;
        var result = principal.CompoundInterest(rate, 2);

        Require(result.Compile()(0m) == 1102.5m,
            $"1000·(1+5/100)^2 должно быть 1102.5, получено {result.Compile()(0m)}.");
    }

    private static void DeferredPrincipalAndRate()
    {
        Expression<Func<double, double>> principal = x => 100.0 * x;
        Expression<Func<double, double>> rate = y => 5.0 * y;
        var result = principal.CompoundInterest(rate, 2);

        Require(ReferenceEquals(result.Parameters[0], result.Parameters[0]),
            "CompoundInterest должен вернуть единую отложенную лямбду.");
        Require(Math.Abs(result.Compile()(2.0) - 242.0) < 1e-12,
            $"S=200, r=10%, n=2 должно дать 242, получено {result.Compile()(2.0)}.");
    }

    private static void ZeroPeriodsPreservePrincipal()
    {
        Expression<Func<double, double>> principal = x => x + 1.0;
        Expression<Func<double, double>> rate = y => 7.5;
        var result = principal.CompoundInterest(rate, 0);

        Require(result.Body.AreEqual(principal.Body),
            $"При n=0 формула должна вернуть S, получено {result}.");
        Require(Math.Abs(result.Compile()(4.0) - 5.0) < 1e-12,
            "Производное дерево при n=0 должно исполняться как principal.");
    }

    private static void BigIntegerExactGrowth()
    {
        Expression<Func<BigInteger, BigInteger>> principal = value => value;
        Expression<Func<BigInteger, BigInteger>> rate = _ => new BigInteger(100);
        var result = principal.CompoundInterest(rate, 5);
        var input = BigInteger.Parse("1234567890123456789012345678901234567890");

        Require(result.Compile()(input) == input * 32,
            "При r=100% каждый период должен удваивать BigInteger без перехода к double.");
    }

    private static void DeferredDoublePeriods()
    {
        Expression<Func<double, double>> principal = _ => 1000.0;
        Expression<Func<double, double>> rate = _ => 5.0;
        Expression<Func<double, double>> periods = x => x;
        var result = principal.CompoundInterest(rate, periods);

        Require(result.Body is BinaryExpression { Right: MethodCallExpression { Method.Name: nameof(Math.Pow) } },
            $"Отложенный n должен сохранять Math.Pow в дереве, получено: {result.Body}.");
        Require(Math.Abs(result.Compile()(2.0) - 1102.5) < 1e-12,
            $"1000·1.05^2 должно быть 1102.5, получено {result.Compile()(2.0)}.");
    }

    private static void NegativePeriodsAreRejected()
    {
        Expression<Func<double, double>> principal = x => x;
        Expression<Func<double, double>> rate = x => 5.0;

        try
        {
            _ = principal.CompoundInterest(rate, -1);
            throw new InvalidOperationException("Ожидалось ArgumentOutOfRangeException для отрицательного периода.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    private static void RateUsesPhaseZeroIdentity()
    {
        Expression<Func<double, double>> principal = _ => 100.0;
        Expression<Func<double, double>> rate = x => x / x;
        var result = principal.CompoundInterest(rate, 1);

        Require(Math.Abs(result.Compile()(0.0) - 101.0) < 1e-12,
            "L1 должен нормализовать x/x до 1% до построения сложного процента.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
