# ReSharper Inspection Policy

**Status:** mandatory project policy.

## 1. Purpose

`IssuesReport.xml`, generated at solution scope by ReSharper, is a mandatory static-analysis inventory for cleanup work in `Ricis.Core`. It is used together with current Release build, direct regression tests, Lean-artifact validation and API compatibility policy. An inspection finding is neither ignored nor converted into an automatic deletion: it starts a dependency-aware remediation decision.

The project does **not** use reflection as a supported reachability mechanism. Therefore repository call graph, explicit HTTP/JSON/generated-code contracts and direct tests are the admissible evidence for a candidate's reachability. Dynamic reflection is not a justification for preserving otherwise unreferenced code.

## 2. Mandatory decision rule

> Before changing or removing a method, lambda path, extension method, field, record property, parameter or type, establish its caller/contract graph and create or confirm a direct regression test for every executable path that must remain.

A method that accepts, builds, transforms or returns an `Expression`, `LambdaExpression`, `Expression<TDelegate>`, deferred RICIS node, proof trace or Lean artifact is treated as **potentially semantically active** even when a local inspection reports no call. Its retained behavior must have a direct test. It may be removed only after a zero-caller graph and an explicit confirmation that no public/API/document/schema contract requires it.

## 3. Inspection priority model

| Priority | ReSharper classes | Required action | Release effect |
|---|---|---|---|
| P0 | `CSharpErrors`, current compiler errors, `AssignNullToNotNullAttribute`, `CS8600`, `CS8602`, real `PossibleNullReferenceException` in production paths | Reproduce with current compiler/test or classify documented false-positive; add regression before correction | Blocks release when reproduced. |
| P1 | `ConditionIsAlwaysTrueOrFalse`, `ConditionalTernaryEqualBranch`, `PossibleMultipleEnumeration`, `CompareOfFloatsByEqualityOperator`, `EqualExpressionComparison` in semantic/proof/payment code | Establish intended semantics, write direct/mutation test, correct only if behavior is wrong or redundant | Blocks affected feature acceptance until triaged. |
| P2 | `UnusedMember.Local`, `UnusedParameter.Local`, `OutParameterValueIsAlwaysDiscarded.Local`, `CollectionNeverQueried.Local` | Prove leaf reachability, write preserving test, remove or redesign in an atomic batch | Required in cleanup sprint; no blind deletion. |
| P3 | `UnusedMember.Global`, `UnusedMemberInSuper.Global` | Treat as public compatibility node; preserve, deprecate with migration, or remove only through a versioned compatibility decision | No removal in patch cleanup. |
| P4 | `NotAccessedPositionalProperty.Local` on JSON/source-generated/provider records | Treat as serialization/wire contract until schema and payload tests prove otherwise | Preserve by default. |
| P5 | Naming, spelling, markup, redundant syntax and style suggestions | Handle in a dedicated non-semantic cleanup batch with dictionary/configuration updates | Never mixed with proof/transport behavior changes. |

## 4. Reachability evidence

A candidate has one of the following classifications.

| Classification | Minimum evidence | Allowed action |
|---|---|---|
| `private_leaf` | Exact repository search gives no callers except declaration; no source generation, JSON or test hook; affected behavior covered | Remove in a test-first atomic batch. |
| `private_state_decision` | Field has no read but writes occur through a public operation | Redesign public behavior or expose intentional state; do not delete writes blindly. |
| `public_compatibility` | Public/protected/interface/extension member or type | Preserve or add `[Obsolete]` and migration documentation; no patch removal. |
| `serialization_contract` | Positional record, `JsonSerializable`, `JsonPropertyName`, HTTP payload or document schema member | Preserve; add/retain serialization contract test. |
| `proof_or_lean_contract` | Typed log, proof document, expression/trace route, Lean source/evidence or public proof extension | Preserve unless a dedicated proof/Lean regression proves removal safe and policy owner approves. |
| `future_domain_capability` | Finance port, lifecycle state, policy enum or documented backlog capability | Preserve and record as intentionally reserved. |
| `inspection_false_positive` | Current Release build and targeted test contradict inspection; source-generator/annotation reasoning documented | Suppress/configure narrowly or retain finding with evidence; do not distort working code. |

## 5. Lambda, expression and extension method rule

All potentially executable lambda/expression paths must be covered by direct tests before cleanup.

| Artifact type | Required direct test |
|---|---|
| `Expression<Func<...>>` input | Valid parse/construct, simplify/derive result and invalid input boundary. |
| Expression visitor/reducer | Before/after structural equivalence and a negative case preserving non-reducible/impure/lifted shape. |
| Public extension method | Direct invocation through extension syntax plus expected behavior/exception contract. |
| Proof extension | Canonical trace/doc evidence, no expression compilation/execution of hypotheses, status boundary. |
| Lambda formatting/parser helper | Round-trip/formatting behavior and parser rejection where applicable. |
| Removed private lambda helper | Test the higher-level observable behavior that previously depended on it. |

No deletion is allowed merely because a lambda-oriented method has no direct repository caller. If it remains public or part of the documented pipeline, it receives a direct regression test. If it is private and has a proven zero caller graph, its observable owner behavior receives a regression test before removal.

## 6. JSON, source generation and provider DTO rule

ReSharper local usage analysis may not observe `System.Text.Json` source generation or provider payload serialization. Positional properties inside `JsonSerializerContext` models and bePaid request records are presumed used by schema/wire contracts. They must be retained unless a test proves both:

1. the serialized JSON schema/payload is unchanged or intentionally versioned; and
2. the removed field is absent from the official receiver/provider contract.

## 7. Float and condition rule

`CompareOfFloatsByEqualityOperator` does not authorise a global epsilon rewrite. Each occurrence is classified as one of:

- exact/discrete constant invariant;
- test assertion with explicit tolerance helper;
- numerical approximation requiring an explicit documented tolerance;
- defect requiring a test-first correction.

`ConditionIsAlwaysTrueOrFalse` and equal ternary findings require a semantic explanation. A condition may be intentionally redundant to preserve an expression capture, a diagnostic trace or an RICIS structural boundary. Such intent must be covered by direct test or simplified only after behavior is locked.

## 8. Mandatory gates

Every cleanup batch must run:

```text
dotnet build Ricis.Core.sln --configuration Release
dotnet run --project RegressionTests/Ricis.Core.RegressionTests.csproj --configuration Release
dotnet run --project Ricis.Finance/Ricis.Finance.RegressionTests/Ricis.Finance.RegressionTests.csproj --configuration Release
python3 scripts/verify_lean_artifacts.py
```

The post-change ReSharper report must be compared by category, not only by total count. A lower total cannot justify a regression in build, public API, JSON schema, provider payload, proof status, trace, LaTeX or Lean boundary.

## 9. Prohibited actions

The following are prohibited:

1. global delete of all `UnusedMember.Global` findings;
2. deleting proof/log/provider DTO positional properties because local access is absent;
3. replacing numeric equality globally with epsilon;
4. removing a public or extension method without its direct regression and compatibility decision;
5. suppressing P0/P1 findings without current build/test evidence;
6. weakening HTTPS, webhook, Lean, trace or public validation guards to satisfy an IDE style finding;
7. claiming reflection reachability or reflection absence as the sole proof of safety.
