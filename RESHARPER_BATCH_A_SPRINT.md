# ReSharper Batch A — completed remediation evidence

**Status:** completed locally; ready for Git commit.

## Scope completed

Batch A applied only dependency-approved private cleanup and one explicit public-state repair. It did not remove any globally visible API, proof/Lean contract, source-generated JSON member, provider wire DTO property, Finance port or documented lifecycle state.

| ID | Change | Dependency decision | Preserved observable contract |
|---|---|---|---|
| A-01 | Removed `ExpressionSimplifierVisitor._parameters`, `SimplifyFraction`, private `ToBigInteger` | All had zero repository caller graph | `RSH01` locks typed lambda simplification. |
| A-02 | Removed `SingularitySolver.IsTranscendentalComposite` | Private zero-caller leaf | `RSH02` locks positive rational-power root extraction. |
| A-03 | Replaced discarded `TryGetPositiveConstant(..., out _)` with `IsPositiveConstant(...)` | Output was never consumed; Boolean predicate is the actual contract | `RSH02` exercises the ratio exponent branch. |
| A-04 | Removed unused `solutionX`/`solutionY` parameters from private linear proof renderer | Numeric values were superseded by retained expression values | Existing `RicisAcademicProofSuite.LinearSystemProof` and rejection scenario remain active. |
| A-05 | Removed unused `parameterName` from private `FxSnapshot.NormalizeCurrency` | Validation remains delegated to `Money` | `FIN13` locks normalization and invalid-currency rejection. |
| A-06 | Materialised algebraic method-call arguments once | Avoided potential repeat visitor enumeration | `RSH03` locks method/argument structural result. |
| A-07 | Added `RicisEngine.Terms` immutable snapshot | `_terms` is now intentional observable collector state, not a write-only hidden field | `RSH04` locks successful collection, snapshot immutability and failed-add atomicity. |

## New direct regression coverage

| ID | Contract |
|---|---|
| `RSH01` | Typed lambda `x => x * 1` remains `x => x` through public simplifier visitor. |
| `RSH02` | `Math.Pow(x, 2/3)` retains structural root `x = 0`. |
| `RSH03` | Method call `Math.Sin(x + 0)` retains method identity and receives reduced argument `x`. |
| `RSH04` | `RicisEngine.Add(x => x / 0)` publishes one typed infinity snapshot; rejected finite expression does not mutate it. |
| `FIN13` | `FxSnapshot` normalizes canonical currency codes and rejects invalid code. |

## Mandatory gates

| Gate | Result |
|---|---:|
| `dotnet build Ricis.Core.sln --configuration Release` | PASS, 0 warnings, 0 errors |
| Core self-contained regression harness | PASS, **357/357** |
| Finance self-contained regression harness | PASS, **13/13** |
| `python3 scripts/verify_lean_artifacts.py` | PASS, **6 artifacts** |
| `git diff --check` | PASS |

## Deliberately deferred nodes

Public API candidates, source-generated proof-log JSON records, bePaid wire records, Finance ports and future lifecycle states remain unchanged. Their remediation needs individual compatibility/schema/provider evidence and will be handled through subsequent graph batches. The separate `RESHARPER_INSPECTION_POLICY.md` remains binding for them.
