using System.Globalization;
using System.Numerics;
using Ricis.Core.Numerics;

internal static class Int2048Suite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("I2048-01: carry проходит через 64-bit limb boundary", AdditionPropagatesCarryAcrossLimb),
        ("I2048-02: subtraction выполняет borrow и перенос знака", SubtractionPropagatesBorrowAndSign),
        ("I2048-03: ordinary and checked addition respect 2048-bit boundary", AdditionOverflowContracts),
        ("I2048-04: unary sign transfer preserves MinValue contracts", NegationMinValueContracts),
        ("I2048-05: limb multiplication and checked overflow preserve range", MultiplicationContracts),
        ("I2048-06: custom division and remainder preserve signed identity", DivisionAndRemainderContracts),
        ("I2048-07: long division matches BigInteger oracle across 2048-bit value", FullWidthDivisionMatchesOracle),
        ("I2048-08: parse and format round-trip signed 2048-bit bounds", ParseAndFormatRoundTrip),
        ("I2048-09: generic INumber arithmetic executes without BigInteger storage", GenericInumberExecution),
        ("I2048-10: checked saturating and truncating conversions retain contracts", ConversionContracts),
        ("I2048-11: CopySign, Clamp and predicates retain signed semantics", NumberUtilityContracts),
    ];

    private static void AdditionPropagatesCarryAcrossLimb()
    {
        var lowAllOnes = Int2048.FromBigInteger((BigInteger.One << 64) - 1);
        var result = lowAllOnes + Int2048.One;

        Require((BigInteger)result == (BigInteger.One << 64),
            "Carry from limb 0 must produce exactly bit 64.");
    }

    private static void SubtractionPropagatesBorrowAndSign()
    {
        var powerOf64 = Int2048.FromBigInteger(BigInteger.One << 64);
        var result = powerOf64 - Int2048.One;
        var negative = Int2048.Zero - Int2048.One;

        Require((BigInteger)result == (BigInteger.One << 64) - 1,
            "Borrow across limb boundary must fill low limb with ones.");
        Require((BigInteger)negative == -BigInteger.One && Int2048.IsNegative(negative),
            "0−1 must transfer the negative sign through two's-complement limbs.");
    }

    private static void AdditionOverflowContracts()
    {
        Require(Int2048.MaxValue + Int2048.One == Int2048.MinValue,
            "Ordinary fixed-width addition must wrap at the signed 2048-bit boundary.");
        RegressionAssertions.Expect<OverflowException>(
            () => _ = checked(Int2048.MaxValue + Int2048.One),
            "Checked addition must reject MaxValue+1.");
    }

    private static void NegationMinValueContracts()
    {
        Require(-Int2048.MinValue == Int2048.MinValue,
            "Ordinary two's-complement negation of MinValue must wrap to itself.");
        RegressionAssertions.Expect<OverflowException>(
            () => _ = checked(-Int2048.MinValue),
            "Checked negation must reject MinValue.");
    }

    private static void MultiplicationContracts()
    {
        var left = Int2048.FromBigInteger(BigInteger.One << 1024);
        var right = Int2048.FromBigInteger(BigInteger.One << 1023);
        var product = left * right;

        Require(product == Int2048.MinValue,
            "Low 2048 multiplication limbs of 2^1024·2^1023 must be MinValue bit pattern.");
        RegressionAssertions.Expect<OverflowException>(
            () => _ = checked(left * right),
            "Checked multiplication must reject positive 2^2047.");
    }

    private static void DivisionAndRemainderContracts()
    {
        var dividend = new Int2048(-100L);
        var divisor = new Int2048(7L);
        var quotient = dividend / divisor;
        var remainder = dividend % divisor;

        Require((BigInteger)quotient == -14 && (BigInteger)remainder == -2,
            "Division must truncate toward zero and remainder must retain dividend sign.");
        Require((quotient * divisor) + remainder == dividend,
            "Custom long division must satisfy a=(a/b)·b+(a%b).");
        RegressionAssertions.Expect<OverflowException>(
            () => _ = Int2048.MinValue / Int2048.NegativeOne,
            "MinValue/−1 is outside the signed Int2048 range.");
    }

    private static void FullWidthDivisionMatchesOracle()
    {
        var dividendValue = Int2048.MaxValue.ToBigInteger() - (BigInteger.One << 919) + 123456789;
        var divisorValue = (BigInteger.One << 511) + 998877665;
        var dividend = Int2048.FromBigInteger(dividendValue);
        var divisor = Int2048.FromBigInteger(divisorValue);

        var quotient = dividend / divisor;
        var remainder = dividend % divisor;

        Require((BigInteger)quotient == dividendValue / divisorValue &&
                (BigInteger)remainder == dividendValue % divisorValue,
            "Custom 2048-bit long division must agree with external BigInteger oracle.");
    }

    private static void ParseAndFormatRoundTrip()
    {
        var maxText = Int2048.MaxValue.ToString("G", CultureInfo.InvariantCulture);
        var minText = Int2048.MinValue.ToString("G", CultureInfo.InvariantCulture);
        var parsedMax = Int2048.Parse(maxText, CultureInfo.InvariantCulture);
        var parsedMin = Int2048.Parse(minText, CultureInfo.InvariantCulture);

        Require(parsedMax == Int2048.MaxValue && parsedMin == Int2048.MinValue,
            "Decimal parser and formatter must round-trip both exact 2048-bit bounds.");
        Require(!Int2048.TryParse((Int2048.MaxValue.ToBigInteger() + 1).ToString(), CultureInfo.InvariantCulture, out _),
            "Parser must reject value above MaxValue.");
    }

    private static void GenericInumberExecution()
    {
        var value = Int2048.FromBigInteger((BigInteger.One << 400) + 9);
        var result = GenericNormalize(value);

        Require(result == value,
            "Generic INumber path must preserve custom Int2048 type and exact limb value.");
    }

    private static void ConversionContracts()
    {
        var aboveMax = Int2048.MaxValue.ToBigInteger() + 1;
        Require(Int2048.CreateSaturating(aboveMax) == Int2048.MaxValue,
            "Saturating conversion must clamp value above MaxValue.");
        Require(Int2048.CreateTruncating(aboveMax) == Int2048.MinValue,
            "Truncating conversion must retain low 2048-bit two's-complement representation.");
        RegressionAssertions.Expect<OverflowException>(
            () => _ = Int2048.CreateChecked(aboveMax),
            "Checked conversion must reject value above MaxValue.");
    }

    private static void NumberUtilityContracts()
    {
        var positive = new Int2048(5L);
        var negative = Int2048.CopySign(positive, Int2048.NegativeOne);
        var clamped = Int2048.Clamp(new Int2048(20L), new Int2048(-2L), new Int2048(7L));

        Require(negative == new Int2048(-5L) && clamped == new Int2048(7L) &&
                Int2048.IsOddInteger(negative) && Int2048.IsFinite(negative) && Int2048.Sign(negative) == -1,
            "INumber utility methods must retain exact signed integer semantics.");
    }

    private static T GenericNormalize<T>(T value) where T : INumber<T> => (value + T.Zero) / T.One;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
