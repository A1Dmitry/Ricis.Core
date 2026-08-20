# RSA-PSS External Vector Research — 2026-08-20

## Purpose

This note records the source investigation performed for **PSS04** in the approved RSA-PSS QA specification. It is evidence only; it does not authorize a change to the fixed RSA-2048 / SHA-256 / MGF1-SHA256 / 32-octet-salt profile.

## Verified sources

| Source | Finding | Relevance |
|---|---|---|
| [NIST CAVP — Digital Signatures](https://csrc.nist.gov/projects/cryptographic-algorithm-validation-program/digital-signatures) | The official CAVP page states that RSA response files are available as test vectors for FIPS 186-2 and FIPS 186-4 and identifies RSA2VS. It cautions that use of vectors does not itself replace CAVP validation. | Authoritative source category for PSS04. |
| [PyCryptodome PSS test loader](https://raw.githubusercontent.com/Legrandin/pycryptodome/master/lib/Crypto/SelfTest/Signature/test_pss.py) | The inspected source references `SigVerPSS_186-3.rsp` under `Signature/PKCS1-PSS` and also lists RSA-PSS SHA-256 MGF1 32-byte-salt Wycheproof inputs. | Confirms the expected NIST filename and a compatible profile family, but is not itself an official vector artifact. |

## Current decision

No external RSA-2048 vector is committed in this increment because the audited source reads did not supply an artifact that simultaneously carries the required public verification values and supports this repository's mandatory runtime `n = p × q` fixture rule. The currently committed test data must therefore be described truthfully as a **versioned BCL-interoperability regression vector**, not as NIST/CAVP data.

PSS04 remains a tracked follow-up requirement: import an official vector only after saving its source URL, retrieval hash, license/provenance note, and an auditable rule-compliant p/q construction path. This is a QA-evidence gap, not permission to weaken the fixture contract or silently substitute an undocumented vector.

## Safety and scope

No external file was executed. No PEM, DER, X.509 parsing, private-key signing API, fallback algorithm, or BigInteger operation was introduced into the production `Ricis.Numerics` assembly during this source investigation.
