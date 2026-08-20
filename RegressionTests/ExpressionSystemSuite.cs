using System.Linq.Expressions;
using Ricis.Core.Expressions;

internal static class ExpressionSystemSuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("ES01: ExpressionSystem сохраняет все lambda-выражения", StoresLambdaExpressions),
        ("ES02: ExpressionSystem сохраняет общую сигнатуру", PreservesSharedSignature),
        ("ES03: ExpressionSystem использует существующий vector overload", ReusesVectorOperations),
        ("ES04: ExpressionSystem отклоняет несовместимые сигнатуры", RejectsIncompatibleSignatures),
        ("ES05: ExpressionSystem не схлопывается в скаляр", KeepsStructuredRepresentation),
        ("API17: ExpressionSystem exposes structural-zero and vector interoperability APIs", ExposesStructuralZeroAndVector),
    ];

    private static void StoresLambdaExpressions()
    {
        var system = CreateSystem();
        Require(system.Count == 3, "Система должна сохранить три lambda-выражения.");
        Require(system.Expressions.Count == 3, "Коллекция Expressions должна содержать три элемента.");
        Require(system[0].ReturnType == typeof(double), "Каждая lambda должна возвращать double.");
        Require(system[1].ToString()!.Contains("y"), "Вторая lambda должна сохраниться структурно.");
    }

    private static void PreservesSharedSignature()
    {
        var system = CreateSystem();
        Require(system.ParameterCount == 2, "Система должна сохранить два общих параметра.");
        Require(system[0].Parameters.Count == 2, "Каждая lambda должна иметь два параметра.");
        Require(system[0].Parameters[0].Type == typeof(double), "Тип первого параметра должен быть double.");
        Require(system[0].Parameters[1].Type == typeof(double), "Тип второго параметра должен быть double.");
    }

    private static void ReusesVectorOperations()
    {
        var left = CreateSystem();
        var right = CreateSystem();
        var sum = left + right;
        var difference = ExpressionSystem<double>.Subtract(left, right);

        Require(sum.Count == left.Count, "Сложение систем должно идти через существующий vector overload.");
        Require(difference.Count == left.Count, "Вычитание систем должно сохранять размерность.");
        Require(sum.Vector.Dimension == 3, "Внутренний vector должен сохранить размерность системы.");
    }

    private static void RejectsIncompatibleSignatures()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var first = Expression.Lambda<Func<double, double>>(x, x);
        var second = Expression.Lambda<Func<double, double, double>>(y, x, y);
        var third = Expression.Lambda<Func<double, int>>(Expression.Constant(1), x);

        RegressionAssertions.Expect<ArgumentException>(
            () => _ = ExpressionSystem<double>.FromLambdas(first, second),
            "Разное число параметров должно отклоняться.");
        RegressionAssertions.Expect<ArgumentException>(
            () => _ = ExpressionSystem<double>.FromLambdas(first, third),
            "Тип результата, отличный от double, должен отклоняться.");
    }

    private static void KeepsStructuredRepresentation()
    {
        var system = CreateSystem();
        var text = system.ToString();
        Require(text.StartsWith("("), "Система должна выводиться как структурная запись.");
        Require(text.Contains(","), "Структурная запись должна сохранять разделение координат.");
        Require(!text.Equals(system[0].ToString(), StringComparison.Ordinal),
            "Система не должна схлопываться в одну lambda.");
    }

    private static void ExposesStructuralZeroAndVector()
    {
        var system = CreateSystem();
        RegressionAssertions.Require(
            ReferenceEquals(system.ToVector(), system.Vector),
            "ToVector должен возвращать тот же interoperability vector, что и свойство Vector.");
        RegressionAssertions.Require(!system.IsStructuralZero(), "Система с ненулевыми coordinates не может быть structural zero.");

        var x = Expression.Parameter(typeof(double), "x");
        var zeroSystem = ExpressionSystem<double>.FromLambdas(
            Expression.Lambda<Func<double, double>>(Expression.Constant(0.0), x));
        RegressionAssertions.Require(zeroSystem.IsStructuralZero(), "Система из нулевой lambda должна быть structural zero.");
    }

    private static ExpressionSystem<double> CreateSystem()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        return ExpressionSystem<double>.FromLambdas(
            Expression.Lambda<Func<double, double, double>>(Expression.Add(x, y), x, y),
            Expression.Lambda<Func<double, double, double>>(Expression.Subtract(x, y), x, y),
            Expression.Lambda<Func<double, double, double>>(Expression.Multiply(x, y), x, y));
    }


    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
