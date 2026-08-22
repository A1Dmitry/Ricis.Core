using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.Resources;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents a deferred complex-valued RICIS function as two independent
/// scalar expression trees: the real part Re(F) and the imaginary part Im(F).
/// The type stores symbolic components only; it never invokes either delegate
/// while constructing complex operations.
/// </summary>
public sealed class RicisComplexFunction<T>
    where T : INumber<T>
{
    /// <summary>
    /// Gets the normalized deferred real component Re(F).
    /// </summary>
    public Expression<Func<T, T>> Real { get; }

    /// <summary>
    /// Gets the normalized deferred imaginary component Im(F).
    /// </summary>
    public Expression<Func<T, T>> Imaginary { get; }

    /// <summary>
    /// Initializes a complex deferred function from real and imaginary scalar
    /// lambdas. Both components are normalized and rebound to one shared
    /// parameter without numerical evaluation.
    /// </summary>
    public RicisComplexFunction(
        Expression<Func<T, T>> real,
        Expression<Func<T, T>> imaginary)
    {
        ArgumentNullException.ThrowIfNull(real);
        ArgumentNullException.ThrowIfNull(imaginary);
        EnsureUnary(real, nameof(real));
        EnsureUnary(imaginary, nameof(imaginary));
        NumericConstants.Register<T>();

        Real = Normalize(real, "complex real component");
        var normalizedImaginary = Normalize(imaginary, "complex imaginary component");
        Imaginary = Expression.Lambda<Func<T, T>>(
            Rebind(normalizedImaginary, Real.Parameters[0], "complex imaginary component"),
            Real.Parameters[0]);
    }

    /// <summary>
    /// Builds the complex conjugate F̄ = Re(F) − i·Im(F) as a new independent
    /// pair of deferred expression trees.
    /// </summary>
    public RicisComplexFunction<T> Conjugate()
    {
        var imaginary = Expression.Lambda<Func<T, T>>(
            Expression.Negate(Imaginary.Body),
            Imaginary.Parameters[0]);
        return new RicisComplexFunction<T>(Real, imaginary);
    }

    /// <summary>
    /// Builds the pointwise complex sum F+G by combining the corresponding real
    /// and imaginary scalar expression trees.
    /// </summary>
    public RicisComplexFunction<T> Add(RicisComplexFunction<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new RicisComplexFunction<T>(
            Real.Sum(other.Real),
            Imaginary.Sum(other.Imaginary));
    }

    /// <summary>
    /// Builds the pointwise complex difference F−G by combining the
    /// corresponding scalar expression trees.
    /// </summary>
    public RicisComplexFunction<T> Subtract(RicisComplexFunction<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new RicisComplexFunction<T>(
            Real.Difference(other.Real),
            Imaginary.Difference(other.Imaginary));
    }

    /// <summary>
    /// Builds the pointwise complex product F·G using
    /// (ac−bd) + i(ad+bc), where F=a+ib and G=c+id.
    /// </summary>
    public RicisComplexFunction<T> Multiply(RicisComplexFunction<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var real = Real.Product(other.Real)
            .Difference(Imaginary.Product(other.Imaginary));
        var imaginary = Real.Product(other.Imaginary)
            .Sum(Imaginary.Product(other.Real));
        return new RicisComplexFunction<T>(real, imaginary);
    }

    /// <summary>
    /// Builds the exact scalar squared norm |F|² = Re(F)² + Im(F)². This is
    /// generic over <typeparamref name="T"/> and therefore retains exact types
    /// such as <see cref="BigInteger"/> without conversion to <see cref="double"/>.
    /// </summary>
    public Expression<Func<T, T>> SquaredNorm() =>
        Real.Product(Real).Sum(Imaginary.Product(Imaginary));

    private static Expression<Func<T, T>> Normalize(
        Expression<Func<T, T>> expression,
        string context)
    {
        var transformed = RicisPhasePipeline.Simplify(expression);
        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                RicisLegacyTextResources.Get("runtime.legacy.8c78f17a7aee") +
                typeof(T).Name + ", " + typeof(T).Name + ">> " +
                RicisLegacyTextResources.Format("runtime.legacy.2406f3633c90", ("context", context)));
    }

    private static void EnsureUnary(Expression<Func<T, T>> expression, string parameterName)
    {
        if (expression.Parameters.Count != 1)
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Get("runtime.legacy.80aaddceab23"),
                parameterName);
        }
    }

    private static Expression Rebind(
        Expression<Func<T, T>> expression,
        ParameterExpression target,
        string context) =>
        new ParameterRebindVisitor(expression.Parameters[0], target).Visit(expression.Body)
        ?? throw new InvalidOperationException(RicisLegacyTextResources.Format("runtime.legacy.9492af51b811", ("context", context)));

    private sealed class ParameterRebindVisitor : ParameterRebindingVisitorBase
    {
        public ParameterRebindVisitor(ParameterExpression from, ParameterExpression to)
            : base(from, to)
        {
        }
    }
}
