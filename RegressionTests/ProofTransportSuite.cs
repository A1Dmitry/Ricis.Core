using Ricis.Core.Proofs;
using Ricis.WebApi.Proofs;

/// <summary>
/// Direct regressions for the PEP-01 authoritative proof transport domain boundary.
/// These tests deliberately execute no WebAPI route, provider, browser, network or Lean compiler.
/// </summary>
public static class ProofTransportSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PEP01: snapshot сохраняет immutable Core derivation и hashes documents", SnapshotPreservesImmutableDerivationAndDocumentHashes),
        ("PEP02: snapshot store отклоняет duplicate proofRunId", SnapshotStoreRejectsDuplicateRunId),
        ("PEP03: expired snapshot возвращает typed expiry, без re-derivation", SnapshotStoreReturnsExpiredWithoutRerun),
        ("PEP04: LEAN_VERIFIED требует complete kernel evidence", LeanVerifiedRequiresCompleteEvidence),
        ("PEP05: non-Lean Core result остаётся REQUIRES_CORE_LEAN", StructuralCoreResultDoesNotPromoteTrust),
        ("PEP06: неизвестный document format не получает export", SnapshotRejectsUnknownDocumentFormat),
        ("PEP07: application service создаёт один snapshot из одной canonical derivation", ApplicationServiceStoresOneCanonicalSnapshot),
        ("PEP08: rejected derivation не создаёт snapshot", ApplicationServiceRejectsWithoutSnapshot),
        ("PEP09: bounded expression deriver создаёт Core documents из одной derivation", ExpressionDeriverCreatesCanonicalCoreDocuments),
        ("PEP10: malformed expression deriver возвращает typed rejection", ExpressionDeriverRejectsMalformedInput),
        ("PEP11: snapshot сохраняет immutable typed Core trace", SnapshotPreservesImmutableTypedTrace),
    ];

    private static void SnapshotPreservesImmutableDerivationAndDocumentHashes()
    {
        var sourceDocuments = new Dictionary<RicisProofDocumentFormat, string>
        {
            [RicisProofDocumentFormat.Json] = "{\"status\":\"REQUIRES_CORE_LEAN\"}",
            [RicisProofDocumentFormat.Latex] = "\\text{Core derivation}",
        };
        var snapshot = CreateStructuralSnapshot(documents: sourceDocuments);
        sourceDocuments[RicisProofDocumentFormat.Json] = "tampered";

        Assert(snapshot.TryGetDocument(RicisProofDocumentFormat.Json, out var json), "JSON export должен существовать в snapshot.");
        Assert(json == "{\"status\":\"REQUIRES_CORE_LEAN\"}", "Snapshot обязан копировать document content, а не хранить mutable caller dictionary.");
        Assert(snapshot.DocumentHashes[RicisProofDocumentFormat.Json] == ProofContentHash.Compute(json), "Snapshot обязан публиковать hash фактического immutable JSON content.");
    }

    private static void SnapshotStoreRejectsDuplicateRunId()
    {
        var clock = new TestProofClock(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
        var store = new InMemoryProofRunSnapshotStore(clock);
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        store.SaveAsync(CreateStructuralSnapshot(runId)).GetAwaiter().GetResult();

        RequireThrows<InvalidOperationException>(
            () => store.SaveAsync(CreateStructuralSnapshot(runId)).GetAwaiter().GetResult(),
            "Одинаковый proofRunId не должен silently overwrite canonical snapshot.");
    }

    private static void SnapshotStoreReturnsExpiredWithoutRerun()
    {
        var clock = new TestProofClock(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
        var store = new InMemoryProofRunSnapshotStore(clock);
        var runId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        store.SaveAsync(CreateStructuralSnapshot(runId, expiresAtUtc: clock.UtcNow.AddMinutes(1))).GetAwaiter().GetResult();
        clock.Advance(TimeSpan.FromMinutes(2));

        var lookup = store.FindAsync(runId).GetAwaiter().GetResult();

        Assert(lookup.Kind == ProofSnapshotLookupKind.Expired, "Expired run обязан возвращать typed Expired outcome.");
        Assert(lookup.Snapshot is null, "Expired lookup не должен возвращать старый snapshot или пересоздавать derivation.");
    }

    private static void LeanVerifiedRequiresCompleteEvidence()
    {
        RequireThrows<ArgumentException>(
            () => _ = new ProofEvidenceMetadata(
                ProofTrustStatus.LeanVerified,
                artifactId: null,
                contentHash: null,
                toolchain: null,
                verificationCommand: null,
                compilerOutputDigest: null,
                axiomOutputDigest: null,
                boundaryResourceKey: "proof.core.lean.verified"),
            "LEAN_VERIFIED без artifact/hash/toolchain/compiler/axiom evidence запрещён.");
    }

    private static void StructuralCoreResultDoesNotPromoteTrust()
    {
        var snapshot = CreateStructuralSnapshot();

        Assert(snapshot.StructuralVerification == ProofStructuralVerification.StructurallyVerified,
            "Fixture обязан представлять verified Core structural derivation.");
        Assert(snapshot.Evidence.TrustStatus == ProofTrustStatus.RequiresCoreLean,
            "Structural Core result не может автоматически стать LEAN_VERIFIED.");
    }

    private static void SnapshotRejectsUnknownDocumentFormat()
    {
        var snapshot = CreateStructuralSnapshot();

        Assert(!snapshot.TryGetDocument((RicisProofDocumentFormat)999, out _),
            "Неизвестный format не должен получать derived document или fallback export.");
    }

    private static void SnapshotPreservesImmutableTypedTrace()
    {
        var trace = new[]
        {
            new Ricis.Core.Logging.RicisLogEntry(
                sequence: 1,
                timestampUtc: DateTimeOffset.Parse("2026-08-21T00:00:00Z"),
                severity: Ricis.Core.Logging.RicisLogSeverity.Info,
                eventCode: "RICIS_PROOF_START",
                message: "test-only trace",
                stageType: "TestStage"),
        };
        var snapshot = new ProofRunSnapshot(
            proofRunId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            correlationId: "pep-correlation-005",
            createdAtUtc: DateTimeOffset.Parse("2026-08-21T00:00:00Z"),
            expiresAtUtc: DateTimeOffset.Parse("2026-08-21T01:00:00Z"),
            coreVersion: "8.0.0-test",
            canonicalClaim: "x => (x / x)",
            normalizedClaim: "x => 1",
            structuralVerification: ProofStructuralVerification.StructurallyVerified,
            evidence: CreateDerivationMaterial().Evidence,
            documents: CreateDerivationMaterial().Documents,
            trace: trace);

        Assert(snapshot.Trace.Count == 1 && snapshot.Trace[0].EventCode == "RICIS_PROOF_START",
            "Snapshot обязан сохранить ordered typed Core trace.");
    }

    private static void ApplicationServiceStoresOneCanonicalSnapshot()
    {
        var clock = new TestProofClock(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
        var store = new InMemoryProofRunSnapshotStore(clock);
        var deriver = new TestProofRunDeriver(ProofRunDerivationOutcome.Accepted(CreateDerivationMaterial()));
        var service = new ProofRunApplicationService(
            deriver,
            store,
            new TestProofRunIdFactory(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "pep-correlation-003"),
            clock);

        var outcome = service.CreateAsync(new ProofRunCreateCommand("x => (x / x)", "x => 1", TimeSpan.FromMinutes(30), [RicisProofDocumentFormat.Json, RicisProofDocumentFormat.Latex])).GetAwaiter().GetResult();

        Assert(outcome is ProofRunCreationAccepted accepted, "Accepted canonical derivation должна создать typed accepted outcome.");
        Assert(deriver.CallCount == 1, "Один create request обязан вызвать canonical deriver ровно один раз.");
        var lookup = store.FindAsync(((ProofRunCreationAccepted)outcome).Snapshot.ProofRunId).GetAwaiter().GetResult();
        Assert(lookup.Kind == ProofSnapshotLookupKind.Found && lookup.Snapshot is not null, "Accepted outcome обязан сохранить immutable snapshot.");
    }

    private static void ApplicationServiceRejectsWithoutSnapshot()
    {
        var clock = new TestProofClock(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
        var store = new InMemoryProofRunSnapshotStore(clock);
        var deriver = new TestProofRunDeriver(ProofRunDerivationOutcome.Rejected("UNSUPPORTED_PROOF_SHAPE", "proof.core.unsupported", retryable: false));
        var service = new ProofRunApplicationService(
            deriver,
            store,
            new TestProofRunIdFactory(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "pep-correlation-004"),
            clock);

        var outcome = service.CreateAsync(new ProofRunCreateCommand("x => x", "x => x", TimeSpan.FromMinutes(30), [RicisProofDocumentFormat.Json])).GetAwaiter().GetResult();

        Assert(outcome is ProofRunCreationRejected, "Rejected canonical derivation должна вернуть typed rejection.");
        Assert(deriver.CallCount == 1, "Rejected request обязан обратиться к deriver только один раз.");
        var lookup = store.FindAsync(Guid.Parse("44444444-4444-4444-4444-444444444444")).GetAwaiter().GetResult();
        Assert(lookup.Kind == ProofSnapshotLookupKind.Missing, "Rejected derivation не должна создавать snapshot.");
    }

    private static void ExpressionDeriverCreatesCanonicalCoreDocuments()
    {
        var deriver = new ExpressionEquivalenceProofRunDeriver(CreateTestDocumentProfile());

        var outcome = deriver.DeriveAsync(
            new ProofRunCreateCommand("x => (x / x)", "x => 1", TimeSpan.FromMinutes(30), [RicisProofDocumentFormat.Json, RicisProofDocumentFormat.Latex])).GetAwaiter().GetResult();

        Assert(outcome is ProofRunDerivationAccepted, "Supported bounded expression должен дать authoritative Core derivation.");
        var accepted = (ProofRunDerivationAccepted)outcome;
        Assert(accepted.Material.Evidence.TrustStatus == ProofTrustStatus.RequiresCoreLean,
            "Generic structural derivation не может mint LEAN_VERIFIED.");
        Assert(accepted.Material.Documents.ContainsKey(RicisProofDocumentFormat.Json) &&
               accepted.Material.Documents.ContainsKey(RicisProofDocumentFormat.Latex),
            "Одна Core derivation должна рендерить requested stored JSON/LaTeX documents.");
    }

    private static void ExpressionDeriverRejectsMalformedInput()
    {
        var deriver = new ExpressionEquivalenceProofRunDeriver(CreateTestDocumentProfile());

        var outcome = deriver.DeriveAsync(
            new ProofRunCreateCommand("not a lambda", "x => 1", TimeSpan.FromMinutes(30), [RicisProofDocumentFormat.Json])).GetAwaiter().GetResult();

        Assert(outcome is ProofRunDerivationRejected rejected && rejected.Code == "PROOF_PARSE_FAILED",
            "Malformed input обязан вернуть typed parser rejection, not exception/fallback proof.");
    }

    private static RicisProofDocumentProfile CreateTestDocumentProfile() => new(
        title: "PEP test profile",
        scope: RicisProofScope.FiniteDerivation,
        @abstract: "Deterministic PEP transport fixture.",
        theorem: "A bounded structural Core derivation is exported from one snapshot.",
        limitations: ["This test profile is not Lean kernel evidence."]);

    private static ProofRunDerivationMaterial CreateDerivationMaterial() => new(
        coreVersion: "8.0.0-test",
        normalizedClaim: "x => 1",
        structuralVerification: ProofStructuralVerification.StructurallyVerified,
        evidence: new ProofEvidenceMetadata(
            ProofTrustStatus.RequiresCoreLean,
            artifactId: null,
            contentHash: null,
            toolchain: null,
            verificationCommand: null,
            compilerOutputDigest: null,
            axiomOutputDigest: null,
            boundaryResourceKey: "proof.core.lean.required"),
        documents: new Dictionary<RicisProofDocumentFormat, string>
        {
            [RicisProofDocumentFormat.Json] = "{\"status\":\"REQUIRES_CORE_LEAN\"}",
        });

    private sealed class TestProofRunDeriver(ProofRunDerivationOutcome outcome) : IProofRunDeriver
    {
        public int CallCount { get; private set; }

        public Task<ProofRunDerivationOutcome> DeriveAsync(
            ProofRunCreateCommand command,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(outcome);
        }
    }

    private sealed class TestProofRunIdFactory(Guid proofRunId, string correlationId) : IProofRunIdFactory
    {
        public Guid CreateProofRunId() => proofRunId;

        public string CreateCorrelationId() => correlationId;
    }

    private static ProofRunSnapshot CreateStructuralSnapshot(
        Guid? runId = null,
        IReadOnlyDictionary<RicisProofDocumentFormat, string>? documents = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        return new ProofRunSnapshot(
            proofRunId: runId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"),
            correlationId: "pep-correlation-001",
            createdAtUtc: DateTimeOffset.Parse("2026-08-21T00:00:00Z"),
            expiresAtUtc: expiresAtUtc ?? DateTimeOffset.Parse("2026-08-21T01:00:00Z"),
            coreVersion: "8.0.0-test",
            canonicalClaim: "x => (x / x)",
            normalizedClaim: "x => 1",
            structuralVerification: ProofStructuralVerification.StructurallyVerified,
            evidence: new ProofEvidenceMetadata(
                ProofTrustStatus.RequiresCoreLean,
                artifactId: null,
                contentHash: null,
                toolchain: null,
                verificationCommand: null,
                compilerOutputDigest: null,
                axiomOutputDigest: null,
                boundaryResourceKey: "proof.core.lean.required"),
            documents: documents ?? new Dictionary<RicisProofDocumentFormat, string>
            {
                [RicisProofDocumentFormat.Json] = "{\"status\":\"REQUIRES_CORE_LEAN\"}",
            });
    }

    private sealed class TestProofClock(DateTimeOffset utcNow) : IProofClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration)
        {
            UtcNow = UtcNow.Add(duration);
        }
    }

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
