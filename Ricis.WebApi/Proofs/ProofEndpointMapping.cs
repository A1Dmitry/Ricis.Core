using Microsoft.AspNetCore.Http.HttpResults;
using Ricis.Core.Logging;
using Ricis.Core.Proofs;

namespace Ricis.WebApi.Proofs;

/// <summary>
/// Fixed v1 HTTP boundary for server-owned authoritative proof snapshots. All
/// paths are static; no browser-supplied route, host, code or Lean source is forwarded.
/// </summary>
internal static class ProofEndpointMapping
{
    private const string ApiVersion = "v1";

    internal static IEndpointRouteBuilder MapProofEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/proofs/v1/runs", CreateRunAsync)
            .WithName("CreateAuthoritativeProofRun")
            .WithTags("Proofs")
            .Produces<ProofRunResponse>(StatusCodes.Status200OK)
            .Produces<ProofApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ProofApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ProofApiErrorResponse>(StatusCodes.Status503ServiceUnavailable)
            .WithOpenApi();

        endpoints.MapGet("/api/proofs/v1/runs/{proofRunId:guid}", GetRunAsync)
            .WithName("GetAuthoritativeProofRun")
            .WithTags("Proofs")
            .Produces<ProofRunResponse>(StatusCodes.Status200OK)
            .Produces<ProofApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        endpoints.MapGet("/api/proofs/v1/runs/{proofRunId:guid}/documents/{format}", GetDocumentAsync)
            .WithName("GetAuthoritativeProofDocument")
            .WithTags("Proofs")
            .Produces<ProofDocumentResponse>(StatusCodes.Status200OK)
            .Produces<ProofApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ProofApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithOpenApi();

        endpoints.MapGet("/api/proofs/v1/capabilities", () => TypedResults.Ok(new ProofCapabilitiesResponse(
                ApiVersion,
                ["ExpressionEquivalence"],
                ["Academic", "Json", "Latex", "Log"],
                "proof.core.lean.genericUnsupported",
                IsDurableSnapshotStore: false)))
            .WithName("GetAuthoritativeProofCapabilities")
            .WithTags("Proofs")
            .Produces<ProofCapabilitiesResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<IResult> CreateRunAsync(
        CreateProofRunRequest? request,
        ProofRunApplicationService applicationService,
        CancellationToken cancellationToken)
    {
        if (request is null || !string.Equals(request.ApiVersion, ApiVersion, StringComparison.Ordinal))
        {
            return TypedResults.BadRequest(Error("PROOF_REQUEST_INVALID", "proof.core.request.invalid", retryable: false));
        }

        if (!TryParseFormats(request.RequestedFormats, out var formats))
        {
            return TypedResults.BadRequest(Error("PROOF_FORMAT_INVALID", "proof.core.format.invalid", retryable: false));
        }

        ProofRunCreateCommand command;
        try
        {
            command = new ProofRunCreateCommand(
                request.Claim ?? string.Empty,
                request.Expected ?? string.Empty,
                TimeSpan.FromMinutes(15),
                formats);
        }
        catch (ArgumentException)
        {
            return TypedResults.BadRequest(Error("PROOF_REQUEST_INVALID", "proof.core.request.invalid", retryable: false));
        }

        var outcome = await applicationService.CreateAsync(command, cancellationToken).ConfigureAwait(false);
        if (outcome is ProofRunCreationAccepted accepted)
        {
            return TypedResults.Ok(ToResponse(accepted.Snapshot));
        }

        var rejected = (ProofRunCreationRejected)outcome;
        return rejected.Code switch
        {
            "UNSUPPORTED_LEAN_SHAPE" or "PROOF_UNSUPPORTED" => TypedResults.UnprocessableEntity(
                Error(rejected.Code, rejected.MessageResourceKey, rejected.Retryable)),
            "CORE_PROOF_UNAVAILABLE" => TypedResults.Json(
                Error(rejected.Code, rejected.MessageResourceKey, rejected.Retryable),
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => TypedResults.BadRequest(Error(rejected.Code, rejected.MessageResourceKey, rejected.Retryable)),
        };
    }

    private static async Task<Results<Ok<ProofRunResponse>, NotFound<ProofApiErrorResponse>>> GetRunAsync(
        Guid proofRunId,
        IProofRunSnapshotStore snapshotStore,
        CancellationToken cancellationToken)
    {
        var lookup = await snapshotStore.FindAsync(proofRunId, cancellationToken).ConfigureAwait(false);
        return lookup.Kind == ProofSnapshotLookupKind.Found
            ? TypedResults.Ok(ToResponse(lookup.Snapshot!))
            : TypedResults.NotFound(Error("PROOF_RUN_NOT_FOUND", "proof.core.snapshot.notFound", retryable: false));
    }

    private static async Task<Results<Ok<ProofDocumentResponse>, NotFound<ProofApiErrorResponse>, UnprocessableEntity<ProofApiErrorResponse>>> GetDocumentAsync(
        Guid proofRunId,
        string format,
        IProofRunSnapshotStore snapshotStore,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RicisProofDocumentFormat>(format, ignoreCase: true, out var parsedFormat) || !Enum.IsDefined(parsedFormat))
        {
            return TypedResults.NotFound(Error("PROOF_FORMAT_NOT_FOUND", "proof.core.format.notFound", retryable: false));
        }

        if (parsedFormat == RicisProofDocumentFormat.Lean)
        {
            return TypedResults.UnprocessableEntity(
                Error("UNSUPPORTED_LEAN_SHAPE", "proof.core.lean.unsupportedShape", retryable: false));
        }

        var lookup = await snapshotStore.FindAsync(proofRunId, cancellationToken).ConfigureAwait(false);
        if (lookup.Kind != ProofSnapshotLookupKind.Found)
        {
            return TypedResults.NotFound(Error("PROOF_RUN_NOT_FOUND", "proof.core.snapshot.notFound", retryable: false));
        }

        var snapshot = lookup.Snapshot!;
        if (!snapshot.TryGetDocument(parsedFormat, out var content))
        {
            return TypedResults.NotFound(Error("PROOF_DOCUMENT_NOT_FOUND", "proof.core.document.notFound", retryable: false));
        }

        return TypedResults.Ok(new ProofDocumentResponse(
            ApiVersion,
            snapshot.ProofRunId,
            snapshot.CorrelationId,
            parsedFormat.ToString(),
            ContentTypeFor(parsedFormat),
            content,
            snapshot.DocumentHashes[parsedFormat],
            snapshot.Evidence.TrustStatus.ToString(),
            snapshot.Evidence.BoundaryResourceKey));
    }

    private static bool TryParseFormats(IReadOnlyList<string>? values, out IReadOnlyList<RicisProofDocumentFormat> formats)
    {
        formats = Array.Empty<RicisProofDocumentFormat>();
        if (values is null || values.Count == 0)
        {
            return false;
        }

        var parsed = new List<RicisProofDocumentFormat>();
        foreach (var value in values)
        {
            if (!Enum.TryParse<RicisProofDocumentFormat>(value, ignoreCase: true, out var format) ||
                !Enum.IsDefined(format) ||
                parsed.Contains(format))
            {
                return false;
            }

            parsed.Add(format);
        }

        formats = parsed.AsReadOnly();
        return true;
    }

    private static ProofRunResponse ToResponse(ProofRunSnapshot snapshot) => new(
        ApiVersion,
        snapshot.ProofRunId,
        snapshot.CorrelationId,
        snapshot.CreatedAtUtc,
        snapshot.ExpiresAtUtc,
        snapshot.CoreVersion,
        snapshot.CanonicalClaim,
        snapshot.NormalizedClaim,
        snapshot.StructuralVerification.ToString(),
        snapshot.Evidence.TrustStatus.ToString(),
        snapshot.Evidence.BoundaryResourceKey,
        snapshot.Trace.Select(entry => new ProofTraceEntryResponse(
            entry.Sequence,
            entry.TimestampUtc,
            entry.Severity.ToString(),
            entry.EventCode,
            entry.StageType,
            entry.Attributes,
            entry.BeforeExpression,
            entry.AfterExpression)).ToArray(),
        snapshot.DocumentHashes.Select(pair => new ProofDocumentDescriptorResponse(pair.Key.ToString(), pair.Value)).ToArray());

    private static ProofApiErrorResponse Error(string code, string resourceKey, bool retryable) =>
        new(ApiVersion, code, resourceKey, retryable, new Dictionary<string, string>(StringComparer.Ordinal));

    private static string ContentTypeFor(RicisProofDocumentFormat format) => format switch
    {
        RicisProofDocumentFormat.Json => "application/json",
        RicisProofDocumentFormat.Latex => "application/x-latex",
        _ => "text/plain; charset=utf-8",
    };
}

internal sealed record CreateProofRunRequest(
    string? ApiVersion,
    string? Claim,
    string? Expected,
    IReadOnlyList<string>? RequestedFormats);

internal sealed record ProofRunResponse(
    string ApiVersion,
    Guid ProofRunId,
    string CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string CoreVersion,
    string CanonicalClaim,
    string NormalizedClaim,
    string StructuralVerification,
    string TrustStatus,
    string EvidenceBoundaryResourceKey,
    IReadOnlyList<ProofTraceEntryResponse> Trace,
    IReadOnlyList<ProofDocumentDescriptorResponse> Documents);

internal sealed record ProofTraceEntryResponse(
    long Sequence,
    DateTimeOffset TimestampUtc,
    string Severity,
    string EventCode,
    string StageType,
    IReadOnlyDictionary<string, string> Attributes,
    string? BeforeExpression,
    string? AfterExpression);

internal sealed record ProofDocumentDescriptorResponse(string Format, string ContentHash);

internal sealed record ProofDocumentResponse(
    string ApiVersion,
    Guid ProofRunId,
    string CorrelationId,
    string Format,
    string ContentType,
    string Content,
    string ContentHash,
    string TrustStatus,
    string EvidenceBoundaryResourceKey);

internal sealed record ProofCapabilitiesResponse(
    string ApiVersion,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<string> Formats,
    string LeanBoundaryResourceKey,
    bool IsDurableSnapshotStore);

internal sealed record ProofApiErrorResponse(
    string ApiVersion,
    string Code,
    string MessageResourceKey,
    bool Retryable,
    IReadOnlyDictionary<string, string> SafeParameters);
