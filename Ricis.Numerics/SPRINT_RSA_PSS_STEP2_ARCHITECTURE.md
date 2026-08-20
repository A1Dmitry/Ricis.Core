# SPRINT RSA-PSS — Step 2: Architecture Contract

**Status:** Fixed-profile RSA-PSS architecture approved and implemented; this contract remains normative for the implemented profile.
**Prerequisite:** Step 1 business/security specification approved
**Target:** RSA-2048 RSASSA-PSS verification, SHA-256 / MGF1-SHA-256 / fixed 32-byte salt
**Implementation authorization:** The fixed profile is implemented under the separately approved Step 1/Step 3 scope; this document does not authorize additional algorithms or trust/PKI features.

## 1. Architecture decision

The RSA-PSS verifier belongs entirely in **`Ricis.Numerics`**, above the existing `ULong2048` arithmetic layer. It must not create a dependency from symbolic `Ricis.Core` to cryptographic byte encoding, private material, certificates or trust policy.

```text
Caller message + 256-byte signature + Rsa2048PublicKey
                         │
                         ▼
             RsaPssSha256Verifier
                         │
     ┌───────────────────┼────────────────────┐
     ▼                   ▼                    ▼
input/key validation  ULong2048.RsaPublicOperation  EMSA-PSS decoder
                           │                    │
                           ▼                    ▼
                     256-byte EM       SHA-256 / MGF1-SHA256
                         │                    │
                         └──────────────┬─────┘
                                        ▼
                      RsaPssVerificationResult
```

The raw public operation and full signature verification remain separate public concepts. RFC 8017 defines RSAVP1 as a primitive and RSASSA-PSS verification as a scheme that additionally requires EMSA-PSS-VERIFY.[1]

## 2. New public contracts

| Type | Responsibility | Immutable invariants |
|---|---|---|
| `Rsa2048PublicKey` | Holds the numeric public pair `(n, e)` | `n` is odd, exactly 2048 bits, `n > 1`; `e` is odd, `3 <= e < n` |
| `RsaPssSha256Verifier` | Stateless verifier for the sole Step 1 profile | No private key state, no network/file I/O, no mutable global configuration |
| `RsaPssVerificationResult` | Value result for callers, QA and proof logs | Valid iff failure is `None`; diagnostics contain no private material |
| `RsaPssVerificationFailure` | Stable public diagnostic taxonomy | No raw decoded message, salt, mask or hash bytes in enum/string output |

### 2.1 Public API shape

```csharp
namespace Ricis.Numerics.Cryptography;

public sealed class Rsa2048PublicKey
{
    public ULong2048 Modulus { get; }
    public ULong2048 PublicExponent { get; }

    public Rsa2048PublicKey(ULong2048 modulus, ULong2048 publicExponent);
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

public readonly record struct RsaPssVerificationResult(
    bool IsValid,
    RsaPssVerificationFailure Failure);

public static class RsaPssSha256Verifier
{
    public static RsaPssVerificationResult Verify(
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signatureBigEndian,
        Rsa2048PublicKey publicKey);

    public static RsaPssVerificationResult Verify(
        ReadOnlySpan<byte> message,
        ULong2048 signatureRepresentative,
        Rsa2048PublicKey publicKey);
}
```

The byte-span overload accepts exactly 256 signature octets and performs OS2IP in big-endian order. The numeric overload is suitable for already validated RICIS number-domain callers. Both route to one internal implementation; neither calls the other through a second conversion path.

## 3. Data and representation rules

| Item | Step 2 rule | Rationale |
|---|---|---|
| Modulus width | Exactly 2048 bits | This is an RSA-2048 profile, not a generic variable-size RSA implementation |
| Signature width | Exactly 256 big-endian octets | `k = ceil(modBits / 8)` for a 2048-bit modulus |
| PSS `emBits` | 2047 | RFC 8017 defines `emBits = modBits - 1` for RSASSA-PSS verification |
| PSS `emLen` | 256 octets | `ceil(2047 / 8) = 256` |
| Salt length | Exactly 32 octets | Explicit PSS-SHA-256 product profile, no default/auto-detect behavior |
| Hash and MGF | SHA-256 and MGF1-SHA256 | Explicit approved Step 1 profile |
| Byte order | Big-endian at all RSA octet boundaries | RFC I2OSP/OS2IP convention; limbs remain internal little-endian |
| Arithmetic provider | `ULong2048.RsaPublicOperation` | Reuses audited custom fixed-width RSA public path |
| Test oracle | `BigInteger` and BCL `RSA` only in test code | Allows parity validation without replacing production provider |
| RSA-2048 fixture construction | Runtime `n = p × q` from two versioned test primes | C# has no valid compile-time 2048-bit integer literal; construction preserves factorization evidence and exact bit-length validation |

A package-internal `ULong2048` byte codec will be introduced as the **single** I2OSP/OS2IP boundary. It will expose neither raw limbs nor ambiguous endianness:

```csharp
internal static class ULong2048OctetCodec
{
    internal const int Width = 256;
    internal static bool TryReadBigEndian(ReadOnlySpan<byte> source, out ULong2048 value);
    internal static void WriteBigEndian(ULong2048 value, Span<byte> destination);
}
```

`Rsa2048PublicKey` also requires a package-internal `ULong2048` bit-length query. It will scan inline limbs directly and will not use `BigInteger` merely to validate the RSA profile.

## 4. Verification flow

The implementation follows RFC 8017's separation of RSAVP1 and EMSA-PSS-VERIFY.[1]

1. Validate the non-null public key and its construction invariants.
2. For byte input, require exactly 256 signature octets and OS2IP them as an unsigned big-endian `ULong2048`.
3. Require `signature < modulus`; return `SignatureRepresentativeOutOfRange` if not.
4. Apply `ULong2048.RsaPublicOperation(signature, e, n)` to obtain the encoded-message representative.
5. I2OSP it into exactly 256 big-endian octets `EM`.
6. Enforce `EM` unused leading bit is zero because `emBits = 2047`.
7. Require trailer field `0xBC`.
8. Split `maskedDB || H || 0xBC`; derive `dbMask = MGF1-SHA256(H, emLen - hLen - 1)` using a four-octet big-endian counter.
9. Recover `DB = maskedDB xor dbMask`; clear the same unused leading bit.
10. Require `DB = PS || 0x01 || salt`, where every `PS` octet is zero and salt has exactly 32 octets.
11. Recompute `H' = SHA256(8 zero octets || SHA256(message) || salt)` and compare `H` and `H'` with `CryptographicOperations.FixedTimeEquals`.
12. Return `Valid/None` only if all checks pass; otherwise return the first stable failure category without attempting alternate algorithms.

## 5. Failure and exception policy

| Situation | API result |
|---|---|
| `publicKey` is null | `ArgumentNullException` — programmer contract violation |
| Invalid key passed to constructor | `ArgumentOutOfRangeException` — impossible public-key profile |
| Signature length not 256 | `IsValid=false`, `SignatureLengthMismatch` |
| Signature representative outside RSA range | `IsValid=false`, `SignatureRepresentativeOutOfRange` |
| Any PSS structural/hash failure | `IsValid=false` with the corresponding stable enum |
| SHA-256 runtime failure | Exception propagates; it is an operational failure, not invalid user signature data |

No fallback to PKCS#1 v1.5, different hash, different salt length, raw RSA acceptance or automatic modulus-size detection is permitted. Such fallbacks are prohibited to avoid algorithm confusion.

## 6. Dependency and DRY rules

The verifier shall use only `System.Security.Cryptography.SHA256`, `CryptographicOperations.FixedTimeEquals`, `ULong2048`, and the new local byte codec. It adds no NuGet dependency and no reference back to symbolic Core, Finance or Web API.

MGF1 and EMSA-PSS decoding are owned by one internal implementation each. The two public `Verify` overloads must converge before the raw operation, and test vectors must be shared between custom and BCL oracle routes rather than copied.

### 3.1 Runtime RSA-2048 fixture contract

No C# source may claim to hold a 2048-bit numeric `const`: the language's integral literal/type system cannot represent such a compile-time value. The QA fixture shall instead contain two versioned **non-secret test primes** as canonical hexadecimal/byte test data. Runtime construction is mandatory:

```text
p, q → parse as explicit unsigned test data → n = p × q → assert bitLength(n) = 2048
  → ULong2048.FromBigInteger(n) → Rsa2048PublicKey(n, e)
```

The fixture factory shall prove `p != q`, require the expected factor widths, calculate `n` exactly, record `n % p = 0` and `n % q = 0`, and reject any product that is not precisely 2048 bits. The static test-data representation is an input encoding, **not** a false language integer constant. Test signing material, if used to create positive PSS test vectors, remains test-only and must never enter the production library.

## 7. QA architecture preview

The next QA step must establish deterministic vector-based tests before code is written.

| QA group | Required evidence |
|---|---|
| Positive PSS | RSA-2048 runtime fixture formed as `n=p×q`, plus at least one PSS-SHA256 valid vector and BCL-generated independent positive case |
| Fixture arithmetic | Verify versioned `p`, `q`, `p != q`, exact `n=p×q`, `n % p = n % q = 0`, and exact 2048-bit modulus width before every dependent vector |
| Signature domain | `s = n`, oversized 257-byte input, 255-byte input, valid zero-padded 256-byte format |
| Key validation | even, short, zero, `e=1`, even exponent, `e >= n` |
| EMSA-PSS structure | Mutate unused bit, trailer, PS, separator and salt length independently |
| MGF/hash | Mutate masked DB and final hash separately |
| Parity | Custom `ULong2048` public operation equals `BigInteger.ModPow` and BCL verification agrees on positive/negative cases |
| Regression | Existing Numerics, Core, Finance and Lean gates remain unchanged and pass |

## 8. Explicit non-goals

This architecture does not authorize private-key signing, key generation, certificate parsing/trust policy, PEM/DER import, RSA-OAEP, PKCS#1 v1.5, variable-size modulus support or multi-profile fallback. Each requires a separately versioned decision.

## 9. Approval decision requested

Approve this architecture contract to authorize **only Step 3 — QA specification and vector plan**. Implementation remains blocked until QA design is separately approved.

## References

[1] [RFC 8017 — PKCS #1: RSA Cryptography Specifications Version 2.2](https://datatracker.ietf.org/doc/html/rfc8017), particularly Sections 4.1–4.2, 5.2.2, 8.1.2, 9.1.2 and B.2.1.
[2] [NIST FIPS 186-5 — Digital Signature Standard](https://csrc.nist.gov/pubs/fips/186-5/final).
