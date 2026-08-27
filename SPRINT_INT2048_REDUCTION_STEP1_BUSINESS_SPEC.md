# SPRINT Universal INumber Reduction — Step 1: Business Specification

**Status:** Revised proposal — awaiting explicit user approval.

**Owner:** Ricis.Core, with separate Numerics integration evidence.

**Agile sequence:** Business analysis → Architecture → QA tests → Implementation.

**Target capability:** Make the safe structural RICIS reduction pipeline operate for **every correctly implemented .NET `INumber<T>` type** through Core generic APIs, including but not limited to `Int2048` and `ULong2048`, without registration, concrete type recognition, project dependency, or numeric fallback.

## 1. Binding architectural boundary

The user-approved architectural rule is absolute:

> **Ricis.Core works with every numeric type exclusively through `INumber<T>`. It does not know, register, whitelist, import, type-check, construct, or depend on any concrete external scalar type or on `Ricis.Numerics`.**

The 2048-bit types are not special cases in Core. They are integration proofs that a universal generic implementation preserves exact fixed-width signed and unsigned semantics when the type fulfils the `INumber<T>` contract. Their concrete names belong only in `Ricis.Numerics.UnitTests` and never in Core production code.

## 2. Business objective

`Ricis.Core` already exposes finite generic APIs constrained by `INumber<T>`. Its non-generic reduction stages, however, still gate portions of L1, SP2, O(1) and standard operations by a hard-coded intrinsic type list. That split creates an inconsistent promise: a type satisfies the generic Core contract, yet its expression tree can be excluded from the same safe structural reductions solely because Core does not name the type.

This sprint removes that inconsistency. A caller who enters Core through a generic `INumber<T>` reduction path must obtain the same exact, structural RICIS rules available to built-in types wherever the source expression supplies the required operator. The engine must not invoke the expression, discover a concrete type, or convert a value to `BigInteger`, `double`, or another numeric domain.

## 3. Scope

| Area | Included decision |
|---|---|
| Core dependency graph | `Ricis.Core` remains fully independent of `Ricis.Numerics`; no project reference is added. |
| Generic participation | Every `T : INumber<T>` is accepted through generic Core reduction APIs automatically; no `Register<T>()` precondition exists. |
| Generic typed identities | Core obtains `T.Zero` and `T.One` from the generic constraint and emits constants typed exactly as `T`. |
| Structural reduction | Generic paths support L1 identity, SP2 safe cancellation, neutral zero/one rules and O(1) bridges without evaluating caller code. |
| Operator capability | A rewrite requiring a particular expression-tree operator is applied only if that valid operator is already supplied by the expression; Core never invents a non-existent operator. |
| Concrete-type isolation | Core production source contains no `Int2048`, `ULong2048`, `Ricis.Numerics`, or concrete external-type branch. |
| Numerics proof boundary | `Ricis.Numerics.UnitTests` invokes the universal generic Core API using real `Int2048` and `ULong2048` expressions to prove both types enter the same contract. |
| Classical non-generic boundary | Existing non-generic APIs preserve their contracts; they do not guess whether an arbitrary runtime type implements `INumber<T>`. |

## 4. Universal signed and unsigned rule

The generic contract must be capability-driven, never concrete-type-driven. `INumber<T>` supplies exact additive and multiplicative identities but not an obligation for Core to reinterpret every syntactic form.

| Structural rule | Universal decision |
|---|---|
| `F/F → T.One`, `F+T.Zero → F`, `F·T.One → F`, `F−T.Zero → F` | Allowed in generic `T : INumber<T>` reduction. |
| `F−F → T.Zero` | Allowed in generic `T : INumber<T>` reduction. |
| `T.Zero−F → −F` | Allowed only if the original scalar expression exposes a valid unary-negation operator. Without it, retain the original subtraction tree. |
| `F·T.Zero → 0_F`, `F/T.Zero → ∞_F` | Allowed structurally only when the original binary operator exists; Core must never execute it. |
| Root certification / polynomial long division | Remains double-only and outside this increment. |

This gives a correct universal result for signed, unsigned, fixed-width, arbitrary-precision and future `INumber<T>` domains without an exception table in Core.

## 5. Critical acceptance behavior

| ID | Required behavior | Acceptance criterion |
|---|---|---|
| UNI-R01 | No opt-in ceremony | A new correct `INumber<T>` works through the generic Core reduction API without registration, source edit or Core rebuild. |
| UNI-R02 | Typed identity | `x/x` returns exactly `T.One`, represented by a constant expression of type `T`. |
| UNI-R03 | Typed O(1) bridges | `F·T.Zero` and `F/T.Zero` create `0_F` / `∞_F` whose expression type remains exactly `T`. |
| UNI-R04 | Typed SP2 cancellation | `(x·y)/x` and associative factor cancellation remain structural and do not compile or invoke `x`/`y`. |
| UNI-R05 | No domain fallback | A generic reduction result introduces neither `Convert`, `BigInteger`, `double`, reflection-based numeric call nor a foreign scalar constant. |
| UNI-R06 | Unsigned capability safety | When a scalar has no unary negation, `0−x` remains a subtraction expression; no invalid negate node is fabricated. |
| UNI-R07 | 2048-bit integration proof | Separate Numerics tests prove the universal behavior for `Int2048` and `ULong2048`, while Core production remains type-agnostic. |
| UNI-R08 | Legacy stability | All existing non-generic reductions, public APIs, Core/Numerics/Finance tests and Lean evidence remain valid. |

## 6. Explicit non-goals

| Excluded work | Reason |
|---|---|
| `Ricis.Core → Ricis.Numerics` project reference | Violates the deliberate project separation. |
| Registration, white list or special-case policy for external numeric types | Contradicts the universal `INumber<T>` contract. |
| Concrete 2048-bit type names or imports in Core production | Core must remain type-agnostic. |
| Implicit `BigInteger`, `double` or reflection fallback | Violates exact generic semantics and transparency. |
| Generic root discovery, finite execution or exact polynomial long division | Separate architectural topic; current facilities are intentionally double-only. |
| Changes to arithmetic, RSA or overflow semantics in Numerics | Belongs exclusively to `Ricis.Numerics`. |
| Removal/weakening of public API | Forbidden without an explicit versioned removal decision and approval. |

## 7. Quality and deployment gates

The implementation must prove that the Core production project has no Numerics reference and that universal generic reduction never uses a concrete type branch. Direct generic Core regression cases must cover independently implemented test `INumber<T>` domains; Numerics-side integration tests must verify `Int2048` and `ULong2048`. The generated MSTest adapter must be fresh, and the full Core, Numerics, Finance and Lean quality gates must pass with zero warnings/errors.

## 8. Approval decision requested

Approve the following bounded business decision to proceed to **Step 2 — architecture**:

> Make safe structural RICIS reduction universally available through generic `INumber<T>` APIs for every correctly implemented numeric type. Preserve Core/Numerics separation, avoid registration and concrete type knowledge, keep each scalar result typed as `T`, avoid every numeric fallback, and leave unsupported syntactic operators unreduced rather than fabricating a new operation.
