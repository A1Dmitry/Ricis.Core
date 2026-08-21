using Ricis.Core.Proofs;

namespace Ricis.WebApi.Proofs;

/// <summary>
/// Builds request-independent proof document metadata from external WebAPI
/// configuration. It deliberately owns no proof execution or HTTP route logic.
/// </summary>
internal static class ProofEndpointComposition
{
    internal static RicisProofDocumentProfile CreateExpressionEquivalenceProfile(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetRequiredSection("ProofTransport:ExpressionEquivalence");
        var title = section.GetValue<string>("Title")
            ?? throw new InvalidOperationException("ProofTransport ExpressionEquivalence title is required.");
        var @abstract = section.GetValue<string>("Abstract")
            ?? throw new InvalidOperationException("ProofTransport ExpressionEquivalence abstract is required.");
        var theorem = section.GetValue<string>("Theorem")
            ?? throw new InvalidOperationException("ProofTransport ExpressionEquivalence theorem is required.");
        var limitations = section.GetSection("Limitations").Get<string[]>()
            ?? Array.Empty<string>();

        return new RicisProofDocumentProfile(
            title,
            RicisProofScope.FiniteDerivation,
            @abstract,
            theorem,
            limitations: limitations);
    }
}
