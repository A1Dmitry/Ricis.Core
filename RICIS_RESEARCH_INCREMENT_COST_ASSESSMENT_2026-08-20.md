# Cost assessment of expensive research increments — 2026-08-20

**Purpose:** close the cost-ordering analysis without pretending that expensive research is complete.

## Generic Fermat / INumber<T>

| Dimension | Assessment |
|---|---|
| Current proven base | Core generic reduction, Numerics `Semiprime<T>`, ULong2048 exact root and bounded BigInteger Fermat baseline. |
| Missing semantic work | Generic exact-root capability, bit-length/order capability, residue tables, bounded profile execution and full BFR-E01–E08 evidence. |
| Cost | **XL**: architecture plus Int2048/ULong2048 QA and cross-bit-length research corpus. |
| Hard blocker | `INumber<T>` alone does not provide every capability; BigInteger fallback is forbidden. |
| Forbidden shortcut | Do not convert ULong2048 to BigInteger and call that generic Fermat. Do not infer O(1) from fixed-width mask operations. |
| Entry gate | Approved capability contract and QA matrix with public-input-only profile. |

## Subject-matter Lean root-to-leaf proof

| Dimension | Assessment |
|---|---|
| Current proven base | Concrete rank-one RICIS engine invariant, local phase composition, route-simulation detector and explicit open boundary. |
| Missing semantic work | Typed propositions, hypotheses, definitions, local theorems and semantic bridges for each named external domain node and every edge. |
| Cost | **XXL**: separate formalization program per domain; cannot be closed by route labels or identity payload transformations. |
| Hard blocker | Current map JSON does not provide sufficient typed mathematical statements to formalize without inventing premises. |
| Forbidden shortcut | Do not classify `edge := { payload with node := next }` as subject-matter proof. Do not promote `KernelChecked` engine invariant to theorem about Hodge/Poincaré/etc. |
| Entry gate | Domain-specific propositions and approved scope for the first external node, then incremental Lean proofs. |

## Decision

Neither research increment is started in this cost-ordered sprint. The sprint closes their assessment, records the hard blockers, and keeps the work visible in the next backlog. This is a completed planning/evidence step, not a mathematical proof claim.
