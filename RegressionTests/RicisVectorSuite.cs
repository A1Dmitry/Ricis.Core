using System.Numerics;
using Ricis.Core.Expressions;

internal static class RicisVectorSuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("VECTOR01: вектор хранит N упорядоченных координат", StoresOrderedCoordinates),
        ("VECTOR02: координаты защищены от изменения входного массива", CoordinatesAreImmutable),
        ("VECTOR03: сложение и вычитание выполняются покомпонентно", ComponentwiseAddSubtract),
        ("VECTOR04: масштабирование и dot product используют INumber<T>", ScaleAndDotUseGenericNumber),
        ("VECTOR05: Zero создаёт типизированный нулевой вектор", TypedZeroVector),
        ("VECTOR06: разные размерности отклоняются явно", MismatchedDimensionsAreRejected),
        ("VECTOR07: пустой вектор отклоняется", EmptyVectorIsRejected),
        ("VECTOR08: generic BigInteger поддерживает векторные операции", BigIntegerVectorOperations),
    ];

    private static void StoresOrderedCoordinates()
    {
        var vector = new RicisVector<int>([3, 1, 4, 1, 5]);
        RegressionAssertions.Require(vector.Dimension == 5, "Размерность должна быть равна числу координат.");
        RegressionAssertions.Require(vector[0] == 3 && vector[4] == 5, "Порядок координат должен сохраняться.");
        RegressionAssertions.Require(vector.ToString() == "(3, 1, 4, 1, 5)", "ToString должен показывать запись координат.");
    }

    private static void CoordinatesAreImmutable()
    {
        var source = new[] { 1, 2, 3 };
        var vector = new RicisVector<int>(source);
        source[0] = 99;
        RegressionAssertions.Require(vector[0] == 1, "RICIS-вектор обязан копировать входные координаты.");
        RegressionAssertions.Require(vector.Coordinates.Count == 3, "Публичная коллекция должна сохранять размерность.");
    }

    private static void ComponentwiseAddSubtract()
    {
        var left = new RicisVector<int>([1, 2, 3]);
        var right = new RicisVector<int>([4, 5, 6]);
        var sum = left + right;
        var difference = left - right;
        RegressionAssertions.Require(sum.Equals(new RicisVector<int>([5, 7, 9])), "Сложение должно быть покомпонентным.");
        RegressionAssertions.Require(difference.Equals(new RicisVector<int>([-3, -3, -3])), "Вычитание должно быть покомпонентным.");
    }

    private static void ScaleAndDotUseGenericNumber()
    {
        var vector = new RicisVector<int>([2, -1, 3]);
        var scaled = 4 * vector;
        var dot = RicisVector<int>.Dot(vector, new RicisVector<int>([5, 2, 1]));
        RegressionAssertions.Require(scaled.Equals(new RicisVector<int>([8, -4, 12])), "Масштабирование должно работать через INumber<T>.");
        RegressionAssertions.Require(dot == 11, "Dot product должен суммировать произведения координат.");
    }

    private static void TypedZeroVector()
    {
        var zero = RicisVector<BigInteger>.Zero(4);
        RegressionAssertions.Require(zero.Dimension == 4, "Нулевой вектор должен иметь запрошенную размерность.");
        RegressionAssertions.Require(zero.All(value => value == BigInteger.Zero), "Каждая координата должна быть BigInteger.Zero.");
    }

    private static void MismatchedDimensionsAreRejected()
    {
        var left = new RicisVector<double>([1, 2]);
        var right = new RicisVector<double>([1, 2, 3]);
        Expect<ArgumentException>(() => _ = left + right, "Разные размерности должны отклоняться.");
        Expect<ArgumentException>(() => RicisVector<double>.Dot(left, right), "Dot product должен проверять размерности.");
    }

    private static void EmptyVectorIsRejected() =>
        Expect<ArgumentException>(() => _ = new RicisVector<int>(Array.Empty<int>()), "Пустой вектор недопустим.");

    private static void BigIntegerVectorOperations()
    {
        var value = BigInteger.Parse("999999999999999999999999999999");
        var vector = new RicisVector<BigInteger>([value, value]);
        var result = vector + vector;
        RegressionAssertions.Require(result[0] == value * 2 && result[1] == value * 2,
            "Большие generic-координаты не должны терять точность.");
    }

    private static void Expect<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

}
