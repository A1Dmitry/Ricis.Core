using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Resources;

namespace Ricis.Numerics.Factorization;

/// <summary>
/// Holds immutable canonical state shared by validated odd semiprime domain objects.
/// The state is protected for derived implementation reuse and is never externally mutable.
/// </summary>
/// <typeparam name="T">The exact numeric domain used for the semiprime.</typeparam>
public abstract class SemiprimeBase<T>
    where T : IComparable<T>,
        IAdditionOperators<T, T, T>,
        ISubtractionOperators<T, T, T>,
        IMultiplyOperators<T, T, T>,
        IDivisionOperators<T, T, T>,
        IModulusOperators<T, T, T>,
        IEqualityOperators<T, T, bool>,
        IComparisonOperators<T, T, bool>,
        IAdditiveIdentity<T, T>,
        IMultiplicativeIdentity<T, T>
{
    /// <summary>The validated product N.</summary>
    protected readonly T number;

    /// <summary>The validated smaller prime factor P.</summary>
    protected readonly T smallerFactor;

    /// <summary>The validated greater-or-equal prime factor Q.</summary>
    protected readonly T greaterFactor;

    /// <summary>Initializes canonical state from a supplied factor pair.</summary>
    /// <param name="first">One validated odd prime factor.</param>
    /// <param name="second">One validated odd prime factor.</param>
    protected SemiprimeBase(T first, T second)
    {
        ValidateOddPrime(first, nameof(first));
        ValidateOddPrime(second, nameof(second));

        if (first <= second)
        {
            smallerFactor = first;
            greaterFactor = second;
        }
        else
        {
            smallerFactor = second;
            greaterFactor = first;
        }

        number = smallerFactor * greaterFactor;
        if (number <= T.MultiplicativeIdentity || IsEven(number))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.03c3b0819b46"));
        }
    }

    /// <summary>Gets validated product N.</summary>
    public T N => number;

    /// <summary>Gets validated smaller-or-equal prime factor P.</summary>
    public T P => smallerFactor;

    /// <summary>Gets validated greater-or-equal prime factor Q.</summary>
    public T Q => greaterFactor;

    /// <summary>Gets the exact non-negative factor gap Q-P.</summary>
    public T Gap => greaterFactor - smallerFactor;

    /// <summary>Gets the exact Fermat midpoint X=(P+Q)/2.</summary>
    public T FermatX => (smallerFactor + greaterFactor) / Two;

    /// <summary>Gets the exact Fermat half-gap Y=(Q-P)/2.</summary>
    public T FermatY => (greaterFactor - smallerFactor) / Two;

    /// <summary>
    /// Gets the canonical deferred identity F/F for the validated scalar domain.
    /// Every access creates an equivalent immutable expression tree from the fixed state.
    /// </summary>
    public Expression<Func<T, T>> CanonicalReductionExpression => BuildCanonicalReductionExpression();

    /// <summary>
    /// Builds the canonical deferred identity F/F for the validated scalar domain.
    /// Derived implementations and proof cases reuse this one expression builder rather than duplicating its tree.
    /// </summary>
    /// <returns>A typed deferred identity expression.</returns>
    protected Expression<Func<T, T>> BuildCanonicalReductionExpression()
    {
        var value = Expression.Parameter(typeof(T), "x");
        var square = Expression.Multiply(value, value);
        return Expression.Lambda<Func<T, T>>(Expression.Divide(square, square), value);
    }

    /// <summary>Gets a typed constant two without hard-coded concrete numeric types.</summary>
    protected static T Two => T.MultiplicativeIdentity + T.MultiplicativeIdentity;

    /// <summary>Checks an odd positive input before N-only recovery.</summary>
    /// <param name="value">The declared N-only input.</param>
    protected static void ValidateOddCompositeInput(T value)
    {
        if (value <= T.MultiplicativeIdentity)
        {
            throw new ArgumentOutOfRangeException(nameof(value), RicisLegacyTextResources.Get("runtime.legacy.1e8a05a3b057"));
        }

        if (IsEven(value))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.efe6e05e06bf"), nameof(value));
        }
    }

    /// <summary>Recovers the unique canonical odd-prime pair for a validated small N-only input.</summary>
    /// <param name="value">The positive odd integer N.</param>
    /// <returns>The recovered factor pair in no assumed order.</returns>
    protected static (T First, T Second) RecoverOddPrimeFactors(T value)
    {
        ValidateOddCompositeInput(value);
        for (var candidate = T.MultiplicativeIdentity + Two; candidate <= value / candidate; candidate += Two)
        {
            if (value % candidate != T.AdditiveIdentity)
            {
                continue;
            }

            var companion = value / candidate;
            if (IsOddPrime(candidate) && IsOddPrime(companion))
            {
                return (candidate, companion);
            }

            break;
        }

        throw new ArgumentException(
            RicisLegacyTextResources.Get("runtime.legacy.36e7df5b5e67"),
            nameof(value));
    }

    /// <summary>Validates one supplied odd prime factor.</summary>
    /// <param name="value">The factor value.</param>
    /// <param name="parameterName">The constructor parameter name.</param>
    protected static void ValidateOddPrime(T value, string parameterName)
    {
        if (value <= T.MultiplicativeIdentity)
        {
            throw new ArgumentOutOfRangeException(parameterName, RicisLegacyTextResources.Get("runtime.legacy.8edfed9716e9"));
        }

        if (IsEven(value))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.2de4f1ee6e4e"), parameterName);
        }

        if (!IsOddPrime(value))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.9abb32a051a3"), parameterName);
        }
    }

    private static bool IsOddPrime(T value)
    {
        if (value <= T.MultiplicativeIdentity || IsEven(value))
        {
            return false;
        }

        for (var candidate = T.MultiplicativeIdentity + Two; candidate <= value / candidate; candidate += Two)
        {
            if (value % candidate == T.AdditiveIdentity)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsEven(T value) => value % Two == T.AdditiveIdentity;
}

/// <summary>
/// Immutable validated odd semiprime. Construction accepts either N alone for exact
/// factor recovery or a pair of supplied factors; all other properties are derived.
/// </summary>
/// <typeparam name="T">The exact numeric domain used for the semiprime.</typeparam>
public sealed class Semiprime<T> : SemiprimeBase<T>
    where T : IComparable<T>,
        IAdditionOperators<T, T, T>,
        ISubtractionOperators<T, T, T>,
        IMultiplyOperators<T, T, T>,
        IDivisionOperators<T, T, T>,
        IModulusOperators<T, T, T>,
        IEqualityOperators<T, T, bool>,
        IComparisonOperators<T, T, bool>,
        IAdditiveIdentity<T, T>,
        IMultiplicativeIdentity<T, T>
{
    /// <summary>
    /// Initializes an immutable semiprime by exact recovery from the only supplied value N.
    /// </summary>
    /// <param name="n">The positive odd semiprime input.</param>
    public Semiprime(T n)
        : this(RecoverOddPrimeFactors(n))
    {
    }

    /// <summary>
    /// Initializes an immutable semiprime from two supplied odd prime factors.
    /// </summary>
    /// <param name="p">One odd prime factor.</param>
    /// <param name="q">One odd prime factor.</param>
    public Semiprime(T p, T q)
        : base(p, q)
    {
    }

    private Semiprime((T First, T Second) recovered)
        : base(recovered.First, recovered.Second)
    {
    }
}
