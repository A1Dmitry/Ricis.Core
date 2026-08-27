#nullable enable

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using Ricis.Core.Logging;

namespace Ricis.Core.Proofs;

/// <summary>
/// Closed trust taxonomy carried by an authoritative Core proof snapshot.
/// A value describes supplied evidence only; it never runs or simulates Lean.
/// </summary>
public enum ProofTrustStatus
{
    /// <summary>Current reproducible Lean kernel evidence is attached.</summary>
    LeanVerified = 0,
    /// <summary>A named external trusted axiom is attached; it is not a default resolution state.</summary>
    TrustedAxiom = 1,
    /// <summary>A Core derivation exists but current Lean kernel evidence is required.</summary>
    RequiresCoreLean = 2,
    /// <summary>Only static validation has passed.</summary>
    StaticCheckPassed = 3,
    /// <summary>The snapshot represents a research assumption.</summary>
    Hypothesis = 4,
    /// <summary>The claim or proof shape has been rejected.</summary>
    Rejected = 5,
}

/// <summary>
/// Structural comparison state emitted by the authoritative Core derivation.
/// It is intentionally independent from <see cref="ProofTrustStatus"/>.
/// </summary>
public enum ProofStructuralVerification
{
    /// <summary>Core structurally matched the bounded expected expression.</summary>
    StructurallyVerified = 0,
    /// <summary>Core completed a derivation but it did not match the expected structure.</summary>
    StructurallyNotVerified = 1,
    /// <summary>Core rejected the input as invalid.</summary>
    Rejected = 2,
    /// <summary>The requested bounded proof scenario is unsupported.</summary>
    Unsupported = 3,
}

/// <summary>
/// Result category for a proof snapshot lookup. Expiry is operational and never
/// triggers another proof derivation.
/// </summary>
public enum ProofSnapshotLookupKind
{
    /// <summary>An active immutable snapshot was found.</summary>
    Found = 0,
    /// <summary>No snapshot exists for the requested run identifier.</summary>
    Missing = 1,
    /// <summary>A snapshot exists but may no longer be read or exported.</summary>
    Expired = 2,
}

/// <summary>
/// Supplies the current UTC time to a proof snapshot store. The clock is injected
/// so expiry handling remains deterministic and testable.
/// </summary>
public interface IProofClock
{
    /// <summary>Gets the current UTC time used for snapshot expiry evaluation.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Production UTC clock used by composition roots that do not inject a specialised clock.</summary>
public sealed class SystemProofClock : IProofClock
{
    /// <summary>Gets the current system UTC time.</summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Evidence metadata attached to a canonical Core derivation. Lean-kernel status
/// is accepted only when every reproducibility field is present.
/// </summary>
public sealed class ProofEvidenceMetadata
{
    /// <summary>Initializes evidence metadata and validates required kernel fields for <see cref="ProofTrustStatus.LeanVerified"/>.</summary>
    public ProofEvidenceMetadata(
        ProofTrustStatus trustStatus,
        string? artifactId,
        string? contentHash,
        string? toolchain,
        string? verificationCommand,
        string? compilerOutputDigest,
        string? axiomOutputDigest,
        string boundaryResourceKey)
    {
        if (!Enum.IsDefined(trustStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(trustStatus), trustStatus, "Unknown proof trust status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(boundaryResourceKey);

        if (trustStatus == ProofTrustStatus.LeanVerified)
        {
            RequireEvidenceValue(artifactId, nameof(artifactId));
            RequireEvidenceValue(contentHash, nameof(contentHash));
            RequireEvidenceValue(toolchain, nameof(toolchain));
            RequireEvidenceValue(verificationCommand, nameof(verificationCommand));
            RequireEvidenceValue(compilerOutputDigest, nameof(compilerOutputDigest));
            RequireEvidenceValue(axiomOutputDigest, nameof(axiomOutputDigest));
        }

        TrustStatus = trustStatus;
        ArtifactId = NormalizeOptional(artifactId);
        ContentHash = NormalizeOptional(contentHash);
        Toolchain = NormalizeOptional(toolchain);
        VerificationCommand = NormalizeOptional(verificationCommand);
        CompilerOutputDigest = NormalizeOptional(compilerOutputDigest);
        AxiomOutputDigest = NormalizeOptional(axiomOutputDigest);
        BoundaryResourceKey = boundaryResourceKey.Trim();
    }

    /// <summary>Gets the closed trust status represented by the supplied evidence.</summary>
    public ProofTrustStatus TrustStatus { get; }

    /// <summary>Gets the optional immutable proof artifact identifier.</summary>
    public string? ArtifactId { get; }

    /// <summary>Gets the optional artifact content digest.</summary>
    public string? ContentHash { get; }

    /// <summary>Gets the optional Lean/Core toolchain identifier.</summary>
    public string? Toolchain { get; }

    /// <summary>Gets the optional reproducible verification command reference.</summary>
    public string? VerificationCommand { get; }

    /// <summary>Gets the optional digest of compiler output.</summary>
    public string? CompilerOutputDigest { get; }

    /// <summary>Gets the optional digest of axiom-output evidence.</summary>
    public string? AxiomOutputDigest { get; }

    /// <summary>Gets the externalised resource key that describes the evidence boundary.</summary>
    public string BoundaryResourceKey { get; }

    private static void RequireEvidenceValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "LEAN_VERIFIED requires complete reproducible kernel evidence.",
                parameterName);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Computes a stable SHA-256 digest for an immutable proof export.</summary>
public static class ProofContentHash
{
    /// <summary>Computes a lowercase SHA-256 digest for non-null document content.</summary>
    public static string Compute(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }
}

/// <summary>
/// Immutable, server-owned projection of exactly one Core proof derivation. It
/// deliberately stores rendered document text, not arbitrary expression trees.
/// </summary>
public sealed class ProofRunSnapshot
{
    private readonly IReadOnlyDictionary<RicisProofDocumentFormat, string> _documents;
    private readonly IReadOnlyDictionary<RicisProofDocumentFormat, string> _documentHashes;
    private readonly IReadOnlyList<RicisLogEntry> _trace;

    /// <summary>Initializes an immutable snapshot for exactly one authoritative Core derivation.</summary>
    public ProofRunSnapshot(
        Guid proofRunId,
        string correlationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string coreVersion,
        string canonicalClaim,
        string normalizedClaim,
        ProofStructuralVerification structuralVerification,
        ProofEvidenceMetadata evidence,
        IReadOnlyDictionary<RicisProofDocumentFormat, string> documents,
        IReadOnlyList<RicisLogEntry>? trace = null)
    {
        if (proofRunId == Guid.Empty)
        {
            throw new ArgumentException("Proof run identifier is required.", nameof(proofRunId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(coreVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalClaim);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedClaim);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(documents);
        var traceCopy = trace is null ? Array.Empty<RicisLogEntry>() : trace.ToArray();
        if (traceCopy.Any(entry => entry is null))
        {
            throw new ArgumentException("Proof trace cannot contain null entries.", nameof(trace));
        }

        if (!Enum.IsDefined(structuralVerification))
        {
            throw new ArgumentOutOfRangeException(nameof(structuralVerification), structuralVerification, "Unknown structural verification state.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("Proof snapshot expiry must be after its creation time.", nameof(expiresAtUtc));
        }

        var documentCopy = new Dictionary<RicisProofDocumentFormat, string>();
        var hashCopy = new Dictionary<RicisProofDocumentFormat, string>();
        foreach (var (format, content) in documents)
        {
            if (!Enum.IsDefined(format))
            {
                throw new ArgumentOutOfRangeException(nameof(documents), format, "Unknown proof document format.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(content);
            var immutableContent = content;
            documentCopy.Add(format, immutableContent);
            hashCopy.Add(format, ProofContentHash.Compute(immutableContent));
        }

        if (documentCopy.Count == 0)
        {
            throw new ArgumentException("At least one canonical proof document is required.", nameof(documents));
        }

        ProofRunId = proofRunId;
        CorrelationId = correlationId.Trim();
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CoreVersion = coreVersion.Trim();
        CanonicalClaim = canonicalClaim.Trim();
        NormalizedClaim = normalizedClaim.Trim();
        StructuralVerification = structuralVerification;
        Evidence = evidence;
        _documents = new ReadOnlyDictionary<RicisProofDocumentFormat, string>(documentCopy);
        _documentHashes = new ReadOnlyDictionary<RicisProofDocumentFormat, string>(hashCopy);
        _trace = Array.AsReadOnly(traceCopy);
    }

    /// <summary>Gets the server-owned immutable proof run identifier.</summary>
    public Guid ProofRunId { get; }

    /// <summary>Gets the safe request correlation identifier.</summary>
    public string CorrelationId { get; }

    /// <summary>Gets the snapshot creation time in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets the expiration time after which no stored result is returned.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets the Core version that produced the derivation.</summary>
    public string CoreVersion { get; }

    /// <summary>Gets the bounded canonical claim accepted by Core.</summary>
    public string CanonicalClaim { get; }

    /// <summary>Gets the Core-normalized claim representation.</summary>
    public string NormalizedClaim { get; }

    /// <summary>Gets the Core structural verification state.</summary>
    public ProofStructuralVerification StructuralVerification { get; }

    /// <summary>Gets the immutable evidence boundary associated with this run.</summary>
    public ProofEvidenceMetadata Evidence { get; }

    /// <summary>Gets immutable stored document projections indexed by approved format.</summary>
    public IReadOnlyDictionary<RicisProofDocumentFormat, string> Documents => _documents;

    /// <summary>Gets immutable SHA-256 document hashes indexed by approved format.</summary>
    public IReadOnlyDictionary<RicisProofDocumentFormat, string> DocumentHashes => _documentHashes;

    /// <summary>Gets the ordered immutable typed Core journal from the same canonical derivation.</summary>
    public IReadOnlyList<RicisLogEntry> Trace => _trace;

    /// <summary>Attempts to obtain a stored approved-format document without recomputing a derivation.</summary>
    public bool TryGetDocument(RicisProofDocumentFormat format, out string content) =>
        _documents.TryGetValue(format, out content!);
}

/// <summary>Typed immutable result of a proof snapshot store lookup.</summary>
public sealed record ProofRunSnapshotLookup(ProofSnapshotLookupKind Kind, ProofRunSnapshot? Snapshot)
{
    /// <summary>Creates a lookup result that exposes one active immutable snapshot.</summary>
    public static ProofRunSnapshotLookup Found(ProofRunSnapshot snapshot) =>
        new(ProofSnapshotLookupKind.Found, snapshot ?? throw new ArgumentNullException(nameof(snapshot)));

    /// <summary>Creates a lookup result for an unknown run identifier.</summary>
    public static ProofRunSnapshotLookup Missing() => new(ProofSnapshotLookupKind.Missing, null);

    /// <summary>Creates a lookup result for an expired run without re-derivation.</summary>
    public static ProofRunSnapshotLookup Expired() => new(ProofSnapshotLookupKind.Expired, null);
}

/// <summary>
/// Persists immutable canonical proof snapshots. Route handlers depend on this
/// port rather than a global mutable dictionary.
/// </summary>
public interface IProofRunSnapshotStore
{
    /// <summary>Saves one new immutable snapshot and rejects overwrite attempts.</summary>
    Task SaveAsync(ProofRunSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Finds an active snapshot or returns a typed missing/expired result without re-derivation.</summary>
    Task<ProofRunSnapshotLookup> FindAsync(Guid proofRunId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Non-durable local snapshot store intended for deterministic tests and local
/// development. Production multi-instance deployment requires a durable adapter.
/// </summary>
public sealed class InMemoryProofRunSnapshotStore : IProofRunSnapshotStore
{
    private readonly IProofClock _clock;
    private readonly Dictionary<Guid, ProofRunSnapshot> _snapshots = [];
    private readonly object _gate = new();

    /// <summary>Initializes the local non-durable store with an injected expiry clock.</summary>
    public InMemoryProofRunSnapshotStore(IProofClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Saves an immutable snapshot once and rejects any duplicate run identifier.</summary>
    public Task SaveAsync(ProofRunSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_snapshots.TryAdd(snapshot.ProofRunId, snapshot))
            {
                throw new InvalidOperationException("Canonical proof snapshot cannot be overwritten.");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Returns an active snapshot or a typed missing/expired outcome without invoking Core.</summary>
    public Task<ProofRunSnapshotLookup> FindAsync(Guid proofRunId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (proofRunId == Guid.Empty)
        {
            return Task.FromResult(ProofRunSnapshotLookup.Missing());
        }

        lock (_gate)
        {
            if (!_snapshots.TryGetValue(proofRunId, out var snapshot))
            {
                return Task.FromResult(ProofRunSnapshotLookup.Missing());
            }

            return Task.FromResult(snapshot.ExpiresAtUtc <= _clock.UtcNow
                ? ProofRunSnapshotLookup.Expired()
                : ProofRunSnapshotLookup.Found(snapshot));
        }
    }
}


/// <summary>
/// Bounded command for creating one authoritative proof run. The command carries
/// expression data only; it cannot carry executable delegates, arbitrary C# or Lean source.
/// </summary>
public sealed class ProofRunCreateCommand
{
    /// <summary>Initializes a bounded proof-run command.</summary>
    public ProofRunCreateCommand(
        string canonicalClaim,
        string expectedClaim,
        TimeSpan snapshotLifetime,
        IReadOnlyList<RicisProofDocumentFormat> requestedFormats)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalClaim);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedClaim);
        ArgumentNullException.ThrowIfNull(requestedFormats);
        if (requestedFormats.Count == 0 || requestedFormats.Distinct().Count() != requestedFormats.Count ||
            requestedFormats.Any(format => !Enum.IsDefined(format)))
        {
            throw new ArgumentException("Requested proof document formats must be a non-empty distinct declared set.", nameof(requestedFormats));
        }

        if (snapshotLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshotLifetime), "Snapshot lifetime must be positive.");
        }

        CanonicalClaim = canonicalClaim.Trim();
        ExpectedClaim = expectedClaim.Trim();
        SnapshotLifetime = snapshotLifetime;
        RequestedFormats = Array.AsReadOnly(requestedFormats.ToArray());
    }

    /// <summary>Gets the restricted claim text validated by the caller's Core grammar boundary.</summary>
    public string CanonicalClaim { get; }

    /// <summary>Gets the restricted expected structural target text.</summary>
    public string ExpectedClaim { get; }

    /// <summary>Gets the requested positive retention duration for the resulting snapshot.</summary>
    public TimeSpan SnapshotLifetime { get; }

    /// <summary>Gets immutable distinct allowlisted document formats requested for the one derivation.</summary>
    public IReadOnlyList<RicisProofDocumentFormat> RequestedFormats { get; }
}

/// <summary>
/// Core-produced material from exactly one successful canonical derivation. The
/// application service copies it into an immutable server-owned snapshot.
/// </summary>
public sealed class ProofRunDerivationMaterial
{
    /// <summary>Initializes one successful authoritative derivation material projection.</summary>
    public ProofRunDerivationMaterial(
        string coreVersion,
        string normalizedClaim,
        ProofStructuralVerification structuralVerification,
        ProofEvidenceMetadata evidence,
        IReadOnlyDictionary<RicisProofDocumentFormat, string> documents,
        IReadOnlyList<RicisLogEntry>? trace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coreVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedClaim);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(documents);
        if (!Enum.IsDefined(structuralVerification))
        {
            throw new ArgumentOutOfRangeException(nameof(structuralVerification), structuralVerification, "Unknown structural verification state.");
        }

        CoreVersion = coreVersion.Trim();
        NormalizedClaim = normalizedClaim.Trim();
        StructuralVerification = structuralVerification;
        Evidence = evidence;
        Documents = new ReadOnlyDictionary<RicisProofDocumentFormat, string>(
            new Dictionary<RicisProofDocumentFormat, string>(documents));
        Trace = Array.AsReadOnly((trace ?? Array.Empty<RicisLogEntry>()).ToArray());
    }

    /// <summary>Gets the exact Core build/version that produced the material.</summary>
    public string CoreVersion { get; }

    /// <summary>Gets the authoritative normalized claim representation.</summary>
    public string NormalizedClaim { get; }

    /// <summary>Gets the structural verification state reported by Core.</summary>
    public ProofStructuralVerification StructuralVerification { get; }

    /// <summary>Gets the immutable trust/evidence metadata reported by Core.</summary>
    public ProofEvidenceMetadata Evidence { get; }

    /// <summary>Gets the Core-rendered document projections from the same derivation.</summary>
    public IReadOnlyDictionary<RicisProofDocumentFormat, string> Documents { get; }

    /// <summary>Gets the ordered immutable Core journal from the same derivation.</summary>
    public IReadOnlyList<RicisLogEntry> Trace { get; }
}

/// <summary>
/// Discriminated result of the one permitted canonical Core derivation attempt.
/// A rejection has no derivation material and therefore cannot create a snapshot.
/// </summary>
public abstract class ProofRunDerivationOutcome
{
    /// <summary>Initializes a derived typed outcome.</summary>
    protected ProofRunDerivationOutcome()
    {
    }

    /// <summary>Creates a successful one-derivation outcome.</summary>
    public static ProofRunDerivationAccepted Accepted(ProofRunDerivationMaterial material) =>
        new(material);

    /// <summary>Creates a typed controlled derivation rejection.</summary>
    public static ProofRunDerivationRejected Rejected(string code, string messageResourceKey, bool retryable) =>
        new(code, messageResourceKey, retryable);
}

/// <summary>Successful canonical derivation material outcome.</summary>
public sealed class ProofRunDerivationAccepted : ProofRunDerivationOutcome
{
    /// <summary>Initializes a successful outcome.</summary>
    public ProofRunDerivationAccepted(ProofRunDerivationMaterial material)
    {
        Material = material ?? throw new ArgumentNullException(nameof(material));
    }

    /// <summary>Gets the material produced by one authoritative Core derivation.</summary>
    public ProofRunDerivationMaterial Material { get; }
}

/// <summary>Safe controlled derivation rejection without proof material.</summary>
public sealed class ProofRunDerivationRejected : ProofRunDerivationOutcome
{
    /// <summary>Initializes a controlled rejection.</summary>
    public ProofRunDerivationRejected(string code, string messageResourceKey, bool retryable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageResourceKey);
        Code = code.Trim();
        MessageResourceKey = messageResourceKey.Trim();
        Retryable = retryable;
    }

    /// <summary>Gets the stable machine-readable failure code.</summary>
    public string Code { get; }

    /// <summary>Gets the externalised localisation resource key for the safe failure message.</summary>
    public string MessageResourceKey { get; }

    /// <summary>Gets whether retry is operationally appropriate.</summary>
    public bool Retryable { get; }
}

/// <summary>
/// Invokes the authoritative Core derivation exactly once for a bounded command.
/// Implementations must not delegate to a TypeScript fallback engine.
/// </summary>
public interface IProofRunDeriver
{
    /// <summary>Attempts one authoritative Core derivation for the supplied bounded command.</summary>
    Task<ProofRunDerivationOutcome> DeriveAsync(
        ProofRunCreateCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates server-owned opaque identifiers for proof runs and correlations.</summary>
public interface IProofRunIdFactory
{
    /// <summary>Creates one new server-owned immutable proof run identifier.</summary>
    Guid CreateProofRunId();

    /// <summary>Creates one safe correlation identifier.</summary>
    string CreateCorrelationId();
}

/// <summary>Default cryptographically random identifier factory for production composition roots.</summary>
public sealed class GuidProofRunIdFactory : IProofRunIdFactory
{
    /// <summary>Creates a new GUID proof-run identifier.</summary>
    public Guid CreateProofRunId() => Guid.NewGuid();

    /// <summary>Creates a URL-safe opaque correlation identifier.</summary>
    public string CreateCorrelationId() => Guid.NewGuid().ToString("N");
}

/// <summary>
/// Application service that turns exactly one accepted Core derivation into one
/// immutable snapshot. It has no knowledge of HTTP, UI, fallback engines or Lean execution.
/// </summary>
public sealed class ProofRunApplicationService
{
    private readonly IProofRunDeriver _deriver;
    private readonly IProofRunSnapshotStore _snapshotStore;
    private readonly IProofRunIdFactory _idFactory;
    private readonly IProofClock _clock;

    /// <summary>Initializes the service with explicit derivation, storage, identity and clock dependencies.</summary>
    public ProofRunApplicationService(
        IProofRunDeriver deriver,
        IProofRunSnapshotStore snapshotStore,
        IProofRunIdFactory idFactory,
        IProofClock clock)
    {
        _deriver = deriver ?? throw new ArgumentNullException(nameof(deriver));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _idFactory = idFactory ?? throw new ArgumentNullException(nameof(idFactory));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Attempts one derivation. An accepted material result is stored once as an
    /// immutable snapshot; a rejection returns without store access.
    /// </summary>
    public async Task<ProofRunCreationOutcome> CreateAsync(
        ProofRunCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var derivation = await _deriver.DeriveAsync(command, cancellationToken).ConfigureAwait(false);
        if (derivation is ProofRunDerivationRejected rejected)
        {
            return new ProofRunCreationRejected(rejected.Code, rejected.MessageResourceKey, rejected.Retryable);
        }

        if (derivation is not ProofRunDerivationAccepted accepted)
        {
            throw new InvalidOperationException("Proof run deriver returned an unknown outcome.");
        }

        var createdAtUtc = _clock.UtcNow;
        var snapshot = new ProofRunSnapshot(
            _idFactory.CreateProofRunId(),
            _idFactory.CreateCorrelationId(),
            createdAtUtc,
            createdAtUtc.Add(command.SnapshotLifetime),
            accepted.Material.CoreVersion,
            command.CanonicalClaim,
            accepted.Material.NormalizedClaim,
            accepted.Material.StructuralVerification,
            accepted.Material.Evidence,
            accepted.Material.Documents,
            accepted.Material.Trace);
        await _snapshotStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return new ProofRunCreationAccepted(snapshot);
    }
}

/// <summary>Discriminated result of an application-service create-run request.</summary>
public abstract class ProofRunCreationOutcome
{
    /// <summary>Initializes a derived typed creation outcome.</summary>
    protected ProofRunCreationOutcome()
    {
    }
}

/// <summary>Accepted create-run outcome containing exactly one immutable snapshot.</summary>
public sealed class ProofRunCreationAccepted : ProofRunCreationOutcome
{
    /// <summary>Initializes an accepted create-run outcome.</summary>
    public ProofRunCreationAccepted(ProofRunSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    /// <summary>Gets the immutable snapshot created from one canonical Core derivation.</summary>
    public ProofRunSnapshot Snapshot { get; }
}

/// <summary>Rejected create-run outcome containing safe resource-key diagnostics only.</summary>
public sealed class ProofRunCreationRejected : ProofRunCreationOutcome
{
    /// <summary>Initializes a rejected create-run outcome.</summary>
    public ProofRunCreationRejected(string code, string messageResourceKey, bool retryable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageResourceKey);
        Code = code.Trim();
        MessageResourceKey = messageResourceKey.Trim();
        Retryable = retryable;
    }

    /// <summary>Gets the stable machine-readable rejection code.</summary>
    public string Code { get; }

    /// <summary>Gets the externalised localisation resource key for the rejection.</summary>
    public string MessageResourceKey { get; }

    /// <summary>Gets whether retry is operationally appropriate.</summary>
    public bool Retryable { get; }
}
