# ReSharper Batch B — completed remediation evidence

**Status:** completed locally; ready for Git commit.

## Scope completed

Batch B processed only findings whose current semantic intent was verified. The work remained dependency-driven: every code simplification either had a new direct regression or an existing observable contract, and no public/proof/serialization/provider contract was removed.

| Finding family | Change | Safety evidence |
|---|---|---|
| Equal ternary branches | Replaced identical conditional branches in `RicisMultivariateAlgebraicVisitor.TryRemoveCommonRight` with one `Expression.Subtract` call | `RSH05` proves `(x + shared) - (y + shared) → x - y` exactly. |
| Redundant proof condition | Removed impossible `quotient is null` branch after a non-null factor determines a binary operand | Existing academic linear/proof protocol tests retain the document contract. |
| Simplifier nullability | Replaced unsafe Boolean casts with `ConstantExpression { Value: true/false }` patterns | Logical/pipeline regression suites remain green; non-Boolean/null constants are no longer cast. |
| Trace/polar nullable traversal | Converted nullable visitor results into controlled node preservation/non-evaluation | Typed trace and public polar utility suites remain green. |
| Provider URI checks | Removed only redundant `Uri.TryCreate` null checks; absolute URI/HTTPS checks are retained | `FIN14` proves a non-HTTPS bank deep link remains rejected. |
| Test nullability | Made `RicisType` null fixtures explicitly nullable and used static equality where appropriate | Existing `API13` contract remains direct and full solution builds without nullable warnings. |

## New direct regression IDs

| ID | Contract |
|---|---|
| `RSH05` | Exact common-right multivariate subtraction does not broaden into an unsafe algebraic rewrite. |
| `FIN14` | A provider-advertised bank application with an `http` deep link is rejected. |

## Public compatibility result

`RESHARPER_PUBLIC_COMPATIBILITY_INVENTORY.md` classifies every remaining `UnusedMember.Global` candidate as one of public structural API, proof/log compatibility API, solver API, legacy façade, Finance port or lifecycle capability. No candidate is deleted in this batch. Direct-test reservations `API17–API32` and `FIN15–FIN18` provide the next ordered public-API coverage work.

## Intentionally retained ReSharper candidates

The following findings remain deliberately deferred, because their current code is semantic/protocol safety rather than cleanup:

1. Null guard for values inside `IReadOnlyDictionary<MobilePlatform, Uri>` in `BankApplicationOption` is retained as defensive provider-boundary validation; `FIN14` covers the important HTTPS rejection.
2. Console and regression-test equal ternaries are not mixed into Core semantic remediation.
3. Source-generated proof-log JSON and bePaid wire DTO properties remain serialization contracts.
4. Public/extension methods, Finance ports and domain states remain compatibility/domain nodes until direct API coverage and a later deprecation decision.

## Mandatory quality gates

| Gate | Result |
|---|---:|
| `dotnet build Ricis.Core.sln --configuration Release` | PASS, **0 warnings, 0 errors** |
| Core regression harness | PASS, **358/358** |
| Finance regression harness | PASS, **14/14** |
| `python3 scripts/verify_lean_artifacts.py` | PASS, **6 artifacts** |
| `git diff --check` | PASS |
