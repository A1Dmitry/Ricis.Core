#nullable enable

using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Numerics.Cryptography;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class RsaPssSha256VerifierSuite
{
    private static readonly byte[] AsciiMessage = Encoding.ASCII.GetBytes("RICIS RSA-PSS SHA-256 ASCII regression message");
    private static readonly byte[] Utf8Message = Encoding.UTF8.GetBytes("RICIS: проверка RSA-PSS — π = 3.141592653589793");
    private const string PinnedPss04MessageHex = "52494349532050535330342070696E6E65642042434C2072656772657373696F6E20766563746F72207631";
    private const string PinnedPss04SignatureHex = "78C8D229DFCF5A09C70B174E7468FAA2DA54727095C497B34C39A1BC80FDB73BB5B45EC2FA17BCE5DA089F5362F25D3F5463F247A3BB7CFBDD6DAEA669837BF0A1D014BF59BCA969F77BC8EA9FBD3A00286E5606A840FE6A9B9C3C06B8ABBE62A97A111369F4B97E17579F86422FD6D9F32199CAFC0F3C3EAC412AB0B0C9E014DFA88687791EE9B210DD3DEF8CFA9B4BF8216622338FBB7EF400048DC5BAA1CEE0D0552B99C103DA8661229B188C7201AF36E61BA17EBB67665781210F86102E866C25CF01A0F6945967B9902C4ABA58687A3FC0433CE03ED79944BD0C3D218DC020B6C986E3B7DCCFBAEEE401B9C9C7CDBB42D093C2BD88AA77F477BBE39128";

    [TestMethod("PSS01: runtime p×q RSA-2048 fixture and BCL ASCII signature verify")]
    public void Pss01_RuntimeFixtureAndAsciiSignatureVerify()
    {
        Rsa2048PssFixture.AssertArithmeticFixture();
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        Assert.IsTrue(Rsa2048PssFixture.VerifyWithBcl(AsciiMessage, signature));
        AssertValid(RsaPssSha256Verifier.Verify(AsciiMessage, signature, Rsa2048PssFixture.PublicKey));
    }

    [TestMethod("PSS02: empty message BCL PSS signature verifies")]
    public void Pss02_EmptyMessageSignatureVerifies()
    {
        var message = Array.Empty<byte>();
        var signature = Rsa2048PssFixture.SignPssSha256(message);
        Assert.IsTrue(Rsa2048PssFixture.VerifyWithBcl(message, signature));
        AssertValid(RsaPssSha256Verifier.Verify(message, signature, Rsa2048PssFixture.PublicKey));
    }

    [TestMethod("PSS03: UTF-8 non-ASCII BCL PSS signature verifies")]
    public void Pss03_Utf8NonAsciiSignatureVerifies()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(Utf8Message);
        Assert.IsTrue(Rsa2048PssFixture.VerifyWithBcl(Utf8Message, signature));
        AssertValid(RsaPssSha256Verifier.Verify(Utf8Message, signature, Rsa2048PssFixture.PublicKey));
    }

    [TestMethod("PSS04: pinned BCL interoperability vector verifies")]
    public void Pss04_PinnedBclInteroperabilityVectorVerifies()
    {
        var message = Convert.FromHexString(PinnedPss04MessageHex);
        var signature = Convert.FromHexString(PinnedPss04SignatureHex);
        Assert.AreEqual(RsaPssSha256Verifier.Rsa2048OctetLength, signature.Length);
        Assert.IsTrue(Rsa2048PssFixture.VerifyWithBcl(message, signature));
        AssertValid(RsaPssSha256Verifier.Verify(message, signature, Rsa2048PssFixture.PublicKey));
    }

    [TestMethod("PSS05: byte and numeric signature overloads have identical valid result")]
    public void Pss05_ByteAndNumericOverloadsAgree()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var numericSignature = ULong2048.FromBigInteger(new BigInteger(signature, isUnsigned: true, isBigEndian: true));
        var bytesResult = RsaPssSha256Verifier.Verify(AsciiMessage, signature, Rsa2048PssFixture.PublicKey);
        var numericResult = RsaPssSha256Verifier.Verify(AsciiMessage, numericSignature, Rsa2048PssFixture.PublicKey);
        AssertValid(bytesResult);
        Assert.AreEqual(bytesResult, numericResult);
    }

    [TestMethod("PSS06: public operation recovers the exact 256-octet encoded message")]
    public void Pss06_PublicOperationRecoversFixedWidthEncodedMessage()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var encodedMessage = RecoverEncodedMessage(signature);
        Assert.AreEqual(RsaPssSha256Verifier.Rsa2048OctetLength, encodedMessage.Length);
        AssertValid(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage));
    }

    [TestMethod("PSSN01: 255-octet signature fails closed")]
    public void Pssn01_ShortSignatureFailsClosed()
    {
        AssertFailure(RsaPssSha256Verifier.Verify(AsciiMessage, new byte[255], Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.SignatureLengthMismatch);
    }

    [TestMethod("PSSN02: 257-octet signature fails closed")]
    public void Pssn02_LongSignatureFailsClosed()
    {
        AssertFailure(RsaPssSha256Verifier.Verify(AsciiMessage, new byte[257], Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.SignatureLengthMismatch);
    }

    [TestMethod("PSSN03: numeric signature s=n fails closed")]
    public void Pssn03_SignatureAtModulusFailsClosed()
    {
        AssertFailure(RsaPssSha256Verifier.Verify(AsciiMessage, Rsa2048PssFixture.Modulus2048, Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.SignatureRepresentativeOutOfRange);
    }

    [TestMethod("PSSN04: numeric signature s>n fails closed")]
    public void Pssn04_SignaturePastModulusFailsClosed()
    {
        AssertFailure(RsaPssSha256Verifier.Verify(AsciiMessage, Rsa2048PssFixture.Modulus2048 + 1UL, Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.SignatureRepresentativeOutOfRange);
    }

    [TestMethod("PSSN05: unused high encoded-message bit fails closed")]
    public void Pssn05_UnusedEncodedMessageBitFailsClosed()
    {
        var encodedMessage = CreateValidEncodedMessage();
        encodedMessage[0] |= 0x80;
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.EncodedMessageUnusedBitsSet);
    }

    [TestMethod("PSSN06: trailer field mutation fails closed")]
    public void Pssn06_TrailerFieldMutationFailsClosed()
    {
        var encodedMessage = CreateValidEncodedMessage();
        encodedMessage[^1] ^= 0x01;
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.TrailerFieldMismatch);
    }

    [TestMethod("PSSN07: PSS zero-padding mutation fails closed")]
    public void Pssn07_ZeroPaddingMutationFailsClosed()
    {
        var encodedMessage = CreateValidEncodedMessage();
        encodedMessage[1] ^= 0x01;
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.PssDataBlockMismatch);
    }

    [TestMethod("PSSN08: PSS separator mutation fails closed")]
    public void Pssn08_SeparatorMutationFailsClosed()
    {
        var encodedMessage = CreateValidEncodedMessage();
        encodedMessage[SeparatorIndex] ^= 0x01;
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.PssDataBlockMismatch);
    }

    [TestMethod("PSSN09: non-32-octet conceptual salt layout fails closed")]
    public void Pssn09_NonFixedSaltLayoutFailsClosed()
    {
        var encodedMessage = CreateValidEncodedMessage();
        encodedMessage[SeparatorIndex] ^= 0x01;
        encodedMessage[SeparatorIndex - 1] ^= 0x01;
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.PssDataBlockMismatch);
    }

    [TestMethod("PSSN10: masked-data-block salt mutation fails closed")]
    public void Pssn10_MaskedDataBlockMutationFailsClosed()
    {
        var encodedMessage = CreateValidEncodedMessage();
        encodedMessage[DataBlockLength - 1] ^= 0x01;
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.HashMismatch);
    }

    [TestMethod("PSSN11: encoded hash mutation with preserved DB fails hash check")]
    public void Pssn11_EncodedHashMutationFailsClosed()
    {
        var encodedMessage = CreateEncodedHashMutationPreservingDataBlock();
        AssertFailure(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage), RsaPssVerificationFailure.HashMismatch);
    }

    [TestMethod("PSSN12: message mutation fails hash verification")]
    public void Pssn12_MessageMutationFailsClosed()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var mutatedMessage = AsciiMessage.ToArray();
        mutatedMessage[^1] ^= 0x01;
        AssertFailure(RsaPssSha256Verifier.Verify(mutatedMessage, signature, Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.HashMismatch);
    }

    [TestMethod("PSSN13: a valid signature under another modulus never verifies")]
    public void Pssn13_DifferentModulusFailsClosed()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var differentKey = new Rsa2048PublicKey(Rsa2048PssFixture.Modulus2048 - 2UL, Rsa2048PssFixture.PublicExponent);
        var result = RsaPssSha256Verifier.Verify(AsciiMessage, signature, differentKey);
        Assert.IsFalse(result.IsValid);
        Assert.AreNotEqual(RsaPssVerificationFailure.None, result.Failure);
    }

    [TestMethod("PSSN14: a valid signature under another exponent never verifies")]
    public void Pssn14_DifferentExponentFailsClosed()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var differentKey = new Rsa2048PublicKey(Rsa2048PssFixture.Modulus2048, Rsa2048PssFixture.PublicExponent + 2UL);
        var result = RsaPssSha256Verifier.Verify(AsciiMessage, signature, differentKey);
        Assert.IsFalse(result.IsValid);
        Assert.AreNotEqual(RsaPssVerificationFailure.None, result.Failure);
    }

    [TestMethod("PSSN15: invalid RSA public-key profiles reject at construction")]
    public void Pssn15_InvalidKeyProfilesRejectAtConstruction()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new Rsa2048PublicKey(ULong2048.Zero, 3UL));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new Rsa2048PublicKey(ULong2048.MaxValue - 1UL, 3UL));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new Rsa2048PublicKey(Rsa2048PssFixture.Modulus2048, 1UL));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new Rsa2048PublicKey(Rsa2048PssFixture.Modulus2048, 2UL));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new Rsa2048PublicKey(Rsa2048PssFixture.Modulus2048, Rsa2048PssFixture.Modulus2048));
    }

    [TestMethod("PAR01: ULong2048 public operation matches BigInteger for valid and mutated representatives")]
    public void Par01_PublicOperationMatchesBigIntegerOracle()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var representative = ULong2048.FromBigInteger(new BigInteger(signature, isUnsigned: true, isBigEndian: true));
        AssertPublicOperationParity(representative);
        AssertPublicOperationParity(representative + 1UL);
    }

    [TestMethod("PAR02: I2OSP/OS2IP preserves 256-octet values and leading zeros")]
    public void Par02_FixedWidthCodecRoundTripsBoundaryValues()
    {
        var maximum = ULong2048.FromBigInteger((BigInteger.One << 2048) - BigInteger.One);
        var values = new[] { ULong2048.Zero, ULong2048.One, Rsa2048PssFixture.Modulus2048 - 1UL, maximum };
        foreach (var value in values)
        {
            var octets = new byte[RsaPssSha256Verifier.Rsa2048OctetLength];
            value.WriteFixedWidthBigEndian(octets);
            Assert.IsTrue(ULong2048.TryReadFixedWidthBigEndian(octets, out var roundTripped));
            Assert.AreEqual(value, roundTripped);
        }

        var one = new byte[RsaPssSha256Verifier.Rsa2048OctetLength];
        ULong2048.One.WriteFixedWidthBigEndian(one);
        CollectionAssert.AreEqual(new byte[255], one[..255]);
        Assert.AreEqual((byte)1, one[^1]);
    }

    [TestMethod("PAR03: fixed-width codec rejects invalid lengths without silent padding")]
    public void Par03_CodecAndPublicBoundaryRejectInvalidLengths()
    {
        Assert.IsFalse(ULong2048.TryReadFixedWidthBigEndian(new byte[255], out _));
        Assert.IsFalse(ULong2048.TryReadFixedWidthBigEndian(new byte[257], out _));
        AssertFailure(RsaPssSha256Verifier.Verify(AsciiMessage, new byte[255], Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.SignatureLengthMismatch);
        AssertFailure(RsaPssSha256Verifier.Verify(AsciiMessage, new byte[257], Rsa2048PssFixture.PublicKey), RsaPssVerificationFailure.SignatureLengthMismatch);
    }

    [TestMethod("PAR04: BCL and custom verifier agree on valid and invalid PSS cases")]
    public void Par04_BclAndCustomVerifierAgree()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(Utf8Message);
        Assert.IsTrue(Rsa2048PssFixture.VerifyWithBcl(Utf8Message, signature));
        AssertValid(RsaPssSha256Verifier.Verify(Utf8Message, signature, Rsa2048PssFixture.PublicKey));

        var invalidSignature = signature.ToArray();
        invalidSignature[^1] ^= 0x01;
        Assert.IsFalse(Rsa2048PssFixture.VerifyWithBcl(Utf8Message, invalidSignature));
        Assert.IsFalse(RsaPssSha256Verifier.Verify(Utf8Message, invalidSignature, Rsa2048PssFixture.PublicKey).IsValid);
    }

    [TestMethod("PAR05: fixture uses standard ULong2048 operators already covered by generated integral tests")]
    public void Par05_FixtureUsesStandardMixedIntegralSurface()
    {
        var nextExponent = Rsa2048PssFixture.PublicExponent + 2UL;
        Assert.AreEqual((BigInteger)Rsa2048PssFixture.PublicExponent + 2, (BigInteger)nextExponent);
        Assert.AreEqual(Rsa2048PssFixture.Modulus2048 - 2UL, Rsa2048PssFixture.Modulus2048 - (ulong)2);
    }

    [TestMethod("API01: public key exposes its validated immutable modulus and exponent")]
    public void Api01_PublicKeyPropertiesExposeValidatedValues()
    {
        var key = Rsa2048PssFixture.PublicKey;
        Assert.AreEqual(Rsa2048PssFixture.Modulus2048, key.Modulus);
        Assert.AreEqual(Rsa2048PssFixture.PublicExponent, key.PublicExponent);
    }

    [TestMethod("API02: verification result factories are fail-closed")]
    public void Api02_VerificationResultFactoriesAreFailClosed()
    {
        Assert.IsTrue(RsaPssVerificationResult.Valid.IsValid);
        Assert.AreEqual(RsaPssVerificationFailure.None, RsaPssVerificationResult.Valid.Failure);
        var invalid = RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.HashMismatch);
        Assert.IsFalse(invalid.IsValid);
        Assert.AreEqual(RsaPssVerificationFailure.HashMismatch, invalid.Failure);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.None));
    }

    [TestMethod("API03: public verifier overloads reject null public keys")]
    public void Api03_VerifierOverloadsRejectNullPublicKeys()
    {
        Assert.ThrowsException<ArgumentNullException>(() => RsaPssSha256Verifier.Verify(AsciiMessage, new byte[256], null!));
        Assert.ThrowsException<ArgumentNullException>(() => RsaPssSha256Verifier.Verify(AsciiMessage, ULong2048.Zero, null!));
    }

    private static int DataBlockLength => RsaPssSha256Verifier.Rsa2048OctetLength - RsaPssSha256Verifier.Sha256Length - 1;
    private static int SeparatorIndex => DataBlockLength - RsaPssSha256Verifier.SaltLength - 1;

    private static void AssertValid(RsaPssVerificationResult result)
    {
        Assert.IsTrue(result.IsValid, result.Failure.ToString());
        Assert.AreEqual(RsaPssVerificationFailure.None, result.Failure);
    }

    private static void AssertFailure(RsaPssVerificationResult result, RsaPssVerificationFailure expected)
    {
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(expected, result.Failure);
    }

    private static byte[] CreateValidEncodedMessage()
    {
        var signature = Rsa2048PssFixture.SignPssSha256(AsciiMessage);
        var encodedMessage = RecoverEncodedMessage(signature);
        AssertValid(RsaPssSha256Verifier.VerifyEncodedMessage(AsciiMessage, encodedMessage));
        return encodedMessage;
    }

    private static byte[] RecoverEncodedMessage(ReadOnlySpan<byte> signature)
    {
        var signatureRepresentative = ULong2048.FromBigInteger(new BigInteger(signature, isUnsigned: true, isBigEndian: true));
        var encodedMessageRepresentative = ULong2048.RsaPublicOperation(signatureRepresentative, Rsa2048PssFixture.PublicExponent, Rsa2048PssFixture.Modulus2048);
        var encodedMessage = new byte[RsaPssSha256Verifier.Rsa2048OctetLength];
        encodedMessageRepresentative.WriteFixedWidthBigEndian(encodedMessage);
        return encodedMessage;
    }

    private static byte[] CreateEncodedHashMutationPreservingDataBlock()
    {
        var encodedMessage = CreateValidEncodedMessage();
        var originalHash = encodedMessage.AsSpan(DataBlockLength, RsaPssSha256Verifier.Sha256Length).ToArray();
        var originalMask = new byte[DataBlockLength];
        RsaPssSha256Verifier.Mgf1Sha256(originalHash, originalMask);
        var dataBlock = new byte[DataBlockLength];
        for (var index = 0; index < DataBlockLength; index++) dataBlock[index] = (byte)(encodedMessage[index] ^ originalMask[index]);

        var mutatedHash = originalHash.ToArray();
        mutatedHash[0] ^= 0x01;
        var mutatedMask = new byte[DataBlockLength];
        RsaPssSha256Verifier.Mgf1Sha256(mutatedHash, mutatedMask);
        for (var index = 0; index < DataBlockLength; index++) encodedMessage[index] = (byte)(dataBlock[index] ^ mutatedMask[index]);
        encodedMessage[0] &= 0x7F;
        mutatedHash.CopyTo(encodedMessage, DataBlockLength);
        return encodedMessage;
    }

    private static void AssertPublicOperationParity(ULong2048 representative)
    {
        var custom = ULong2048.RsaPublicOperation(representative, Rsa2048PssFixture.PublicExponent, Rsa2048PssFixture.Modulus2048);
        var oracle = BigInteger.ModPow((BigInteger)representative, (BigInteger)Rsa2048PssFixture.PublicExponent, Rsa2048PssFixture.Modulus);
        Assert.AreEqual(oracle, (BigInteger)custom);
    }
}
