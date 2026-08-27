# Adversarial Audit Report — RICIS root-to-leaf proof

**Iteration:** 1/5
**Roles:** Analyst → Developer → QA
**Corpus:** all tracked project Markdown files (`git ls-files '*.md'`), plus referenced Lean/C# source, manifest, map and regression suites.

## Executive verdict

The requested result — **proof of every mathematical node and transition from the map root to the leaf** — was **not completed**. The implementation contains a valid kernel-checked theorem for a concrete RICIS-engine invariant, but the route through Hodge, group-ring zero divisors, Weierstrass singularity, Atiyah–Singer, Poincaré, Morse, knot theory and spectral asymptotics is represented by labels and payload-preserving transformations. It is therefore a **SimulatedRoute / ProvedEngineInvariant**, not a subject-matter proof of those ten domain claims.

This is not a Lean compiler failure. Lean correctly proves the theorem that was written. The defect is a **scope/semantic acceptance failure**: a structural route theorem was previously described to the user as if it satisfied the stronger full proof request.

## Adversarial evidence

The project’s own Markdown policy already establishes the correct trust boundary. `RICIS_III_CONCEPT.md` requires expression/type/index/payload preservation and says external classical or domain reasoning cannot be silently substituted. `LEAN_ARTIFACT_POLICY.md` says kernel compilation proves the saved theorem source, while regression evidence and audit documents do not replace it. `RICIS3_LONGEST_ROUTE_LEAN_AUDIT.md` explicitly calls the earlier artifact a route-composition theorem under external premises. `LongestRouteSpectralDesign.md` explicitly lists local preservation fields as premises and excludes independent proofs of Hodge, Poincaré, Atiyah–Singer, knot theory and spectral asymptotics.

The concrete source confirms the adversarial finding:

| Source location | Observation | Consequence |
|---|---|---|
| `LongestRouteConcreteEngineProof.lean:5–17` | Ten domain names are constructors of `RouteNode` | Names are labels, not domain definitions |
| `:20–43` | `Payload` contains only `node`, rational determinant, inverse payload, product payload and `indexed`; `RouteInvariant` has three structural equalities | No Hodge/ring/manifold/operator/knot/spectrum proposition exists |
| `:45–63` | `l1`, `sp4`, `sp2` and `verify` are identity functions; `a6` only assigns `determinant * inversePayload` | The named phases do not implement the domain semantics of each node |
| `:65–67` | `edge next payload := { payload with node := next }` | Every dependency edge only renames a label and preserves the same payload |
| `:85–88` | `edge_preserves` returns the incoming invariant unchanged | No mathematical transition between adjacent external domains is proved |
| `:90–179` | Each depth theorem repeats the same generic `edge_preserves` and `local_run_preserves` | Ten checkpoints are structurally valid but semantically route-independent |
| `:181–186` | Leaf theorem proves only leaf label plus determinant zero | It does not prove spectral asymptotics or its reduction |

The decisive mutation test is conceptual and exact: replacing `.hodge`, `.poincare` or `.spectralAsymptotics` by any other `RouteNode` constructor leaves `RouteInvariant` and `edge_preserves` unchanged. A theorem whose proof is invariant under replacement of the domain node cannot prove the domain theorem attached to that node.

## Classification

| Artifact/claim | Correct status | Reason |
|---|---|---|
| `root_determinant_is_zero` | `ProvedSubjectMatter` for the rank-one arithmetic identity | Lean proves `1·1−1·1=0` over `ℚ` |
| `root_payload_invariant` | `ProvedEngineInvariant` | Concrete payload and structural product are checked |
| `local_run_preserves` | `ProvedEngineInvariant` | It proves preservation of the defined payload invariant |
| Nine `depthN_edge_proof` theorems | `SimulatedRoute` plus engine invariant preservation | Edges only mutate the enum label |
| `full_root_to_leaf_engine_proof` | `ProvedEngineInvariant` | It proves the concrete rank-one payload invariant at the labelled leaf |
| Hodge/Poincaré/Atiyah–Singer/Morse/knot/spectral claims | `Open` | No domain definitions, hypotheses or subject-matter theorems are present |
| Lean manifest `KernelChecked` | Correct only for the theorem source | It must not be read as `ProvedSubjectMatter` for map-node claims |
| C# regression PASS | `TestedRuntime` | It tests source/manifest contracts, not external mathematical truth |
| Green map `resolved` status | `AuditOnly` operational metadata | It records route generation and proof-record shape, not domain proof |

## Scoring ledger

| Role | Result | Points |
|---|---|---:|
| Analyst | The revised TЗ makes the semantic distinction explicit | `+100` provisional; final acceptance depends on corrective scope |
| Developer | Kernel artifact and tests are real, but the stronger user-requested subject proof is not delivered | `0` acceptance bonus; `-30` for scope mismatch |
| QA | Confirmed unique simulation/overclaim defect with source-level evidence | `+20` |
| Critical end-to-end failure | Not hidden; detected before final acceptance | `0` penalty |

**Current audit score:** `+90` provisional. It is not a release score because the requested subject-matter proof remains open.

## Corrective backlog

The next implementation must not add more labels or generic `edge_preserves` lemmas. For each node it must first introduce a typed subject proposition and its hypotheses: a concrete singularity expression at the root; a precise Hodge proposition; a specified ring and zero-divisor statement; a concrete Weierstrass function and regularity lemmas; operator/index definitions for Atiyah–Singer; manifold hypotheses and theorem for Poincaré; smooth function and critical-point theorem for Morse; knot representation/invariant theorem; and operator/spectrum/asymptotic definitions for the spectral leaf. Every edge then needs a semantic bridge theorem connecting the actual output proposition of one node to the input proposition of the next.

Until those definitions and bridges exist, the honest project status is **engine-level route proven, subject-matter route not proven**. No document, manifest description or user-facing result should call this a full proof of the named external mathematics.

## References

[1]: `RICIS_III_CONCEPT.md` — canonical identity, indexing and classical-fallback boundary.
[2]: `LEAN_ARTIFACT_POLICY.md` — artifact statuses and kernel trust boundary.
[3]: `FormalVerification/Lean/Artifacts/LongestRouteSpectralDesign.md` — explicit route-composition claim and non-claims.
[4]: `RICIS3_LONGEST_ROUTE_LEAN_AUDIT.md` — route selection and external-premise boundary.
[5]: `RICIS_RIEMANN_IMPROVED_FORMULATION.md` — project precedent distinguishing conditional algebraic lemma from full external theorem.
[6]: `FormalVerification/Lean/Routes/LongestRouteConcreteEngineProof.lean` — source-level adversarial evidence.
