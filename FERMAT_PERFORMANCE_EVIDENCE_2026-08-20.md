# Fermat Factorizer Performance Evidence — 2026-08-20

## Trigger and scope

Visual Studio CPU profiling isolated the slow test to the individual MSTest case that invokes `FermatFactorizer.ProveDocument` for `N = 1700000020900000063`. The stack attributed nearly all selected CPU time to `FermatFactorizer.Solve` and its repeated `IntegerSqrtExact(BigInteger)` calls. The public proof contract, factorization result, and document rendering contract were retained unchanged.

## Baseline and measured result

Both measurements used the same Release MSTest case, `Regression_142`, before and after the arithmetic change. Wall-clock timing includes normal VSTest process overhead and is recorded as comparative evidence, not as a CI threshold.

| Version | Exact-root algorithm | VSTest duration | Process wall time |
|---|---|---:|---:|
| Baseline | Binary search, one full-width `mid * mid` per iteration | 67 s | 85,488 ms |
| Step 1 | Decreasing integer Newton iteration | 13 s | 15,149 ms |
| Step 2 | Newton iteration plus exact square-residue prefilter modulo 64 | 6 s | 7,592 ms |
| Total improvement | Same exact result | approximately 11.2× | approximately 11.3× |

The baseline command was:

```bash
dotnet test UnitTests/Ricis.Core.UnitTests.csproj -c Release --no-build --filter 'FullyQualifiedName~Regression_142'
```

## Correctness invariant

`IntegerSqrtExact(n)` retains the pre-existing contract of returning `floor(sqrt(n))` for `n >= 0` and `-1` for `n < 0`.

> The initial estimate `r₀ = 2^ceil(bitLength(n)/2)` is an upper bound for `sqrt(n)`. For an integer upper estimate `r`, the iteration `r_next = floor((r + floor(n / r)) / 2)` is decreasing until it reaches the fixed floor root. The method returns only when the next estimate is no smaller, so the returned value is `floor(sqrt(n))`.

`IntegerSqrtCeiling` continues to distinguish exact squares through `floor * floor == n`; therefore its ceiling-root behavior is unchanged. The Fermat loop now applies an exact necessary-condition prefilter: a square modulo 64 is only one of `{0, 1, 4, 9, 16, 17, 25, 33, 36, 41, 49, 57}`. A delta outside this set cannot be a square and safely skips the expensive root calculation. A delta inside it is still subject to the independent exact checks `y*y == delta` and `p*q == n`; the residue test is never accepted as proof.

## Regression guards

The existing `FERMAT01` retains the original N-only proof/document scenario. `FERMAT03` was added as a direct public `FermatFactorizer.Solve` test; it verifies all exact structural invariants:

1. `P * Q == N`;
2. `X * X - N == Y * Y`;
3. `P == X - Y` and `Q == X + Y`.

`FERMAT04` covers all twelve square residue classes modulo 64 using semiprime Fermat reconstructions, preventing any future prefilter-mask edit from rejecting a valid exact-square delta.

A hard wall-clock assertion is deliberately not used in CI: shared runners and developer machines make such thresholds flaky. The non-flaky algorithmic guard is the exact root contract plus the independent reconstruction checks above. The versioned measurement remains reproducible with the baseline command.

## Gate result

After the change, the independent MSTest suite, console regression runner, Finance harness, Release solution build, and all versioned Lean artifacts must pass before publication.
