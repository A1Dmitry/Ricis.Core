using System.Numerics;
using Ricis.Core.Logging;

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
            throw new ArgumentException("Должен быть передан хотя бы один proof-document export.", nameof(documents));
        }

        if (documents.Any(entry => !Enum.IsDefined(entry.Key) || string.IsNullOrWhiteSpace(entry.Value)))
        {
            throw new ArgumentException("Каждый proof-document export должен иметь известный формат и непустое содержимое.", nameof(documents));
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
            : throw new KeyNotFoundException($"Для формата {format} не был сформирован proof-document export.");
}
