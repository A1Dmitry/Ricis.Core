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

    [TestMethod("U2048-07: RSA public operation rejects invalid signature representative")]
    public void RsaPublicOperationRejectsOutOfRangeSignature()
    {
        var modulus = new ULong2048(3233);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            ULong2048.RsaPublicOperation(modulus, new ULong2048(17), modulus));
    }
}
