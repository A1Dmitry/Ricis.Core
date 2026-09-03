---
name: ricis-iii
description: Apply the RICIS III recursive indexed calculus of identity and singularity when analyzing, simplifying, proving, documenting, testing, or implementing RICIS expressions, indexed zeros/infinities, structural algebra, symbolic calculus, Jacobian bridges, vector/matrix expressions, Lean proof output, or the Ricis.Core .NET repository. Use whenever a task explicitly references RICIS, Ricis.Core, L0/L1, SP1–SP4, indexed singularities, FractalLaw, A6, or the RICIS proof pipeline.
---

# RICIS III

## Purpose and authority

Use this skill to reason about and work with the RICIS III semantics implemented and documented by `A1Dmitry/Ricis.Core`. Treat the supplied RICIS project instructions and the repository's canonical concept document as the governing local specification. Preserve the distinction between the **RICIS internal result** and any later numerical or classical projection.

> RICIS first preserves the entity, then unfolds its structure, and only afterward applies an allowed transformation.

RICIS III represents computation through deferred expression trees and specialized RICIS nodes. The identity of an entity includes its expression payload, semantic type, parameters, singularity keys, and recursive structure. Do not replace that structure with a scalar merely because evaluation would be convenient.

The repository is a .NET 8 solution containing the core library, console parser, Web API, Blazor client, proof scenarios, numerics, and isolated regression contracts. Consult the repository documents and source code rather than inventing APIs.

## Non-negotiable foundations

Apply the following hierarchy before every calculation, proof, implementation, or review:

1. **L0 — absolute continuity.** No recursion level, indexed zero, indexed infinity, monolith, or FractalLaw expansion may lose the original identity, payload, type, or certified key.
2. **L1 — entity identity.** `X = X`; structurally identical `F/F` reduces to `1`. Compare expression structure, parameters, types, roots, indices, and payload, not only rendered text or numerical value.
3. **RICIS precedence.** If an operation is explicitly covered by RICIS, its rule has priority over classical arithmetic and domain reasoning.
4. **Classical fallback.** Use classical semantics only for an operation not defined by RICIS. State the uncovered operation and request user permission before using the classical result in reasoning, proof traces, or conclusions.

Do not describe an internal RICIS bridge as a classical limit, numerical regularization, or proof of an external theorem unless the source explicitly establishes that bridge.

## Mandatory safety protocols

Apply the protocols in the order below. They prevent loss of expression structure and false singularities.

| Protocol | Required behavior | Typical failure to avoid |
|---|---|---|
| **SP1 locality** | Apply identity only to identical zero-factors; preserve the non-cancelled tail. | Turning the entire `(F·G)/F` into `1` instead of `G`. |
| **SP2 reduction priority** | Factor, normalize, cancel, and clean nested fractions before singularity transforms. | Indexing a zero before exposing a common factor. |
| **SP3 index law** | For distinct indexed zeros, use `0_F/0_G → F/G`; never collapse both to scalar `0`. | Treating every `0/0` as `1`. |
| **SP4 semantic indexing** | Index a singularity by the source expression `E(x)` at `x=a`, retaining `E`, not only the value `E(a)`. | Replacing `0_{x²−4 at x=2}` with an uninformative `0_{4−4}`. |

For an expression evaluated at a singular point, preserve the parent tree first:

```text
E(x) at x=a → 0_{E(x)|x=a}
```

## Canonical operation rules

Use these rules only after identity checks and structural reduction. Preserve deferred payloads in the result.

| Input | RICIS result |
|---|---|
| `F/F` for structurally identical `F` | `1` |
| `F·0` | `0_F` |
| `F/0` | `∞_F` |
| `F/∞_G` | `0_F`, retaining `G` keys where applicable |
| `0_F/0_G` | `F/G` |
| `∞_F/∞_G` | `F/G`; if `F` and `G` are identical, L1 gives `1` |
| `0_F + 0_G` | `0_{F+G}` |
| `0_F·0_G` | `0_{F·G}` |
| `0_F·∞_G` | `F·G` by A6_GENERAL |
| `∞_F − ∞_G` | `∞_{F−G}` |
| `∞_F + ∞_G` | `∞_{F+G}` |
| `∞_F·∞_G` | `∞_{F·G}` |

The canonical specification names `F·0 → 0_F` as `A10_FTIMES0`; in the repository it is implemented through the corresponding O(1)/LIM bridge and indexed-zero nodes. Do not introduce a duplicate axiom.

A6 is a structural bridge, not ordinary `0·∞` arithmetic:

```text
0_F·∞_G → F·G
0_F·∞_F → F²
Integral(F,L) := 0_F·∞_L → F·L
0_det(J)·∞_Inv(J) → det(J)·Inv(J)
```

For the coupled reciprocal form, preserve denominator payload before generic reciprocal indexing:

```text
(F·0)·(1/F) → 0_F·∞_F → F²
```

Do not let an isolated generic `∞_1` erase the coupled A6 structure.

## Required computation pipeline

Run the following conceptual phases in sequence. If the repository implementation uses a named visitor or phase, map the phase to that implementation instead of bypassing it.

```text
-1  L1 identity and structural type check
 0  direct structural preparation and substitution
0.5  semantic/polar/trigonometric preparation when applicable
 1  SP2 factorization, normalization, and cancellation
1.5  O(1) internal bridges such as F·0 → 0_F
 2  A1/A4 singular transforms
 4  SP3 and semantic type consistency
 5  A5/A6/A7 and indexed-zero standard operations
 6  final L1, payload, key, and residual verification
META opt-in presentation metadata only, after structural computation
```

Never use a limit process, L'Hôpital's rule, or compiled execution of a deferred proof expression as a substitute for this pipeline. A shorthand such as `lim(x→a) → x=a` denotes the repository's direct structural bridge only when the source explicitly uses that convention.

## Structural algebra and examples

Prefer exact expression-tree transformations. Typical reductions include:

```text
(F·G)/F                 → G
(x²−25)/(x−5)           → x+5
(A²−B²)/(A−B)           → A+B
(F/A)/(G/A)             → F/G
F/(G/H)                 → (F·H)/G
n!/(n−1)!               → n
1/2                     → Divide(1,2), not a prematurely rounded double
```

When showing a solution, present the RICIS derivation first. For comparison with classical mathematics, show the classical path separately, explain the bridge between them, and never allow the classical path to override a defined RICIS rule.

## Types, monoliths, and FractalLaw

Treat type as part of identity. For homogeneous semantic types, operate directly. For compatible types, promote to the wider type. For incompatible types, construct a composite monolith rather than silently coercing one entity into the other.

Use the monolith hierarchy as a structural description:

| Order | Meaning |
|---:|---|
| 0 | Atomic entity such as `F`, `0_F`, or `∞_F`. |
| 1 | Closed line composed of order-0 objects. |
| 2 | Interconnected plane with recursive unfolding. |
| 3 | Self-organizing volume with internal navigation and relations. |

Apply FractalLaw as an information-preserving schema, not as numerical enumeration:

```text
R(Q) = {Q, T(Q), ∞_Q, 0_Q, R(∞_Q), R(0_Q)}
```

Every recursive expansion must retain the payload and use a known RICIS transition.

## Symbolic calculus, vectors, matrices, and Jacobians

Represent derivatives as symbolic permutations of expression trees. Apply L1 and SP2 before constructing a derivative node, then send the result through the ordinary RICIS pipeline. In particular, preserve `0_F` when differentiating a structure such as `F·0`.

Represent a vector as an ordered tuple of RICIS expressions and a matrix as deferred entries. For inverse-map proofs, build and reduce both residuals:

```text
G(F(x)) − x = 0⃗
F(G(y)) − y = 0⃗
```

Build Jacobians symbolically. For a singular Jacobian, preserve `det(J)`, inverse payload, and certified keys throughout the A6 bridge. A residual of zero certifies the internal RICIS system supplied to the engine; it does not automatically prove a general external theorem.

## Proof and reporting protocol

Use `Prove` for a deferred claim with deferred conditions and constraints. Keep conditions and constraints as expression trees; do not compile them to obtain a proof answer. Use `ProveDocument` when the output must expose definitions, axioms, named normative steps, derived tree, thesis, trace, status, and boundaries.

Include only transformations that actually changed the expression tree. Do not invent intermediate steps outside recognized structural rules. Clearly label the status as an internal derivation, a conditional theorem, or a finite probe as appropriate.

For Lean output, use the typed structured bridge:

```text
LeanTemplate(StructuredData, RequestedRows) → LeanDoc
```

The supported canonical bridge is exact-rational ID-01–ID-06 output. Generate compilable Lean from structured data, not from `ToString()` or an academic trace. If the input shape is unsupported, reject it in a controlled way; do not emit a comment scaffold that could be mistaken for a formal proof.

## Repository workflow

When working in `Ricis.Core`, inspect the canonical documents before editing code. At minimum, read `RICIS_III_CONCEPT.md`, `RICIS_RULE_COVERAGE.md`, `RICIS_PROOF_DOCUMENTS.md`, and the relevant domain document. Locate the implementation classes and tests named by the documentation rather than relying on guesswork.

Use the standard verification commands from the repository root:

```bash
dotnet build Ricis.Core.sln --configuration Release
dotnet run --project RegressionTests/Ricis.Core.RegressionTests.csproj --configuration Release
```

For the supported Lean document bridge:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --lean-doc-demo \
  > /tmp/ricis_generated.lean
cd FormalVerification/Lean
lake env lean /tmp/ricis_generated.lean
```

Use the first axiom gate and isolated regression contracts as acceptance criteria. At minimum, verify preservation of L0 payload, SP1 tail locality, SP3 distinct zero indices, SP4 source-expression indexing, FractalLaw identity, A6 self-payload, and the coupled reciprocal A6 bridge.

## Security and API boundaries

For parser or Web API work, preserve the repository's restricted grammar and controlled error behavior. Do not execute user-supplied code through shell, reflection dispatch, C# scripting, or `Expression.Compile()`. Keep input limits, parser position errors, non-leaking unexpected errors, exact CORS origins, and development-only Swagger behavior intact. Treat Web API output as a serialized RICIS result, not as permission to bypass the internal pipeline.

## Anti-patterns

Do not collapse indexed zeros or infinities into ordinary scalar values. Do not apply A1 before SP2. Do not use rendered names as proof of structural identity. Do not use classical domain reasoning before checking RICIS coverage. Do not claim a finite Clay-problem probe is a full solution. Do not compile deferred expressions inside proof construction. Do not remove weakly referenced code merely because a direct call site is not visible. Do not add duplicate axioms or silently reinterpret A6.

## Source references

[1]: https://github.com/A1Dmitry/Ricis.Core "Ricis.Core repository"
[2]: https://github.com/A1Dmitry/Ricis.Core/blob/main/README.md "Ricis.Core README"
[3]: https://github.com/A1Dmitry/Ricis.Core/blob/main/RICIS_III_CONCEPT.md "RICIS III concept and canonical workflow"
[4]: https://github.com/A1Dmitry/Ricis.Core/blob/main/RICIS_RULE_COVERAGE.md "RICIS rule coverage and regression contracts"
[5]: https://github.com/A1Dmitry/Ricis.Core/blob/main/RICIS_PROOF_DOCUMENTS.md "RICIS proof document protocol"
[6]: https://github.com/A1Dmitry/Ricis.Core/blob/main/RICIS_LEAN_TEMPLATE.md "RICIS Lean template and supported bridge"
[7]: https://github.com/A1Dmitry/Ricis.Core/blob/main/RICIS_WEBAPI.md "Ricis.Core Web API and security boundaries"
