#nullable enable
#pragma warning disable CS1591

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Ricis.Numerics.Cryptography;

public sealed class Rsa2048PublicKey
{
    public Rsa2048PublicKey(ULong2048 modulus, ULong2048 publicExponent)
    {
        if (modulus.GetBitLength() != 2048 || modulus <= ULong2048.One || (modulus & 1UL) == ULong2048.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(modulus), "RSA-2048 modulus must be an odd 2048-bit integer greater than one.");
        }

        if (publicExponent < 3UL || publicExponent >= modulus || (publicExponent & 1UL) == ULong2048.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(publicExponent), "RSA public exponent must be odd and satisfy 3 <= e < modulus.");
        }

        Modulus = modulus;
        PublicExponent = publicExponent;
    }

    public ULong2048 Modulus { get; }
    public ULong2048 PublicExponent { get; }
}

public enum RsaPssVerificationFailure
{
    None = 0,
    InvalidPublicKey,
    SignatureLengthMismatch,
    SignatureRepresentativeOutOfRange,
    EncodedMessageLengthMismatch,
    EncodedMessageUnusedBitsSet,
    TrailerFieldMismatch,
    PssDataBlockMismatch,
    HashMismatch
}

public readonly record struct RsaPssVerificationResult
{
    private RsaPssVerificationResult(bool isValid, RsaPssVerificationFailure failure)
    {
        IsValid = isValid;
        Failure = failure;
    }

    public bool IsValid { get; }
    public RsaPssVerificationFailure Failure { get; }

    public static RsaPssVerificationResult Valid { get; } = new(true, RsaPssVerificationFailure.None);
    public static RsaPssVerificationResult Invalid(RsaPssVerificationFailure failure)
    {
        if (failure == RsaPssVerificationFailure.None) throw new ArgumentOutOfRangeException(nameof(failure));
        return new(false, failure);
    }
}

public static class RsaPssSha256Verifier
{
    public const int Rsa2048OctetLength = 256;
    public const int Sha256Length = 32;
    public const int SaltLength = 32;
    private const byte TrailerField = 0xBC;
    private const int EncodedMessageBits = 2047;

    public static RsaPssVerificationResult Verify(
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signatureBigEndian,
        Rsa2048PublicKey publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        if (signatureBigEndian.Length != Rsa2048OctetLength)
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.SignatureLengthMismatch);
        }

        if (!ULong2048.TryReadFixedWidthBigEndian(signatureBigEndian, out var signature))
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.SignatureLengthMismatch);
        }

        return Verify(message, signature, publicKey);
    }

    public static RsaPssVerificationResult Verify(
        ReadOnlySpan<byte> message,
        ULong2048 signatureRepresentative,
        Rsa2048PublicKey publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        if (signatureRepresentative >= publicKey.Modulus)
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.SignatureRepresentativeOutOfRange);
        }

        var encodedMessageRepresentative = ULong2048.RsaPublicOperation(
            signatureRepresentative,
            publicKey.PublicExponent,
            publicKey.Modulus);
        Span<byte> encodedMessage = stackalloc byte[Rsa2048OctetLength];
        encodedMessageRepresentative.WriteFixedWidthBigEndian(encodedMessage);
        return VerifyEncodedMessage(message, encodedMessage);
    }

    internal static RsaPssVerificationResult VerifyEncodedMessage(ReadOnlySpan<byte> message, ReadOnlySpan<byte> encodedMessage)
    {
        if (encodedMessage.Length != Rsa2048OctetLength)
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.EncodedMessageLengthMismatch);
        }

        // emBits = modBits - 1 = 2047 for this fixed RSA-2048 profile.
        if ((encodedMessage[0] & 0x80) != 0)
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.EncodedMessageUnusedBitsSet);
        }

        if (encodedMessage[^1] != TrailerField)
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.TrailerFieldMismatch);
        }

        var dataBlockLength = Rsa2048OctetLength - Sha256Length - 1;
        var maskedDataBlock = encodedMessage[..dataBlockLength];
        var encodedHash = encodedMessage.Slice(dataBlockLength, Sha256Length);
        Span<byte> dataBlockMask = stackalloc byte[dataBlockLength];
        Mgf1Sha256(encodedHash, dataBlockMask);
        Span<byte> dataBlock = stackalloc byte[dataBlockLength];
        for (var index = 0; index < dataBlockLength; index++) dataBlock[index] = (byte)(maskedDataBlock[index] ^ dataBlockMask[index]);
        dataBlock[0] &= 0x7F;

        var separatorIndex = dataBlockLength - SaltLength - 1;
        for (var index = 0; index < separatorIndex; index++)
        {
            if (dataBlock[index] != 0) return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.PssDataBlockMismatch);
        }

        if (dataBlock[separatorIndex] != 0x01)
        {
            return RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.PssDataBlockMismatch);
        }

        var salt = dataBlock[(separatorIndex + 1)..];
        Span<byte> messageHash = stackalloc byte[Sha256Length];
        SHA256.HashData(message, messageHash);
        Span<byte> hashInput = stackalloc byte[8 + Sha256Length + SaltLength];
        hashInput.Clear();
        messageHash.CopyTo(hashInput[8..]);
        salt.CopyTo(hashInput[(8 + Sha256Length)..]);
        Span<byte> recomputedHash = stackalloc byte[Sha256Length];
        SHA256.HashData(hashInput, recomputedHash);

        return CryptographicOperations.FixedTimeEquals(encodedHash, recomputedHash)
            ? RsaPssVerificationResult.Valid
            : RsaPssVerificationResult.Invalid(RsaPssVerificationFailure.HashMismatch);
    }

    internal static void Mgf1Sha256(ReadOnlySpan<byte> seed, Span<byte> destination)
    {
        Span<byte> input = stackalloc byte[Sha256Length + sizeof(uint)];
        if (seed.Length != Sha256Length) throw new ArgumentOutOfRangeException(nameof(seed), "MGF1-SHA256 seed must be exactly 32 bytes.");
        seed.CopyTo(input);
        Span<byte> digest = stackalloc byte[Sha256Length];

        uint counter = 0;
        var offset = 0;
        while (offset < destination.Length)
        {
            BinaryPrimitives.WriteUInt32BigEndian(input[Sha256Length..], counter++);
            SHA256.HashData(input, digest);
            var copyLength = Math.Min(Sha256Length, destination.Length - offset);
            digest[..copyLength].CopyTo(destination[offset..]);
            offset += copyLength;
        }
    }
}
