# SPRINT Universal INumber Reduction — Step 2: Architecture Contract

**Status:** Generic Core `INumber<T>` reduction architecture implemented and direct-tested; future numeric-domain capabilities beyond this pipeline remain separate proposals.

**Prerequisite:** Step 1 business specification approved.

**Target:** Universal safe structural reduction for every `T : INumber<T>` without `Ricis.Core → Ricis.Numerics` dependency, registration, white list, reflection or numeric-domain fallback.

## 1. Architecture decision

The existing untyped `RicisPhasePipeline.Simplify(Expression)` remains the compatibility route for current expression trees. A new generic family of overloads becomes the authoritative path for scalar-generic Core APIs:

```csharp
public static Expression<Func<T, T>> Simplify<T>(Expression<Func<T, T>> expression)
    where T : INumber<T>;

public static Expression<Func<T, T>> SimplifyWithTrace<T>(
    Expression<Func<T, T>> expression,
    ICollection<RicisPhaseTraceStep> trace)
    where T : INumber<T>;

public static Expression<Func<T, T>> SimplifyWithLog<T, TLogStage>(
    Expression<Func<T, T>> expression,
    ILog<TLogStage> log)
    where T : INumber<T>;

public static Expression<Func<T, T>> SimplifyWithTraceAndLog<T, TLogStage>(
    Expression<Func<T, T>> expression,
    ICollection<RicisPhaseTraceStep> trace,
    ILog<TLogStage> log)
    where T : INumber<T>;
```

Each generic overload creates an immutable **generic scalar policy** from the static `INumber<T>` contract and passes it explicitly into the existing stage sequence. The policy is internal to `Ricis.Core`, generic in `T`, requires no dynamic registration, contains no concrete external type name, and is not mutable/global/thread-local state.

```text
Expression<Func<T,T>>
       │  where T : INumber<T>
       ▼
RicisPhasePipeline.Simplify<T>
       │
       ▼
GenericScalarPolicy<T>
  ├─ scalar type: typeof(T)
  ├─ zero: T.Zero
  ├─ one:  T.One
  └─ expression-operator capability checks
       │
       ▼
Existing normative stage order
ID → POL → SP2 → LOG → LIM → A1/A4 → SP3 → A5/A6/A7
       │
       ▼
Expression<Func<T,T>> with exact T payload
```

The policy is the sole additional context. It does not change RICIS rule ordering, does not compute a numerical answer, and does not alter the legacy non-generic pipeline's semantics.

## 2. Dependency graph and type isolation

| Component | Allowed knowledge | Prohibited knowledge |
|---|---|---|
| `Ricis.Core` production | .NET `INumber<T>`, `T.Zero`, `T.One`, original expression-tree operators and `typeof(T)` inside a generic policy | `Int2048`, `ULong2048`, `Ricis.Numerics`, `BigInteger` fallback, reflection-based interface discovery, external project reference |
| `Ricis.Numerics` production | Its fixed-width arithmetic semantics | Reference back to Core |
| `Ricis.Numerics.UnitTests` | Both projects and concrete 2048-bit types, solely to prove the Core generic contract | Production replacement for Core policy |
| Legacy raw Core API | Existing compatibility behavior | Guessing whether an arbitrary runtime type implements `INumber<T>` |

The solution graph remains acyclic. The test project is the only place where both assemblies meet.

## 3. Scalar-policy responsibility

The generic policy centralizes every currently duplicated numeric-domain decision. It must provide the following internal operations for the exact `T` of the generic entry point.

| Policy operation | Meaning | Required use |
|---|---|---|
| `IsScalarType(Type)` | True only for `typeof(T)` in this generic invocation | Distinguishes the current scalar from arbitrary operator-backed subexpressions. |
| `Zero()` / `One()` | `Expression.Constant(T.Zero, typeof(T))` and `Expression.Constant(T.One, typeof(T))` | Typed L1/SP2/A4–A7 construction. |
| `IsZero(Expression)` / `IsOne(Expression)` | Tests a `ConstantExpression` of exactly `T` against `T.Zero` / `T.One` | O(1), ordinary neutral-element reductions and reciprocal recognition. |
| `SupportsUnaryNegation(Expression)` | True only when the supplied scalar expression exposes a valid unary-negation operator | Guards `0−F → −F` without signedness guessing. |
| `CanApplyBuiltInRicisRule(BinaryExpression)` | Admits native built-in arithmetic or an operator whose result is the generic `T` | Lets safe RICIS rules handle `INumber<T>` expression-tree operators without admitting unrelated custom semantic domains. |

No operation in this policy may compile an expression, invoke user code, inspect an external type by name, look up a registration table or convert a value.

## 4. Stage integration

| Existing stage | Required architecture change | Generic invariant |
|---|---|---|
| IdentityReductionVisitor | Consume scalar policy for `F/F → T.One`. | Returns a constant exactly typed `T`. |
| AlgebraicReductionVisitor | Use policy for `Zero`, `One`, intrinsic-operator admission and the `0−F` capability gate. | SP2 remains structural; no `BigInteger`/`double` conversion. |
| LimitBridge / LimitBridgeVisitor | Use policy zero/one predicates and operation admission. | `F·T.Zero → 0_F`, `F/T.Zero → ∞_F` preserve `T`. |
| RicisTransformVisitor | Use policy only for direct type-safe zero/identity guards. | Root certification stays double-only; generic path never enters numerical root inspection. |
| StandardOperationsVisitor | Use policy for typed identities and scalar guards in A4–A7. | All extension payloads retain `T`. |
| TypeConsistencyVisitor | No rule change expected; verify its existing exact-type guard remains authoritative. | No payload type widening. |
| Other stage families | Receive the policy only if they currently consult `NumericConstants` for scalar identity. | No parallel helper/duplicate identity logic. |

The stage list changes from cached stateless visitor objects to factories parameterized by an immutable policy. The legacy route supplies a compatibility built-in policy; generic routes supply `GenericScalarPolicy<T>`. This prevents global mutable context and is safe for concurrent simplification.

## 5. Generic caller routing

Every Core API already constrained as `where T : INumber<T>` must route its normalisation step through the generic pipeline overload rather than erase `T` by calling `Simplify(Expression)`. This includes finite expression preparation, proof/document normalisation, vector/matrix/complex expression normalisation, and generic financial/analytic helper paths that currently hold `Expression<Func<T,T>>`.

The routing change is mechanical and DRY: one generic pipeline method becomes the common target. No domain-specific duplicate visitor is permitted.

## 6. Operator-capability policy

Core must preserve the original scalar operation whenever an approved structural rule cannot be represented by the expression-tree operator set. The critical case is unsigned subtraction.

```text
source: T.Zero - x
if source scalar node exposes unary negation for T:
    rewrite structurally as -x
else:
    preserve original T.Zero - x tree
```

This is not signedness detection and does not name an unsigned type. It simply refuses to fabricate an expression node that the original numeric domain does not support.

## 7. Public API and compatibility

The generic overloads are additive public API and therefore require direct regression tests. Existing raw overloads, logging behavior, traces and stage order remain stable. No public member is removed, renamed or silently redirected through a different scalar domain.

The old `NumericConstants.Register<T>()` remains a legacy utility until a separate removal decision, but it is **not used** by the generic universal route and is not a precondition for `T : INumber<T>` reduction.

## 8. Non-goals

This architecture does not add generic root finding, generic polynomial division, finite user-expression execution, numeric conversions, reflection detection, static global current-type context, a new Core→Numerics reference, or any concrete 2048-bit identifier in Core production code.

## 9. Approval decision requested

Approve this architecture to authorize **Step 3 — QA matrix**:

> Add generic `RicisPhasePipeline` entry points for `T : INumber<T>` and carry an immutable `GenericScalarPolicy<T>` explicitly through the existing normative stages. Route generic Core callers to these overloads, preserve the legacy raw path, apply only structurally representable operators, and keep Core independent from concrete numeric libraries.
