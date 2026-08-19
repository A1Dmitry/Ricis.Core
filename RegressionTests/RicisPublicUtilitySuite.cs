using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core;
using Ricis.Core.Expressions;
using Ricis.Core.Rationals;

internal static class RicisPublicUtilitySuite
{
    public static IReadOnlyList<(string Name, Action Body)> Tests =
    [
        ("API01: ExactEvaluator вычисляет рациональное expression", ExactEvaluatorComputesRationalExpression),
        ("API02: ExactEvaluator отклоняет неподдерживаемый узел и неизвестный параметр", ExactEvaluatorRejectsUnsupportedShape),
        ("API03: CircleSectors нормализует радианную четверть и форматирует сектора", CircleSectorsNormalizeAndFormat),
        ("API04: CircleSectors отклоняет NaN, infinity и неверное число секторов", CircleSectorsRejectsInvalidInput),
        ("API05: PolarConverter возвращает exact sin/cos и оставляет неизвестный сектор", PolarConverterExactAndUnknownSector),
        ("API06: PolarConverter сворачивает sin и pole tan через public API", PolarConverterCollapsesTrigAndPole),
        ("API07: NumericConstants возвращает typed identities и predicates", NumericConstantsExposeTypedIdentities),
        ("API08: NumericConstants отклоняет незарегистрированный тип", NumericConstantsRejectsUnregisteredType),
        ("API09: RicisType сохраняет equality/hash contract и compatibility", RicisTypePreservesEqualityContract),
        ("API10: RicisType строит canonical operations and tuple", RicisTypeBuildsCanonicalOperations),
        ("API11: GetHashCode не меняет expression tree и HashSet semantics", RicisTypeHashDoesNotAlterTree),
    ];

    private static void ExactEvaluatorComputesRationalExpression()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var expression = Expression.Add(
            Expression.Multiply(x, Expression.Constant(2.0)),
            Expression.Constant(1.0));

        var success = expression.TryEvaluate("x", Rational.Create(3), out var result);

        Assert(success && result == Rational.Create(7), $"Ожидалось 7, получено {result}.");
    }

    private static void ExactEvaluatorRejectsUnsupportedShape()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var unsupported = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);
        var unknownParameter = Expression.Add(x, Expression.Parameter(typeof(double), "y"));

        Assert(!unsupported.TryEvaluate("x", Rational.Create(1), out _), "Math.Sin не должен считаться exact rational evaluation.");
        Assert(!unknownParameter.TryEvaluate("x", Rational.Create(1), out _), "Неизвестный parameter должен быть отклонён.");
    }

    private static void CircleSectorsNormalizeAndFormat()
    {
        var quarter = CircleSectors.FromRadians(Math.PI / 2);
        Assert(quarter.Fraction == new Rational(1, 4), $"π/2 должен дать 1/4 круга, получено {quarter.Fraction}.");
        Assert(quarter.InSectors(4).Contains("ровно", StringComparison.Ordinal), "Четверть должна попасть на точную границу сектора.");
        Assert(quarter.ToString().Contains("полного круга", StringComparison.Ordinal), "ToString должен описать долю полного круга.");

        var negativeQuarter = CircleSectors.FromRadians(-Math.PI / 2);
        Assert(negativeQuarter.Fraction == new Rational(3, 4), "Отрицательный угол должен нормализоваться в [0,1).");
    }

    private static void CircleSectorsRejectsInvalidInput()
    {
        AssertThrows<ArgumentException>(() => CircleSectors.FromRadians(double.NaN));
        AssertThrows<ArgumentException>(() => CircleSectors.FromRadians(double.PositiveInfinity));
        AssertThrows<ArgumentOutOfRangeException>(() => CircleSectors.FromRadians(0).InSectors(0));
    }

    private static void PolarConverterExactAndUnknownSector()
    {
        var exact = PolarConverter.ExactSinCos(new Rational(1, 4));
        Assert(exact.sin == 1.0 && exact.cos == 0.0, "Для 1/4 круга должны быть sin=1 и cos=0.");

        var unknown = PolarConverter.ExactSinCos(new Rational(1, 7));
        Assert(unknown.sin is null && unknown.cos is null, "Неизвестный алгебраический сектор должен остаться symbolic.");
    }

    private static void PolarConverterCollapsesTrigAndPole()
    {
        var sin = PolarConverter.TryCollapseTrig(nameof(Math.Sin), Math.PI / 2);
        Assert(sin is ConstantExpression { Value: double value } && value == 1.0, "sin(π/2) должен свернуться в 1.");

        var tan = PolarConverter.TryCollapseTrig(nameof(Math.Tan), Math.PI / 2);
        Assert(tan is InfinityExpression, "tan(π/2) должен стать индексированной infinity.");

        var parameter = Expression.Parameter(typeof(double), "x");
        var call = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, parameter);
        Assert(ReferenceEquals(call, PolarConverter.CollapseConstantTrig(call)), "Непостоянный аргумент не должен сворачиваться.");
    }

    private static void NumericConstantsExposeTypedIdentities()
    {
        var zero = NumericConstants.ZeroOf(typeof(int));
        var one = NumericConstants.OneOf(typeof(int));
        Assert(zero.Value is int zeroValue && zeroValue == 0, "ZeroOf(int) должен вернуть typed zero.");
        Assert(one.Value is int oneValue && oneValue == 1, "OneOf(int) должен вернуть typed one.");
        var found = NumericConstants.TryOneOf(typeof(int), out var registered);
        Assert(found && registered.Value is int && (int)registered.Value == 1,
            "TryOneOf(int) должен вернуть зарегистрированную единицу.");
        Assert(NumericConstants.IsIntrinsicNumeric(typeof(BigInteger)) && !NumericConstants.IsIntrinsicNumeric(typeof(string)),
            "Intrinsic numeric classification должна различать scalar и string.");
        Assert(NumericConstants.IsZero(0) && NumericConstants.IsOne(1) && !NumericConstants.IsOne(0),
            "Identity predicates должны работать для зарегистрированного int.");
    }

    private static void NumericConstantsRejectsUnregisteredType()
    {
        Assert(!NumericConstants.TryOneOf(typeof(string), out _), "TryOneOf(string) должен вернуть false.");
        AssertThrows<NotSupportedException>(() => NumericConstants.ZeroOf(typeof(string)));
    }

    private static void RicisTypePreservesEqualityContract()
    {
        var scalar = new RicisType("A", false);
        var composite = new RicisType("A", true);

        Assert(scalar.Equals(composite) && scalar.Equals((object)composite), "Equality должна зависеть от Signature.");
        Assert(scalar.GetHashCode() == composite.GetHashCode(), "Равные RicisType обязаны иметь одинаковый hash code.");
        Assert(scalar.IsCompatibleWith(RicisType.Scalar) && RicisType.Scalar.IsCompatibleWith(composite),
            "Scalar должен быть совместим с любым RicisType.");
    }

    private static void RicisTypeBuildsCanonicalOperations()
    {
        var space = new RicisType("Space");
        var time = new RicisType("Time");
        Assert(RicisType.Operate(space, space, "/").Equals(RicisType.Scalar), "A/A должен давать Scalar.");
        Assert(RicisType.Operate(RicisType.Scalar, space, "*").Equals(space), "Scalar*A должен сохранять A.");
        Assert(RicisType.Operate(space, time, "*").Signature == "(Space*Time)", "Разные типы должны образовать operation signature.");
        Assert(RicisType.CreateTuple(time, space).Signature == "Tuple<Space,Time>", "Tuple должен иметь canonical ordering.");
    }

    private static void RicisTypeHashDoesNotAlterTree()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var left = Expression.Add(x, Expression.Constant(1.0));
        var right = Expression.Add(Expression.Constant(1.0), x);
        Assert(left.AreEqual(right), "Изменение RicisType.GetHashCode не должно менять structural expression comparison.");

        var set = new HashSet<RicisType> { new("A", false) };
        Assert(set.Contains(new RicisType("A", true)), "HashSet должен находить RicisType с равным Signature.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            throw new InvalidOperationException($"Ожидалось исключение {typeof(TException).Name}.");
        }
        catch (TException)
        {
        }
    }
}
