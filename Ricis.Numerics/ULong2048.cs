#nullable enable
#pragma warning disable CS1591

using System.Numerics;

namespace Ricis.Numerics;

/// <summary>
/// Represents an unsigned fixed-width 2048-bit integer backed by thirty-two little-endian <see cref="ulong"/> limbs.
/// This is the canonical magnitude domain for RSA-2048 moduli and signature representatives.
/// </summary>
public readonly struct ULong2048 : IComparable, IComparable<ULong2048>, IEquatable<ULong2048>
{
    private const int LimbCount = 32;
    private readonly ulong[]? _limbs;

    private ULong2048(ulong[] limbs, bool takeOwnership) => _limbs = takeOwnership ? limbs : (ulong[])limbs.Clone();

    public ULong2048(ulong value)
    {
        _limbs = new ulong[LimbCount];
        _limbs[0] = value;
    }

    public static ULong2048 Zero => default;
    public static ULong2048 One => new(1UL);
    public static ULong2048 MaxValue { get; } = CreateMaxValue();

    public static implicit operator ULong2048(ulong value) => new(value);
    public static explicit operator BigInteger(ULong2048 value) => value.ToBigInteger();
    public static explicit operator ULong2048(BigInteger value) => FromBigInteger(value);

    /// <summary>Creates a ULong2048 from a nonnegative BigInteger inside the fixed 2048-bit range.</summary>
    public static ULong2048 FromBigInteger(BigInteger value)
    {
        if (value < BigInteger.Zero || value > MaxValue.ToBigInteger())
        {
            throw new OverflowException("Value is outside ULong2048 range.");
        }

        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        var fixedBytes = new byte[LimbCount * sizeof(ulong)];
        bytes.AsSpan(0, Math.Min(bytes.Length, fixedBytes.Length)).CopyTo(fixedBytes);
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++)
        {
            limbs[index] = BitConverter.ToUInt64(fixedBytes, index * sizeof(ulong));
        }

        return new ULong2048(limbs, takeOwnership: true);
    }

    /// <summary>Returns a nonnegative BigInteger interop value without changing custom fixed-width storage.</summary>
    public BigInteger ToBigInteger()
    {
        var bytes = new byte[LimbCount * sizeof(ulong)];
        for (var index = 0; index < LimbCount; index++)
        {
            BitConverter.TryWriteBytes(bytes.AsSpan(index * sizeof(ulong), sizeof(ulong)), GetLimb(index));
        }

        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }

    public static ULong2048 operator +(ULong2048 left, ULong2048 right) => AddRaw(left, right);
    public static ULong2048 operator -(ULong2048 left, ULong2048 right) => SubtractRaw(left, right);
    public static ULong2048 operator *(ULong2048 left, ULong2048 right) => MultiplyRaw(left, right);
    public static ULong2048 operator /(ULong2048 left, ULong2048 right) => DivideUnsigned(left, right, out _);
    public static ULong2048 operator %(ULong2048 left, ULong2048 right)
    {
        _ = DivideUnsigned(left, right, out var remainder);
        return remainder;
    }

    public static ULong2048 operator &(ULong2048 left, ULong2048 right) => Bitwise(left, right, static (x, y) => x & y);
    public static ULong2048 operator |(ULong2048 left, ULong2048 right) => Bitwise(left, right, static (x, y) => x | y);
    public static ULong2048 operator ^(ULong2048 left, ULong2048 right) => Bitwise(left, right, static (x, y) => x ^ y);
    public static ULong2048 operator ~(ULong2048 value)
    {
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++) limbs[index] = ~value.GetLimb(index);
        return new ULong2048(limbs, takeOwnership: true);
    }

    public static ULong2048 operator <<(ULong2048 value, int shiftAmount) => ShiftLeft(value, shiftAmount);
    public static ULong2048 operator >>(ULong2048 value, int shiftAmount) => ShiftRight(value, shiftAmount);
    public static ULong2048 operator >>>(ULong2048 value, int shiftAmount) => ShiftRight(value, shiftAmount);

    /// <summary>Returns a BigInteger result for mixed exact arithmetic where a caller explicitly supplies BigInteger.</summary>
    public static BigInteger operator +(ULong2048 left, BigInteger right) => left.ToBigInteger() + right;
    public static BigInteger operator +(BigInteger left, ULong2048 right) => left + right.ToBigInteger();
    public static BigInteger operator -(ULong2048 left, BigInteger right) => left.ToBigInteger() - right;
    public static BigInteger operator -(BigInteger left, ULong2048 right) => left - right.ToBigInteger();
    public static BigInteger operator *(ULong2048 left, BigInteger right) => left.ToBigInteger() * right;
    public static BigInteger operator *(BigInteger left, ULong2048 right) => left * right.ToBigInteger();
    public static BigInteger operator /(ULong2048 left, BigInteger right) => left.ToBigInteger() / right;
    public static BigInteger operator %(ULong2048 left, BigInteger right) => left.ToBigInteger() % right;

    public static bool operator ==(ULong2048 left, ULong2048 right) => left.Equals(right);
    public static bool operator !=(ULong2048 left, ULong2048 right) => !left.Equals(right);
    public static bool operator <(ULong2048 left, ULong2048 right) => left.CompareTo(right) < 0;
    public static bool operator <=(ULong2048 left, ULong2048 right) => left.CompareTo(right) <= 0;
    public static bool operator >(ULong2048 left, ULong2048 right) => left.CompareTo(right) > 0;
    public static bool operator >=(ULong2048 left, ULong2048 right) => left.CompareTo(right) >= 0;

    /// <summary>Performs custom fixed-width modular multiplication without materialising a 4096-bit product.</summary>
    public static ULong2048 MultiplyModulo(ULong2048 left, ULong2048 right, ULong2048 modulus)
    {
        if (modulus == Zero) throw new DivideByZeroException();
        var factor = left % modulus;
        var multiplier = right % modulus;
        var result = Zero;

        while (multiplier != Zero)
        {
            if ((multiplier.GetLimb(0) & 1UL) != 0) result = AddModulo(result, factor, modulus);
            multiplier = multiplier >> 1;
            if (multiplier != Zero) factor = AddModulo(factor, factor, modulus);
        }

        return result;
    }

    /// <summary>Performs exact right-to-left modular exponentiation for RSA public operations.</summary>
    public static ULong2048 ModPow(ULong2048 value, ULong2048 exponent, ULong2048 modulus)
    {
        if (modulus == Zero) throw new DivideByZeroException();
        var result = One % modulus;
        var factor = value % modulus;
        var remainingExponent = exponent;

        while (remainingExponent != Zero)
        {
            if ((remainingExponent.GetLimb(0) & 1UL) != 0) result = MultiplyModulo(result, factor, modulus);
            remainingExponent = remainingExponent >> 1;
            if (remainingExponent != Zero) factor = MultiplyModulo(factor, factor, modulus);
        }

        return result;
    }

    /// <summary>Applies the RSA public verification primitive s^e mod n after checking the signature representative range.</summary>
    public static ULong2048 RsaPublicOperation(ULong2048 signature, ULong2048 publicExponent, ULong2048 modulus)
    {
        if (modulus <= One) throw new ArgumentOutOfRangeException(nameof(modulus), "RSA modulus must exceed one.");
        if (signature >= modulus) throw new ArgumentOutOfRangeException(nameof(signature), "RSA signature representative must be less than modulus.");
        return ModPow(signature, publicExponent, modulus);
    }

    public int CompareTo(ULong2048 other)
    {
        for (var index = LimbCount - 1; index >= 0; index--)
        {
            var left = GetLimb(index);
            var right = other.GetLimb(index);
            if (left != right) return left < right ? -1 : 1;
        }

        return 0;
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ULong2048 other) return CompareTo(other);
        throw new ArgumentException("Object must be ULong2048.", nameof(obj));
    }

    public bool Equals(ULong2048 other)
    {
        for (var index = 0; index < LimbCount; index++) if (GetLimb(index) != other.GetLimb(index)) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is ULong2048 other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var index = 0; index < LimbCount; index++) hash.Add(GetLimb(index));
        return hash.ToHashCode();
    }

    public override string ToString() => ToBigInteger().ToString();

    private ulong GetLimb(int index) => _limbs is null ? 0UL : _limbs[index];

    private static ULong2048 CreateMaxValue()
    {
        var limbs = new ulong[LimbCount];
        Array.Fill(limbs, ulong.MaxValue);
        return new ULong2048(limbs, takeOwnership: true);
    }

    private static ULong2048 AddRaw(ULong2048 left, ULong2048 right)
    {
        var limbs = new ulong[LimbCount];
        ulong carry = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var sum = left.GetLimb(index) + right.GetLimb(index);
            var carryFromOperands = sum < left.GetLimb(index) ? 1UL : 0UL;
            var result = sum + carry;
            limbs[index] = result;
            carry = carryFromOperands | (result < sum ? 1UL : 0UL);
        }

        return new ULong2048(limbs, takeOwnership: true);
    }

    private static ULong2048 SubtractRaw(ULong2048 left, ULong2048 right)
    {
        var limbs = new ulong[LimbCount];
        ulong borrow = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var rightWithBorrow = right.GetLimb(index) + borrow;
            var borrowFromAdd = rightWithBorrow < right.GetLimb(index) ? 1UL : 0UL;
            var leftValue = left.GetLimb(index);
            limbs[index] = leftValue - rightWithBorrow;
            borrow = leftValue < rightWithBorrow || borrowFromAdd != 0 ? 1UL : 0UL;
        }

        return new ULong2048(limbs, takeOwnership: true);
    }

    private static ULong2048 MultiplyRaw(ULong2048 left, ULong2048 right)
    {
        var limbs = new ulong[LimbCount];
        for (var leftIndex = 0; leftIndex < LimbCount; leftIndex++)
        {
            UInt128 carry = 0;
            for (var rightIndex = 0; leftIndex + rightIndex < LimbCount; rightIndex++)
            {
                var target = leftIndex + rightIndex;
                var total = ((UInt128)left.GetLimb(leftIndex) * right.GetLimb(rightIndex)) + limbs[target] + carry;
                limbs[target] = (ulong)total;
                carry = total >> 64;
            }
        }

        return new ULong2048(limbs, takeOwnership: true);
    }

    private static ULong2048 DivideUnsigned(ULong2048 numerator, ULong2048 denominator, out ULong2048 remainder)
    {
        if (denominator == Zero) throw new DivideByZeroException();
        var quotientLimbs = new ulong[LimbCount];
        var remainderLimbs = new ulong[LimbCount];
        var denominatorLimbs = denominator.CopyLimbs();

        for (var bit = (LimbCount * 64) - 1; bit >= 0; bit--)
        {
            ShiftLeftOne(remainderLimbs);
            remainderLimbs[0] |= (numerator.GetLimb(bit / 64) >> (bit % 64)) & 1UL;
            if (CompareLimbs(remainderLimbs, denominatorLimbs) >= 0)
            {
                SubtractLimbs(remainderLimbs, denominatorLimbs);
                quotientLimbs[bit / 64] |= 1UL << (bit % 64);
            }
        }

        remainder = new ULong2048(remainderLimbs, takeOwnership: true);
        return new ULong2048(quotientLimbs, takeOwnership: true);
    }

    private static ULong2048 AddModulo(ULong2048 left, ULong2048 right, ULong2048 modulus)
    {
        // left and right are reduced. Comparing to modulus-right avoids any 2048-bit overflow.
        var complement = modulus - right;
        return left >= complement ? left - complement : left + right;
    }

    private ulong[] CopyLimbs()
    {
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++) limbs[index] = GetLimb(index);
        return limbs;
    }

    private static ULong2048 Bitwise(ULong2048 left, ULong2048 right, Func<ulong, ulong, ulong> operation)
    {
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++) limbs[index] = operation(left.GetLimb(index), right.GetLimb(index));
        return new ULong2048(limbs, takeOwnership: true);
    }

    private static ULong2048 ShiftLeft(ULong2048 value, int shiftAmount)
    {
        if (shiftAmount < 0) return ShiftRight(value, -shiftAmount);
        if (shiftAmount >= LimbCount * 64) return Zero;
        var limbShift = shiftAmount / 64;
        var bitShift = shiftAmount % 64;
        var limbs = new ulong[LimbCount];
        for (var index = LimbCount - 1; index >= limbShift; index--)
        {
            limbs[index] = value.GetLimb(index - limbShift) << bitShift;
            if (bitShift != 0 && index > limbShift) limbs[index] |= value.GetLimb(index - limbShift - 1) >> (64 - bitShift);
        }

        return new ULong2048(limbs, takeOwnership: true);
    }

    private static ULong2048 ShiftRight(ULong2048 value, int shiftAmount)
    {
        if (shiftAmount < 0) return ShiftLeft(value, -shiftAmount);
        if (shiftAmount >= LimbCount * 64) return Zero;
        var limbShift = shiftAmount / 64;
        var bitShift = shiftAmount % 64;
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++)
        {
            var source = index + limbShift;
            if (source >= LimbCount) continue;
            limbs[index] = value.GetLimb(source) >> bitShift;
            if (bitShift != 0 && source + 1 < LimbCount) limbs[index] |= value.GetLimb(source + 1) << (64 - bitShift);
        }

        return new ULong2048(limbs, takeOwnership: true);
    }

    private static void ShiftLeftOne(ulong[] value)
    {
        ulong carry = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var nextCarry = value[index] >> 63;
            value[index] = (value[index] << 1) | carry;
            carry = nextCarry;
        }
    }

    private static int CompareLimbs(ulong[] left, ulong[] right)
    {
        for (var index = LimbCount - 1; index >= 0; index--)
        {
            if (left[index] == right[index]) continue;
            return left[index] < right[index] ? -1 : 1;
        }

        return 0;
    }

    private static void SubtractLimbs(ulong[] left, ulong[] right)
    {
        ulong borrow = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var rightWithBorrow = right[index] + borrow;
            var borrowFromAdd = rightWithBorrow < right[index] ? 1UL : 0UL;
            var leftValue = left[index];
            left[index] = leftValue - rightWithBorrow;
            borrow = leftValue < rightWithBorrow || borrowFromAdd != 0 ? 1UL : 0UL;
        }
    }
}
