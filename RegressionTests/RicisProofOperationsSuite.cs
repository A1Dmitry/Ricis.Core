using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

internal static class RicisProofOperationsSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PROOF01: Compose — подстановка строит F∘G без вызова делегата", ComposeBuildsDeferredExpression),
        ("PROOF02: At — является точным alias структурной подстановки", AtAliasesCompose),
        ("PROOF03: Difference — одинаковые нормализованные F и G дают типизированный 0", DifferenceProvesIdentity),
        ("PROOF04: Ratio — L1 упрощает F/G до 1 до вычисления", RatioPreservesPhaseZeroIdentity),
        ("PROOF05: Product — сохраняет generic BigInteger без double", ProductPreservesBigInteger),
        ("PROOF06: Compose — L1 применяется к внешней функции до подстановки", ComposeNormalizesOuterIdentity)
    ];

    private static void ComposeBuildsDeferredExpression()
    {
        Expression<Func<double, double>> outer = x => (x * x) + 1.0;
        Expression<Func<double, double>> inner = u => u + 2.0;
        var composed = outer.Compose(inner);

        Require(Math.Abs(composed.Compile()(1.0) - 10.0) <= 1e-12,
            $"PROOF01: ожидалось F(G(1))=(1+2)²+1=10, получено {composed.Compile()(1.0):G17}.");
        Require(composed.Body.ToString().Contains("u"),
            "PROOF01: Compose должен сохранить тело внутренней лямбды как expression tree.");
    }

    private static void AtAliasesCompose()
    {
        Expression<Func<double, double>> function = x => (x * x) - 1.0;
        Expression<Func<double, double>> argument = u => u + 3.0;
        var viaCompose = function.Compose(argument);
        var viaAt = function.At(argument);

        Require(viaAt.Body.AreEqual(viaCompose.Body),
            $"PROOF02: At должен быть тождественен Compose; получено {viaAt.Body} и {viaCompose.Body}.");
    }

    private static void DifferenceProvesIdentity()
    {
        Expression<Func<double, double>> left = x => x + 1.0;
        Expression<Func<double, double>> right = y => y + 1.0;
        var difference = left.Difference(right);

        Require(difference.Body is ConstantExpression { Value: double value } && value == 0.0,
            $"PROOF03: F−F должен дать типизированный 0, получено {difference.Body}.");
        Require(difference.Compile()(0.0) == 0.0,
            "PROOF03: доказательное нулевое выражение должно исполняться без исключения.");
    }

    private static void RatioPreservesPhaseZeroIdentity()
    {
        Expression<Func<double, double>> numerator = x => x / x;
        Expression<Func<double, double>> denominator = y => y / y;
        var ratio = numerator.Ratio(denominator);

        Require(ratio.Body is ConstantExpression { Value: double value } && value == 1.0,
            $"PROOF04: L1 требует единицу до мостов, получено {ratio.Body}.");
        Require(ratio.Compile()(0.0) == 1.0,
            "PROOF04: производное отношение должно исполняться в нулевой точке.");
    }

    private static void ProductPreservesBigInteger()
    {
        Expression<Func<BigInteger, BigInteger>> left = x => x + BigInteger.One;
        Expression<Func<BigInteger, BigInteger>> right = y => y - BigInteger.One;
        var product = left.Product(right);
        var actual = product.Compile()(new BigInteger(3));

        Require(actual == new BigInteger(8),
            $"PROOF05: ожидалось (3+1)·(3−1)=8, получено {actual}.");
    }

    private static void ComposeNormalizesOuterIdentity()
    {
        Expression<Func<double, double>> outer = x => x / x;
        Expression<Func<double, double>> inner = y => y - 1.0;
        var composed = outer.Compose(inner);

        Require(composed.Body is ConstantExpression { Value: double value } && value == 1.0,
            $"PROOF06: L1 должен нормализовать внешнее F/F до подстановки; получено {composed.Body}.");
        Require(composed.Compile()(1.0) == 1.0,
            "PROOF06: Compose не должен создавать 0/0 после нормализации L1.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
