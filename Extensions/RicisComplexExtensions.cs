using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.Extensions;

/// <summary>
/// Expression-tree builders for <see cref="RicisComplexFunction{T}"/>.
/// The extensions keep complex functions as symbolic real and imaginary pairs
/// instead of compiling delegates or converting values to <see cref="Complex"/>.
/// </summary>
public static class RicisComplexExtensions
{
    /// <summary>
    /// Lifts a deferred scalar function F into the complex plane as F+i·0.
    /// </summary>
    public static RicisComplexFunction<T> AsComplex<T>(this Expression<Func<T, T>> real)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(real);
        if (real.Parameters.Count != 1)
        {
            throw new ArgumentException("AsComplex требует лямбду ровно с одним параметром.", nameof(real));
        }

        NumericConstants.Register<T>();
        var imaginary = Expression.Lambda<Func<T, T>>(
            NumericConstants.ZeroOf(typeof(T)),
            real.Parameters[0]);
        return new RicisComplexFunction<T>(real, imaginary);
    }

    /// <summary>
    /// Builds a deferred complex function Re+i·Im from two scalar lambdas.
    /// The constructor of <see cref="RicisComplexFunction{T}"/> normalizes and
    /// rebinds both components to a shared parameter.
    /// </summary>
    public static RicisComplexFunction<T> AsComplex<T>(
        this Expression<Func<T, T>> real,
        Expression<Func<T, T>> imaginary)
        where T : INumber<T> => new(real, imaginary);

    /// <summary>
    /// Returns the deferred real expression Re(F) of a complex RICIS function.
    /// </summary>
    public static Expression<Func<T, T>> Re<T>(this RicisComplexFunction<T> function)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        return function.Real;
    }

    /// <summary>
    /// Returns the deferred imaginary expression Im(F) of a complex RICIS function.
    /// </summary>
    public static Expression<Func<T, T>> Im<T>(this RicisComplexFunction<T> function)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        return function.Imaginary;
    }

    /// <summary>
    /// Builds the Euclidean norm |F| for a double-domain complex deferred
    /// function as sqrt(Re(F)²+Im(F)²). The square root remains an explicit
    /// <see cref="Math.Sqrt(double)"/> expression-tree call.
    /// </summary>
    public static Expression<Func<double, double>> Norm(this RicisComplexFunction<double> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        var squaredNorm = function.SquaredNorm();
        var body = Expression.Call(
            typeof(Math),
            nameof(Math.Sqrt),
            Type.EmptyTypes,
            squaredNorm.Body);
        var raw = Expression.Lambda<Func<double, double>>(body, squaredNorm.Parameters[0]);
        return RicisPhasePipeline.Simplify(raw) as Expression<Func<double, double>>
            ?? throw new InvalidOperationException(
                "RICIS-конвейер должен сохранить Expression<Func<double,double>> для комплексной Norm.");
    }
}
