# RICIS III Structural Algebra Coverage

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.

## Purpose

This document records the structural-algebra rules executed by the Ricis.Core optimization pipeline. The inventory treats RICIS rules as highest priority and inherits ordinary classical structural algebra only where RICIS has not defined a different semantic rule.

The rules operate on expression structure. They do not execute deferred functions, replace deferred payloads with sampled values, or use limits and L'Hôpital transformations.

## Pipeline location

Structural algebra is executed by `AlgebraicReductionVisitor` in `RicisPhasePipeline` after identity-of-essence and polar phases and before O(1), singularity, type-consistency, and standard-operation phases.

Indexed-zero and indexed-infinity extension expressions are excluded from ordinary zero/negation rewrites in this phase. Their payload must remain available to the later RICIS phases.

## Covered structural rules

| Family | Rule | Status | Regression |
|---|---|---:|---|
| Identity | `F/F -> 1` for structurally identical intrinsic scalar expressions | Covered | `RC01`, `RC30`, `RC36–RC40`, `RC43` |
| Units | `F*1 -> F`, `1*F -> F`, `F/1 -> F` | Covered | `RC05`, existing suites |
| Additive zero | `F+0 -> F`, `0+F -> F` | Covered | `RC44` path and existing standard-operation coverage |
| Subtractive zero | `F-0 -> F` | Added | `RC44` |
| Zero subtraction | `0-F -> -F` | Added | `RC45` |
| Self subtraction | `F-F -> 0` for ordinary intrinsic expressions | Added | `RC46` |
| Double negation | `-(-F) -> F` for ordinary intrinsic expressions | Added | `RC47` |
| Common factors | `(F*G)/F -> G`, `F/(F*G) -> 1/G` | Covered | `RC03`, `RC38`, stress suites |
| Associative factors | Cancel matching factors independently of multiplication tree association | Covered | stress suites |
| Shared ratio | `(F/A)/(G/A) -> F/G` | Covered | `RC04` |
| Nested ratio | `F/(G/H) -> (F*H)/G` | Covered | `NESTED_RATIO` and proof suites |
| Difference of squares | `(A²-B²)/(A-B) -> A+B` | Covered | `RC23` |
| Power difference | `a^N/a^(N-X) -> a^X` | Added | `RC41`, `RC42` |
| Factorial difference | `n!/(n-1)! -> n` | Covered | factorial regression |
| Analytic self-ratio | `G(F)/G(F) -> 1` for `Log`, `Log10`, `Sqrt`, `Exp`, `Sin`, `Cos`, `Tan`, `Sinh`, `Cosh`, `Tanh` | Covered | `RC43` |
| Commutative identity | `(F+G)/(G+F) -> 1`, `(F*G)/(G*F) -> 1` | Covered | structural comparer and probe |

## RICIS-protected boundaries

The following are not ordinary cancellation rules and must not be introduced as unconditional rewrites merely because they resemble school algebra:

```text
sqrt(F^2)       -> F
log(F*G)        -> log(F)+log(G)
log(F/G)        -> log(F)-log(G)
exp(log(F))     -> F
log(exp(F))     -> F
abs(F)^2        -> F^2
```

These forms require domain, branch, sign, or an explicit RICIS rule. Their absence is not a missing generic cancellation rule. In particular, `G(F)/G(F) -> 1` remains valid as identity-of-essence even when `G` is a logarithm, root, or trigonometric function.

## RICIS payload safety

Ordinary reductions added to `AlgebraicReductionVisitor` explicitly exclude `RicisExpression` extension nodes. Consequently, expressions such as `0_F`, `∞_F`, keyed poles, and deferred payload-bearing derivatives are not collapsed by classical zero or negation rewrites before their RICIS phase.

## Verification

The full regression suite was run after the changes:

```text
Build succeeded
All 313 regression tests passed
```

The newly added structural-algebra checks are:

```text
RC41: F^n/F^(n−1) -> F structurally
RC42: a^N/a^(N−X) -> a^X structurally
RC43: analytic G(F)/G(F) -> 1
RC44: F−0 -> F
RC45: 0−F -> −F
RC46: F−F -> 0
RC47: −(−F) -> F
```

No RICIS axiom was changed. The modifications extend the optimization step with inherited structural algebra and add regression protection for each new rule.
