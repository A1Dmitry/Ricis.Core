using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;

/// <summary>
/// Regression scenarios for RICIS vector calculus and the exact Navier-Stokes proof method.
/// </summary>
internal static class RicisNavierStokesProofSuite
{
    /// <summary>Returns Navier-Stokes proof regression cases.</summary>
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("NS01: частная производная строится точным expression tree", PartialDerivativeIsExact),
        ("NS02: градиент, дивергенция и лапласиан покрывают типовые операторы поля", VectorOperatorsAreExact),
        ("NS03: стационарный вихрь имеет нулевой остаток Навье—Стокса", StationaryVortexProof),
        ("NS04: proof отвергает сжимаемое поле", CompressibleFieldIsRejected),
        ("NS05: proof отвергает невалидную вязкость", InvalidViscosityIsRejected),
        ("NS06: стационарная производная канонизирует -0 в 0", StationaryDerivativeCanonicalizesZero),
        ("NS07: производная F·0 сохраняет индексированный ноль 0_F", DerivativePreservesIndexedZero),
        ("NS08: конечное F/∞_G даёт индексированный ноль 0_F", FiniteOverInfinityProducesIndexedZero),
    ];

    private static void PartialDerivativeIsExact()
    {
        Expression<Func<double, double, double, double, double>> field =
            (x, y, z, t) => (x * y) - t;
        var dx = field.PartialDerivative(RicisFieldCoordinate.X);
        var dy = field.PartialDerivative(RicisFieldCoordinate.Y);
        var dt = field.PartialDerivative(RicisFieldCoordinate.T);

        Require(dx.Compile()(2.0, 3.0, 5.0, 7.0) == 3.0 &&
                dy.Compile()(2.0, 3.0, 5.0, 7.0) == 2.0 &&
                dt.Compile()(2.0, 3.0, 5.0, 7.0) == -1.0,
            $"Частные производные должны быть (y,x,-1), получено ({dx}, {dy}, {dt}).");
    }

    private static void VectorOperatorsAreExact()
    {
        var (velocity, pressure) = StationaryVortex();
        var divergence = velocity.Divergence();
        var gradient = pressure.Gradient();
        var laplacian = velocity.Laplacian();

        Require(divergence.Body.IsZero(), $"Вихрь обязан быть несжимаем: получено {divergence}.");
        Require(gradient.U.Compile()(2.0, 3.0, 0.0, 0.0) == 2.0 &&
                gradient.V.Compile()(2.0, 3.0, 0.0, 0.0) == 3.0 &&
                gradient.W.Body.IsZero(),
            $"Градиент p должен быть (x,y,0), получено ({gradient.U}, {gradient.V}, {gradient.W}).");
        Require(laplacian.IsStructuralZero(), $"Лапласиан линейного вихря должен быть нулевым, получено {laplacian.U}, {laplacian.V}, {laplacian.W}.");
    }

    private static void StationaryVortexProof()
    {
        var (velocity, pressure) = StationaryVortex();
        var document = new StringBuilder();

        var result = velocity.ProveNavierStokesIdentity(pressure, 1.0, document);
        var text = document.ToString();

        Require(result.IsCertified, "Стационарный вихрь обязан получить сертификат нулевого остатка.");
        Require(result.Convection.U.Compile()(2.0, 3.0, 0.0, 0.0) == -2.0 &&
                result.Convection.V.Compile()(2.0, 3.0, 0.0, 0.0) == -3.0,
            $"Конвективное поле должно быть (-x,-y,0), получено ({result.Convection.U}, {result.Convection.V}).");
        Require(text.Contains("NS-01", StringComparison.Ordinal) &&
                text.Contains("NS-07", StringComparison.Ordinal) &&
                text.Contains("нулевой нормализованный остаток", StringComparison.Ordinal),
            "Документ обязан содержать полную нормативную цепочку NS-01–NS-07.");
    }

    private static void CompressibleFieldIsRejected()
    {
        var velocity = new RicisVectorField3(
            (x, y, z, t) => x,
            (x, y, z, t) => 0.0,
            (x, y, z, t) => 0.0);
        Expression<Func<double, double, double, double, double>> pressure =
            (x, y, z, t) => 0.0;

        RequireInvalidOperation(
            () => _ = velocity.ProveNavierStokesIdentity(pressure, 1.0, new StringBuilder()),
            "Сжимаемое поле ∇·u=1 не может получить proof-сертификат.");
    }

    private static void StationaryDerivativeCanonicalizesZero()
    {
        var (velocity, _) = StationaryVortex();
        var timeDerivative = velocity.TimeDerivative();

        Require(timeDerivative.U.Body is ConstantExpression { Value: double value } &&
                BitConverter.DoubleToInt64Bits(value) == 0L,
            $"Стационарная производная -y по t должна быть каноническим +0, получено {timeDerivative.U.Body}.");
    }

    private static void DerivativePreservesIndexedZero()
    {
        Expression<Func<double, double, double, double, double>> field =
            (x, y, z, t) => (x * x) * 0.0;

        var derivative = field.PartialDerivative(RicisFieldCoordinate.X);

        Require(derivative.Body is ZeroInfinityExpression indexedZero &&
                indexedZero.Numerator.ToString().Contains("x", StringComparison.Ordinal) &&
                derivative.Compile()(3.0, 0.0, 0.0, 0.0) == 0.0,
            $"Производная F·0 обязана сохранять 0_F и оставаться исполнимой как ноль, получено {derivative.Body}.");
    }

    private static void FiniteOverInfinityProducesIndexedZero()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var denominator = new PoleInfinityExpression(
            Expression.Add(x, Expression.Constant(1.0)),
            [(x, 4.0)],
            []);
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(7.0), denominator),
            x);

        var reduced = RicisPhasePipeline.Simplify(source) as Expression<Func<double, double>>
            ?? throw new InvalidOperationException("RICIS-конвейер обязан сохранить тип F/∞_G.");

        Require(reduced.Body is ZeroInfinityExpression indexedZero &&
                indexedZero.Numerator is ConstantExpression { Value: double numerator } && numerator == 7.0 &&
                indexedZero.Roots is [{ Param: var key, Value: 4.0 }] && key == x &&
                reduced.Compile()(11.0) == 0.0,
            $"F/∞_G должно дать 0_F с исходным индексом и ключом G, получено {reduced.Body}.");
    }

    private static void InvalidViscosityIsRejected()
    {
        var (velocity, pressure) = StationaryVortex();
        try
        {
            _ = velocity.ProveNavierStokesIdentity(pressure, double.NaN, new StringBuilder());
            throw new InvalidOperationException("Ожидалось отклонение NaN-вязкости.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    private static (RicisVectorField3 Velocity, Expression<Func<double, double, double, double, double>> Pressure) StationaryVortex()
    {
        var velocity = new RicisVectorField3(
            (x, y, z, t) => -y,
            (x, y, z, t) => x,
            (x, y, z, t) => 0.0);
        Expression<Func<double, double, double, double, double>> pressure =
            (x, y, z, t) => ((x * x) + (y * y)) / 2.0;
        return (velocity, pressure);
    }

    private static void RequireInvalidOperation(Action action, string message)
    {
        try
        {
            action();
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
