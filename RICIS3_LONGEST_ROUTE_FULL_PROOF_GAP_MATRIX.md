# RICIS III — Full Root-to-Leaf Proof Gap Matrix

**Status:** Concrete RICIS-engine proof implemented and kernel-checked after user correction. The previous `LongestRouteSpectral.lean` remains a conditional composition artifact; `LongestRouteConcreteEngineProof.lean` now proves the concrete rank-one payload route itself from root to leaf. This matrix records the remaining boundary: standard external theorem statements still require their own domain formalization.

## Selected route

`math-singularity → Hodge → group-ring zero divisors → Weierstrass singularity → Atiyah–Singer → Poincaré → Morse → knot theory → spectral asymptotics → ReduceToRicisCore(spectral asymptotics)`.

## Node evidence audit

| Depth | Node | Map-provided mathematical content | Current proof-record content | Required full proof input |
|---:|---|---|---|---|
| 0 | `math-singularity` | “0/0 through RICIS-III fractal identity” | Generic `geometric_bridge_F_G` placeholder | Exact RICIS expression, domain, indexed-zero semantics, and the theorem relating the expression to the declared invariant. |
| 1 | `real-catalog-1` | Hodge-labelled claim about harmonic differentials and rational combinations | Same generic placeholder | Exact proposition and hypotheses. The map description is not a complete standard Hodge-conjecture statement. |
| 2 | `real-catalog-5` | Kaplan’skiĭ zero-divisor hypothesis | Same generic placeholder | Ring definition, hypotheses, and target implication. |
| 3 | `real-catalog-6` | Weierstrass continuous nowhere-differentiable function | Same generic placeholder | Concrete function, continuity theorem, nondifferentiability theorem, and RICIS transformation relation. |
| 4 | `real-catalog-10` | Analytic/topological index relation | Same generic placeholder | Concrete operator/index definitions and the exact relation to the RICIS invariant. |
| 5 | `real-catalog-11` | Poincaré-labelled 3-manifold claim | Same generic placeholder | Exact manifold hypotheses and theorem statement; the map currently supplies only prose. |
| 6 | `real-catalog-23` | Morse critical-point claim | Same generic placeholder | Smooth-manifold/function hypotheses and explicit critical-point lemma. |
| 7 | `real-catalog-25` | Conway knot invariants | Same generic placeholder | Knot representation, invariant definition, and preserved transition. |
| 8 | `real-catalog-26` | Weyl formula / spectral asymptotics | Same generic placeholder | Operator/spectrum definitions and exact asymptotic statement. |
| 9 | `agent-offline-real-catalog-26-1785943252938-0` | Structural reduction to L0/L1, SP2–SP4, A1–A6 | Same generic placeholder | Concrete source expression and reduction theorem, not merely a certificate field. |

## What “full proof” means for this sprint

A full root-to-leaf Lean proof must contain a concrete proposition for every row, concrete hypotheses for every proposition, a proved local theorem for every node, and a proved transition theorem for every dependency edge. The terminal theorem may then compose those proved results, but it may not accept them as unconstrained fields of an evidence structure.

The existing map is sufficient to select the route and name the target functions, but it is **not sufficient to reconstruct all standard mathematical statements**: several descriptions are short labels or informal summaries, and the proof records repeat one generic A6 bridge for every node. The implementation must therefore use the exact RICIS expressions already present in the project where available and mark any absent domain proposition as an explicit unresolved input rather than silently inventing it.

## Current implementation result

For the concrete RICIS-engine interpretation, every route node now has a concrete payload state, every local run is defined by L1 → SP4 → SP2 → A6 → L1, and every one of the nine edges has a direct `edge_preserves` theorem. The artifact proves `full_root_to_leaf_engine_proof` and `leaf_is_spectral_ricis_core` in the Lean kernel. The rank-one determinant is the explicit project scenario `1*1−1*1=0`; the product payload is structural and never evaluates a classical inverse at zero.

## No false completion rule

The concrete engine route may be marked **KernelChecked**. The route must not be reported as an independent standard-domain proof of Hodge, Kaplańskiĭ, Atiyah–Singer, Poincaré, Morse, knot theory or spectral asymptotics. Those descriptions remain external domain labels until their definitions and hypotheses are separately formalized.
