# RSA-PSS normative findings — 2026-08-20

## Scope of the proposed next Agile increment

The candidate increment is **public-key RSA-PSS verification using SHA-256**. It is not private-key signing, key generation, encryption, OAEP, certificate parsing, ASN.1 parsing, or a replacement for platform cryptographic providers.

## Normative algorithm findings

RFC 8017 defines the individual public verification primitive RSAVP1 separately from signature schemes. `ULong2048.RsaPublicOperation(s, e, n)` corresponds only to the mathematical public operation `s^e mod n`; it is therefore insufficient to conclude that a signature is valid. Complete RSASSA-PSS verification must also:

1. Validate the RSA public-key and signature-representative input domain, notably `0 <= s < n`.
2. Apply RSAVP1 to recover the encoded message representative.
3. Convert it to the precisely sized big-endian encoded message through I2OSP.
4. Verify EMSA-PSS with a fixed, explicit profile: SHA-256 both for message hash and MGF1, and a fixed salt-length policy.
5. Treat every malformed encoding, length violation, trailer mismatch, unused-bit violation, MGF-derived mismatch, or hash mismatch as a non-valid signature result, not as a partial success.

The design must retain the explicit separation between the raw public primitive and full signature verification. This protects callers from confusing a modular exponentiation result with authentication.

## Policy findings

NIST FIPS 186-5 identifies digital signatures as a mechanism for detecting unauthorized modification and authenticating a claimed signatory. For this project, the business requirement is therefore a boolean/public validation result with a structured diagnostic for audit and QA; it is not a private-key capability.

## Implementation boundary

| Boundary | Decision |
|---|---|
| Fixed-width modulus/signature representative | `ULong2048` public RSA operation |
| PSS encoded-message verification | byte-oriented SHA-256, MGF1-SHA256 and EMSA-PSS decoder |
| Interop/test oracle | `BigInteger` only at explicit conversion/oracle boundary |
| Private material | Out of scope; no signing or private exponent APIs |
| Security profile | RSA-PSS with SHA-256, fixed salt length, exact RSA-2048 modulus length |
| Failure policy | Fail closed: return invalid result for malformed signature/encoding; programmer misuse such as null public-key object follows ordinary argument validation |

## Sources

1. [RFC 8017 — PKCS #1: RSA Cryptography Specifications Version 2.2](https://datatracker.ietf.org/doc/html/rfc8017), especially Sections 4.1–4.2, 5.2.2, 8.1.2, 9.1.2 and B.2.1.
2. [NIST FIPS 186-5 — Digital Signature Standard](https://csrc.nist.gov/pubs/fips/186-5/final), publication page and cited final standard.
