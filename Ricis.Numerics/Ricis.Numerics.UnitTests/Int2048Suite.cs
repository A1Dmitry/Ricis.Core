using System.Globalization;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class Int2048Suite
{
    [TestMethod("I2048-01: carry проходит через 64-bit limb boundary")]
    public void AdditionPropagatesCarryAcrossLimb()
    {
        var lowAllOnes = Int2048.FromBigInteger((BigInteger.One << 64) - 1);
        var result = lowAllOnes + Int2048.One;

        Assert.AreEqual(BigInteger.One << 64, (BigInteger)result);
    }

    [TestMethod("I2048-02: subtraction выполняет borrow и перенос знака")]
    public void SubtractionPropagatesBorrowAndSign()
    {
        var powerOf64 = Int2048.FromBigInteger(BigInteger.One << 64);
        var result = powerOf64 - Int2048.One;
        var negative = Int2048.Zero - Int2048.One;

        Assert.AreEqual((BigInteger.One << 64) - 1, (BigInteger)result);
        Assert.IsTrue((BigInteger)negative == -BigInteger.One && Int2048.IsNegative(negative));
    }

    [TestMethod("I2048-03: ordinary and checked addition respect 2048-bit boundary")]
    public void AdditionOverflowContracts()
    {
        Assert.AreEqual(Int2048.MinValue, Int2048.MaxValue + Int2048.One);
        Assert.ThrowsException<OverflowException>(() => _ = checked(Int2048.MaxValue + Int2048.One));
    }

    [TestMethod("I2048-04: unary sign transfer preserves MinValue contracts")]
    public void NegationMinValueContracts()
    {
        Assert.AreEqual(Int2048.MinValue, -Int2048.MinValue);
        Assert.ThrowsException<OverflowException>(() => _ = checked(-Int2048.MinValue));
    }

    [TestMethod("I2048-05: limb multiplication and checked overflow preserve range")]
    public void MultiplicationContracts()
    {
        var left = Int2048.FromBigInteger(BigInteger.One << 1024);
        var right = Int2048.FromBigInteger(BigInteger.One << 1023);

        Assert.AreEqual(Int2048.MinValue, left * right);
        Assert.ThrowsException<OverflowException>(() => _ = checked(left * right));
    }

    [TestMethod("I2048-06: custom division and remainder preserve signed identity")]
    public void DivisionAndRemainderContracts()
    {
        var dividend = new Int2048(-100L);
        var divisor = new Int2048(7L);
        var quotient = dividend / divisor;
        var remainder = dividend % divisor;

        Assert.AreEqual(new BigInteger(-14), (BigInteger)quotient);
        Assert.AreEqual(new BigInteger(-2), (BigInteger)remainder);
        Assert.AreEqual(dividend, (quotient * divisor) + remainder);
        Assert.ThrowsException<OverflowException>(() => _ = Int2048.MinValue / Int2048.NegativeOne);
    }

    [TestMethod("I2048-07: long division matches BigInteger oracle across 2048-bit value")]
    public void FullWidthDivisionMatchesOracle()
    {
        var dividendValue = Int2048.MaxValue.ToBigInteger() - (BigInteger.One << 919) + 123456789;
        var divisorValue = (BigInteger.One << 511) + 998877665;
        var dividend = Int2048.FromBigInteger(dividendValue);
        var divisor = Int2048.FromBigInteger(divisorValue);

        Assert.AreEqual(dividendValue / divisorValue, (BigInteger)(dividend / divisor));
        Assert.AreEqual(dividendValue % divisorValue, (BigInteger)(dividend % divisor));
    }

    [TestMethod("I2048-08: parse and format round-trip signed 2048-bit bounds")]
    public void ParseAndFormatRoundTrip()
    {
        var maxText = Int2048.MaxValue.ToString("G", CultureInfo.InvariantCulture);
        var minText = Int2048.MinValue.ToString("G", CultureInfo.InvariantCulture);

        Assert.AreEqual(Int2048.MaxValue, Int2048.Parse(maxText, CultureInfo.InvariantCulture));
        Assert.AreEqual(Int2048.MinValue, Int2048.Parse(minText, CultureInfo.InvariantCulture));
        Assert.IsFalse(Int2048.TryParse((Int2048.MaxValue.ToBigInteger() + 1).ToString(), CultureInfo.InvariantCulture, out _));
    }

    [TestMethod("I2048-09: generic INumber arithmetic executes without BigInteger storage")]
    public void GenericInumberExecution()
    {
        var value = Int2048.FromBigInteger((BigInteger.One << 400) + 9);
        Assert.AreEqual(value, GenericNormalize(value));
    }

    [TestMethod("I2048-10: checked saturating and truncating conversions retain contracts")]
    public void ConversionContracts()
    {
        var aboveMax = Int2048.MaxValue.ToBigInteger() + 1;

        Assert.AreEqual(Int2048.MaxValue, Int2048.CreateSaturating(aboveMax));
        Assert.AreEqual(Int2048.MinValue, Int2048.CreateTruncating(aboveMax));
        Assert.ThrowsException<OverflowException>(() => _ = Int2048.CreateChecked(aboveMax));
    }

    [TestMethod("I2048-11: BigInteger overloads retain exact mixed arithmetic")]
    public void BigIntegerOverloadsRetainExactMixedArithmetic()
    {
        var custom = Int2048.FromBigInteger((BigInteger.One << 1024) + 99);
        var external = (BigInteger.One << 2050) + 7;

        Assert.AreEqual(custom.ToBigInteger() + external, custom + external);
        Assert.AreEqual(external + custom.ToBigInteger(), external + custom);
        Assert.AreEqual(custom.ToBigInteger() - external, custom - external);
        Assert.AreEqual(custom.ToBigInteger() * external, custom * external);
        Assert.AreEqual(custom.ToBigInteger() / 7, custom / new BigInteger(7));
        Assert.AreEqual(custom.ToBigInteger() % 7, custom % new BigInteger(7));
    }

    [TestMethod("I2048-12: CopySign, Clamp and predicates retain signed semantics")]
    public void NumberUtilityContracts()
    {
        var positive = new Int2048(5L);
        var negative = Int2048.CopySign(positive, Int2048.NegativeOne);
        var clamped = Int2048.Clamp(new Int2048(20L), new Int2048(-2L), new Int2048(7L));

        Assert.AreEqual(new Int2048(-5L), negative);
        Assert.AreEqual(new Int2048(7L), clamped);
        Assert.IsTrue(Int2048.IsOddInteger(negative) && Int2048.IsFinite(negative) && Int2048.Sign(negative) == -1);
    }

    private static T GenericNormalize<T>(T value) where T : INumber<T> => (value + T.Zero) / T.One;
}
