# ReSharper Batch A — test-first regression matrix

**Status:** QA specification before implementation.

## Test strategy

Batch A may remove only proven private leaves or simplify private signatures. Tests are written first around the observable owners; private helper implementation is not tested through reflection. Every expression/lambda path retains a positive case and an invalid/non-reducible case.

| ID | Candidate / owner | Existing evidence | New direct regression contract | Negative/safety contract |
|---|---|---|---|---|
| `RSH01` | `ExpressionSimplifierVisitor` private `_parameters`, `SimplifyFraction`, `ToBigInteger` | `RicisCsharpInvariantSuite`, `RicisPipelineSafetySuite`, `RicisLogicalReductionSuite` | Simplification of typed numeric lambda retains same structural/evaluation result after private leaf removal | Logical/conditional rules do not alter an impure/non-reducible operand. |
| `RSH02` | `SingularitySolver.IsTranscendentalComposite` private leaf | Existing solver and stress suites | Trigonometric/log/composite expressions retain root/rejection behaviour | Non-finite, non-polynomial and singular expressions remain controlled rejections. |
| `RSH03` | `TryGetPositiveConstant(..., out value)` private helper | Existing solver suites | Positive literal and positive ratio preserve acceptance after Boolean-only refactor | Zero, negative, non-finite and non-constant input are rejected identically. |
| `RSH04` | `AppendLinearSystemProtocol.solutionX/solutionY` private parameters | `RicisAcademicProofSuite.LinearSystemProof` | Linear-system proof has same verification status and deterministic document trace after parameter pruning | Unsafe/non-finite linear system remains rejected. |
| `RSH05` | `ProviderPayment.NormalizeCurrency.parameterName` private parameter | Finance regression suite | Valid currency normalizes to the same immutable value | Invalid/empty currency keeps exact exception/error contract. |
| `RSH06` | `RicisEngine._terms` public-owner state decision | `QA11` proves finite expression rejection | `RicisEngine` exposes an immutable/read-only term view; successful indexed-infinity `Add` increases count and preserves stored typed infinity | Finite/indexed-zero/non-infinity expression rejection leaves term count unchanged. |
| `RSH07` | Source-generated typed proof-log JSON DTOs | Existing typed proof-log suite | `RicisProofLogJsonContext.Default` serializes all required v1 schema fields under a Release build | Missing/invalid log entry cannot be emitted as valid schema. |
| `RSH08` | Algebraic method-call traversal multiple enumeration | Algebraic reduction/safety suites | Visiting a method call transforms each argument exactly once, preserving method/argument structure | User-defined/unsupported method call stays structurally unchanged when no safe reduction exists. |

## Lambda/extension inventory rule for this batch

| Owner | Lambda/expression route | Required invocation form |
|---|---|---|
| `ExpressionSimplifierVisitor` | `Expression<Func<double,double>>` / Boolean expression body | Construct tree, invoke visitor, compare output tree/evaluation where pure. |
| `SingularitySolver` | Deferred expression root | Call public solver entrypoint through existing suite; inspect controlled root/rejection output. |
| `RicisAcademicProofExtensions` | Binary lambda system claim/constraints | Call public extension (`Prove...`) with real lambdas, verify trace/document status without compiling hypotheses. |
| `ProviderPayment` | `Money`/currency value object | Call public constructor/domain path; assert value and exception contract. |
| `RicisEngine` | `Expression<Func<double,double>>` | Call `Add` and new read-only output API; assert atomic state. |
| Typed proof log | `ILog<T>`/typed trace | Use existing public renderer/log call; assert schema, not private records directly. |

## Definition of Ready for implementation

1. The test IDs above are registered in the existing self-contained harness.
2. Each test is run against pre-change code and passes, demonstrating it locks an existing public/observable invariant.
3. The candidate has a zero repository caller graph or an intentional state-design decision.
4. The code change is confined to Batch A; no public Core/Finance API is silently removed.
5. Full Core, Finance and Lean gates remain green after every atomic subset.
