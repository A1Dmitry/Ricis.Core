using System.Linq.Expressions;
using Ricis.ConsoleApp;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;
using Ricis.Core.Proofs;

namespace Ricis.WebApi.Proofs;

/// <summary>
/// Bounded WebAPI adapter for the existing Core expression-equivalence proof
/// facility. It parses only the established unary double grammar, performs one
/// Core derivation and emits no Lean kernel claim for a generic expression.
/// </summary>
public sealed class ExpressionEquivalenceProofRunDeriver : IProofRunDeriver
{
    private readonly RicisProofDocumentProfile _profile;

    /// <summary>
    /// Initializes the adapter with externally supplied document metadata. The
    /// profile is a composition/configuration concern and is not hardcoded here.
    /// </summary>
    public ExpressionEquivalenceProofRunDeriver(RicisProofDocumentProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>
    /// Parses a claim/expected pair through the established bounded grammar and
    /// invokes the existing Core checked-document pipeline exactly once.
    /// </summary>
    public Task<ProofRunDerivationOutcome> DeriveAsync(
        ProofRunCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        Expression<Func<double, double>> claim;
        Expression<Func<double, double>> expected;
        try
        {
            var parser = new LambdaTextParser();
            claim = parser.Parse(command.CanonicalClaim);
            expected = parser.Parse(command.ExpectedClaim);
        }
        catch (LambdaParseException)
        {
            return Task.FromResult<ProofRunDerivationOutcome>(
                ProofRunDerivationOutcome.Rejected(
                    "PROOF_PARSE_FAILED",
                    "proof.core.input.parseFailed",
                    retryable: false));
        }

        if (command.RequestedFormats.Contains(RicisProofDocumentFormat.Lean))
        {
            return Task.FromResult<ProofRunDerivationOutcome>(
                ProofRunDerivationOutcome.Rejected(
                    "UNSUPPORTED_LEAN_SHAPE",
                    "proof.core.lean.unsupportedShape",
                    retryable: false));
        }

        try
        {
            var log = new RicisProofLog<RicisProofOrchestrationStage>();
            var artifacts = Array.Empty<Expression<Func<double, bool>>>().ProveDocumentsCheckedWithLog(
                Array.Empty<Expression<Func<double, bool>>>(),
                claim,
                expected,
                _profile,
                command.RequestedFormats,
                log);
            var structuralStatus = artifacts.Proof.IsVerified
                ? ProofStructuralVerification.StructurallyVerified
                : ProofStructuralVerification.StructurallyNotVerified;
            var evidence = new ProofEvidenceMetadata(
                ProofTrustStatus.RequiresCoreLean,
                artifactId: null,
                contentHash: null,
                toolchain: null,
                verificationCommand: null,
                compilerOutputDigest: null,
                axiomOutputDigest: null,
                boundaryResourceKey: "proof.core.lean.required");
            var material = new ProofRunDerivationMaterial(
                typeof(ExpressionEquivalenceProofRunDeriver).Assembly.GetName().Version?.ToString() ?? "unknown",
                artifacts.Proof.Derived.ToString(),
                structuralStatus,
                evidence,
                artifacts.Documents,
                log.Snapshot());
            return Task.FromResult<ProofRunDerivationOutcome>(ProofRunDerivationOutcome.Accepted(material));
        }
        catch (ArgumentException)
        {
            return Task.FromResult<ProofRunDerivationOutcome>(
                ProofRunDerivationOutcome.Rejected(
                    "PROOF_UNSUPPORTED",
                    "proof.core.unsupported",
                    retryable: false));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<ProofRunDerivationOutcome>(
                ProofRunDerivationOutcome.Rejected(
                    "CORE_PROOF_PROCESSING_FAILED",
                    "proof.core.processingFailed",
                    retryable: false));
        }
    }
}
