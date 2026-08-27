# SPRINT ULong2048 Fixed-Width Shift Root — Step 2: Architecture Contract

**Status:** Exact fixed-width root architecture approved and implemented; measured performance remains an explicit evidence boundary and is not claimed as improved.

## 1. Goal and boundary

This increment replaces neither the established `BigInteger` Fermat baseline nor its result evidence. It adds an **allocation-free fixed-width exact floor-root primitive** for `ULong2048`, so the 2048-bit numeric domain can calculate roots without `BigInteger`, byte arrays or a Core dependency.

The direct target is the N-only Fermat candidate relation:

> `Δ = X² − N`; accept a candidate only when `Δ = Y²` exactly.

`Ricis.Core` remains independent. It receives no reference to Numerics and no factorization algorithm.

## 2. Public API

```csharp
public static ULong2048 IntegerSquareRootFloor(ULong2048 value)
```

The result `r` has the exact certificate:

> `r² ≤ value < (r + 1)²`.

The method is a public numeric primitive and receives a direct independent test suite. No `BigInteger` overload, fallback, allocation or floating-point code is admitted into this method.

## 3. Restoring two-bit algorithm

The method operates directly on existing inline `ULong2048Limbs` and reuses only established internal primitives: limb comparison, subtraction, shifts, bit length and fixed-width multiplication.

| State | Representation | Invariant |
|---|---|---|
| `remainder` | 32 inline `ulong` limbs | Unprocessed/restoring remainder. |
| `root` | 32 inline `ulong` limbs | Partial floor root after each two-bit step. |
| `bit` | One-hot `ULong2048` | Starts at the greatest even bit position not exceeding `bitLength(value)-1`; shifts right by two. |
| `trial` | 32 inline `ulong` limbs | `root + bit`; subtracted only if `remainder ≥ trial`. |

Each iteration executes the standard exact restoring transition:

```text
trial = root + bit
if remainder ≥ trial:
    remainder -= trial
    root = (root >> 1) + bit
else:
    root >>= 1
bit >>= 2
```

The loop has at most **1024** fixed iterations for a 2048-bit input. It is bounded by representation width, not presented as a general factorization bound.

## 4. One-bit correction certificate

After the restoring loop, the implementation checks the floor certificate locally. `root²` fits the 2048-bit domain because `root < 2^1024`. The `(root+1)²` branch is evaluated only if `root+1` has at most 1024 bits; otherwise it is mathematically greater than every representable `ULong2048` input and the upper inequality is already satisfied. This prevents the `2^2048` overflow edge from wrapping to zero.

No correction can silently use an overflowing multiplication. Any invariant violation is a deterministic internal failure exposed by direct regression tests.

## 5. Integration sequence

1. Add the root primitive and its direct parity/certificate tests in `Ricis.Numerics.UnitTests`.
2. Add a test-only N-only Fermat candidate fixture whose `Δ` is an `ULong2048`; prove exact parity against `BigInteger` only at the test oracle boundary.
3. Benchmark the fixed-width root against the current `BigInteger` restoring root and the independent Newton baseline.
4. Introduce a public ULong2048 Fermat search overload only in a separately approved API increment, because it requires a generic factorization-result and trace contract. The current public `FermatFactorizer.Search(BigInteger)` stays the benchmark/oracle baseline until then.

## 6. Non-goals

This increment does not claim a general O(1) factorization method, does not factor arbitrary RSA-2048 moduli, and does not use known factors to compute a root. Bit-mask prefilters are independent of the root primitive and must report their candidate/root counters separately.

## 7. Acceptance criteria

The implementation is accepted only if all direct root/certificate/boundary tests pass, `ULong2048` hot-path allocations remain zero, the full project quality gate is green, and a reproducible benchmark reports the result honestly whether it improves or regresses performance.
