# RICIS Status and Evidence Manifest — 2026-08-20

**TaskId:** `STATUS-EVIDENCE-SYNC-2026-08-20-01`
**Status:** Implemented and QA-pending
**Current gate boundary:** 386/386 Core regression, 18/18 Finance regression, 8/8 Lean artifacts.
**Rule:** Historical counts are retained with provenance; no completed increment is represented as `Proposed` without an explicit historical/deferred boundary.

| Domain | Canonical status | Evidence source | Direct regression / artifact evidence | Claim boundary |
|---|---|---|---|---|
| RSA-PSS fixed profile | `Implemented` | [`Ricis.Numerics/RSA_PSS_NORMATIVE_FINDINGS_2026-08-20.md`](Ricis.Numerics/RSA_PSS_NORMATIVE_FINDINGS_2026-08-20.md), Step 1/2/3 specs | [`RsaPssSha256VerifierSuite.cs`](Ricis.Numerics/Ricis.Numerics.UnitTests/RsaPssSha256VerifierSuite.cs) | Fixed RSA-2048 RSASSA-PSS/SHA-256 only; deferred profiles and PKI remain open |
| ULong2048 mixed operators | `Implemented` and `Tested` | [`ULONG2048_MIXED_INTEGRAL_OPERATOR_MATRIX.md`](Ricis.Numerics/ULONG2048_MIXED_INTEGRAL_OPERATOR_MATRIX.md) | [`ULong2048Suite.cs`](Ricis.Numerics/Ricis.Numerics.UnitTests/ULong2048Suite.cs), generated source and generator | Exact operator contract is covered; no unsupported performance improvement claim |
| Public API / CLI utility surface | `Implemented` and `Tested` | [`PUBLIC_API_CLI_AUDIT.md`](PUBLIC_API_CLI_AUDIT.md), [`PUBLIC_API_TEST_POLICY.md`](PUBLIC_API_TEST_POLICY.md) | API01–API08, API11–API16, API26 in Core regression/MSTest adapter | Every new or changed public method still requires a direct named test |
| Universal `INumber<T>` Core reduction | `Implemented` and `Tested` | [`SPRINT_UNIVERSAL_INUMBER_REDUCTION_STEP2_ARCHITECTURE.md`](SPRINT_UNIVERSAL_INUMBER_REDUCTION_STEP2_ARCHITECTURE.md), [`SPRINT_UNIVERSAL_INUMBER_SEMIPRIME_STEP3_QA_SPEC.md`](SPRINT_UNIVERSAL_INUMBER_SEMIPRIME_STEP3_QA_SPEC.md) | Core generic regression catalog and Numerics integration boundary | Core dependency prohibition remains in force; numeric-domain capabilities are not inferred from `INumber<T>` alone |
| Generic Fermat `INumber<T>` algorithm | `Deferred` | [`INUMBER_SEMIPRIME_PATH_AUDIT_2026-08-20.md`](INUMBER_SEMIPRIME_PATH_AUDIT_2026-08-20.md) | [`FermatSemiprimeSuite.cs`](Ricis.Numerics/Ricis.Numerics.UnitTests/FermatSemiprimeSuite.cs), [`CompositeFermatPruningSuite.cs`](Ricis.Numerics/Ricis.Numerics.UnitTests/CompositeFermatPruningSuite.cs) | Current BigInteger Fermat path and bounded pruning evidence do not prove a generic solver or O(1) factorization |
| Composite Fermat pruning | `Implemented` and `Tested` | [`SPRINT_COMPOSITE_FERMAT_PRUNING_STEP2_ARCHITECTURE.md`](Ricis.Numerics/SPRINT_COMPOSITE_FERMAT_PRUNING_STEP2_ARCHITECTURE.md), [`SPRINT_COMPOSITE_FERMAT_PRUNING_STEP3_QA_MATRIX.md`](Ricis.Numerics/SPRINT_COMPOSITE_FERMAT_PRUNING_STEP3_QA_MATRIX.md) | `CFP-01`–`CFP-20`, `CFP-API-01`–`06` | Conditional bounded search and local certificates only; no universal completeness/O(1) claim |
| Finance production-path readiness | `Blocked` | [`FINANCE_PRODUCTION_PATH_READINESS_2026-08-20.md`](Ricis.Finance/FINANCE_PRODUCTION_PATH_READINESS_2026-08-20.md) | FIN-03/04/05/10/11 readiness matrix | Host/external decisions absent; no production implementation claim |
| Generic Fermat capability boundary | `Deferred` | [`GENERIC_FERMAT_INUMBER_CAPABILITY_BOUNDARY_2026-08-20.md`](Ricis.Numerics/GENERIC_FERMAT_INUMBER_CAPABILITY_BOUNDARY_2026-08-20.md) | BFR-E01–E08 matrix | No generic solver, BigInteger fallback or O(1) claim |
| Lean subject-matter boundary | `Deferred` | [`RICIS_LEAN_SUBJECT_MATTER_BOUNDARY_2026-08-20.md`](RICIS_LEAN_SUBJECT_MATTER_BOUNDARY_2026-08-20.md) | 8/8 engine/route artifact gate | KernelChecked engine artifacts are not subject-matter theorem proofs |

## QA search contract

The package is accepted only if a separate QA pass confirms that each row uses an allowed status, every `Implemented`/`KernelChecked` row has an evidence link, historical counts are labelled, and no row upgrades a deferred or blocked research boundary to complete.
