using System.Linq.Expressions;
using Ricis.Core.Expressions;

internal static class RicisVectorExpressionSuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("VEX01: символьный вектор сохраняет N lambda-координат", StoresLambdaCoordinates),
        ("VEX02: символьное сложение выполняется покомпонентно", ComponentwiseSymbolicAdd),
        ("VEX03: Zero expression vector сохраняет структурный ноль", SymbolicZeroIsStructural),
        ("VEX04: треугольные F и G строятся как векторные отображения", BuildsJacobianFamilyMaps),
        ("VEX05: композиция векторных отображений сохраняет размерность", CompositionPreservesDimension),
        ("VEX06: несовместимые сигнатуры отклоняются", IncompatibleSignaturesAreRejected),
    ];

    private static void StoresLambdaCoordinates()
    {
        var vector = CreateF();
        Require(vector.Dimension == 2, "F должен иметь две координаты.");
        Require(vector.ParameterCount == 2, "Каждая координата должна иметь два параметра.");
        Require(vector[0].ReturnType == typeof(double), "Координата должна возвращать double.");
    }

    private static void ComponentwiseSymbolicAdd()
    {
        var f = CreateF();
        var zero = RicisVectorExpression<double>.Zero(f[0].Parameters, 2);
        var sum = f + zero;
        Require(sum.Dimension == f.Dimension, "Сложение должно сохранять размерность.");
        Require(sum[0].Parameters.Count == 2, "Сложение должно сохранять число параметров.");
    }

    private static void SymbolicZeroIsStructural()
    {
        var parameters = new[]
        {
            Expression.Parameter(typeof(double), "x"),
            Expression.Parameter(typeof(double), "y")
        };
        var zero = RicisVectorExpression<double>.Zero(parameters, 2);
        Require(zero.IsStructuralZero(), "Каждая координата Zero должна быть RICIS-нулём.");
    }

    private static void BuildsJacobianFamilyMaps()
    {
        var f = CreateF();
        var g = CreateG();
        Require(f.Dimension == 2 && g.Dimension == 2, "F и G должны быть двумерными отображениями.");
        Require(f.ToString().Contains("y"), "Векторная запись F должна содержать вторую координату.");
        Require(g.ToString().Contains("x"), "Векторная запись G должна содержать первую координату.");
    }

    private static void CompositionPreservesDimension()
    {
        var composition = RicisVectorExpression<double>.Compose(CreateG(), CreateF());
        Require(composition.Dimension == 2, "Композиция G∘F должна иметь две координаты.");
        Require(composition.ParameterCount == 2, "Композиция должна сохранять два входных параметра.");
    }

    private static void IncompatibleSignaturesAreRejected()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var z = Expression.Parameter(typeof(double), "z");
        var two = new RicisVectorExpression<double>([
            Expression.Lambda<Func<double, double, double>>(x, x, y),
            Expression.Lambda<Func<double, double, double>>(y, x, y)]);
        var three = new RicisVectorExpression<double>([
            Expression.Lambda<Func<double, double, double, double>>(x, x, y, z),
            Expression.Lambda<Func<double, double, double, double>>(y, x, y, z)]);
        Expect<ArgumentException>(() => _ = two + three, "Разное число параметров должно отклоняться.");
    }

    private static RicisVectorExpression<double> CreateF()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var p = Expression.Add(
            Expression.Add(Expression.Multiply(Expression.Multiply(y, y), y), Expression.Multiply(Expression.Constant(2.0), y)),
            Expression.Constant(1.0));
        return new RicisVectorExpression<double>([
            Expression.Lambda<Func<double, double, double>>(Expression.Add(x, p), x, y),
            Expression.Lambda<Func<double, double, double>>(y, x, y)
        ]);
    }

    private static RicisVectorExpression<double> CreateG()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var p = Expression.Add(
            Expression.Add(Expression.Multiply(Expression.Multiply(y, y), y), Expression.Multiply(Expression.Constant(2.0), y)),
            Expression.Constant(1.0));
        return new RicisVectorExpression<double>([
            Expression.Lambda<Func<double, double, double>>(Expression.Subtract(x, p), x, y),
            Expression.Lambda<Func<double, double, double>>(y, x, y)
        ]);
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
