using System.Collections;
using System.Collections.ObjectModel;
using System.Numerics;
using Ricis.Core.Resources;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents an immutable RICIS vector as an ordered collection of N scalar coordinates.
/// Each coordinate is a value of the generic numeric domain <typeparamref name="T"/>;
/// vector operations are structural and do not introduce a second arithmetic system.
/// </summary>
/// <typeparam name="T">The scalar coordinate type.</typeparam>
public sealed class RicisVector<T> : IReadOnlyList<T>, IEquatable<RicisVector<T>>
    where T : INumber<T>
{
    private readonly ReadOnlyCollection<T> _coordinates;

    /// <summary>
    /// Initializes a vector from an ordered non-empty coordinate sequence.
    /// </summary>
    /// <param name="coordinates">The coordinates defining the vector direction.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="coordinates"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    public RicisVector(IEnumerable<T> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        var copied = coordinates.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.8bb7e5417163"), nameof(coordinates));
        }

        _coordinates = Array.AsReadOnly(copied);
    }

    /// <summary>
    /// Gets the number of coordinates in the vector.
    /// </summary>
    public int Dimension => _coordinates.Count;

    /// <summary>
    /// Gets the number of coordinates for the <see cref="IReadOnlyCollection{T}"/> contract.
    /// </summary>
    public int Count => _coordinates.Count;

    /// <summary>
    /// Gets the coordinate at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based coordinate index.</param>
    public T this[int index] => _coordinates[index];

    /// <summary>
    /// Gets the immutable coordinate sequence.
    /// </summary>
    public IReadOnlyList<T> Coordinates => _coordinates;

    /// <summary>
    /// Gets an enumerator over the ordered coordinates.
    /// </summary>
    public IEnumerator<T> GetEnumerator() => _coordinates.GetEnumerator();

    /// <summary>
    /// Gets a non-generic enumerator over the ordered coordinates.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates the typed zero vector of the requested dimension.
    /// </summary>
    /// <param name="dimension">The positive number of coordinates.</param>
    /// <returns>A vector whose every coordinate is типизированного нуля координатного домена.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="dimension"/> is not positive.</exception>
    public static RicisVector<T> Zero(int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), dimension, RicisLegacyTextResources.Get("runtime.legacy.f973462e4805"));
        }

        return new RicisVector<T>(Enumerable.Repeat(T.Zero, dimension));
    }

    /// <summary>
    /// Adds two vectors componentwise.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The componentwise sum.</returns>
    /// <exception cref="ArgumentException">Thrown when the dimensions differ.</exception>
    public static RicisVector<T> Add(RicisVector<T> left, RicisVector<T> right)
    {
        ValidatePair(left, right);
        return new RicisVector<T>(left.Zip(right, static (a, b) => a + b));
    }

    /// <summary>
    /// Subtracts the second vector from the first componentwise.
    /// </summary>
    /// <param name="left">The minuend vector.</param>
    /// <param name="right">The subtrahend vector.</param>
    /// <returns>The componentwise difference.</returns>
    /// <exception cref="ArgumentException">Thrown when the dimensions differ.</exception>
    public static RicisVector<T> Subtract(RicisVector<T> left, RicisVector<T> right)
    {
        ValidatePair(left, right);
        return new RicisVector<T>(left.Zip(right, static (a, b) => a - b));
    }

    /// <summary>
    /// Scales a vector componentwise by a scalar.
    /// </summary>
    /// <param name="vector">The vector to scale.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The scaled vector.</returns>
    public static RicisVector<T> Scale(RicisVector<T> vector, T scalar)
    {
        ArgumentNullException.ThrowIfNull(vector);
        return new RicisVector<T>(vector.Select(component => component * scalar));
    }

    /// <summary>
    /// Computes the ordinary finite-domain dot product of two equal-dimensional vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The scalar dot product.</returns>
    /// <exception cref="ArgumentException">Thrown when the dimensions differ.</exception>
    public static T Dot(RicisVector<T> left, RicisVector<T> right)
    {
        ValidatePair(left, right);
        var result = T.Zero;
        for (var i = 0; i < left.Dimension; i++)
        {
            result += left[i] * right[i];
        }

        return result;
    }

    /// <summary>
    /// Returns the componentwise sum of two vectors.
    /// </summary>
    public static RicisVector<T> operator +(RicisVector<T> left, RicisVector<T> right) => Add(left, right);

    /// <summary>
    /// Returns the componentwise difference of two vectors.
    /// </summary>
    public static RicisVector<T> operator -(RicisVector<T> left, RicisVector<T> right) => Subtract(left, right);

    /// <summary>
    /// Returns a vector scaled by a scalar on the right.
    /// </summary>
    public static RicisVector<T> operator *(RicisVector<T> vector, T scalar) => Scale(vector, scalar);

    /// <summary>
    /// Returns a vector scaled by a scalar on the left.
    /// </summary>
    public static RicisVector<T> operator *(T scalar, RicisVector<T> vector) => Scale(vector, scalar);

    /// <summary>
    /// Determines whether two vectors have equal dimensions and equal coordinates.
    /// </summary>
    public bool Equals(RicisVector<T> other)
    {
        if (ReferenceEquals(this, other)) return true;
        return other is not null && _coordinates.SequenceEqual(other._coordinates);
    }

    /// <summary>
    /// Determines whether an object is an equal vector in the same scalar domain.
    /// </summary>
    public override bool Equals(object obj) => obj is RicisVector<T> other && Equals(other);

    /// <summary>
    /// Returns a coordinate-based hash code.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var coordinate in _coordinates) hash.Add(coordinate);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns the coordinate record of the vector.
    /// </summary>
    public override string ToString() => $"({string.Join(", ", _coordinates)})";

    private static void ValidatePair(RicisVector<T> left, RicisVector<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Dimension != right.Dimension)
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Format("runtime.legacy.83720e5b59fb", ("left.Dimension", left.Dimension), ("right.Dimension", right.Dimension)));
        }
    }
}
