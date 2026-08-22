using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Phases;
using Ricis.Core.Resources;

namespace Ricis.Core.Expressions;

/// <summary>
/// Visits every coordinate of a deferred RICIS vector expression and applies the
/// complete internal phase pipeline without compiling or numerically evaluating it.
/// </summary>
/// <typeparam name="T">The scalar coordinate type.</typeparam>
public sealed class RicisVectorExpressionVisitor<T>
    where T : INumber<T>
{
    /// <summary>
    /// Normalizes every coordinate lambda through the normative RICIS pipeline.
    /// </summary>
    /// <param name="vector">The deferred vector expression.</param>
    /// <returns>A new normalized vector expression.</returns>
    public RicisVectorExpression<T> Visit(RicisVectorExpression<T> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        var coordinates = vector.Coordinates
            .Select(coordinate => VisitCoordinate(coordinate))
            .ToArray();
        return new RicisVectorExpression<T>(coordinates);
    }

    /// <summary>
    /// Builds the coordinate identity mapping for a square vector signature.
    /// </summary>
    /// <param name="vector">A vector whose parameter count equals its dimension.</param>
    /// <returns>The delayed identity vector.</returns>
    /// <exception cref="ArgumentException">Thrown when the vector is not square.</exception>
    public RicisVectorExpression<T> Identity(RicisVectorExpression<T> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.ParameterCount != vector.Dimension)
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.8f4fadc3ac1b"), nameof(vector));

        var parameters = CreateParameters(vector[0].Parameters);
        return new RicisVectorExpression<T>(parameters.Select(parameter =>
            Expression.Lambda(parameter, parameters)));
    }

    /// <summary>
    /// Builds and normalizes the vector residual <c>left−right</c>.
    /// </summary>
    public RicisVectorExpression<T> Residual(
        RicisVectorExpression<T> left,
        RicisVectorExpression<T> right)
    {
        ValidatePair(left, right);
        return Visit(RicisVectorExpression<T>.Subtract(left, right));
    }

    /// <summary>
    /// Proves a vector identity exactly when every normalized residual coordinate
    /// is a structural RICIS zero.
    /// </summary>
    public RicisVectorProofResult<T> ProveIdentity(
        RicisVectorExpression<T> left,
        RicisVectorExpression<T> right)
    {
        var residual = Residual(left, right);
        return new RicisVectorProofResult<T>(residual, residual.IsStructuralZero());
    }

    private static LambdaExpression VisitCoordinate(LambdaExpression coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        Expression current = coordinate;
        var algebra = new RicisMultivariateAlgebraicVisitor<T>();
        for (var pass = 0; pass < 8; pass++)
        {
            var phased = RicisPhasePipeline.Simplify(current);
            var reduced = algebra.Visit(phased);
            if (reduced.AreEqual(phased))
            {
                return reduced as LambdaExpression
                    ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.b0fb0238ea83"));
            }

            current = reduced;
        }

        return RicisPhasePipeline.Simplify(current) as LambdaExpression
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.f494b5f0fa4e"));
    }

    private static ParameterExpression[] CreateParameters(IReadOnlyList<ParameterExpression> source) =>
        source.Select(parameter => Expression.Parameter(parameter.Type, parameter.Name ?? "x")).ToArray();

    private static void ValidatePair(RicisVectorExpression<T> left, RicisVectorExpression<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Dimension != right.Dimension || left.ParameterCount != right.ParameterCount)
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.a1fc52a0ce73"));
        for (var i = 0; i < left.ParameterCount; i++)
        {
            if (left[0].Parameters[i].Type != right[0].Parameters[i].Type)
                throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.c48b0b41e034"));
        }
    }
}

/// <summary>
/// Contains the normalized residual and the exact structural result of a vector identity proof.
/// </summary>
/// <typeparam name="T">The scalar coordinate type.</typeparam>
public sealed class RicisVectorProofResult<T>
    where T : INumber<T>
{
    /// <summary>
    /// Initializes an identity proof result.
    /// </summary>
    public RicisVectorProofResult(RicisVectorExpression<T> residual, bool isProved)
    {
        Residual = residual ?? throw new ArgumentNullException(nameof(residual));
        IsProved = isProved;
    }

    /// <summary>
    /// Gets the normalized vector residual.
    /// </summary>
    public RicisVectorExpression<T> Residual { get; }

    /// <summary>
    /// Gets whether every residual coordinate is a structural RICIS zero.
    /// </summary>
    public bool IsProved { get; }

    /// <inheritdoc />
    public override string ToString() => $"Proved={IsProved}; Residual={Residual}";
}
