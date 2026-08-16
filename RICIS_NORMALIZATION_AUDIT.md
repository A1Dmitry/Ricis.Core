# RICIS III Normalization Audit

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.

## Audit objective

The objective is broad normalization of complex expression trees without replacing RICIS semantics with an unguarded classical simplifier. RICIS phases must run first where they define indexed zero, indexed infinity, A6 payload products, certified keys, deferred identities, or singular transformations.

## Current normalization layers

| Layer | Responsibility | Status |
|---|---|---:|
| `IdentityReductionVisitor` | Highest-priority structural identity `F/F -> 1` | Active |
| `PolarTrigVisitor` | Exact polar/trigonometric structural reductions | Active |
| `AlgebraicReductionVisitor` | SP2 cancellation, ratios, powers, factors, difference of squares, ordinary safe subtraction rules | Active |
| `LimitBridgeVisitor` | O(1) indexed-zero/indexed-infinity bridges | Active |
| `RicisTransformVisitor` | A1/A4 singular transformation and payload indexing | Active |
| `TypeConsistencyVisitor` | Type and payload preservation | Active |
| `StandardOperationsVisitor` | A5–A7, Z-01/Z-02, A6 indexed operations | Active |
| `ExpressionSimplifierVisitor` | Broad classical structural helper: constants, `x+x`, `x*x`, commutation, fractions, distribution, conditionals | Public helper; not a global RICIS phase |

## Baseline corpus findings

A direct corpus probe found that the following ordinary identities already normalize correctly through the RICIS pipeline:

```text
F+0 -> F
0+F -> F
F*1 -> F
1*F -> F
F*0 -> 0_F
0*F -> 0_F
(F+G)/(G+F) -> 1
(F*G)/(G*F) -> 1
F/(G/H) -> (F*H)/G
```

The following real gaps were added to `AlgebraicReductionVisitor` and protected by regression tests:

```text
F-0   -> F
0-F   -> -F
F-F   -> 0
-(-F) -> F
a^N/a^(N-X) -> a^X
```

The ordinary rules explicitly exclude `RicisExpression` extension nodes, so they cannot erase indexed payload semantics.

## Canonical helper audit

`ExpressionSimplifierVisitor` contains broader ordinary algebra useful for complex expression trees:

```text
x+x -> 2*x
x*x -> x^2 (or an exact product when Power is unavailable)
(a+b)*c -> a*c+b*c
(x/a)+(y/b) -> (x*b+y*a)/(a*b)
constant operations -> typed constants
constant conditionals -> selected branch
```

These rules remain available as a public classical structural helper. They must not be inserted blindly after all RICIS phases, because they can change canonical tree shape, distribute expressions expected to remain deferred, and interact with special scalar types.

A trial insertion as a global final phase produced 20 regression failures, including altered A6 payload products, lost deferred scalar types for `int`/`long`, changed exact-ratio representation (`1/2` became `0.5`), changed polynomial canonical shape, and proof trace phase-count changes. That trial was reverted. This is a safety result, not a failure of the ordinary helper.

## Safe normalization strategy

The correct general strategy is staged normalization:

1. Run RICIS identity, polar, SP2, O(1), singular, type, and standard-operation phases in the normative order.
2. Apply safe ordinary structural rules inside SP2 where they cannot erase RICIS payload or alter singular semantics.
3. Keep broad classical canonicalization available through an explicit normalization helper or an opt-in profile, with type-preservation and payload guards.
4. Add an expression-tree corpus test for every new canonical rule and a negative test for every RICIS extension boundary.
5. Never replace deferred expressions with sampled numeric values merely to make a tree look simpler.

## Verification baseline

The safe implementation baseline passes:

```text
Build succeeded
All 315 regression tests passed
```

The baseline includes `RC41–RC49`, Fermat N-only proof logging, analytic self-ratio coverage, ordinary subtraction controls, and indexed-payload protection.

## Remaining extension candidates

The following are candidates for a future opt-in canonical profile, not unconditional RICIS rewrites:

```text
x+x -> 2*x
x*x -> x^2
(a+b)*c -> a*c+b*c
fraction addition and multiplication
constant folding for registered scalar types
conditional branch reduction
```

Each candidate requires a typed-value test, a deferred-expression test, a BigInteger/custom-scalar test where applicable, and a negative RICIS payload test before it can become part of the default pipeline.
