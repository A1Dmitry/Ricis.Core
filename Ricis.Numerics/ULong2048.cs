#nullable enable
#pragma warning disable CS1591

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ricis.Numerics;

[InlineArray(32)]
internal struct ULong2048Limbs
{
    private ulong _element0;
}

/// <summary>
/// Represents an allocation-free unsigned fixed-width 2048-bit integer backed by thirty-two inline little-endian <see cref="ulong"/> limbs.
/// This is the canonical magnitude domain for RSA-2048 moduli and signature representatives.
/// </summary>
public readonly partial struct ULong2048 : IComparable, IComparable<ULong2048>, IEquatable<ULong2048>,
    IAdditionOperators<ULong2048, ULong2048, ULong2048>,
    ISubtractionOperators<ULong2048, ULong2048, ULong2048>,
    IMultiplyOperators<ULong2048, ULong2048, ULong2048>,
    IDivisionOperators<ULong2048, ULong2048, ULong2048>,
    IModulusOperators<ULong2048, ULong2048, ULong2048>,
    IEqualityOperators<ULong2048, ULong2048, bool>,
    IComparisonOperators<ULong2048, ULong2048, bool>,
    IAdditiveIdentity<ULong2048, ULong2048>,
    IMultiplicativeIdentity<ULong2048, ULong2048>
{
    private const int LimbCount = 32;
    private const int BitCount = LimbCount * 64;
    private readonly ULong2048Limbs _limbs;

    private ULong2048(ULong2048Limbs limbs) => _limbs = limbs;

    public ULong2048(ulong value)
    {
        _limbs = default;
        _limbs[0] = value;
    }

    public static ULong2048 Zero => default;
    public static ULong2048 One => new(1UL);
    public static ULong2048 AdditiveIdentity => Zero;
    public static ULong2048 MultiplicativeIdentity => One;
    public static ULong2048 MaxValue { get; } = CreateMaxValue();

    public static implicit operator ULong2048(ulong value) => new(value);
    public static explicit operator BigInteger(ULong2048 value) => value.ToBigInteger();
    public static explicit operator ULong2048(BigInteger value) => FromBigInteger(value);

    /// <summary>Creates a ULong2048 from a nonnegative BigInteger inside the fixed 2048-bit range.</summary>
    public static ULong2048 FromBigInteger(BigInteger value)
    {
        if (value.Sign < 0 || value.GetBitLength() > BitCount)
        {
            throw new OverflowException("Value is outside ULong2048 range.");
        }

        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        Span<byte> fixedBytes = stackalloc byte[LimbCount * sizeof(ulong)];
        bytes.AsSpan().CopyTo(fixedBytes);
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++)
        {
            limbs[index] = BinaryPrimitives.ReadUInt64LittleEndian(fixedBytes.Slice(index * sizeof(ulong), sizeof(ulong)));
        }

        return new ULong2048(limbs);
    }

    /// <summary>Returns a nonnegative BigInteger interop value without changing inline custom fixed-width storage.</summary>
    public BigInteger ToBigInteger()
    {
        Span<byte> bytes = stackalloc byte[LimbCount * sizeof(ulong)];
        for (var index = 0; index < LimbCount; index++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(index * sizeof(ulong), sizeof(ulong)), GetLimb(index));
        }

        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }

    public static ULong2048 operator +(ULong2048 left, ULong2048 right) => AddRaw(in left, in right);
    public static ULong2048 operator checked +(ULong2048 left, ULong2048 right) => CheckedAdd(left, right);
    public static ULong2048 operator -(ULong2048 left, ULong2048 right) => SubtractRaw(in left, in right);
    public static ULong2048 operator checked -(ULong2048 left, ULong2048 right) => CheckedSubtract(left, right);
    public static ULong2048 operator *(ULong2048 left, ULong2048 right) => MultiplyRaw(in left, in right);
    public static ULong2048 operator checked *(ULong2048 left, ULong2048 right) => CheckedMultiply(left, right);
    public static ULong2048 operator /(ULong2048 left, ULong2048 right) => DivideUnsigned(in left, in right, out _);
    public static ULong2048 operator %(ULong2048 left, ULong2048 right)
    {
        _ = DivideUnsigned(in left, in right, out var remainder);
        return remainder;
    }

    public static ULong2048 operator &(ULong2048 left, ULong2048 right) => And(in left, in right);
    public static ULong2048 operator |(ULong2048 left, ULong2048 right) => Or(in left, in right);
    public static ULong2048 operator ^(ULong2048 left, ULong2048 right) => Xor(in left, in right);
    public static ULong2048 operator ~(ULong2048 value)
    {
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++) limbs[index] = ~value.GetLimb(index);
        return new ULong2048(limbs);
    }

    public static ULong2048 operator <<(ULong2048 value, int shiftAmount) => ShiftLeft(in value, shiftAmount);
    public static ULong2048 operator >>(ULong2048 value, int shiftAmount) => ShiftRight(in value, shiftAmount);
    public static ULong2048 operator >>>(ULong2048 value, int shiftAmount) => ShiftRight(in value, shiftAmount);

    /// <summary>Returns an exact BigInteger sum for an explicitly mixed ULong2048/BigInteger operation.</summary>
    public static BigInteger operator +(ULong2048 left, BigInteger right) => left.ToBigInteger() + right;
    public static BigInteger operator +(BigInteger left, ULong2048 right) => left + right.ToBigInteger();
    public static BigInteger operator -(ULong2048 left, BigInteger right) => left.ToBigInteger() - right;
    public static BigInteger operator -(BigInteger left, ULong2048 right) => left - right.ToBigInteger();
    public static BigInteger operator *(ULong2048 left, BigInteger right) => left.ToBigInteger() * right;
    public static BigInteger operator *(BigInteger left, ULong2048 right) => left * right.ToBigInteger();
    public static BigInteger operator /(ULong2048 left, BigInteger right) => left.ToBigInteger() / right;
    public static BigInteger operator /(BigInteger left, ULong2048 right) => left / right.ToBigInteger();
    public static BigInteger operator %(ULong2048 left, BigInteger right) => left.ToBigInteger() % right;
    public static BigInteger operator %(BigInteger left, ULong2048 right) => left % right.ToBigInteger();
    public static BigInteger operator &(ULong2048 left, BigInteger right) => left.ToBigInteger() & right;
    public static BigInteger operator &(BigInteger left, ULong2048 right) => left & right.ToBigInteger();
    public static BigInteger operator |(ULong2048 left, BigInteger right) => left.ToBigInteger() | right;
    public static BigInteger operator |(BigInteger left, ULong2048 right) => left | right.ToBigInteger();
    public static BigInteger operator ^(ULong2048 left, BigInteger right) => left.ToBigInteger() ^ right;
    public static BigInteger operator ^(BigInteger left, ULong2048 right) => left ^ right.ToBigInteger();
    public static bool operator ==(ULong2048 left, BigInteger right) => left.ToBigInteger() == right;
    public static bool operator ==(BigInteger left, ULong2048 right) => left == right.ToBigInteger();
    public static bool operator !=(ULong2048 left, BigInteger right) => left.ToBigInteger() != right;
    public static bool operator !=(BigInteger left, ULong2048 right) => left != right.ToBigInteger();
    public static bool operator <(ULong2048 left, BigInteger right) => left.ToBigInteger() < right;
    public static bool operator <(BigInteger left, ULong2048 right) => left < right.ToBigInteger();
    public static bool operator <=(ULong2048 left, BigInteger right) => left.ToBigInteger() <= right;
    public static bool operator <=(BigInteger left, ULong2048 right) => left <= right.ToBigInteger();
    public static bool operator >(ULong2048 left, BigInteger right) => left.ToBigInteger() > right;
    public static bool operator >(BigInteger left, ULong2048 right) => left > right.ToBigInteger();
    public static bool operator >=(ULong2048 left, BigInteger right) => left.ToBigInteger() >= right;
    public static bool operator >=(BigInteger left, ULong2048 right) => left >= right.ToBigInteger();

    public static bool operator ==(ULong2048 left, ULong2048 right) => left.Equals(right);
    public static bool operator !=(ULong2048 left, ULong2048 right) => !left.Equals(right);
    public static bool operator <(ULong2048 left, ULong2048 right) => left.CompareTo(right) < 0;
    public static bool operator <=(ULong2048 left, ULong2048 right) => left.CompareTo(right) <= 0;
    public static bool operator >(ULong2048 left, ULong2048 right) => left.CompareTo(right) > 0;
    public static bool operator >=(ULong2048 left, ULong2048 right) => left.CompareTo(right) >= 0;

    /// <summary>Performs custom allocation-free fixed-width modular multiplication without materialising a 4096-bit product.</summary>
    public static ULong2048 MultiplyModulo(ULong2048 left, ULong2048 right, ULong2048 modulus)
    {
        if (modulus == Zero) throw new DivideByZeroException();
        var leftReduced = (left % modulus)._limbs;
        var rightReduced = (right % modulus)._limbs;
        if ((modulus.GetLimb(0) & 1UL) == 0)
        {
            return new ULong2048(MultiplyModuloGenericLimbs(in leftReduced, in rightReduced, in modulus._limbs));
        }

        var rSquared = ComputeMontgomeryRSquared(in modulus._limbs);
        var n0Prime = ComputeMontgomeryN0Prime(modulus.GetLimb(0));
        var leftMontgomery = MontgomeryMultiplyLimbs(in leftReduced, in rSquared, in modulus._limbs, n0Prime);
        var rightMontgomery = MontgomeryMultiplyLimbs(in rightReduced, in rSquared, in modulus._limbs, n0Prime);
        var productMontgomery = MontgomeryMultiplyLimbs(in leftMontgomery, in rightMontgomery, in modulus._limbs, n0Prime);
        ULong2048Limbs one = default;
        one[0] = 1;
        return new ULong2048(MontgomeryMultiplyLimbs(in productMontgomery, in one, in modulus._limbs, n0Prime));
    }

    /// <summary>Performs exact allocation-free right-to-left modular exponentiation for RSA public operations.</summary>
    public static ULong2048 ModPow(ULong2048 value, ULong2048 exponent, ULong2048 modulus)
    {
        if (modulus == Zero) throw new DivideByZeroException();
        if ((modulus.GetLimb(0) & 1UL) == 0) return ModPowGeneric(value, exponent, modulus);

        var rSquared = ComputeMontgomeryRSquared(in modulus._limbs);
        var n0Prime = ComputeMontgomeryN0Prime(modulus.GetLimb(0));
        ULong2048Limbs one = default;
        one[0] = 1;
        var result = MontgomeryMultiplyLimbs(in one, in rSquared, in modulus._limbs, n0Prime);
        var reducedValue = (value % modulus)._limbs;
        var factor = MontgomeryMultiplyLimbs(in reducedValue, in rSquared, in modulus._limbs, n0Prime);
        var remainingExponent = exponent._limbs;

        while (!IsZero(in remainingExponent))
        {
            if ((remainingExponent[0] & 1UL) != 0) result = MontgomeryMultiplyLimbs(in result, in factor, in modulus._limbs, n0Prime);
            ShiftRightOne(ref remainingExponent);
            if (!IsZero(in remainingExponent)) factor = MontgomeryMultiplyLimbs(in factor, in factor, in modulus._limbs, n0Prime);
        }

        return new ULong2048(MontgomeryMultiplyLimbs(in result, in one, in modulus._limbs, n0Prime));
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

    internal int GetBitLength()
    {
        for (var index = LimbCount - 1; index >= 0; index--)
        {
            var limb = GetLimb(index);
            if (limb != 0) return (index * 64) + (64 - BitOperations.LeadingZeroCount(limb));
        }

        return 0;
    }

    /// <summary>
    /// Computes the exact nonnegative floor square root with the fixed-width binary restoring shift algorithm.
    /// The result <c>r</c> satisfies <c>r² ≤ value &lt; (r+1)²</c> without using BigInteger,
    /// byte-array conversion or floating-point arithmetic.
    /// </summary>
    /// <param name="value">The unsigned 2048-bit input.</param>
    /// <returns>The exact floor square root.</returns>
    public static ULong2048 IntegerSquareRootFloor(ULong2048 value)
    {
        if (value == Zero) return Zero;

        var initialBitPosition = (value.GetBitLength() - 1) & ~1;
        ULong2048Limbs bit = default;
        bit[initialBitPosition / 64] = 1UL << (initialBitPosition % 64);
        var remainder = value._limbs;
        ULong2048Limbs root = default;

        while (!IsZero(in bit))
        {
            var trial = root;
            var trialOverflow = AddLimbsInPlace(ref trial, in bit) != 0;
            if (!trialOverflow && CompareLimbs(in remainder, in trial) >= 0)
            {
                SubtractLimbs(ref remainder, in trial);
                ShiftRightOne(ref root);
                var rootOverflow = AddLimbsInPlace(ref root, in bit);
                if (rootOverflow != 0)
                {
                    throw new InvalidOperationException("ULong2048 restoring square-root invariant overflowed.");
                }
            }
            else
            {
                ShiftRightOne(ref root);
            }

            ShiftRightOne(ref bit);
            ShiftRightOne(ref bit);
        }

        var result = new ULong2048(root);
        return CorrectFloorSquareRoot(value, result);
    }

    internal void WriteFixedWidthBigEndian(Span<byte> destination)
    {
        if (destination.Length != LimbCount * sizeof(ulong)) throw new ArgumentException("Destination must be exactly 256 bytes.", nameof(destination));
        for (var index = 0; index < LimbCount; index++)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice((LimbCount - 1 - index) * sizeof(ulong), sizeof(ulong)), GetLimb(index));
        }
    }

    internal static bool TryReadFixedWidthBigEndian(ReadOnlySpan<byte> source, out ULong2048 value)
    {
        if (source.Length != LimbCount * sizeof(ulong))
        {
            value = default;
            return false;
        }

        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++)
        {
            limbs[index] = BinaryPrimitives.ReadUInt64BigEndian(source.Slice((LimbCount - 1 - index) * sizeof(ulong), sizeof(ulong)));
        }

        value = new ULong2048(limbs);
        return true;
    }

    private ulong GetLimb(int index) => _limbs[index];

    private static ULong2048 CreateMaxValue()
    {
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++) limbs[index] = ulong.MaxValue;
        return new ULong2048(limbs);
    }

    private static ULong2048 CheckedAdd(ULong2048 left, ULong2048 right)
    {
        var result = left + right;
        if (result < left) throw new OverflowException("ULong2048 addition overflow.");
        return result;
    }

    private static ULong2048 CheckedSubtract(ULong2048 left, ULong2048 right)
    {
        if (left < right) throw new OverflowException("ULong2048 subtraction overflow.");
        return left - right;
    }

    private static ULong2048 CheckedMultiply(ULong2048 left, ULong2048 right)
    {
        var exact = left.ToBigInteger() * right.ToBigInteger();
        if (exact.GetBitLength() > BitCount) throw new OverflowException("ULong2048 multiplication overflow.");
        return left * right;
    }

    private static ULong2048 AddRaw(in ULong2048 left, in ULong2048 right) => new(AddLimbs(in left._limbs, in right._limbs));

    private static ULong2048 SubtractRaw(in ULong2048 left, in ULong2048 right) => new(SubtractLimbsValue(in left._limbs, in right._limbs));

    private static ULong2048Limbs AddLimbs(in ULong2048Limbs left, in ULong2048Limbs right)
    {
        var limbs = left;
        _ = AddLimbsInPlace(ref limbs, in right);
        return limbs;
    }

    private static ulong AddLimbsInPlace(ref ULong2048Limbs left, in ULong2048Limbs right)
    {
        ulong carry = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var leftValue = left[index];
            var sum = leftValue + right[index];
            var carryFromOperands = sum < leftValue ? 1UL : 0UL;
            var result = sum + carry;
            left[index] = result;
            carry = carryFromOperands | (result < sum ? 1UL : 0UL);
        }

        return carry;
    }

    private static ULong2048Limbs SubtractLimbsValue(in ULong2048Limbs left, in ULong2048Limbs right)
    {
        var result = left;
        SubtractLimbs(ref result, in right);
        return result;
    }

    private static ULong2048 MultiplyRaw(in ULong2048 left, in ULong2048 right)
    {
        ULong2048Limbs limbs = default;
        for (var leftIndex = 0; leftIndex < LimbCount; leftIndex++)
        {
            UInt128 carry = 0;
            var leftLimb = left.GetLimb(leftIndex);
            for (var rightIndex = 0; leftIndex + rightIndex < LimbCount; rightIndex++)
            {
                var target = leftIndex + rightIndex;
                var total = ((UInt128)leftLimb * right.GetLimb(rightIndex)) + limbs[target] + carry;
                limbs[target] = (ulong)total;
                carry = total >> 64;
            }
        }

        return new ULong2048(limbs);
    }

    private static ULong2048 DivideUnsigned(in ULong2048 numerator, in ULong2048 denominator, out ULong2048 remainder)
    {
        if (denominator == Zero) throw new DivideByZeroException();
        ULong2048Limbs quotient = default;
        ULong2048Limbs partialRemainder = default;

        for (var bit = BitCount - 1; bit >= 0; bit--)
        {
            ShiftLeftOne(ref partialRemainder);
            partialRemainder[0] |= (numerator.GetLimb(bit / 64) >> (bit % 64)) & 1UL;
            if (CompareLimbs(in partialRemainder, in denominator._limbs) >= 0)
            {
                SubtractLimbs(ref partialRemainder, in denominator._limbs);
                quotient[bit / 64] |= 1UL << (bit % 64);
            }
        }

        remainder = new ULong2048(partialRemainder);
        return new ULong2048(quotient);
    }

    private static ULong2048 ModPowGeneric(ULong2048 value, ULong2048 exponent, ULong2048 modulus)
    {
        var result = (One % modulus)._limbs;
        var factor = (value % modulus)._limbs;
        var remainingExponent = exponent._limbs;
        while (!IsZero(in remainingExponent))
        {
            if ((remainingExponent[0] & 1UL) != 0) result = MultiplyModuloGenericLimbs(in result, in factor, in modulus._limbs);
            ShiftRightOne(ref remainingExponent);
            if (!IsZero(in remainingExponent)) factor = MultiplyModuloGenericLimbs(in factor, in factor, in modulus._limbs);
        }

        return new ULong2048(result);
    }

    private static ULong2048Limbs MultiplyModuloGenericLimbs(in ULong2048Limbs left, in ULong2048Limbs right, in ULong2048Limbs modulus)
    {
        var factor = left;
        var multiplier = right;
        ULong2048Limbs result = default;

        while (!IsZero(in multiplier))
        {
            if ((multiplier[0] & 1UL) != 0) AddModuloInPlace(ref result, in factor, in modulus);
            ShiftRightOne(ref multiplier);
            if (!IsZero(in multiplier)) AddModuloInPlace(ref factor, in factor, in modulus);
        }

        return result;
    }

    private static void AddModuloInPlace(ref ULong2048Limbs left, in ULong2048Limbs right, in ULong2048Limbs modulus)
    {
        // Both inputs are reduced. If the raw 2048-bit sum carries, or is at least modulus, one subtraction yields the exact reduced sum.
        var carry = AddLimbsInPlace(ref left, in right);
        if (carry != 0 || CompareLimbs(in left, in modulus) >= 0) SubtractLimbs(ref left, in modulus);
    }

    private static ulong ComputeMontgomeryN0Prime(ulong modulusLeastSignificantLimb)
    {
        // Newton iteration in Z/(2^64): inverse doubles the valid bit count per iteration.
        ulong inverse = 1;
        for (var iteration = 0; iteration < 6; iteration++) inverse *= 2UL - (modulusLeastSignificantLimb * inverse);
        return 0UL - inverse;
    }

    private static ULong2048Limbs ComputeMontgomeryRSquared(in ULong2048Limbs modulus)
    {
        // R = 2^2048. Repeated exact doubling derives R^2 mod modulus using only inline limbs.
        ULong2048Limbs result = default;
        result[0] = 1;
        for (var bit = 0; bit < BitCount * 2; bit++) AddModuloInPlace(ref result, in result, in modulus);
        return result;
    }

    private static ULong2048Limbs MontgomeryMultiplyLimbs(in ULong2048Limbs left, in ULong2048Limbs right, in ULong2048Limbs modulus, ulong n0Prime)
    {
        Span<ulong> workspace = stackalloc ulong[(LimbCount * 2) + 1];
        for (var leftIndex = 0; leftIndex < LimbCount; leftIndex++)
        {
            UInt128 carry = 0;
            var leftLimb = left[leftIndex];
            for (var rightIndex = 0; rightIndex < LimbCount; rightIndex++)
            {
                var target = leftIndex + rightIndex;
                var total = ((UInt128)leftLimb * right[rightIndex]) + workspace[target] + carry;
                workspace[target] = (ulong)total;
                carry = total >> 64;
            }

            PropagateWorkspaceCarry(workspace, leftIndex + LimbCount, carry);
        }

        for (var index = 0; index < LimbCount; index++)
        {
            var reductionFactor = unchecked(workspace[index] * n0Prime);
            UInt128 carry = 0;
            for (var modulusIndex = 0; modulusIndex < LimbCount; modulusIndex++)
            {
                var target = index + modulusIndex;
                var total = ((UInt128)reductionFactor * modulus[modulusIndex]) + workspace[target] + carry;
                workspace[target] = (ulong)total;
                carry = total >> 64;
            }

            PropagateWorkspaceCarry(workspace, index + LimbCount, carry);
        }

        ULong2048Limbs result = default;
        for (var index = 0; index < LimbCount; index++) result[index] = workspace[index + LimbCount];
        if (workspace[LimbCount * 2] != 0 || CompareLimbs(in result, in modulus) >= 0)
        {
            SubtractLimbs(ref result, in modulus);
        }

        return result;
    }

    private static void PropagateWorkspaceCarry(Span<ulong> workspace, int startIndex, UInt128 carry)
    {
        var index = startIndex;
        while (carry != 0)
        {
            if (index >= workspace.Length) throw new InvalidOperationException("Montgomery workspace overflow.");
            var total = ((UInt128)workspace[index]) + carry;
            workspace[index] = (ulong)total;
            carry = total >> 64;
            index++;
        }
    }

    private static ULong2048 And(in ULong2048 left, in ULong2048 right)
    {
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++) limbs[index] = left.GetLimb(index) & right.GetLimb(index);
        return new ULong2048(limbs);
    }

    private static ULong2048 Or(in ULong2048 left, in ULong2048 right)
    {
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++) limbs[index] = left.GetLimb(index) | right.GetLimb(index);
        return new ULong2048(limbs);
    }

    private static ULong2048 Xor(in ULong2048 left, in ULong2048 right)
    {
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++) limbs[index] = left.GetLimb(index) ^ right.GetLimb(index);
        return new ULong2048(limbs);
    }

    private static ULong2048 ShiftLeft(in ULong2048 value, int shiftAmount)
    {
        if (shiftAmount < 0) return ShiftRight(in value, -shiftAmount);
        if (shiftAmount >= BitCount) return Zero;
        var limbShift = shiftAmount / 64;
        var bitShift = shiftAmount % 64;
        ULong2048Limbs limbs = default;
        for (var index = LimbCount - 1; index >= limbShift; index--)
        {
            limbs[index] = value.GetLimb(index - limbShift) << bitShift;
            if (bitShift != 0 && index > limbShift) limbs[index] |= value.GetLimb(index - limbShift - 1) >> (64 - bitShift);
        }

        return new ULong2048(limbs);
    }

    private static ULong2048 ShiftRight(in ULong2048 value, int shiftAmount)
    {
        if (shiftAmount < 0) return ShiftLeft(in value, -shiftAmount);
        if (shiftAmount >= BitCount) return Zero;
        var limbShift = shiftAmount / 64;
        var bitShift = shiftAmount % 64;
        ULong2048Limbs limbs = default;
        for (var index = 0; index < LimbCount; index++)
        {
            var source = index + limbShift;
            if (source >= LimbCount) continue;
            limbs[index] = value.GetLimb(source) >> bitShift;
            if (bitShift != 0 && source + 1 < LimbCount) limbs[index] |= value.GetLimb(source + 1) << (64 - bitShift);
        }

        return new ULong2048(limbs);
    }

    private static ULong2048 CorrectFloorSquareRoot(ULong2048 value, ULong2048 root)
    {
        var square = root * root;
        if (square > value)
        {
            root -= One;
            square = root * root;
        }

        var next = root + One;
        if (next.GetBitLength() <= BitCount / 2 && (next * next) <= value)
        {
            root = next;
        }

        return root;
    }

    private static bool IsZero(in ULong2048Limbs value)
    {
        for (var index = 0; index < LimbCount; index++) if (value[index] != 0) return false;
        return true;
    }

    private static void ShiftRightOne(ref ULong2048Limbs value)
    {
        ulong carry = 0;
        for (var index = LimbCount - 1; index >= 0; index--)
        {
            var nextCarry = value[index] << 63;
            value[index] = (value[index] >> 1) | carry;
            carry = nextCarry;
        }
    }

    private static void ShiftLeftOne(ref ULong2048Limbs value)
    {
        ulong carry = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var nextCarry = value[index] >> 63;
            value[index] = (value[index] << 1) | carry;
            carry = nextCarry;
        }
    }

    private static int CompareLimbs(in ULong2048Limbs left, in ULong2048Limbs right)
    {
        for (var index = LimbCount - 1; index >= 0; index--)
        {
            if (left[index] == right[index]) continue;
            return left[index] < right[index] ? -1 : 1;
        }

        return 0;
    }

    private static void SubtractLimbs(ref ULong2048Limbs left, in ULong2048Limbs right)
    {
        ulong borrow = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var rightValue = right[index];
            var rightWithBorrow = rightValue + borrow;
            var borrowFromAdd = rightWithBorrow < rightValue ? 1UL : 0UL;
            var leftValue = left[index];
            left[index] = leftValue - rightWithBorrow;
            borrow = leftValue < rightWithBorrow || borrowFromAdd != 0 ? 1UL : 0UL;
        }
    }
}
