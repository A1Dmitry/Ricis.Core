using System.Numerics;
using Ricis.Core.Logging;
using Ricis.Core.Resources;

namespace Ricis.Core.Proofs;

/// <summary>
/// Collects the result of one checked RICIS proof run, its immutable typed audit
/// journal, and textual exports rendered from that same derivation.
/// </summary>
/// <typeparam name="T">The scalar type of the deferred claim.</typeparam>
public sealed class RicisCheckedProofArtifacts<T>
    where T : INumber<T>
{
    /// <summary>
    /// Creates a proof-artifact collection from one checked result, one ordered
    /// trace snapshot, and non-duplicated rendered documents.
    /// </summary>
    public RicisCheckedProofArtifacts(
        RicisCheckedProofResult<T> proof,
        IReadOnlyList<RicisLogEntry> trace,
        IReadOnlyDictionary<RicisProofDocumentFormat, string> documents)
    {
        Proof = proof ?? throw new ArgumentNullException(nameof(proof));
        Trace = trace?.ToArray() ?? throw new ArgumentNullException(nameof(trace));
        if (documents is null || documents.Count == 0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.9b3da1634db4"), nameof(documents));
        }

        if (documents.Any(entry => !Enum.IsDefined(entry.Key) || string.IsNullOrWhiteSpace(entry.Value)))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.2cd684706ec6"), nameof(documents));
        }

        Documents = new Dictionary<RicisProofDocumentFormat, string>(documents);
    }

    /// <summary>Gets the checked symbolic result and real verification lambda.</summary>
    public RicisCheckedProofResult<T> Proof { get; }

    /// <summary>Gets the sequence-ordered immutable typed trace from the one proof run.</summary>
    public IReadOnlyList<RicisLogEntry> Trace { get; }

    /// <summary>Gets documents rendered from the same checked derivation, indexed by presentation format.</summary>
    public IReadOnlyDictionary<RicisProofDocumentFormat, string> Documents { get; }

    /// <summary>Returns a rendered document or throws when the requested export was not generated.</summary>
    public string GetDocument(RicisProofDocumentFormat format) =>
        Documents.TryGetValue(format, out var document)
            ? document
            : throw new KeyNotFoundException(RicisLegacyTextResources.Format("report.legacy.8d18c1bd4a2c", ("format", format)));
}
