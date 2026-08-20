# Route Proof Adversarial QA

**Status:** Implemented and regression-checked.
**Direct tests:** `RSA01`–`RSA05`.

## Purpose

This QA layer prevents a graph of labels and preserved payloads from being reported as a proof of the mathematical domains named by the graph. It is deliberately adversarial: it treats successful Lean compilation as evidence only for the theorem actually stated in the source.

## Detection rules

| ID | Detector | Required finding |
|---|---|---|
| RSA01 | Edge semantic body | A dependency edge that only updates `node` while preserving every payload field is `SimulatedRoute`. |
| RSA02 | Local-stage bodies | Identity implementations of named phases without node-specific propositions are `SimulatedRoute`. |
| RSA03 | Subject proposition inventory | A route node name without a typed domain proposition, hypotheses and local theorem is `Open`, not `ProvedSubjectMatter`. |
| RSA04 | Evidence status boundary | `KernelChecked` is retained for the actual Lean theorem but cannot upgrade an engine invariant to an external subject theorem. |
| RSA05 | Structural certificate fields | Preservation fields supplied as premises are `ConditionalTheorem`, not independently proved node semantics. |

## Current finding

`LongestRouteConcreteEngineProof.lean` is correctly classified as a concrete `ProvedEngineInvariant`: it proves the rank-one determinant payload invariant from the root label to the leaf label. RSA01–RSA03 also identify that the named external domains are not semantically implemented: `edge` only changes the route label, four local stages are identity functions, and no Hodge/Poincaré/Atiyah–Singer/Morse/knot/spectral propositions are defined.

`LongestRouteSpectral.lean` is correctly classified as `ConditionalTheorem` because its preservation fields are explicit premises.

## False-positive boundary

The detector must not reject a valid engine invariant merely because it is structural. It rejects only a stronger status claim. A concrete RICIS theorem may remain `KernelChecked` and `ProvedEngineInvariant`; it may not be labelled `ProvedSubjectMatter` without typed subject definitions and semantic bridge theorems.

## Acceptance gate

A future route may receive `ProvedSubjectMatter` only when RSA01–RSA05 produce no simulation/open finding for the claimed nodes, each edge has a semantic transition theorem, and the Lean source compiles without `sorry`, `admit`, or hidden axiom placeholders. The C# direct suite and the Lean artifact remain separate evidence channels.
