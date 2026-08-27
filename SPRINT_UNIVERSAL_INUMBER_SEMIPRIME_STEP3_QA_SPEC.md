# SPRINT Universal INumber + Immutable Semiprime — Step 3: QA Specification

**Status:** Approved; generic Core reduction and immutable `Semiprime<T>` migration implemented and tested. Symbolic BFR/order-band evidence `BFR01–BFR12` remains an explicit research backlog and is not marked complete by this document.

**Prerequisites:** The approved universal `INumber<T>` Core boundary, symbolic-expression-first research contract, and immutable fail-closed `Semiprime` domain contract.

**Test policy:** Every public constructor and public method introduced by this increment must have one direct named regression test. Existing canonical catalog scenarios remain the source of truth for Core; `Ricis.Numerics.UnitTests` provides the only concrete `Int2048`/`ULong2048` integration boundary.

## 1. Test topology

| Test location | Responsibility | Concrete numeric knowledge |
|---|---|---|
| `RegressionTests` / generated Core MSTest adapter | Universal `INumber<T>` pipeline behavior, typed trace/log overloads and legacy raw compatibility. | Built-in test types and Core-local generic probes only. |
| `Ricis.Numerics.UnitTests` | Direct public contract tests for migrated `FermatFactorizer`/`Semiprime`, plus test-only integration with Core. | `Int2048` and `ULong2048` only in this test boundary. |
| Fermat research suite | Exact N-only bounded-search invariants, mask accounting, shift-root correction and all negative profile outcomes. | `BigInteger` baseline oracle for current solver; never a hidden production fallback. |
| Generated MSTest adapter | One visible test method per Core catalog scenario. | No independent logic. |

## 2. Immutable Semiprime<T> tests

The production design under test is `Ricis.Numerics.Factorization.SemiprimeBase<T>` with protected immutable canonical state and `sealed Semiprime<T>` as the public descendant. It requires the minimal exact integral operator surface, not `INumber<T>`, so the unsigned RSA magnitude domain remains supported. The N-only and factor-pair construction routes are constructors/overloads only; no profile parameters are accepted.

| ID | Direct public surface | Fixture / action | Expected result |
|---|---|---|---|
| SP01 | `Semiprime<T>(T n)` | `n=5959` in a small generic numeric domain. | Recovers canonical `p=59`, `q=101`, `n=p·q`; all values retain `T`. |
| SP02 | `Semiprime<T>(T p, T q)` | Supply reversed valid pair `(101,59)`. | Object canonicalizes to `P=59`, `Q=101`, `N=5959`. |
| SP03 | N constructor guard | `N≤1`. | Exact documented argument exception; no partially constructed instance. |
| SP04 | N constructor guard | Even `N`. | Exact documented argument exception before candidate search. |
| SP05 | N constructor guard | Odd prime `N`. | Exact not-semiprime/factor-recovery failure; no false factors. |
| SP06 | pair constructor guard | `p≤1`, `q≤1`, or even factor. | Exact documented argument exception. |
| SP07 | pair constructor guard | Odd composite factor `(9,101)`. | Exact primality-validation failure. |
| SP08 | pair constructor guard | Valid equal-prime pair `(101,101)`. | Accepted as valid semiprime with `P=Q=101`. |
| SP09 | invariant accessors | Construct valid object by both routes. | `P≤Q`, `P·Q=N`, `P,Q` odd and certified prime. |
| SP10 | immutability | Reflection/API scan of `Semiprime<T>` public surface. | `sealed`; no settable public property, mutation method or exposed mutable collection. |
| SP11 | base encapsulation | Reflection/API scan of `SemiprimeBase<T>`. | Canonical state is `protected readonly`; no Core proof case receives a Numerics object or creates a project dependency. |
| SP12 | input surface | Reflection verifies constructor parameter lists. | Only `(T n)` and `(T p,T q)`; no `Pmin/Pmax/κ/B/d` input parameter. |

The constructor may use generic division/remainder, order, equality, identities and exact multiplication supplied by the minimal Numerics exact-operator contract. It must not downcast or name a concrete numeric type. For production-scale large factors, primality-validation performance is an explicit subject of later capability-profile work; this increment tests correctness and fail-closed behavior, not an unproven universal primality-performance claim.

## 3. Universal reduction tests

| ID | Path | Assertion |
|---|---|---|
| UNR01 | `RicisPhasePipeline.Simplify<T>` with built-in `T`. | `x/x` becomes a typed `T.One`. |
| UNR02 | Generic typed neutral elements. | `x+T.Zero`, `x−T.Zero`, `x·T.One` reduce structurally and retain exactly `T`. |
| UNR03 | Generic O(1). | `x·T.Zero` and `x/T.Zero` create typed indexed zero/infinity payloads. |
| UNR04 | Generic SP2. | `(x·y)/x` and an associative product cancellation reduce structurally without compiling a lambda. |
| UNR05 | Capability guard. | A generic scalar expression lacking unary negation preserves `T.Zero−x` unchanged. |
| UNR06 | Scalar-boundary integrity. | Generic result contains no foreign `int`, `BigInteger`, `double` or `Convert` node. |
| UNR07 | Legacy raw compatibility. | Existing untyped built-in pipeline catalog remains unchanged. |
| UNR08 | Core isolation. | Production Core source/project graph contains no Numerics project reference or concrete 2048-bit identifier. |
| UNR09 | Generic caller routing. | Every existing `where T:INumber<T>` normalisation API invokes generic pipeline path, not raw erasure. |

## 4. Numerics integration tests

| ID | Type | Action | Expected result |
|---|---|---|---|
| NUM01 | `Int2048` | `Semiprime<Int2048>(59,101)`. | Exact validated canonical state through the Numerics generic operator profile. |
| NUM02 | `ULong2048` | `Semiprime<ULong2048>(59,101)`. | Exact validated canonical state and unsigned-safe order. |
| NUM03 | `Int2048` | Test-only `RicisPhasePipeline.Simplify<T>(x/x)` and O(1) bridge. | Typed results remain `Int2048`; Core has no Numerics reference. |
| NUM04 | `ULong2048` | `Semiprime<ULong2048>(59,101)`. | Exact unsigned-safe canonical state through the Numerics operator profile. |
| NUM05 | `ULong2048` | Interface scan. | It remains intentionally outside Core `INumber<T>` route; no false unary-negation claim is made. |
| NUM06 | both types | `Semiprime<T>(5959)` where supported by the exact operator profile. | N-only constructor returns 59×101 with no concrete type branch. |
| NUM07 | boundary | Source/project graph scan. | Core production neither references Numerics nor uses a `BigInteger` fallback for generic reduction. |

## 5. Symbolic Fermat and order-band tests — research backlog after numeric migration

| ID | Evidence | Assertion |
|---|---|---|
| BFR01 | Symbolic construction | The trace contains original expression trees for `N=P·Q`, Fermat coordinates, transition and reconstruction. |
| BFR02 | Reducer fidelity | Every reported derivation step equals an actual Core reducer output; unsupported algebra remains explicit, not hand-written. |
| BFR03 | Exact recurrence | For valid candidates, `Δ(x+2)=Δ(x)+4x+4` exactly. |
| BFR04 | Parity | `N mod 4` selects the unique candidate parity; the target is visited for fixtures in each valid residue case. |
| BFR05 | Exact reconstruction | Found output always satisfies `p·q=N`, `x²−N=y²`, `p=x−y`, `q=x+y`. |
| BFR06 | Sieve soundness | The modular sieve never rejects the delta for a known exact square; all modulo-64 residue classes are exercised. |
| BFR07 | Order derivation | `Pmin/Pmax`, gap, coordinates and band observations appear only as derived expression/results, never constructor inputs. |
| BFR08 | `OBκ` positive boundary | Versioned fixtures whose factor order range is at most `κ·N^(1/4)` succeed within declared `K(κ)`. |
| BFR09 | `OBκ` negative boundary | A same-bit-order but broad pair proves that coarse order equality alone does not justify a constant candidate claim. |
| BFR10 | Input leakage | N-only path has no factor/gap/answer parameter; fixture oracle values are not readable by runner inputs. |
| BFR11 | Trace classification | Reports distinguish exact finite result, conditional profile evidence, experimental measurement and unresolved H-O1. |
| BFR12 | Complexity ledger | Trace separately records candidates, mask rejections, mask passes and exact-root checks; it never labels an experiment O(1) without profile proof. |
| BFR13 | Shift-root correction | The root result satisfies `r²≤Δ<(r+1)²`; exact squares satisfy `r²=Δ`, including 2048-bit boundary fixtures. |
| BFR14 | Radical expression | `√(P·Q)=√P·√Q` is recorded as symbolic only; it never replaces exact integer-square certification for distinct prime factors. |

## 6. Acceptance gates

The implementation is accepted only if all direct tests above pass, generated adapter freshness passes, the full solution builds warning-free, Core and Numerics test suites pass, existing Finance/Lean gates remain green, and a source-hygiene scan confirms the Core→Numerics boundary. Any public member added during implementation receives a direct test before the commit is permitted.

## 7. Scope decision

The numeric migration, universal Core `INumber<T>` path, direct public API tests and shift-root/mask-accounting evidence are implemented under this approved matrix. The symbolic order-band items BFR01–BFR14 remain explicitly tracked research backlog until an independently approved Core-neutral evidence contract is implemented. No component may claim a general O(1) factorization result; it may report only the direct numerical and symbolic evidence actually available.
