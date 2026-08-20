#nullable enable
#pragma warning disable CS1591 // Mandatory System.Numerics interface members inherit documented framework contracts; Int2048-specific semantics are versioned in INT2048_DESIGN.md.

using System.Globalization;
using System.Numerics;
using System.Text;

namespace Ricis.Core.Numerics;

/// <summary>
/// Represents a signed fixed-width 2048-bit integer using thirty-two little-endian 64-bit limbs.
/// </summary>
public readonly struct Int2048 :
    INumber<Int2048>,
    IComparable,
    IComparable<Int2048>,
    IEquatable<Int2048>,
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    private const int LimbCount = 32;
    private const int SignLimbIndex = LimbCount - 1;
    private readonly ulong[] _limbs;

    private Int2048(ulong[] limbs, bool takeOwnership)
    {
        _limbs = takeOwnership ? limbs : (ulong[])limbs.Clone();
    }

    /// <summary>Initializes an Int2048 from a signed 64-bit value with sign extension.</summary>
    public Int2048(long value)
    {
        _limbs = new ulong[LimbCount];
        _limbs[0] = unchecked((ulong)value);
        var signExtension = value < 0 ? ulong.MaxValue : 0UL;
        for (var index = 1; index < LimbCount; index++)
        {
            _limbs[index] = signExtension;
        }
    }

    /// <summary>Initializes an Int2048 from an unsigned 64-bit value.</summary>
    public Int2048(ulong value)
    {
        _limbs = new ulong[LimbCount];
        _limbs[0] = value;
    }

    /// <summary>Gets the smallest representable value, −2^2047.</summary>
    public static Int2048 MinValue { get; } = CreateMinValue();

    /// <summary>Gets the largest representable value, 2^2047−1.</summary>
    public static Int2048 MaxValue { get; } = CreateMaxValue();

    /// <summary>Gets the additive identity.</summary>
    public static Int2048 Zero => default;

    /// <summary>Gets the multiplicative identity.</summary>
    public static Int2048 One => new(1L);

    /// <summary>Gets negative one.</summary>
    public static Int2048 NegativeOne => new(-1L);

    /// <summary>Gets the radix of the binary representation.</summary>
    public static int Radix => 2;

    /// <summary>Gets the additive identity.</summary>
    public static Int2048 AdditiveIdentity => Zero;

    /// <summary>Gets the multiplicative identity.</summary>
    public static Int2048 MultiplicativeIdentity => One;

    /// <summary>Returns true when the value is negative.</summary>
    public bool IsNegativeValue => (GetLimb(SignLimbIndex) & (1UL << 63)) != 0;

    /// <summary>Returns the signed value as a diagnostic BigInteger conversion.</summary>
    public BigInteger ToBigInteger()
    {
        var bytes = new byte[(LimbCount * sizeof(ulong)) + 1];
        for (var index = 0; index < LimbCount; index++)
        {
            BitConverter.TryWriteBytes(bytes.AsSpan(index * sizeof(ulong), sizeof(ulong)), GetLimb(index));
        }

        bytes[^1] = IsNegativeValue ? byte.MaxValue : (byte)0;
        return new BigInteger(bytes, isUnsigned: false, isBigEndian: false);
    }

    /// <summary>Converts an in-range BigInteger to the custom fixed-width representation.</summary>
    public static Int2048 FromBigInteger(BigInteger value)
    {
        if (value < MinValue.ToBigInteger() || value > MaxValue.ToBigInteger())
        {
            throw new OverflowException("Value is outside Int2048 range.");
        }

        var bytes = value.ToByteArray(isUnsigned: false, isBigEndian: false);
        var limbs = new ulong[LimbCount];
        var signExtension = value.Sign < 0 ? byte.MaxValue : (byte)0;
        var fixedBytes = new byte[LimbCount * sizeof(ulong)];
        fixedBytes.AsSpan().Fill(signExtension);
        bytes.AsSpan(0, Math.Min(bytes.Length, fixedBytes.Length)).CopyTo(fixedBytes);
        for (var index = 0; index < LimbCount; index++)
        {
            limbs[index] = BitConverter.ToUInt64(fixedBytes, index * sizeof(ulong));
        }

        return new Int2048(limbs, takeOwnership: true);
    }

    public static implicit operator Int2048(long value) => new(value);
    public static implicit operator Int2048(ulong value) => new(value);
    public static explicit operator BigInteger(Int2048 value) => value.ToBigInteger();
    public static explicit operator Int2048(BigInteger value) => FromBigInteger(value);

    public static Int2048 operator +(Int2048 left, Int2048 right) => AddRaw(left, right);

    public static Int2048 operator checked +(Int2048 left, Int2048 right)
    {
        var result = AddRaw(left, right);
        if (left.IsNegativeValue == right.IsNegativeValue && result.IsNegativeValue != left.IsNegativeValue)
        {
            throw new OverflowException("Int2048 addition overflow.");
        }

        return result;
    }

    public static Int2048 operator -(Int2048 left, Int2048 right) => AddRaw(left, NegateRaw(right));

    public static Int2048 operator checked -(Int2048 left, Int2048 right)
    {
        var result = left - right;
        if (left.IsNegativeValue != right.IsNegativeValue && result.IsNegativeValue != left.IsNegativeValue)
        {
            throw new OverflowException("Int2048 subtraction overflow.");
        }

        return result;
    }

    public static Int2048 operator +(Int2048 value) => value;
    public static Int2048 operator -(Int2048 value) => NegateRaw(value);

    public static Int2048 operator checked -(Int2048 value)
    {
        if (value == MinValue)
        {
            throw new OverflowException("Int2048 negation overflow.");
        }

        return NegateRaw(value);
    }

    public static Int2048 operator ++(Int2048 value) => value + One;
    public static Int2048 operator checked ++(Int2048 value) => checked(value + One);
    public static Int2048 operator --(Int2048 value) => value - One;
    public static Int2048 operator checked --(Int2048 value) => checked(value - One);

    public static Int2048 operator *(Int2048 left, Int2048 right) => MultiplyRaw(left, right);

    public static Int2048 operator checked *(Int2048 left, Int2048 right)
    {
        var product = left * right;
        var expected = left.ToBigInteger() * right.ToBigInteger();
        if (expected < MinValue.ToBigInteger() || expected > MaxValue.ToBigInteger())
        {
            throw new OverflowException("Int2048 multiplication overflow.");
        }

        return product;
    }

    public static Int2048 operator /(Int2048 left, Int2048 right)
    {
        if (right == Zero)
        {
            throw new DivideByZeroException();
        }

        if (left == MinValue && right == NegativeOne)
        {
            throw new OverflowException("Int2048 division overflow.");
        }

        var quotientMagnitude = DivideUnsigned(AbsoluteRaw(left), AbsoluteRaw(right), out _);
        var quotient = new Int2048(quotientMagnitude, takeOwnership: true);
        return left.IsNegativeValue ^ right.IsNegativeValue ? NegateRaw(quotient) : quotient;
    }

    public static Int2048 operator %(Int2048 left, Int2048 right)
    {
        if (right == Zero)
        {
            throw new DivideByZeroException();
        }

        _ = DivideUnsigned(AbsoluteRaw(left), AbsoluteRaw(right), out var remainderMagnitude);
        var remainder = new Int2048(remainderMagnitude, takeOwnership: true);
        return left.IsNegativeValue ? NegateRaw(remainder) : remainder;
    }

    public static Int2048 operator ~(Int2048 value)
    {
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++)
        {
            limbs[index] = ~value.GetLimb(index);
        }

        return new Int2048(limbs, takeOwnership: true);
    }

    public static Int2048 operator &(Int2048 left, Int2048 right) => Bitwise(left, right, static (x, y) => x & y);
    public static Int2048 operator |(Int2048 left, Int2048 right) => Bitwise(left, right, static (x, y) => x | y);
    public static Int2048 operator ^(Int2048 left, Int2048 right) => Bitwise(left, right, static (x, y) => x ^ y);

    public static Int2048 operator <<(Int2048 value, int shiftAmount) => ShiftLeft(value, shiftAmount);
    public static Int2048 operator >>(Int2048 value, int shiftAmount) => ShiftRight(value, shiftAmount, signExtend: true);
    public static Int2048 operator >>>(Int2048 value, int shiftAmount) => ShiftRight(value, shiftAmount, signExtend: false);

    public static bool operator ==(Int2048 left, Int2048 right) => left.Equals(right);
    public static bool operator !=(Int2048 left, Int2048 right) => !left.Equals(right);
    public static bool operator <(Int2048 left, Int2048 right) => left.CompareTo(right) < 0;
    public static bool operator <=(Int2048 left, Int2048 right) => left.CompareTo(right) <= 0;
    public static bool operator >(Int2048 left, Int2048 right) => left.CompareTo(right) > 0;
    public static bool operator >=(Int2048 left, Int2048 right) => left.CompareTo(right) >= 0;

    public int CompareTo(Int2048 other)
    {
        if (IsNegativeValue != other.IsNegativeValue)
        {
            return IsNegativeValue ? -1 : 1;
        }

        for (var index = SignLimbIndex; index >= 0; index--)
        {
            var left = GetLimb(index);
            var right = other.GetLimb(index);
            if (left != right)
            {
                return left < right ? -1 : 1;
            }
        }

        return 0;
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Int2048 other) return CompareTo(other);
        throw new ArgumentException("Object must be an Int2048.", nameof(obj));
    }

    public bool Equals(Int2048 other)
    {
        for (var index = 0; index < LimbCount; index++)
        {
            if (GetLimb(index) != other.GetLimb(index)) return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is Int2048 other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var index = 0; index < LimbCount; index++) hash.Add(GetLimb(index));
        return hash.ToHashCode();
    }

    public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        ToBigInteger().ToString(string.IsNullOrEmpty(format) ? "G" : format, formatProvider);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        ToBigInteger().TryFormat(destination, out charsWritten, format, provider);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var text = ToString(format.ToString(), provider);
        var required = Encoding.UTF8.GetByteCount(text);
        if (required > utf8Destination.Length)
        {
            bytesWritten = 0;
            return false;
        }

        bytesWritten = Encoding.UTF8.GetBytes(text, utf8Destination);
        return true;
    }

    public static Int2048 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);
    public static Int2048 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);
    public static Int2048 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Integer, provider);
    public static bool TryParse(string? s, IFormatProvider? provider, out Int2048 result) => TryParse(s, NumberStyles.Integer, provider, out result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int2048 result) => TryParse(s, NumberStyles.Integer, provider, out result);
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Int2048 result) => TryParse(utf8Text, NumberStyles.Integer, provider, out result);

    public static Int2048 Parse(string s, NumberStyles style, IFormatProvider? provider) =>
        FromBigInteger(BigInteger.Parse(s, style, provider));

    public static Int2048 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) =>
        FromBigInteger(BigInteger.Parse(s, style, provider));

    public static Int2048 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider) =>
        Parse(Encoding.UTF8.GetString(utf8Text), style, provider);

    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Int2048 result)
    {
        if (BigInteger.TryParse(s, style, provider, out var value) && IsInRange(value))
        {
            result = FromBigInteger(value);
            return true;
        }

        result = Zero;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Int2048 result)
    {
        if (BigInteger.TryParse(s, style, provider, out var value) && IsInRange(value))
        {
            result = FromBigInteger(value);
            return true;
        }

        result = Zero;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Int2048 result)
    {
        return TryParse(Encoding.UTF8.GetString(utf8Text), style, provider, out result);
    }

    public static Int2048 Abs(Int2048 value) => value.IsNegativeValue ? checked(-value) : value;
    public static bool IsCanonical(Int2048 value) => true;
    public static bool IsComplexNumber(Int2048 value) => false;
    public static bool IsEvenInteger(Int2048 value) => (value.GetLimb(0) & 1) == 0;
    public static bool IsFinite(Int2048 value) => true;
    public static bool IsImaginaryNumber(Int2048 value) => false;
    public static bool IsInfinity(Int2048 value) => false;
    public static bool IsInteger(Int2048 value) => true;
    public static bool IsNaN(Int2048 value) => false;
    public static bool IsNegative(Int2048 value) => value.IsNegativeValue;
    public static bool IsNegativeInfinity(Int2048 value) => false;
    public static bool IsNormal(Int2048 value) => value != Zero;
    public static bool IsOddInteger(Int2048 value) => (value.GetLimb(0) & 1) != 0;
    public static bool IsPositive(Int2048 value) => !value.IsNegativeValue;
    public static bool IsPositiveInfinity(Int2048 value) => false;
    public static bool IsRealNumber(Int2048 value) => true;
    public static bool IsSubnormal(Int2048 value) => false;
    public static bool IsZero(Int2048 value) => value == Zero;

    public static Int2048 MaxMagnitude(Int2048 x, Int2048 y) => CompareMagnitude(x, y) >= 0 ? x : y;
    public static Int2048 MaxMagnitudeNumber(Int2048 x, Int2048 y) => MaxMagnitude(x, y);
    public static Int2048 MinMagnitude(Int2048 x, Int2048 y) => CompareMagnitude(x, y) <= 0 ? x : y;
    public static Int2048 MinMagnitudeNumber(Int2048 x, Int2048 y) => MinMagnitude(x, y);
    public static Int2048 Max(Int2048 x, Int2048 y) => x >= y ? x : y;
    public static Int2048 MaxNumber(Int2048 x, Int2048 y) => Max(x, y);
    public static Int2048 Min(Int2048 x, Int2048 y) => x <= y ? x : y;
    public static Int2048 MinNumber(Int2048 x, Int2048 y) => Min(x, y);

    public static Int2048 Clamp(Int2048 value, Int2048 min, Int2048 max)
    {
        if (min > max) throw new ArgumentException("Minimum cannot exceed maximum.", nameof(min));
        return value < min ? min : value > max ? max : value;
    }

    public static Int2048 CopySign(Int2048 value, Int2048 sign)
    {
        var magnitude = AbsoluteRaw(value);
        var result = new Int2048(magnitude, takeOwnership: true);
        return sign.IsNegativeValue ? NegateRaw(result) : result;
    }

    public static int Sign(Int2048 value) => value == Zero ? 0 : value.IsNegativeValue ? -1 : 1;

    public static Int2048 CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        if (TryConvertFromChecked(value, out var result)) return result;
        throw new NotSupportedException($"Conversion from {typeof(TOther)} is not supported.");
    }

    public static Int2048 CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        if (TryConvertFromSaturating(value, out var result)) return result;
        throw new NotSupportedException($"Conversion from {typeof(TOther)} is not supported.");
    }

    public static Int2048 CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        if (TryConvertFromTruncating(value, out var result)) return result;
        throw new NotSupportedException($"Conversion from {typeof(TOther)} is not supported.");
    }

    public static bool TryConvertFromChecked<TOther>(TOther value, out Int2048 result) where TOther : INumberBase<TOther> =>
        TryConvertFrom(value, ConversionMode.Checked, out result);

    public static bool TryConvertFromSaturating<TOther>(TOther value, out Int2048 result) where TOther : INumberBase<TOther> =>
        TryConvertFrom(value, ConversionMode.Saturating, out result);

    public static bool TryConvertFromTruncating<TOther>(TOther value, out Int2048 result) where TOther : INumberBase<TOther> =>
        TryConvertFrom(value, ConversionMode.Truncating, out result);

    public static bool TryConvertToChecked<TOther>(Int2048 value, out TOther result) where TOther : INumberBase<TOther> =>
        TryConvertTo(value, ConversionMode.Checked, out result);

    public static bool TryConvertToSaturating<TOther>(Int2048 value, out TOther result) where TOther : INumberBase<TOther> =>
        TryConvertTo(value, ConversionMode.Saturating, out result);

    public static bool TryConvertToTruncating<TOther>(Int2048 value, out TOther result) where TOther : INumberBase<TOther> =>
        TryConvertTo(value, ConversionMode.Truncating, out result);

    private ulong GetLimb(int index) => _limbs is null ? 0UL : _limbs[index];

    private static Int2048 CreateMinValue()
    {
        var limbs = new ulong[LimbCount];
        limbs[SignLimbIndex] = 1UL << 63;
        return new Int2048(limbs, takeOwnership: true);
    }

    private static Int2048 CreateMaxValue()
    {
        var limbs = new ulong[LimbCount];
        Array.Fill(limbs, ulong.MaxValue);
        limbs[SignLimbIndex] = ulong.MaxValue >> 1;
        return new Int2048(limbs, takeOwnership: true);
    }

    private static Int2048 AddRaw(Int2048 left, Int2048 right)
    {
        var limbs = new ulong[LimbCount];
        ulong carry = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var leftValue = left.GetLimb(index);
            var rightValue = right.GetLimb(index);
            var sum = leftValue + rightValue;
            var carryFromOperands = sum < leftValue ? 1UL : 0UL;
            var result = sum + carry;
            var carryFromCarry = result < sum ? 1UL : 0UL;
            limbs[index] = result;
            carry = carryFromOperands | carryFromCarry;
        }

        return new Int2048(limbs, takeOwnership: true);
    }

    private static Int2048 NegateRaw(Int2048 value) => AddRaw(~value, One);

    private static Int2048 MultiplyRaw(Int2048 left, Int2048 right)
    {
        var limbs = new ulong[LimbCount];
        for (var leftIndex = 0; leftIndex < LimbCount; leftIndex++)
        {
            UInt128 carry = 0;
            var leftLimb = left.GetLimb(leftIndex);
            for (var rightIndex = 0; rightIndex + leftIndex < LimbCount; rightIndex++)
            {
                var targetIndex = leftIndex + rightIndex;
                var total = ((UInt128)leftLimb * right.GetLimb(rightIndex)) + limbs[targetIndex] + carry;
                limbs[targetIndex] = (ulong)total;
                carry = total >> 64;
            }
        }

        return new Int2048(limbs, takeOwnership: true);
    }

    private static ulong[] AbsoluteRaw(Int2048 value)
    {
        var magnitude = new ulong[LimbCount];
        if (!value.IsNegativeValue)
        {
            for (var index = 0; index < LimbCount; index++) magnitude[index] = value.GetLimb(index);
            return magnitude;
        }

        ulong carry = 1;
        for (var index = 0; index < LimbCount; index++)
        {
            var inverted = ~value.GetLimb(index);
            var sum = inverted + carry;
            magnitude[index] = sum;
            carry = sum < inverted ? 1UL : 0UL;
        }

        return magnitude;
    }

    private static ulong[] DivideUnsigned(ulong[] numerator, ulong[] denominator, out ulong[] remainder)
    {
        if (IsZero(denominator)) throw new DivideByZeroException();
        var quotient = new ulong[LimbCount];
        remainder = new ulong[LimbCount];

        for (var bit = (LimbCount * 64) - 1; bit >= 0; bit--)
        {
            ShiftLeftOne(remainder);
            remainder[0] |= (numerator[bit / 64] >> (bit % 64)) & 1UL;
            if (CompareUnsigned(remainder, denominator) >= 0)
            {
                SubtractUnsigned(remainder, denominator);
                quotient[bit / 64] |= 1UL << (bit % 64);
            }
        }

        return quotient;
    }

    private static bool IsZero(ulong[] value)
    {
        for (var index = 0; index < LimbCount; index++) if (value[index] != 0) return false;
        return true;
    }

    private static int CompareUnsigned(ulong[] left, ulong[] right)
    {
        for (var index = SignLimbIndex; index >= 0; index--)
        {
            if (left[index] == right[index]) continue;
            return left[index] < right[index] ? -1 : 1;
        }

        return 0;
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

    private static void SubtractUnsigned(ulong[] left, ulong[] right)
    {
        ulong borrow = 0;
        for (var index = 0; index < LimbCount; index++)
        {
            var rightWithBorrow = right[index] + borrow;
            var borrowFromAdd = rightWithBorrow < right[index] ? 1UL : 0UL;
            var leftValue = left[index];
            left[index] = leftValue - rightWithBorrow;
            borrow = (leftValue < rightWithBorrow || borrowFromAdd != 0) ? 1UL : 0UL;
        }
    }

    private static Int2048 Bitwise(Int2048 left, Int2048 right, Func<ulong, ulong, ulong> operation)
    {
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++) limbs[index] = operation(left.GetLimb(index), right.GetLimb(index));
        return new Int2048(limbs, takeOwnership: true);
    }

    private static Int2048 ShiftLeft(Int2048 value, int shiftAmount)
    {
        if (shiftAmount < 0) return ShiftRight(value, -shiftAmount, signExtend: true);
        if (shiftAmount >= LimbCount * 64) return Zero;
        var limbShift = shiftAmount / 64;
        var bitShift = shiftAmount % 64;
        var limbs = new ulong[LimbCount];
        for (var index = LimbCount - 1; index >= limbShift; index--)
        {
            var source = value.GetLimb(index - limbShift);
            limbs[index] |= source << bitShift;
            if (bitShift != 0 && index > limbShift)
            {
                limbs[index] |= value.GetLimb(index - limbShift - 1) >> (64 - bitShift);
            }
        }

        return new Int2048(limbs, takeOwnership: true);
    }

    private static Int2048 ShiftRight(Int2048 value, int shiftAmount, bool signExtend)
    {
        if (shiftAmount < 0) return ShiftLeft(value, -shiftAmount);
        var fill = signExtend && value.IsNegativeValue ? ulong.MaxValue : 0UL;
        if (shiftAmount >= LimbCount * 64)
        {
            var allFill = new ulong[LimbCount];
            Array.Fill(allFill, fill);
            return new Int2048(allFill, takeOwnership: true);
        }

        var limbShift = shiftAmount / 64;
        var bitShift = shiftAmount % 64;
        var limbs = new ulong[LimbCount];
        for (var index = 0; index < LimbCount; index++)
        {
            var sourceIndex = index + limbShift;
            var low = sourceIndex < LimbCount ? value.GetLimb(sourceIndex) : fill;
            limbs[index] = low >> bitShift;
            if (bitShift != 0)
            {
                var high = sourceIndex + 1 < LimbCount ? value.GetLimb(sourceIndex + 1) : fill;
                limbs[index] |= high << (64 - bitShift);
            }
        }

        return new Int2048(limbs, takeOwnership: true);
    }

    private static int CompareMagnitude(Int2048 left, Int2048 right)
    {
        var leftMagnitude = AbsoluteRaw(left);
        var rightMagnitude = AbsoluteRaw(right);
        return CompareUnsigned(leftMagnitude, rightMagnitude);
    }

    private static bool IsInRange(BigInteger value) => value >= MinValue.ToBigInteger() && value <= MaxValue.ToBigInteger();

    private static bool TryConvertFrom<TOther>(TOther value, ConversionMode mode, out Int2048 result) where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(Int2048))
        {
            result = (Int2048)(object)value;
            return true;
        }

        if (typeof(TOther) == typeof(BigInteger)) return ConvertFromBigInteger((BigInteger)(object)value, mode, out result);
        if (typeof(TOther) == typeof(byte)) return ConvertFromBigInteger((byte)(object)value, mode, out result);
        if (typeof(TOther) == typeof(sbyte)) return ConvertFromBigInteger((sbyte)(object)value, mode, out result);
        if (typeof(TOther) == typeof(short)) return ConvertFromBigInteger((short)(object)value, mode, out result);
        if (typeof(TOther) == typeof(ushort)) return ConvertFromBigInteger((ushort)(object)value, mode, out result);
        if (typeof(TOther) == typeof(int)) return ConvertFromBigInteger((int)(object)value, mode, out result);
        if (typeof(TOther) == typeof(uint)) return ConvertFromBigInteger((uint)(object)value, mode, out result);
        if (typeof(TOther) == typeof(long)) return ConvertFromBigInteger((long)(object)value, mode, out result);
        if (typeof(TOther) == typeof(ulong)) return ConvertFromBigInteger((ulong)(object)value, mode, out result);
        if (typeof(TOther) == typeof(Int128)) return ConvertFromBigInteger((Int128)(object)value, mode, out result);
        if (typeof(TOther) == typeof(UInt128)) return ConvertFromBigInteger((UInt128)(object)value, mode, out result);

        result = Zero;
        return false;
    }

    private static bool ConvertFromBigInteger(BigInteger value, ConversionMode mode, out Int2048 result)
    {
        if (IsInRange(value))
        {
            result = FromBigInteger(value);
            return true;
        }

        if (mode == ConversionMode.Checked) throw new OverflowException("Value is outside Int2048 range.");
        if (mode == ConversionMode.Saturating)
        {
            result = value < MinValue.ToBigInteger() ? MinValue : MaxValue;
            return true;
        }

        var modulus = BigInteger.One << (LimbCount * 64);
        var truncated = value % modulus;
        if (truncated < 0) truncated += modulus;
        if (truncated >= (BigInteger.One << ((LimbCount * 64) - 1))) truncated -= modulus;
        result = FromBigInteger(truncated);
        return true;
    }

    private static bool TryConvertTo<TOther>(Int2048 value, ConversionMode mode, out TOther result) where TOther : INumberBase<TOther>
    {
        if (typeof(TOther) == typeof(Int2048)) { result = (TOther)(object)value; return true; }
        if (typeof(TOther) == typeof(BigInteger)) { result = (TOther)(object)value.ToBigInteger(); return true; }
        return TryConvertToPrimitive(value.ToBigInteger(), mode, out result);
    }

    private static bool TryConvertToPrimitive<TOther>(BigInteger value, ConversionMode mode, out TOther result) where TOther : INumberBase<TOther>
    {
        try
        {
            object? converted = typeof(TOther) == typeof(byte) ? ConvertValue<byte>(value, mode) :
                typeof(TOther) == typeof(sbyte) ? ConvertValue<sbyte>(value, mode) :
                typeof(TOther) == typeof(short) ? ConvertValue<short>(value, mode) :
                typeof(TOther) == typeof(ushort) ? ConvertValue<ushort>(value, mode) :
                typeof(TOther) == typeof(int) ? ConvertValue<int>(value, mode) :
                typeof(TOther) == typeof(uint) ? ConvertValue<uint>(value, mode) :
                typeof(TOther) == typeof(long) ? ConvertValue<long>(value, mode) :
                typeof(TOther) == typeof(ulong) ? ConvertValue<ulong>(value, mode) : null;
            if (converted is null)
            {
                result = default!;
                return false;
            }

            result = (TOther)converted;
            return true;
        }
        catch (OverflowException) when (mode == ConversionMode.Checked)
        {
            throw;
        }
    }

    private static T ConvertValue<T>(BigInteger value, ConversionMode mode) where T : struct
    {
        if (mode == ConversionMode.Checked) return checked((T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture));
        if (mode == ConversionMode.Saturating)
        {
            if (typeof(T) == typeof(byte)) return (T)(object)(value < byte.MinValue ? byte.MinValue : value > byte.MaxValue ? byte.MaxValue : (byte)value);
            if (typeof(T) == typeof(sbyte)) return (T)(object)(value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value);
            if (typeof(T) == typeof(short)) return (T)(object)(value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)value);
            if (typeof(T) == typeof(ushort)) return (T)(object)(value < ushort.MinValue ? ushort.MinValue : value > ushort.MaxValue ? ushort.MaxValue : (ushort)value);
            if (typeof(T) == typeof(int)) return (T)(object)(value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)value);
            if (typeof(T) == typeof(uint)) return (T)(object)(value < uint.MinValue ? uint.MinValue : value > uint.MaxValue ? uint.MaxValue : (uint)value);
            if (typeof(T) == typeof(long)) return (T)(object)(value < long.MinValue ? long.MinValue : value > long.MaxValue ? long.MaxValue : (long)value);
            if (typeof(T) == typeof(ulong)) return (T)(object)(value < ulong.MinValue ? ulong.MinValue : value > ulong.MaxValue ? ulong.MaxValue : (ulong)value);
        }

        if (typeof(T) == typeof(byte)) return (T)(object)(byte)value;
        if (typeof(T) == typeof(sbyte)) return (T)(object)(sbyte)value;
        if (typeof(T) == typeof(short)) return (T)(object)(short)value;
        if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)value;
        if (typeof(T) == typeof(int)) return (T)(object)(int)value;
        if (typeof(T) == typeof(uint)) return (T)(object)(uint)value;
        if (typeof(T) == typeof(long)) return (T)(object)(long)value;
        if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)value;
        throw new NotSupportedException();
    }

    private enum ConversionMode
    {
        Checked,
        Saturating,
        Truncating,
    }
}
