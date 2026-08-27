using System.Numerics;

namespace Ricis.Numerics.Factorization;

internal static class FixedWidthFermatRoot
{
    private const int MaximumInputBits = 2048;

    internal static BigInteger Floor(BigInteger value)
    {
        if (value.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Square-root input must be non-negative.");
        }

        if (value.GetBitLength() > MaximumInputBits)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Composite fixed-width input must not exceed 2048 bits.");
        }

        return ULong2048.IntegerSquareRootFloor(ULong2048.FromBigInteger(value)).ToBigInteger();
    }
}
