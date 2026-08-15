using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

internal static class RicisContinuousSugarSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("SUGAR01: Abs — строит конечное условное дерево |F|", AbsoluteValueBuildsPiecewiseTree),
        ("SUGAR02: Abs — одинаковые |F| сохраняют L1 в отношении", AbsoluteValuePreservesIdentity),
        ("SUGAR03: Min/Max — связывают разные параметры без вычисления", ExtremumsRebindParameters),
        ("SUGAR04: Max — одинаковые нормализованные функции дают F", MaximumIdentity),
        ("SUGAR05: Clamp — постоянные границы задают min(max(F,L),U)", ConstantClamp),
        ("SUGAR06: Clamp — отложенные границы остаются expression tree", DeferredClamp),
        ("SUGAR07: Положительная и отрицательная части сохраняют BigInteger", SignedPartsPreserveBigInteger),
        ("SUGAR08: Distance — строит |F−G| из независимых лямбд", DistanceBuildsAbsoluteDifference),
    ];

    private static void AbsoluteValueBuildsPiecewiseTree()
    {
        Expression<Func<double, double>> source = x => x - 1.0;
        var absolute = source.Abs();

        Require(absolute.Body is ConditionalExpression,
            $"Abs должен вернуть условное expression-дерево, получено: {absolute.Body.NodeType}.");
        var execute = absolute.Compile();
        Require(Math.Abs(execute(-2.0) - 3.0) < 1e-12, $"|−3| должно быть 3, получено {execute(-2.0)}.");
        Require(Math.Abs(execute(4.0) - 3.0) < 1e-12, $"|3| должно быть 3, получено {execute(4.0)}.");
    }

    private static void AbsoluteValuePreservesIdentity()
    {
        Expression<Func<double, double>> source = x => x - 1.0;
        var result = source.Abs().Ratio(source.Abs());

        Require(result.Body is ConstantExpression { Value: double value } && value == 1.0,
            $"L1 должен сократить |F|/|F| до 1, получено: {result}.");
        Require(result.Compile()(1.0) == 1.0,
            "Производное дерево |F|/|F| обязано исполняться в классической нулевой точке F.");
    }

    private static void ExtremumsRebindParameters()
    {
        Expression<Func<double, double>> first = x => x + 1.0;
        Expression<Func<double, double>> second = y => 2.0 * y;
        var minimum = first.Min(second);
        var maximum = first.Max(second);

        Require(Math.Abs(minimum.Compile()(2.0) - 3.0) < 1e-12,
            $"min(x+1, 2x) при x=2 должен быть 3, получено {minimum.Compile()(2.0)}.");
        Require(Math.Abs(maximum.Compile()(2.0) - 4.0) < 1e-12,
            $"max(x+1, 2x) при x=2 должен быть 4, получено {maximum.Compile()(2.0)}.");
        Require(minimum.Body is ConditionalExpression && maximum.Body is ConditionalExpression,
            "Min и Max должны быть выражены конечными условными узлами.");
    }

    private static void MaximumIdentity()
    {
        Expression<Func<double, double>> source = x => (x * x) + 1.0;
        var maximum = source.Max(source);

        Require(maximum.Body.AreEqual(source.Body),
            $"max(F,F) должен вернуть F без лишней ветви, получено: {maximum}.");
        Require(Math.Abs(maximum.Compile()(3.0) - 10.0) < 1e-12,
            "max(F,F) должен исполняться как исходная функция.");
    }

    private static void ConstantClamp()
    {
        Expression<Func<double, double>> source = x => 2.0 * x;
        var clamped = source.Clamp(-1.0, 1.0).Compile();

        Require(Math.Abs(clamped(-3.0) + 1.0) < 1e-12, $"Clamp снизу должен вернуть −1, получено {clamped(-3.0)}.");
        Require(Math.Abs(clamped(0.25) - 0.5) < 1e-12, $"Clamp внутри диапазона должен вернуть 0.5, получено {clamped(0.25)}.");
        Require(Math.Abs(clamped(3.0) - 1.0) < 1e-12, $"Clamp сверху должен вернуть 1, получено {clamped(3.0)}.");
    }

    private static void DeferredClamp()
    {
        Expression<Func<double, double>> source = x => 2.0 * x;
        Expression<Func<double, double>> lower = y => y - 1.0;
        Expression<Func<double, double>> upper = z => z + 1.0;
        var clamped = source.Clamp(lower, upper);
        var execute = clamped.Compile();

        Require(Math.Abs(execute(-2.0) + 3.0) < 1e-12,
            $"Отложенная нижняя граница должна вернуть −3, получено {execute(-2.0)}.");
        Require(Math.Abs(execute(0.0)) < 1e-12,
            $"Отложенный Clamp в диапазоне должен вернуть 0, получено {execute(0.0)}.");
        Require(Math.Abs(execute(2.0) - 3.0) < 1e-12,
            $"Отложенная верхняя граница должна вернуть 3, получено {execute(2.0)}.");
    }

    private static void SignedPartsPreserveBigInteger()
    {
        Expression<Func<BigInteger, BigInteger>> identity = value => value;
        var positive = identity.PositivePart().Compile();
        var negative = identity.NegativePart().Compile();

        Require(positive(BigInteger.Parse("-987654321098765432109876543210")) == BigInteger.Zero,
            "Положительная часть BigInteger должна вернуть типизированный ноль для отрицательного значения.");
        Require(positive(BigInteger.Parse("987654321098765432109876543210")) == BigInteger.Parse("987654321098765432109876543210"),
            "Положительная часть не должна приводить BigInteger к double.");
        Require(negative(BigInteger.Parse("987654321098765432109876543210")) == BigInteger.Zero,
            "Отрицательная часть BigInteger должна вернуть типизированный ноль для положительного значения.");
        Require(negative(BigInteger.Parse("-987654321098765432109876543210")) == BigInteger.Parse("-987654321098765432109876543210"),
            "Отрицательная часть не должна приводить BigInteger к double.");
    }

    private static void DistanceBuildsAbsoluteDifference()
    {
        Expression<Func<double, double>> first = x => x + 2.0;
        Expression<Func<double, double>> second = y => y - 3.0;
        var distance = first.Distance(second);

        Require(distance.Body is ConditionalExpression,
            $"Distance должен завершаться абсолютным условным деревом, получено {distance.Body.NodeType}.");
        Require(Math.Abs(distance.Compile()(4.0) - 5.0) < 1e-12,
            $"|F−G| при x=4 должен быть 5, получено {distance.Compile()(4.0)}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
