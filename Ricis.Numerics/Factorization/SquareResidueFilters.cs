using System.Numerics;

namespace Ricis.Numerics.Factorization;

internal static class SquareResidueFilters
{
    private const ulong Modulo64Mask =
        (1UL << 0) | (1UL << 1) | (1UL << 4) | (1UL << 9) |
        (1UL << 16) | (1UL << 17) | (1UL << 25) | (1UL << 33) |
        (1UL << 36) | (1UL << 41) | (1UL << 49) | (1UL << 57);

    private static readonly bool[] Modulo7 = CreateSquareTable(7);
    private static readonly bool[] Modulo31 = CreateSquareTable(31);
    private static readonly bool[] Modulo127 = CreateSquareTable(127);

    internal static bool CouldBeSquareModulo64(BigInteger value)
    {
        var residue = (int)(value & 63);
        return ((Modulo64Mask >> residue) & 1UL) != 0;
    }

    internal static bool CouldBeSquareModulo7(BigInteger value) => IsAccepted(value, 7, Modulo7);

    internal static bool CouldBeSquareModulo31(BigInteger value) => IsAccepted(value, 31, Modulo31);

    internal static bool CouldBeSquareModulo127(BigInteger value) => IsAccepted(value, 127, Modulo127);

    private static bool IsAccepted(BigInteger value, int modulus, IReadOnlyList<bool> table)
    {
        var residue = (int)(value % modulus);
        if (residue < 0)
        {
            residue += modulus;
        }

        return table[residue];
    }

    private static bool[] CreateSquareTable(int modulus)
    {
        var table = new bool[modulus];
        for (var value = 0; value < modulus; value++)
        {
            table[(value * value) % modulus] = true;
        }

        return table;
    }
}
