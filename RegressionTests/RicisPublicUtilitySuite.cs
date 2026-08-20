using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
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
        ("API12: RicisType constructor, properties and constants are stable", RicisTypeExposesStableMetadata),
        ("API13: RicisType equality handles null and unrelated objects", RicisTypeEqualityHandlesNullAndObjects),
        ("API14: RicisType compatibility matrix is complete", RicisTypeCompatibilityMatrix),
        ("API15: RicisType Operate covers scalar, identity and composite branches", RicisTypeOperateCoversBranches),
        ("API16: RicisType tuple and string representations are canonical", RicisTypeRepresentationsAreCanonical),
        ("API19: Expression extensions evaluate finite scalar contracts", ExpressionExtensionsEvaluateFiniteScalars),
        ("API20: Expression extensions expose ordering and parameter discovery", ExpressionExtensionsOrderAndFindParameters),
        ("API21: Expression extensions classify transcendental shape and BigInteger conversion", ExpressionExtensionsClassifyAndConvert),
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

    private static void ExpressionExtensionsEvaluateFiniteScalars()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var squarePlusOne = Expression.Add(Expression.Multiply(x, x), Expression.Constant(1.0));

        Assert(squarePlusOne.Evaluate(x, 3.0) == 10.0, "Evaluate(expr, parameter, value) должен вычислять finite expression.");
        Assert(squarePlusOne.Evaluate("x", 4.0) == 17.0, "Evaluate(expr, parameterName, value) должен применять named substitution.");
    }

    private static void ExpressionExtensionsOrderAndFindParameters()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var complex = Expression.Add(x, Expression.Constant(1.0));

        Assert(complex.ShouldCommute(x), "Более сложное левое поддерево должно иметь больший ordering score.");
        Assert(!x.ShouldCommute(complex), "Простое левое поддерево не должно требовать commute.");
        Assert(ReferenceEquals(complex.FindParameter(), x), "FindParameter должен вернуть первый structural parameter.");
    }

    private static void ExpressionExtensionsClassifyAndConvert()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var sine = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);

        Assert(sine.IsTranscendentalCandidate(), "Math.Sin должен классифицироваться как трансцендентный candidate.");
        Assert(!Expression.Add(x, Expression.Constant(1.0)).IsTranscendentalCandidate(), "Обычная алгебраическая сумма не должна классифицироваться как трансцендентная.");
        Assert(((object)123L).ToBigInteger() == new BigInteger(123) && ((object)"unsupported").ToBigInteger() == BigInteger.Zero,
            "ToBigInteger должен сохранять поддерживаемый long и безопасно нормализовать unsupported value к нулю.");
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

    private static void RicisTypeExposesStableMetadata()
    {
        var composite = new RicisType("A", true);
        Assert(composite.Signature == "A" && composite.IsComposite, "Constructor должен сохранить Signature и IsComposite.");
        Assert(RicisType.Scalar.Signature == "Scalar" && !RicisType.Scalar.IsComposite, "Scalar должен быть каноническим простым типом.");
        Assert(RicisType.InfinityZero.NodeType == ExpressionType.Constant && RicisType.InfinityZero.Value is double zero && zero == 0.0,
            "InfinityZero должен быть double constant 0.");
        Assert(RicisType.InfinityOne.NodeType == ExpressionType.Constant && RicisType.InfinityOne.Value is double one && one == 1.0,
            "InfinityOne должен быть double constant 1.");
    }

    private static void RicisTypeEqualityHandlesNullAndObjects()
    {
        var value = new RicisType("A");
        RicisType? other = null;
        Assert(!value.Equals(other), "Equals(RicisType?) должен вернуть false для null.");
        Assert(!value.Equals((object?)null), "Equals(object) должен вернуть false для null.");
        Assert(!object.Equals(value, "A"), "RicisType не должен быть равен unrelated object.");
        Assert(object.Equals(value, new RicisType("A", true)), "Equals(object) должен сравнивать Signature.");
    }

    private static void RicisTypeCompatibilityMatrix()
    {
        var space = new RicisType("Space");
        var time = new RicisType("Time");
        Assert(space.IsCompatibleWith(space), "Тип должен быть совместим сам с собой.");
        Assert(!space.IsCompatibleWith(time), "Разные non-scalar типы несовместимы.");
        Assert(space.IsCompatibleWith(RicisType.Scalar), "Non-scalar должен быть совместим со Scalar.");
        Assert(RicisType.Scalar.IsCompatibleWith(time), "Scalar должен быть совместим с non-scalar.");
    }

    private static void RicisTypeOperateCoversBranches()
    {
        var a = new RicisType("A");
        var b = new RicisType("B");
        Assert(RicisType.Operate(RicisType.Scalar, a, "*").Equals(a), "Scalar слева должен вернуть правый тип.");
        Assert(RicisType.Operate(a, RicisType.Scalar, "*").Equals(a), "Scalar справа должен вернуть левый тип.");
        Assert(RicisType.Operate(a, a, "/").Equals(RicisType.Scalar), "A/A должен вернуть Scalar.");
        Assert(RicisType.Operate(a, a, "*").Signature == "(A*A)", "Операция * над одинаковыми типами должна сохранить composite signature.");
        Assert(RicisType.Operate(a, b, "/").Signature == "(A/B)", "Операция / над разными типами должна сохранить composite signature.");
    }

    private static void RicisTypeRepresentationsAreCanonical()
    {
        var tuple = RicisType.CreateTuple(new RicisType("Time"), new RicisType("Space"));
        Assert(tuple.IsComposite && tuple.Signature == "Tuple<Space,Time>", "Tuple должен сортировать Signature канонически.");
        Assert(tuple.ToString() == tuple.Signature, "ToString должен возвращать Signature.");
        Assert(new RicisType("A").ToString() == "A", "Простой тип должен печатать Signature.");
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
