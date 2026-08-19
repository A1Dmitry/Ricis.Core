# Lean artifact inventory

## Текущие собственные Lean sources

| Artifact | Содержимое | Evidence |
|---|---|---|
| `FormalVerification/Lean/RicisIdentity/TypeIdentity.lean` | ID-01–ID-06, collapsed type guard, exact rational model, `#print axioms id06_exact_half` | Kernel-compilable structured theorem source |
| `FormalVerification/Lean/Generated/ComplexSingularityA6.lean` | Structured A6 payload, `a6_indexed_zero_infinity_bridge`, commutative payload theorem | Kernel-compilable generated theorem source |

External `.lake` and Mathlib sources are dependencies and are not project evidence artifacts.

## JAC-001 fixed outputs

The structured Lean export emitted by `--jacobian-proof-lean` is fixed at `FormalVerification/Lean/Artifacts/jacobian/JacobianProof.lean` and registered as `RICIS-JAC-001-KERNEL-EXPORT`. The typed-log report is fixed separately at `FormalVerification/Lean/Artifacts/audit/JacobianAuditReport.lean` and remains `AuditOnly`; it is not presented as a kernel theorem. The LaTeX rendering remains a rendered document output and is validated independently by the CI LaTeX gate.

## Evidence distinction

A kernel-compiled theorem source is `KernelChecked`. A regression assertion that checks the generated source, trace, or renderer boundary is `RegressionChecked`; it does not become a Lean theorem automatically. A comment-only typed-log Lean report is `AuditOnly`. A LaTeX PDF is `RenderedOnly` unless its originating structured Lean artifact is separately compiled.

## Operational gate

`FormalVerification/Lean/Artifacts/manifest.json` is the authoritative registry. `scripts/verify_lean_artifacts.py --compile` validates the manifest, trust boundaries, source existence, forbidden markers, and compilation of every registered source with the pinned Lean toolchain. CI invokes this command after Lean setup, so a missing, altered, or uncompilable declared artifact fails the quality gate.
