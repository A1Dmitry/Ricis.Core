# Bounded-Fermat Research Increment — Step 1: Verifiable Hypothesis

**Status:** Proposed research contract — awaiting explicit approval.

**Scope:** This document defines a reproducible investigation. It does **not** claim a new general factorization algorithm, a security break, an O(1) result, or a mathematical discovery before the stated tests and proof obligations have been completed.

## 1. Research question

For an odd semiprime `N = p·q` under an explicit balance promise, can a bounded, parity-aware Fermat search recover the factor pair exactly while using only the stated search interval and the stated non-duplicating filters?

A second, stronger proposition is deliberately separated:

> **H-O1 (unproved):** A computable function of the public input alone determines the factor gap, or restricts the search to a constant number of exact candidates, for every input in a precisely defined class.

H-O1 is not assumed by the implementation, test oracle, benchmark, or acceptance criteria. It is a target for attempted falsification. A formula containing an unknown `d = q−p` is a reconstruction identity, not a factorization algorithm, unless `d` is independently computed from `N` by a specified, verified and bounded procedure.

## 2. Established algebraic core

For odd factors `p ≤ q` of an odd `N`, define

```text
x* = (p + q) / 2
y* = (q − p) / 2
Δ(x) = x² − N.
```

Then `x*` and `y*` are integers and satisfy

```text
Δ(x*) = y*²,
p = x* − y*,
q = x* + y*,
p·q = N.
```

A unit candidate advance obeys

```text
Δ(x + 1) = Δ(x) + 2x + 1.
```

For odd `N`, only one parity of `x` can make `Δ(x)` a square: `x` is odd when `N mod 4 = 1`, and even when `N mod 4 = 3`. After aligning the initial candidate to that parity, the exact recurrence becomes

```text
Δ(x + 2) = Δ(x) + 4x + 4.
```

The current Core Fermat solver already implements the `x²−N=y²` reconstruction and an exact modulo-64 square-residue prefilter; it does not yet implement the parity-two search or an explicit bounded balance profile.

## 3. Bounded balance profile BFR-2

The first reproducible research profile is deliberately narrow:

```text
BFR-2:
  N = p·q,
  p and q are odd primes,
  p ≤ q < 2p.
```

The balance promise is equivalent to

```text
0 ≤ y* < x*/3,
√N ≤ x* < √(9N/8),
0 ≤ y* < √(N/8).
```

These are **search bounds only**. They prove that the correct Fermat point lies in the stated bounded arc when the BFR-2 promise is true. They do not prove that the number of candidate points is constant as the bit length grows.

The candidate start is the least integer `x0 ≥ √N` matching the parity required by `N mod 4`; candidate points then advance by exactly two. The upper bound is the greatest candidate of the same parity strictly below `√(9N/8)`.

## 4. Normalized conditions relative to N

Let `d=q−p`, `p≤q`, and use the exact Fermat target

```text
x* = (p+q)/2,
y* = d/2.
```

The natural normalized coordinates are

```text
u(N) = x*/√N = (p+q)/(2√(pq)),
v(N) = y*/√N = (q−p)/(2√(pq)).
```

They satisfy the exact relation `u(N)²−v(N)²=1`. Thus `u(N)→1` if and only if `v(N)→0`, equivalently if and only if the relative factor gap `d/√N→0`. This limit alone is **not** sufficient for constant candidate work: it merely says the target is relatively close to `√N`.

The exact unnormalised offset is

```text
D(N) = x* − √N
     = (√q − √p)² / 2
     = d² / (2(√q + √p)²).
```

The parity-aligned search starts at a point differing from `ceil(√N)` by at most one and advances by two. Therefore the number of exact candidate deltas is bounded by `ceil((D(N)+1)/2)+1`. A sufficient relative-to-`N` promise for constant work is

```text
Cκ:  0 ≤ q−p ≤ κ·N^(1/4),
```

for a fixed public constant `κ`. Under `Cκ`, the denominator in `D(N)` is at least `8√N`, so

```text
D(N) ≤ κ² / 8,
```

and the parity-aligned candidate count is bounded by a constant depending only on `κ`, not on the bit length of `N`. At the same time,

```text
u(N) − 1 = D(N)/√N ≤ κ²/(8√N) → 0.
```

This precisely captures the requested stronger condition: `u(N)→1` **with a bounded scaled offset** `√N·(u(N)−1)`, rather than the weaker bare limit.

| Condition | What it proves | What it does not prove |
|---|---|---|
| `q/p→1` or `d/√N→0` | `u(N)→1`; target is relatively near `√N`. | Constant candidate count. |
| `d=O(N^α)`, `α<1/4` | `D(N)→0`; eventually at most the endpoint candidate after integer alignment. | A public way to recognize the class without a promise. |
| `Cκ: d≤κN^(1/4)` | `D(N)≤κ²/8`; constant candidate bound for fixed `κ`. | Universal factorization or a free membership predicate. |
| `d=Θ(N^β)`, `β>1/4` | Predicts growing direct-Fermat work `Θ(N^(2β−1/2))` up to endpoint/parity effects. | Failure of other algorithms or filtered variants. |

`Cκ` is a **promise class**. Its membership depends on the unknown factors if only `N` is supplied. The non-circular public decider is exactly the bounded run itself: execute the declared `K(κ)` parity candidates; a reconstructed pair proves membership/success, and exhaustion reports `not found within declared profile` rather than “not factorable.” No private factor or hidden `d` enters the algorithm.

### 4.1 Public order-band contract

The user’s order formulation is represented as a public profile

```text
OrderBand(N; Pmin, Pmax):
  N is odd,
  Pmin(N) ≤ p ≤ q ≤ Pmax(N),
  p and q are odd primes,
  Pmin(N) ≤ √N ≤ Pmax(N).
```

`Pmin` and `Pmax` are declared profile functions or explicit public parameters. They are never reconstructed from hidden factors and are recorded in the immutable trace before search begins. The band directly yields

```text
q − p ≤ Pmax(N) − Pmin(N).
```

Therefore the refined order-band condition

```text
OBκ: Pmax(N) − Pmin(N) ≤ κ·N^(1/4)
```

implies `Cκ` and gives the same constant candidate bound. This is the precise formal version of “minimum and maximum order form the range.”

| Public band | Guaranteed factor relation | Direct-Fermat implication |
|---|---|---|
| Coarse same-bit-order: `2^(b−1) ≤ p≤q<2^b` | `q/p<2`; both factors have bit length `b`. | BFR-2 balance only; the interval has width `Θ(√N)`, so it does not alone make candidate work constant. |
| Refined cell inside one order: `Pmin≤p≤q≤Pmax`, `Pmax−Pmin≤κN^(1/4)` | Odd prime factors share a narrow public order-range. | `OBκ⇒Cκ`; constant parity-candidate bound for fixed `κ`. |
| Shrinking refined cell: `Pmax−Pmin=o(N^(1/4))` | Relative and absolute concentration sharpen with size. | The Fermat offset tends to zero before integer/parity alignment. |

A QA fixture may carry its true `p,q` only as an oracle. The runtime algorithm receives only `N` and the public `OrderBand` profile and must derive every accepted candidate from those values.

### 4.2 Symbolic-expression-first contract

All research conditions are constructed as RICIS expression trees before numeric execution. A numeric candidate is an instantiation of the symbolic contract; it is never an independent second formulation.

| Symbolic object | Canonical deferred form | Purpose |
|---|---|---|
| Factor system | `N = P·Q`, `P≤Q`, `P,Q` odd | Defines the fixture oracle and the reconstruction target. |
| Radical factorization | `√N = √(P·Q) = √P·√Q` | Deferred symbolic radical rule; it must retain its domain preconditions and never pretend that distinct prime roots are integers. |
| Fermat coordinates | `2X=P+Q`, `2Y=Q−P`, `X²−Y²=N` | Defines the exact finite derivation. |
| Order range | `Pmin(N)≤P≤Pmax(N)` and `Pmin(N)≤Q≤Pmax(N)` | States the declared public profile. |
| Narrow-band premise | `Pmax(N)−Pmin(N)≤κ·B`, `B⁴≤N<(B+1)⁴` | Represents `OBκ` without a floating-point fourth root. |
| Candidate transition | `Δ(X+2)=Δ(X)+4X+4`, `Δ(X)=X²−N` | Drives the parity-aligned finite computation. |
| Completion | `P=X−Y`, `Q=X+Y`, `(X−Y)(X+Y)=N` | Connects an exact square to exact reconstruction. |

The runner builds these expressions from named parameters `N,P,Q,X,Y,Pmin,Pmax,κ,B` and passes them through the existing `RicisPhasePipeline`/proof machinery. The trace must record the original tree, the exact reducer output and the stage that made each change. A document may state only reductions that the engine actually performed; a desired algebraic identity that is not currently implemented becomes an explicit failing QA case or a separately approved reducer rule, never hand-written proof text.

The finite arithmetic checker may evaluate a fully bound candidate only after its symbolic tree and profile constraints have been recorded. The radical identity is a symbolic transformation: for distinct primes, `√P` and `√Q` are not integer values and cannot replace the exact predicate `Δ=y²`. Exact-square certification remains an integer operation with no floating-point root. The baseline emits concrete `BigInteger` evidence today, while the planned universal `INumber<T>` route uses the same expression schema with a capability-appropriate exact-integer backend. Neither route may substitute floating-point roots for the `B⁴≤N<(B+1)⁴` certificate.

### 4.3 Fail-closed Semiprime domain contract

The domain model uses inheritance where the existing Core hierarchy already establishes the correct responsibility:

```text
Ricis.Numerics.Factorization
  └── SemiprimeBase (abstract)
        └── Semiprime (sealed immutable value object)
              └── protected readonly N, P, Q and canonical numeric helpers

Ricis.Core
  └── RicisProofCase
        └── consumes only caller-provided immutable primitive/value evidence
              ├── symbolic premises and reduced expressions
              └── BoundedFermatTrace/result renderer
```

`SemiprimeBase` centralizes the protected immutable scalar state, canonical ordering and common numeric-expression helpers in `Ricis.Numerics`; `Semiprime` is its sealed public descendant. The Core proof layer never takes a `Semiprime` parameter and never references Numerics. A caller that wants symbolic rendering supplies immutable value evidence at the external boundary, preserving both C# single-inheritance correctness and the project split. This is the DRY route: exactly one validated numeric factor state exists, with no duplicated Core solver.

Neither numeric type inherits `RicisExpression`: that hierarchy represents special singularity extension nodes, while a finite semiprime is a numeric domain value, not a new singular arithmetic form.

`Semiprime` is an immutable public value object with exactly two construction routes and no profile-parameter constructor:

```csharp
Semiprime(N)
Semiprime(p, q)
```

| Route | Constructor work | Fail-closed outcome |
|---|---|---|
| `Semiprime(N)` | Rejects `N≤1` or even `N`; runs the declared exact factor-recovery protocol; verifies `p≤q`, `p·q=N`, and primality of both recovered factors. | Returns only a validated odd semiprime; otherwise throws a precise validation/factor-recovery exception. |
| `Semiprime(p,q)` | Rejects non-positive, even or non-prime factors; orders the factors; computes `N=p·q`; verifies the reconstruction exactly. | Returns only a validated odd semiprime; invalid supplied factors cannot construct an object. |

The constructor receives **only** `N`, or **only** `p,q`. `Pmin`, `Pmax`, `κ`, `B`, `d`, Fermat coordinates, candidate boundaries and all symbolic premises are derived immutable properties. They are never caller inputs, and no factor value is accepted as a hidden profile parameter.

`Semiprime` is `sealed` and immutable by construction: it has no public or internal setter, no mutation method, no externally mutable collection, and no lazy state whose validation result can change after construction. Its scalar properties are get-only values set exactly once; any symbolic collection is exposed as an immutable snapshot/array. All derived values are deterministic functions of the fixed validated state. A new factorization state therefore requires construction of a new object, never mutation of an existing one.

For `Semiprime(N)`, a constructor does not claim validity before exact recovery and validation complete. For `Semiprime(p,q)`, factor knowledge is intentional test/setup input and is recorded as such in the proof trace; it does not model the N-only factor-recovery path.

The object derives `Pmin/Pmax` from the validated factor pair’s order classification only after construction. A research runner whose input is N alone may derive provisional public bounds from N as expressions, but may not claim the narrow-band premise until its exact reconstruction evidence establishes it.

## 5. Non-duplicating filter order

Each filter has one independent purpose and must be traceable in the test runner.

| Order | Filter | Exact role | Must not claim |
|---:|---|---|---|
| 1 | Positivity and oddness | Rejects invalid Fermat-BFR input before arithmetic. | Primality or balance. |
| 2 | BFR-2 interval | Limits the search only under the explicit promise `q<2p`. | Universal factorization. |
| 3 | Parity alignment | Removes the impossible half of `x` candidates using `N mod 4`. | More than a factor-two candidate reduction. |
| 4 | Modular square-residue sieve | Avoids exact square-root calls for impossible `Δ mod m`. | Proof that a surviving delta is square. |
| 5 | Exact integer square root | Decides whether `Δ=y²` exactly. | Factor primality. |
| 6 | Reconstruction | Verifies `p=x−y`, `q=x+y`, `p·q=N`. | That no earlier factor exists. |
| 7 | Optional primality certificate | Establishes semiprime profile membership for a fixture. | Needed for the algebraic reconstruction itself. |

No filter may use `p`, `q`, `d=q−p`, private-key material, a precomputed answer or an oracle-derived hidden bound as an algorithm input.

## 6. Complexity claim to test, not assume

For direct Fermat enumeration, the exact target point is

```text
x* = (p+q)/2,
```

while the natural start is `ceil(√N)`. The raw candidate distance is therefore determined by

```text
x* − √N = (√q − √p)² / 2.
```

The parity-aware candidate count is approximately half this distance after endpoint alignment. Hence BFR-2 gives an explicit finite interval but does **not** by itself establish O(1) work as a function of input bit length. Classical literature likewise treats Fermat's method as fast for factors near `√N` and slow when they are not close; broader deterministic bounds require additional structured search [1].

H-O1 is accepted only if all of the following hold:

1. `h(N)` is specified as an executable, public-input-only function.
2. Its evaluation cost is included in the complexity account.
3. The produced candidate/bound is proven to contain `x*` for the exact declared input class.
4. A cross-bit-length deterministic corpus shows a constant **verified upper bound** on total exact candidate checks for that class, or a mathematical proof replaces the empirical claim.
5. Negative and adversarial fixtures attempt to violate each assumption.

Without all five items, the result remains a bounded-search optimization, not an O(1) factorization result.

## 7. Generic numeric boundary

This research increment must not weaken the universal Core `INumber<T>` boundary. `INumber<T>` alone does not expose a standard bit-length or integer-square-root primitive, so a future generic bounded-Fermat implementation requires a separate minimal capability contract for exact ordered integral arithmetic. It must be designed without a `BigInteger` fallback and independently tested with `Int2048` and `ULong2048` only from the Numerics-side test boundary.

The existing `FermatFactorizer.Solve(BigInteger)` is an oracle/baseline for research comparison, not the generic implementation under test.

## 8. Unified Ricis.Core proof/compute contour

The research runner and proof report must use one immutable execution record. The calculation is the source of facts; the proof/document layer renders those facts and checks their stated invariants rather than reimplementing the search.

```text
Public input N + public BFR profile
        │
        ▼
Bounded candidate engine
  ├─ range/parity alignment
  ├─ Δ recurrence
  ├─ modular sieve decision
  └─ exact-root/reconstruction decision
        │
        ▼
Immutable BoundedFermatTrace
  ├─ public profile and endpoints
  ├─ every candidate count and rejection reason
  ├─ exact-square evidence where reached
  ├─ reconstructed P,Q and invariant checks
  └─ declared termination reason
        │
        ├────────────► computation result
        │
        └────────────► Ricis.Core proof/document renderer
                          ├─ Log
                          ├─ LaTeX
                          ├─ JSON
                          └─ Lean-oriented structured export
```

The trace must state whether a reported conclusion is: (a) exact algebraic reconstruction for one supplied `N`; (b) bound soundness for the BFR-2 profile; (c) an experimental observation; or (d) an unresolved H-O1 claim. No renderer may upgrade category (c) or (d) into a theorem.

The existing `RicisProofDocumentProfile`, `RicisProofDocumentTemplates` and injected `ILog<T>` mechanism are reused where their contracts fit. Any new trace DTO is a value-only model; it must not hold delegates, private factors, or an unbounded cached candidate sequence.

## 9. Required evidence matrix


| Evidence ID | Obligation | Pass criterion |
|---|---|---|
| BFR-E01 | Algebraic reconstruction | Every positive fixture returns exact `p·q=N` and `x²−N=y²`. |
| BFR-E02 | Bound soundness | Every BFR-2 fixture has `x*` inside the declared parity-adjusted interval. |
| BFR-E03 | Parity soundness | Every BFR-2 fixture reaches `x*` using the `+2` recurrence. |
| BFR-E04 | Filter soundness | Sieve may reject only non-squares; no true `y*²` delta is rejected. |
| BFR-E05 | Negative profile boundary | Inputs outside `q<2p`, even inputs, primes and non-semiprimes do not produce a false success. |
| BFR-E06 | No-answer leakage | Algorithm input contains `N` and public profile parameters only; fixture factors are oracle-only. |
| BFR-E07 | Complexity accounting | Record candidate visits, sieve passes, exact roots and wall-clock time separately. |
| BFR-E08 | H-O1 falsification | Test every proposed `h(N)` against a held-out, cross-bit-length corpus before any complexity conclusion. |

## 10. Decision requested

Approve this hypothesis contract to proceed to **QA design**. The next step will specify the exact fixture corpus, positive/negative cases, independent oracles and reporting format. It will not yet add a production generic Fermat solver or claim O(1).

## References

[1] R. Sherman Lehman, “Factoring Large Integers,” *Mathematics of Computation* 28(126), 1974, pp. 637–646. [AMS PDF](https://www.ams.org/journals/mcom/1974-28-126/S0025-5718-1974-0340163-2/S0025-5718-1974-0340163-2.pdf)

[2] “Fermat's factorization method,” reference overview of the difference-of-squares recurrence and candidate distance. [Wikipedia](https://en.wikipedia.org/wiki/Fermat%27s_factorization_method)
