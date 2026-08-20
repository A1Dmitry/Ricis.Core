# SPRINT RSA-PSS — Step 3: QA Specification and Vector Plan

**Status:** Proposed — awaiting explicit user approval
**Prerequisites:** Step 1 business/security specification and Step 2 architecture contract approved
**Implementation authorization:** Not granted by this document
**Profile under test:** RSA-2048 RSASSA-PSS / SHA-256 / MGF1-SHA256 / fixed 32-octet salt.

## 1. QA objective

The QA objective is to prove that the future verifier accepts only a structurally correct and hash-correct RSA-PSS signature under the approved fixed profile. Testing must separately establish:

1. the fixed-width public RSA operation is exact;
2. the encoded-message decoding follows the PSS rules rather than accepting a raw modular result;
3. every required malformed condition fails closed;
4. the 2048-bit modulus fixture is demonstrably constructed as `n = p × q` at runtime, not claimed as a C# numeric literal;
5. custom `ULong2048` results agree with independent `BigInteger` and BCL verification oracles.

## 2. Mandatory test fixture contract

The QA project will add one internal **test-only** `Rsa2048PssFixture`. It contains versioned non-secret test data as hexadecimal/byte encodings, never as C# 2048-bit integer literals.

| Fixture component | Representation | Required verification at fixture construction |
|---|---|---|
| `p`, `q` | Canonical big-endian hexadecimal test strings, expected 1024-bit primes | Parse as positive `BigInteger`; require `p != q`, expected bit widths and primality/provenance evidence |
| `n` | Runtime only: `p * q` | Require `bitLength(n) = 2048`, `n % p = 0`, `n % q = 0`; convert once to `ULong2048` |
| `e` | Ordinary compatible integer, normally `65537`, never hard-coded into production algorithm | Require odd `3 <= e < n` |
| `d`/CRT data | Test-only encoded data if required for local BCL signing | Must not be referenced by production assembly or public API |
| Message | Versioned byte array/string | SHA-256 input exactly defined, including empty and non-ASCII cases |
| Signature | 256-octet big-endian test vector | Parsed independently from numeric fixture and required to be `< n` |

The fixture constructor itself is a test target. A vector cannot run until its p×q reconstruction and 2048-bit checks pass. This makes the origin of the modulus auditable even though C# cannot express it as a native 2048-bit constant.

## 3. Positive vectors

| ID | Scenario | Independent oracle | Required result |
|---|---|---|---|
| PSS01 | Runtime p×q fixture, non-empty ASCII message, 32-byte salt PSS signature | BCL `RSA.VerifyData(..., RSASignaturePadding.Pss)` in test-only code | Valid / `None` |
| PSS02 | Same public key, empty message | BCL PSS verifier | Valid / `None` |
| PSS03 | Same public key, UTF-8 non-ASCII message | BCL PSS verifier | Valid / `None` |
| PSS04 | Pinned externally sourced RSA-2048 PSS-SHA256 verification vector | NIST CAVP or equivalent documented vector | Valid / `None` |
| PSS05 | Numeric signature overload and 256-byte overload | Exact equality of both result paths | Both valid / `None` |
| PSS06 | Raw public operation parity | `BigInteger.ModPow(s, e, n)` | Recovered EM equal byte-for-byte |

The external vector becomes a versioned project artifact with source URL, retrieval hash and licence/provenance note. It is a regression input, not executable untrusted content.

## 4. Negative mutation matrix

Each mutation starts from an independently known-valid PSS fixture. The test changes exactly one controlled element unless the test is specifically a multi-failure robustness test.

| ID | Mutation | Expected stable failure |
|---|---|---|
| PSSN01 | 255-byte signature | `SignatureLengthMismatch` |
| PSSN02 | 257-byte signature | `SignatureLengthMismatch` |
| PSSN03 | Numeric signature `s = n` | `SignatureRepresentativeOutOfRange` |
| PSSN04 | Numeric signature `s > n` | `SignatureRepresentativeOutOfRange` |
| PSSN05 | Flip an unused high bit in recovered `EM` test seam | `EncodedMessageUnusedBitsSet` |
| PSSN06 | Change trailer byte away from `0xBC` | `TrailerFieldMismatch` |
| PSSN07 | Change a `PS` zero octet | `PssDataBlockMismatch` |
| PSSN08 | Remove/move the `0x01` separator | `PssDataBlockMismatch` |
| PSSN09 | Make the decoded salt length not 32 | `PssDataBlockMismatch` |
| PSSN10 | Flip one masked data-block bit | `PssDataBlockMismatch` or `HashMismatch`, according to the first architecture-defined check reached |
| PSSN11 | Flip one `H` byte | `HashMismatch` |
| PSSN12 | Flip one message byte | `HashMismatch` |
| PSSN13 | Use valid signature under a different public modulus | Invalid; never `None` |
| PSSN14 | Use valid signature with changed public exponent | Invalid; never `None` |
| PSSN15 | Even, short, zero or invalid `(n,e)` constructor inputs | `ArgumentOutOfRangeException` at key construction |

The test seam used for EMSA-PSS structural mutations is package-internal and will be introduced only for the testable decoder contract; public verification remains byte-signature based. It exists to identify the exact rejected PSS condition without attempting to forge an RSA signature.

## 5. Cross-provider and encoding tests

| ID | Requirement |
|---|---|
| PAR01 | `ULong2048.RsaPublicOperation(s,e,n)` equals `BigInteger.ModPow(s,e,n)` for fixture signature and mutation representatives |
| PAR02 | I2OSP/OS2IP round-trip exactly preserves zero, leading-zero 256-octet form, `n-1` and `2^2048-1` at the appropriate boundary |
| PAR03 | Big-endian codec must reject non-256 signature length at the public verifier boundary; no silent padding/truncation |
| PAR04 | `ULong2048` custom public operation and BCL verifier agree on valid/invalid PSS cases within the approved profile |
| PAR05 | Every mixed integral overload already used in fixture construction remains covered by generated `ULong2048` tests; no test-specific arithmetic route is introduced |

## 6. Regression, performance and security gates

| Gate | Required result |
|---|---|
| RSA-PSS direct tests | All positive, negative, fixture, raw parity and codec tests pass independently in Test Explorer |
| `Ricis.Numerics.UnitTests` | Existing 34 tests plus the full RSA-PSS suite pass |
| Benchmark | Existing `ULong2048` custom-vs-BigInteger parity scenario still passes; no wall-clock threshold is used |
| Full solution | 14/14 project membership, Release build with 0 warnings/0 errors, Core MSTest, console regression, Finance regression and Lean evidence pass |
| Source hygiene | `git diff --check`, generated operator/test freshness and no private test fixture source linked into production assembly |
| Security review | No algorithm fallback; no raw public operation treated as signature validity; no private key API added to production |

## 7. Acceptance criteria for implementation approval

The user may approve Step 4 implementation only when accepting all of these commitments:

1. implementation is limited to the Step 1 fixed PSS-SHA256 profile;
2. every positive/negative group above is implemented before a completion claim;
3. RSA-2048 test modulus is runtime-constructed from versioned p and q;
4. `BigInteger` and BCL remain test/oracle boundaries, not hidden substitutions for the fixed-width production public operation;
5. any deviation discovered by an external official vector blocks release and is recorded as an incident.

## 8. Approval decision requested

Approve this Step 3 QA specification to authorize **Step 4 — implementation** of the exact planned profile. Any additional profile, signing, certificate or key parsing work remains a new Agile increment.

## References

[1] [RFC 8017 — PKCS #1: RSA Cryptography Specifications Version 2.2](https://datatracker.ietf.org/doc/html/rfc8017), Sections 4.1–4.2, 5.2.2, 8.1.2, 9.1.2 and B.2.1.
[2] [NIST CAVP — Digital Signatures](https://csrc.nist.gov/projects/cryptographic-algorithm-validation-program/digital-signatures), official test-vector resource.
[3] [NIST FIPS 186-5 — Digital Signature Standard](https://csrc.nist.gov/pubs/fips/186-5/final).
