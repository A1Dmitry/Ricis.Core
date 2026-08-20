# SPRINT ULong2048 Fixed-Width Shift Root — Step 3: QA Specification

**Status:** Proposed — awaiting approval.

## 1. Test policy

Every public member introduced by this increment receives an independent direct MSTest method. `BigInteger` is a test oracle only; it does not appear in the fixed-width production root implementation. Test operands are deterministic and versioned.

## 2. Direct root contract matrix

| ID | Public surface | Input | Required evidence |
|---|---|---|---|
| ROOT01 | `IntegerSquareRootFloor` | `0` | Returns `0`; floor certificate holds. |
| ROOT02 | `IntegerSquareRootFloor` | `1` | Returns `1`; exact-square equality holds. |
| ROOT03 | `IntegerSquareRootFloor` | `2`, `3` | Returns `1`; non-square upper certificate holds. |
| ROOT04 | `IntegerSquareRootFloor` | small squares `4`, `9`, `64`, `59536` | Exact roots; output square equals input. |
| ROOT05 | `IntegerSquareRootFloor` | one below/exact/one above each selected square | Floor transitions only at the square; no off-by-one correction error. |
| ROOT06 | `IntegerSquareRootFloor` | deterministic 512-, 1024- and 2047-bit roots squared | Exact roots and all limb boundaries exercised. |
| ROOT07 | `IntegerSquareRootFloor` | largest representable input `2^2048−1` | Returns `2^1024−1`; upper certificate is proved without forming overflowing `(r+1)^2`. |
| ROOT08 | `IntegerSquareRootFloor` | deterministic non-square near 2048-bit maximum | `r²≤n<(r+1)²` against `BigInteger` oracle. |
| ROOT09 | `IntegerSquareRootFloor` | fixed corpus of 32 deterministic values spanning bit-lengths 1…2048 | Exact equality with independent `BigInteger` floor-root oracle. |
| ROOT10 | `IntegerSquareRootFloor` | hot loop over 2048-bit values | No managed allocations on the calling thread after warmup. |

## 3. Fermat candidate integration evidence

| ID | Action | Required evidence |
|---|---|---|
| FROOT01 | Form a deterministic `ULong2048` Fermat delta from `x²−n`. | Fixed-width root equals oracle root; exact square is accepted. |
| FROOT02 | Mutate exact delta by one. | Fixed-width root stays floor; equality check rejects candidate. |
| FROOT03 | Apply existing modulo-64 square mask before root. | A known square delta always passes; a rejected residue never reaches root in the trace fixture. |
| FROOT04 | Run the current `BigInteger` `FermatFactorizer.Search` baseline on the same small semiprime oracle. | Reconstructed `p,q` and candidate semantics agree; no production cross-domain conversion is introduced. |

## 4. Performance evidence

| ID | Protocol | Required output |
|---|---|---|
| PERF01 | Release build; deterministic 2048-bit perfect square; warmup; repeated timed calls. | JSON/Markdown result comparing ULong fixed-width root with `BigInteger` restoring and Newton baselines. |
| PERF02 | Hot loop allocation measurement. | Fixed-width root reports zero managed allocations. |
| PERF03 | Interpretation gate. | Documentation states improvement only if measured; otherwise records regression honestly. |

## 5. Acceptance gates

The increment may proceed only if all direct ROOT/FROOT tests pass, the full solution builds with zero warnings/errors, generator freshness checks pass, Core/Numerics/Finance/Lean gates are green, source hygiene excludes `BigInteger` and byte-array conversion from the fixed-width root body, and the performance evidence is versioned before commit.
