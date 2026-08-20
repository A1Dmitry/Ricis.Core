using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class ULong2048Suite
{
    [TestMethod("U2048-01: full unsigned 2048-bit bound round-trips")]
    public void FullUnsignedBoundRoundTrips()
    {
        var expected = (BigInteger.One << 2048) - 1;
        var value = ULong2048.FromBigInteger(expected);

        Assert.AreEqual(expected, value.ToBigInteger());
        Assert.AreEqual(ULong2048.Zero, value + ULong2048.One);
    }

    [TestMethod("U2048-02: unsigned division and remainder match BigInteger oracle")]
    public void DivisionAndRemainderMatchOracle()
    {
        var dividendValue = (BigInteger.One << 2047) + (BigInteger.One << 911) + 123456789;
        var divisorValue = (BigInteger.One << 1001) + 998877665;
        var dividend = ULong2048.FromBigInteger(dividendValue);
        var divisor = ULong2048.FromBigInteger(divisorValue);

        Assert.AreEqual(dividendValue / divisorValue, (BigInteger)(dividend / divisor));
        Assert.AreEqual(dividendValue % divisorValue, (BigInteger)(dividend % divisor));
    }

    [TestMethod("U2048-03: explicit BigInteger overloads retain exact mixed arithmetic")]
    public void BigIntegerOverloadsRetainExactArithmetic()
    {
        var value = ULong2048.FromBigInteger((BigInteger.One << 512) + 42);
        var addend = (BigInteger.One << 600) + 7;

        Assert.AreEqual(value.ToBigInteger() + addend, value + addend);
        Assert.AreEqual(addend + value.ToBigInteger(), addend + value);
        Assert.AreEqual(value.ToBigInteger() * addend, value * addend);
        Assert.AreEqual(value.ToBigInteger() % addend, value % addend);
    }

    [TestMethod("U2048-04: custom modular multiplication matches BigInteger")]
    public void ModularMultiplicationMatchesOracle()
    {
        var modulusValue = (BigInteger.One << 1024) - 109;
        var leftValue = (BigInteger.One << 900) + 4567;
        var rightValue = (BigInteger.One << 850) + 8910;
        var result = ULong2048.MultiplyModulo(
            ULong2048.FromBigInteger(leftValue),
            ULong2048.FromBigInteger(rightValue),
            ULong2048.FromBigInteger(modulusValue));

        Assert.AreEqual((leftValue * rightValue) % modulusValue, (BigInteger)result);
    }

    [TestMethod("U2048-05: RSA public operation matches known RSA primitive vector")]
    public void RsaPublicOperationMatchesKnownPrimitive()
    {
        var modulus = new ULong2048(3233);
        var exponent = new ULong2048(17);
        var signature = new ULong2048(588);

        Assert.AreEqual(new ULong2048(65), ULong2048.RsaPublicOperation(signature, exponent, modulus));
    }

    [TestMethod("U2048-06: RSA public operation matches BigInteger at 2048-bit boundary")]
    public void RsaPublicOperationMatchesBigIntegerAtFullWidth()
    {
        var modulusValue = (BigInteger.One << 2048) - 159;
        var exponentValue = new BigInteger(65537);
        var signatureValue = (BigInteger.One << 2000) + (BigInteger.One << 123) + 77;
        var result = ULong2048.RsaPublicOperation(
            ULong2048.FromBigInteger(signatureValue),
            ULong2048.FromBigInteger(exponentValue),
            ULong2048.FromBigInteger(modulusValue));

        Assert.AreEqual(BigInteger.ModPow(signatureValue, exponentValue, modulusValue), (BigInteger)result);
    }

    [TestMethod("U2048-07: inline add subtract and shift hot path is allocation-free")]
    public void InlineHotOperatorsAreAllocationFree()
    {
        var left = ULong2048.FromBigInteger((BigInteger.One << 1700) + 12345);
        var right = ULong2048.FromBigInteger((BigInteger.One << 1600) + 67890);
        _ = (left + right) - right;
        _ = left >> 17;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = ULong2048.Zero;
        for (var index = 0; index < 1_024; index++)
        {
            result = ((left + right) - right) >> 17;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(left >> 17, result);
        Assert.AreEqual(0L, allocated, "Inline ULong2048 hot operators must not allocate on the managed heap.");
    }

    [TestMethod("U2048-08: Montgomery path matches BigInteger across odd modulus matrix")]
    public void MontgomeryPathMatchesOracleAcrossOddModulusMatrix()
    {
        var cases = new[]
        {
            new BigInteger(3233),
            (BigInteger.One << 1024) - 109,
            (BigInteger.One << 2047) + (BigInteger.One << 1023) + 1,
            (BigInteger.One << 2048) - 159,
        };
        var exponent = new BigInteger(65537);

        foreach (var modulusValue in cases)
        {
            var leftValue = modulusValue - 7;
            var rightValue = modulusValue - 11;
            var modulus = ULong2048.FromBigInteger(modulusValue);
            var left = ULong2048.FromBigInteger(leftValue);
            var right = ULong2048.FromBigInteger(rightValue);

            Assert.AreEqual((leftValue * rightValue) % modulusValue, (BigInteger)ULong2048.MultiplyModulo(left, right, modulus));
            Assert.AreEqual(BigInteger.ModPow(leftValue, exponent, modulusValue), (BigInteger)ULong2048.ModPow(left, ULong2048.FromBigInteger(exponent), modulus));
        }
    }

    [TestMethod("U2048-09: BigInteger symmetric arithmetic bitwise and comparison overloads are exact")]
    public void BigIntegerSymmetricOperatorsAreExact()
    {
        var value = ULong2048.FromBigInteger((BigInteger.One << 240) + 12345);
        var left = -((BigInteger.One << 250) + 7);
        var right = new BigInteger(7);
        var valueBigInteger = value.ToBigInteger();

        Assert.AreEqual(valueBigInteger + left, value + left);
        Assert.AreEqual(left + valueBigInteger, left + value);
        Assert.AreEqual(valueBigInteger - left, value - left);
        Assert.AreEqual(left - valueBigInteger, left - value);
        Assert.AreEqual(valueBigInteger * right, value * right);
        Assert.AreEqual(right * valueBigInteger, right * value);
        Assert.AreEqual(valueBigInteger / right, value / right);
        Assert.AreEqual(right / valueBigInteger, right / value);
        Assert.AreEqual(valueBigInteger % right, value % right);
        Assert.AreEqual(right % valueBigInteger, right % value);
        Assert.AreEqual(valueBigInteger & left, value & left);
        Assert.AreEqual(left & valueBigInteger, left & value);
        Assert.AreEqual(valueBigInteger | left, value | left);
        Assert.AreEqual(left | valueBigInteger, left | value);
        Assert.AreEqual(valueBigInteger ^ left, value ^ left);
        Assert.AreEqual(left ^ valueBigInteger, left ^ value);
        Assert.IsTrue(value > left && left < value && value != left && left != value);
    }

    [TestMethod("U2048-10: RSA public operation rejects invalid signature representative")]
    public void RsaPublicOperationRejectsOutOfRangeSignature()
    {
        var modulus = new ULong2048(3233);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            ULong2048.RsaPublicOperation(modulus, new ULong2048(17), modulus));
    }
}
