using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Phases;
using Ricis.Core.Extensions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents an immutable vector whose coordinates are delayed scalar RICIS lambda expressions.
/// The vector is an aggregate of scalar expressions; it does not define a competing arithmetic system.
/// </summary>
/// <typeparam name="T">The scalar result type of every coordinate expression.</typeparam>
public sealed class RicisVectorExpression<T>
    where T : INumber<T>
{
    private readonly ReadOnlyCollection<LambdaExpression> _coordinates;

    /// <summary>
    /// Initializes a vector from non-empty lambdas with a common parameter signature and result type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="coordinates">The delayed scalar coordinate expressions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="coordinates"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when coordinates are empty, have different signatures, or do not return <typeparamref name="T"/>.</exception>
    public RicisVectorExpression(IEnumerable<LambdaExpression> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        var copied = coordinates.ToArray();
        if (copied.Length == 0) throw new ArgumentException("Символьный RICIS-вектор обязан иметь координаты.", nameof(coordinates));
        ValidateSignature(copied);
        _coordinates = Array.AsReadOnly(copied);
    }

    /// <summary>
    /// Gets the number of vector coordinates.
    /// </summary>
    public int Dimension => _coordinates.Count;

    /// <summary>
    /// Gets the number of input parameters in every coordinate lambda.
    /// </summary>
    public int ParameterCount => _coordinates[0].Parameters.Count;

    /// <summary>
    /// Gets the immutable delayed coordinate expressions.
    /// </summary>
    public IReadOnlyList<LambdaExpression> Coordinates => _coordinates;

    /// <summary>
    /// Gets the coordinate lambda at the specified zero-based index.
    /// </summary>
    public LambdaExpression this[int index] => _coordinates[index];

    /// <summary>
    /// Adds two symbolic vectors componentwise and normalizes every resulting lambda through RICIS.
    /// </summary>
    public static RicisVectorExpression<T> Add(RicisVectorExpression<T> left, RicisVectorExpression<T> right)
    {
        ValidatePair(left, right);
        return Combine(left, right, Expression.Add);
    }

    /// <summary>
    /// Subtracts two symbolic vectors componentwise and normalizes every resulting lambda through RICIS.
    /// </summary>
    public static RicisVectorExpression<T> Subtract(RicisVectorExpression<T> left, RicisVectorExpression<T> right)
    {
        ValidatePair(left, right);
        return Combine(left, right, Expression.Subtract);
    }

    /// <summary>
    /// Composes a same-dimensional symbolic vector mapping with another vector mapping.
    /// </summary>
    public static RicisVectorExpression<T> Compose(RicisVectorExpression<T> outer, RicisVectorExpression<T> inner)
    {
        ValidatePair(outer, inner);
        if (outer.ParameterCount != outer.Dimension || inner.ParameterCount != inner.Dimension)
        {
            throw new ArgumentException("Композиция векторных отображений требует N координат и N входных параметров.");
        }

        var parameters = CreateParameters(outer[0].Parameters);
        var innerBodies = inner.Coordinates
            .Select(coordinate => RebindTo(coordinate, parameters))
            .Select(coordinate => coordinate.Body)
            .ToArray();
        var composed = new List<LambdaExpression>(outer.Dimension);
        foreach (var coordinate in outer.Coordinates)
        {
            var outerBody = RebindTo(coordinate, parameters).Body;
            var body = new CoordinateSubstitutionVisitor(parameters, innerBodies).Visit(outerBody)
                ?? throw new InvalidOperationException("Не удалось построить композицию векторных RICIS-координат.");
            composed.Add(Normalize(body, parameters));
        }

        return new RicisVectorExpression<T>(composed);
    }

    /// <summary>
    /// Returns the typed symbolic zero vector for a given parameter signature and dimension.
    /// </summary>
    public static RicisVectorExpression<T> Zero(IReadOnlyList<ParameterExpression> parameters, int dimension)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Count == 0) throw new ArgumentException("Нужен хотя бы один параметр.", nameof(parameters));
        if (dimension <= 0) throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Размерность должна быть положительной.");
        return new RicisVectorExpression<T>(Enumerable.Range(0, dimension)
            .Select(_ => Normalize(Expression.Constant(T.Zero), parameters)));
    }

    /// <summary>
    /// Determines whether every coordinate is a structural RICIS zero.
    /// </summary>
    public bool IsStructuralZero() => _coordinates.All(coordinate => coordinate.Body.IsZero());

    /// <summary>
    /// Returns the componentwise symbolic vector record.
    /// </summary>
    public override string ToString() => $"({string.Join(", ", _coordinates)})";

    /// <summary>
    /// Adds two symbolic vectors using componentwise expression-tree addition.
    /// </summary>
    public static RicisVectorExpression<T> operator +(RicisVectorExpression<T> left, RicisVectorExpression<T> right) => Add(left, right);

    /// <summary>
    /// Subtracts two symbolic vectors using componentwise expression-tree subtraction.
    /// </summary>
    public static RicisVectorExpression<T> operator -(RicisVectorExpression<T> left, RicisVectorExpression<T> right) => Subtract(left, right);

    private static RicisVectorExpression<T> Combine(
        RicisVectorExpression<T> left,
        RicisVectorExpression<T> right,
        Func<Expression, Expression, BinaryExpression> operation)
    {
        var parameters = CreateParameters(left[0].Parameters);
        var result = new List<LambdaExpression>(left.Dimension);
        for (var i = 0; i < left.Dimension; i++)
        {
            var leftBody = RebindTo(left[i], parameters).Body;
            var rightBody = RebindTo(right[i], parameters).Body;
            result.Add(Normalize(operation(leftBody, rightBody), parameters));
        }

        return new RicisVectorExpression<T>(result);
    }

    private static LambdaExpression Normalize(Expression body, IReadOnlyList<ParameterExpression> parameters)
    {
        var lambda = Expression.Lambda(body, parameters);
        return RicisPhasePipeline.Simplify(lambda) as LambdaExpression
            ?? throw new InvalidOperationException("RICIS не сохранил многопеременную lambda-форму.");
    }

    private static LambdaExpression RebindTo(LambdaExpression source, IReadOnlyList<ParameterExpression> target)
    {
        if (source.Parameters.Count != target.Count) throw new ArgumentException("Сигнатуры lambda выражений не совпадают.");
        var body = new ParameterRebindVisitor(source.Parameters, target).Visit(source.Body)
            ?? throw new InvalidOperationException("Не удалось переназначить параметры RICIS-координаты.");
        return Expression.Lambda(body, target);
    }

    private static ParameterExpression[] CreateParameters(IReadOnlyList<ParameterExpression> source) =>
        source.Select(parameter => Expression.Parameter(parameter.Type, parameter.Name ?? "x")).ToArray();

    private static void ValidatePair(RicisVectorExpression<T> left, RicisVectorExpression<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Dimension != right.Dimension || left.ParameterCount != right.ParameterCount)
            throw new ArgumentException("Размерности и число параметров символьных RICIS-векторов должны совпадать.");
        for (var i = 0; i < left.ParameterCount; i++)
        {
            if (left[0].Parameters[i].Type != right[0].Parameters[i].Type)
                throw new ArgumentException("Типы параметров символьных RICIS-векторов должны совпадать.");
        }
    }

    private static void ValidateSignature(IReadOnlyList<LambdaExpression> coordinates)
    {
        var first = coordinates[0];
        if (first.ReturnType != typeof(T)) throw new ArgumentException("Координата возвращает тип, отличный от T.");
        for (var i = 1; i < coordinates.Count; i++)
        {
            var current = coordinates[i];
            if (current.ReturnType != typeof(T) || current.Parameters.Count != first.Parameters.Count)
                throw new ArgumentException("Все координаты должны иметь одинаковый тип результата и сигнатуру параметров.");
            for (var parameter = 0; parameter < first.Parameters.Count; parameter++)
            {
                if (current.Parameters[parameter].Type != first.Parameters[parameter].Type)
                    throw new ArgumentException("Все координаты должны иметь одинаковые типы параметров.");
            }
        }
    }

    private sealed class ParameterRebindVisitor : ParameterMappingVisitorBase
    {
        public ParameterRebindVisitor(
            IReadOnlyList<ParameterExpression> source,
            IReadOnlyList<ParameterExpression> target)
            : base(source, target)
        {
        }
    }

    private sealed class CoordinateSubstitutionVisitor : ParameterMappingVisitorBase
    {
        public CoordinateSubstitutionVisitor(
            IReadOnlyList<ParameterExpression> parameters,
            IReadOnlyList<Expression> replacements)
            : base(parameters, replacements)
        {
        }
    }
}
