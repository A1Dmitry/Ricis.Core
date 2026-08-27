using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents a structured system of delayed scalar expressions.
/// The system preserves each expression as a separate geometric line or boundary
/// and delegates vector-level validation and operations to <see cref="RicisVectorExpression{T}"/>.
/// </summary>
/// <typeparam name="T">The scalar result type of every expression in the system.</typeparam>
public sealed class ExpressionSystem<T>
    where T : INumber<T>
{
    private readonly RicisVectorExpression<T> _vector;

    /// <summary>
    /// Initializes a system from a non-empty set of scalar lambda expressions.
    /// </summary>
    /// <param name="expressions">The delayed expressions forming the system.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expressions"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when expressions are empty or have incompatible signatures.</exception>
    public ExpressionSystem(IEnumerable<LambdaExpression> expressions)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        _vector = new RicisVectorExpression<T>(expressions);
    }

    /// <summary>
    /// Creates a system from one or more lambda expressions.
    /// </summary>
    /// <param name="expressions">The delayed scalar expressions forming the system.</param>
    /// <returns>A structured expression system.</returns>
    public static ExpressionSystem<T> FromLambdas(params LambdaExpression[] expressions)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        return new ExpressionSystem<T>(expressions);
    }

    /// <summary>
    /// Gets the number of expressions in the system.
    /// </summary>
    public int Count => _vector.Dimension;

    /// <summary>
    /// Gets the number of input parameters shared by every expression.
    /// </summary>
    public int ParameterCount => _vector.ParameterCount;

    /// <summary>
    /// Gets the immutable expressions forming the system.
    /// </summary>
    public IReadOnlyList<LambdaExpression> Expressions => _vector.Coordinates;

    /// <summary>
    /// Gets the expression at the specified zero-based index.
    /// </summary>
    public LambdaExpression this[int index] => _vector[index];

    /// <summary>
    /// Gets the underlying RICIS vector expression without rebuilding or copying its logic.
    /// </summary>
    public RicisVectorExpression<T> Vector => _vector;

    /// <summary>
    /// Determines whether every expression in the system is a structural RICIS zero.
    /// </summary>
    public bool IsStructuralZero() => _vector.IsStructuralZero();

    /// <summary>
    /// Adds two systems componentwise through the existing RICIS vector overload.
    /// </summary>
    public static ExpressionSystem<T> Add(ExpressionSystem<T> left, ExpressionSystem<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new ExpressionSystem<T>((left._vector + right._vector).Coordinates);
    }

    /// <summary>
    /// Subtracts two systems componentwise through the existing RICIS vector overload.
    /// </summary>
    public static ExpressionSystem<T> Subtract(ExpressionSystem<T> left, ExpressionSystem<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new ExpressionSystem<T>((left._vector - right._vector).Coordinates);
    }

    /// <summary>
    /// Returns the underlying vector expression for interoperability with existing RICIS APIs.
    /// </summary>
    public RicisVectorExpression<T> ToVector() => _vector;

    /// <summary>
    /// Returns the structured system record without collapsing its expressions into a scalar.
    /// </summary>
    public override string ToString() => _vector.ToString();

    /// <summary>
    /// Adds two systems componentwise through the existing RICIS vector overload.
    /// </summary>
    public static ExpressionSystem<T> operator +(ExpressionSystem<T> left, ExpressionSystem<T> right) => Add(left, right);

    /// <summary>
    /// Subtracts two systems componentwise through the existing RICIS vector overload.
    /// </summary>
    public static ExpressionSystem<T> operator -(ExpressionSystem<T> left, ExpressionSystem<T> right) => Subtract(left, right);
}
