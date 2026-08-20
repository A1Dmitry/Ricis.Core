# Removal / Migration Decision — Fermat Numeric API to Ricis.Numerics

**Status:** Approved by explicit user instruction: “лучше ферма перетащи в нумерик.”

**Decision date:** 2026-08-20.

## Decision

The numeric Fermat factorization baseline and immutable semiprime numeric domain belong to `Ricis.Numerics`, not `Ricis.Core`.

`Ricis.Core` remains a symbolic reduction, proof and document engine. It must not obtain a `ProjectReference` to `Ricis.Numerics`, name Numerics factorization types, or add a late-bound/reflection adapter. Numeric computation is performed in Numerics; a caller may provide immutable primitive/value evidence to Core proof facilities at its own boundary.

## Public surface affected

| Existing Core member | Action |
|---|---|
| `Ricis.Core.Solvers.Fermat.FermatFactorizer.Solve(BigInteger)` | Migrate as `Ricis.Numerics.Factorization.FermatFactorizer.Solve(BigInteger)`. |
| `Ricis.Core.Solvers.Fermat.FermatFactorizationResult` | Migrate as `Ricis.Numerics.Factorization.FermatFactorizationResult`. |
| `Ricis.Core.Solvers.Fermat.FermatFactorizer.ProveDocument(...)` | Remove from numeric API; Core document composition is a separate symbolic-proof responsibility and cannot stay coupled to the numeric solver without violating the project boundary. |
| New `SemiprimeBase<T>`, `Semiprime<T>` | Relocate to `Ricis.Numerics.Factorization` before commit. |

## Caller graph

A repository-wide source audit found no production caller outside the solver implementation. The only Core callers are direct regression cases in `RegressionTests/RicisFermatSystemSuite.cs`:

1. `FermatFactorizer.ProveDocument(...)` in FERMAT01;
2. `FermatFactorizer.Solve(...)` in FERMAT03;
3. `FermatFactorizer.Solve(...)` in FERMAT04.

These numeric regressions move to `Ricis.Numerics.UnitTests`. The Core catalog retains symbolic-only regression coverage and no longer validates numeric factorization through a forbidden dependency.

## Compatibility and safety

This is an explicit public API migration with direct replacement tests in Numerics. No silent deletion occurs. The Core source graph must remain Numerics-free, and no code may convert a Numerics instance to `BigInteger` merely to bypass the new boundary. The old API is not retained as a forwarding wrapper because that would require Core→Numerics coupling or reflection, both prohibited by the approved architecture.

## Verification obligations

1. `Ricis.Numerics` direct tests cover every migrated public `Solve` and immutable `Semiprime` public member.
2. Core regression tests no longer import the migrated factorization namespace.
3. `Ricis.Core.csproj` has no Numerics reference; source hygiene finds no `Ricis.Numerics` identifier in Core production source.
4. Full solution, Core, Numerics, Finance and Lean gates remain green.
