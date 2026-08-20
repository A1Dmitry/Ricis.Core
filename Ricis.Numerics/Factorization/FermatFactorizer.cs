using System.Numerics;

namespace Ricis.Numerics.Factorization;

/// <summary>
/// Exact difference-of-squares factorization baseline for positive odd integers.
/// This baseline is intentionally separate from the pruned-search research path.
/// </summary>
public static class FermatFactorizer
{
    // Squares modulo 64 are exactly { 0, 1, 4, 9, 16, 17, 25, 33, 36, 41, 49, 57 }.
    private const ulong SquareResidueModulo64Mask =
        (1UL << 0) | (1UL << 1) | (1UL << 4) | (1UL << 9) |
        (1UL << 16) | (1UL << 17) | (1UL << 25) | (1UL << 33) |
        (1UL << 36) | (1UL << 41) | (1UL << 49) | (1UL << 57);

    /// <summary>
    /// Solves <c>x²-N=y²</c> and returns the exact reconstruction
    /// <c>P=x-y</c>, <c>Q=x+y</c> for a positive odd semiprime baseline input.
    /// </summary>
    /// <param name="n">The only numeric factorization input.</param>
    /// <returns>The exact candidate and reconstruction evidence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> is not positive and odd.</exception>
    public static FermatFactorizationResult Solve(BigInteger n) => Search(n).Factorization;

    /// <summary>
    /// Performs the N-only Fermat baseline with a bit-mask square-residue prefilter.
    /// The result reports all candidate and exact-root work explicitly; it does not claim a
    /// constant bound or conceal candidate enumeration behind the prefilter.
    /// </summary>
    /// <param name="n">The only numeric factorization input.</param>
    /// <returns>Exact factorization evidence and measured pruning counters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> is not positive and odd.</exception>
    public static FermatSearchResult Search(BigInteger n)
    {
        if (n <= 0 || n.IsEven)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "N must be a positive odd integer.");
        }

        var x = IntegerSquareRootCeilingByShift(n);
        var delta = x * x - n;
        var candidates = BigInteger.Zero;
        var maskRejected = BigInteger.Zero;
        var maskPassed = BigInteger.Zero;
        var rootChecks = BigInteger.Zero;

        while (true)
        {
            candidates++;
            if (!CouldBePerfectSquare(delta))
            {
                maskRejected++;
                delta += (2 * x) + 1;
                x++;
                continue;
            }

            maskPassed++;
            rootChecks++;
            var y = IntegerSquareRootFloorByShift(delta);
            if (y * y == delta)
            {
                var p = x - y;
                var q = x + y;
                if (p > 1 && q > 1 && p * q == n)
                {
                    var inputBits = checked((int)n.GetBitLength());
                    var factorBits = checked((int)Math.Max(p.GetBitLength(), q.GetBitLength()));
                    var scale = BigInteger.One << factorBits;
                    var factorization = new FermatFactorizationResult(
                        n, p, q, x, y, delta, inputBits, factorBits, scale);
                    return new FermatSearchResult(factorization, candidates, maskRejected, maskPassed, rootChecks);
                }
            }

            delta += (2 * x) + 1;
            x++;
        }
    }

    private static bool CouldBePerfectSquare(BigInteger value)
    {
        var residue = (int)(value & 63);
        return ((SquareResidueModulo64Mask >> residue) & 1UL) != 0;
    }

    private static BigInteger IntegerSquareRootCeilingByShift(BigInteger value)
    {
        var floor = IntegerSquareRootFloorByShift(value);
        return floor * floor == value ? floor : floor + BigInteger.One;
    }

    /// <summary>
    /// Computes the exact floor square root with the binary restoring shift method.
    /// It consumes two input bits per iteration and ends with one bounded correction bit.
    /// </summary>
    /// <param name="value">The non-negative integer input.</param>
    /// <returns>The unique floor root r satisfying r²≤value&lt;(r+1)².</returns>
    internal static BigInteger IntegerSquareRootFloorByShift(BigInteger value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Square-root input must be non-negative.");
        }

        if (value < 2)
        {
            return value;
        }

        var leadingPairShift = checked((int)(value.GetBitLength() - 1)) & ~1;
        var bit = BigInteger.One << leadingPairShift;
        var remainder = value;
        var root = BigInteger.Zero;

        while (bit != BigInteger.Zero)
        {
            var trial = root + bit;
            if (remainder >= trial)
            {
                remainder -= trial;
                root = (root >> 1) + bit;
            }
            else
            {
                root >>= 1;
            }

            bit >>= 2;
        }

        // The restoring invariant produces the floor root. Keep a single-bit
        // correction guard explicit so the certificate is local and auditable.
        var square = root * root;
        if (square > value)
        {
            return root - BigInteger.One;
        }

        var corrected = root + BigInteger.One;
        return corrected * corrected <= value ? corrected : root;
    }
}
