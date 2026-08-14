using System.Numerics;

namespace Ricis.Core.SpecialFunctions;

/// <summary>
/// Exact factorial over <see cref="BigInteger"/>. It is intentionally a
/// first-class method so a LINQ expression tree can keep <c>n!</c> symbolic
/// until an algebraic RICIS phase reduces it or a derived finite expression is
/// compiled.
/// </summary>
public static class Factorial
{
    /// <summary>
    /// Returns <c>value!</c> exactly. The factorial is defined here only for
    /// non-negative integer arguments.
    /// </summary>
    public static BigInteger Of(BigInteger value)
    {
        if (value < BigInteger.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                "Факториал определён только для неотрицательных целых чисел.");
        }

        var result = BigInteger.One;
        for (var factor = new BigInteger(2); factor <= value; factor++)
        {
            result *= factor;
        }

        return result;
    }
}
