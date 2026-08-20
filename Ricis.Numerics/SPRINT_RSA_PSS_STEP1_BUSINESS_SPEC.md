# SPRINT RSA-PSS — Step 1: Business and Security Specification

**Status:** Proposed — awaiting explicit user approval
**Owner:** Ricis.Numerics
**Agile sequence:** Business analysis → Architecture → QA tests → Implementation
**Target capability:** RSA-2048 public-key signature verification using **RSASSA-PSS / SHA-256**.

## 1. Business objective

The project now has a fixed-width unsigned `ULong2048` domain that performs the raw public RSA operation `s^e mod n`. The business objective of this sprint is to make that mathematical result usable as an **authenticated signature verification decision** for RICIS artifacts and future external document/proof interchange.

The distinction is mandatory. A successful public modular exponentiation is not a signature-verification decision: RFC 8017 separates RSAVP1 from RSASSA-PSS and requires the recovered encoded message to pass EMSA-PSS verification before a signature is valid.[1]

> **Definition.** This increment verifies an RSA-PSS signature using a supplied public key. It does not create keys, hold private material, sign data, encrypt data, parse certificates, or claim PKI trust-chain validation.

## 2. Scope

The first implementation profile is deliberately narrow and explicit:

| Dimension | Step 1 decision |
|---|---|
| Scheme | RSASSA-PSS verification |
| Message hash | SHA-256 |
| MGF | MGF1 with SHA-256 |
| Modulus domain | Exact 2048-bit RSA public modulus represented by `ULong2048` |
| Public exponent | `ULong2048`; normal `65537` must be supported but not hard-coded |
| Salt policy | Fixed 32 bytes — equal to SHA-256 output length |
| Input key material | Caller supplies modulus and public exponent as already validated number values; PEM/DER/X.509 parsing is out of scope |
| Result | Boolean validity plus structured, non-secret diagnostic code suitable for logs/QA |
| Raw operation | `ULong2048.RsaPublicOperation` remains public but is explicitly labelled insufficient for authentication alone |

The choice of PSS-SHA-256 and fixed salt length is a profile decision, not a hidden default. It keeps verification deterministic at the API boundary and makes security tests unambiguous.

## 3. Required business behavior

The future public verification API shall accept a message byte sequence, a fixed-width signature representative and an RSA public key `(n, e)`. It shall return **valid** only when every condition below is true.

| ID | Required behavior | Acceptance criterion |
|---|---|---|
| RSA-PSS-B01 | Validate public-key/signature input domain | Reject `n <= 1`, even/invalid modulus profile, `e` outside allowed public domain, and `s >= n` |
| RSA-PSS-B02 | Execute only the public operation | Recover `m = s^e mod n` through the existing `ULong2048` public RSA path; no private exponent API is introduced |
| RSA-PSS-B03 | Respect fixed RSA-2048 representation | Convert recovered representative to a 256-octet big-endian encoded message; reject incompatible key/encoding size |
| RSA-PSS-B04 | Verify PSS encoding | Enforce trailer byte, unused leading-bit rule, DB/PS structure, MGF1 unmasking, salt length and recomputed SHA-256 hash |
| RSA-PSS-B05 | Fail closed | Any malformed input, range violation, encoding mismatch or hash mismatch returns invalid; no partial acceptance or fallback to raw RSA |
| RSA-PSS-B06 | Preserve auditability | Return a stable diagnostic category without revealing private material or creating an oracle for signing/decryption |
| RSA-PSS-B07 | Preserve existing contracts | Existing `ULong2048`, `BigInteger` interop, tests and raw `RsaPublicOperation` behavior remain compatible |

Digital signatures are used to detect unauthorized data modification and to authenticate the claimed signer; this is the relevant business outcome of the verification decision.[2]

## 4. Security and quality constraints

The verification code must operate only on public data. The first increment therefore has no private-key timing-hardening claim. Nevertheless, it must preserve ordinary secure engineering controls: strict bounds checking, no silent narrowing, no permissive encoding recovery, explicit profile parameters and exact test vectors.

Malformed signatures are untrusted data. The verifier must treat their bytes only as input, must not follow any embedded instruction, must not write files, call network services or downgrade to another signature scheme. Public verification failure is a normal business result; it is not an exception path unless the caller violates the programming contract such as passing a null required object.

| Constraint | Required decision |
|---|---|
| Cryptographic boundary | Verification only; signing/private keys excluded |
| Key-size profile | Exactly RSA-2048 in the first increment |
| Algorithm agility | No hidden agility. A future PSS-SHA-384/512 or PKCS#1 v1.5 profile requires a separate versioned decision and tests |
| BigInteger role | Explicit conversion/oracle/provider boundary; not a hidden replacement for `ULong2048` fixed-width domain |
| Result observability | Stable error code for QA/logging, without raw internal state dumps by default |
| Tests | Published valid vectors plus negative mutations for every PSS structural requirement |
| Performance | Correctness is mandatory. Benchmark evidence is informative; no wall-clock CI threshold |

## 5. Non-goals and deferred work

The following are explicitly **not** part of Step 1 or its later implementation without a new approved specification.

| Deferred item | Reason |
|---|---|
| Private-key RSA signing | Different threat model and secret-data handling |
| Key generation/factor generation | Not needed for public verification |
| PEM, DER, X.509, CMS or certificate-chain parsing | Separate interchange/trust domain |
| Trust-anchor and revocation policy | Product security policy, not arithmetic |
| RSA-OAEP/encryption/decryption | Separate algorithm and security contract |
| PKCS#1 v1.5 verification | Compatibility profile requiring separate acceptance criteria |
| RSA-PSS profile agility | Must not be introduced through defaults or implicit fallback |
| Constant-time private operations | No private operation exists in this sprint |

## 6. Dependencies and delivery sequence

The sprint depends on the already completed `ULong2048` fixed-width public RSA operation, its Montgomery path, exact BigInteger interoperability and direct test suite. It adds a byte-oriented encoding layer above that arithmetic, rather than modifying symbolic Core.

| Next Agile step | Deliverable after approval |
|---|---|
| Step 2 — Architecture | Public API shapes, result/error taxonomy, byte order, dependencies, provider boundary and exact class ownership |
| Step 3 — QA | RFC-derived/vector-backed tests, negative mutation matrix and platform parity plan |
| Step 4 — Implementation | `RsaPssSha256Verifier` implementation only after Step 2/3 approvals |
| Step 5 — Deploy | Full solution gate, benchmark/parity evidence, versioned security artifacts and GitHub publication |

## 7. Approval decision requested

Please approve or amend the following product decision:

> Implement **RSA-2048 RSASSA-PSS verification with SHA-256, MGF1-SHA-256 and a fixed 32-byte salt**, using `ULong2048` for the public fixed-width RSA operation and returning a fail-closed validity result with structured diagnostics. Private-key actions, certificate parsing/trust validation, PKCS#1 v1.5 and algorithm fallback remain out of scope.

Approval authorizes only **Step 2: architecture contract**. It does not authorize code implementation until Step 2 and Step 3 are separately accepted.

## References

[1] [RFC 8017 — PKCS #1: RSA Cryptography Specifications Version 2.2](https://datatracker.ietf.org/doc/html/rfc8017), Sections 4.1–4.2, 5.2.2, 8.1.2, 9.1.2 and B.2.1.
[2] [NIST FIPS 186-5 — Digital Signature Standard](https://csrc.nist.gov/pubs/fips/186-5/final), abstract and final publication.
