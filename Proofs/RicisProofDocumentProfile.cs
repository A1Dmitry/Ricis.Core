namespace Ricis.Core.Proofs;

/// <summary>
/// Declares the epistemic scope assigned to a document produced from a RICIS
/// derivation. The scope is descriptive and never upgrades a finite derivation
/// into a proof of external assumptions.
/// </summary>
public enum RicisProofScope
{
    /// <summary>
    /// The document certifies only a finite derivation from the expression trees
    /// explicitly supplied to the proof API.
    /// </summary>
    FiniteDerivation,

    /// <summary>
    /// The document states a conditional theorem whose conclusion is valid only
    /// under its enumerated formal premises.
    /// </summary>
    ConditionalTheorem,
}

/// <summary>
/// Supplies academic metadata and explicit proof boundaries for a document
/// generated from a RICIS symbolic derivation.
/// </summary>
public sealed class RicisProofDocumentProfile
{
    /// <summary>
    /// Initializes a document profile.
    /// </summary>
    /// <param name="title">The title printed at the beginning of the document.</param>
    /// <param name="scope">The declared epistemic scope of the derived conclusion.</param>
    /// <param name="abstract">A concise statement of the document's purpose.</param>
    /// <param name="theorem">The theorem or finite claim stated in the document.</param>
    /// <param name="definitions">Definitions required to read the formal premises.</param>
    /// <param name="axioms">Named axioms, lemmas, or external premises used by the stated theorem.</param>
    /// <param name="limitations">Statements deliberately excluded from the document's proof status.</param>
    /// <exception cref="ArgumentException">Thrown when a required textual field is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="scope"/> is not a declared proof scope.</exception>
    public RicisProofDocumentProfile(
        string title,
        RicisProofScope scope,
        string @abstract,
        string theorem,
        IEnumerable<string> definitions = null,
        IEnumerable<string> axioms = null,
        IEnumerable<string> limitations = null)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new ArgumentOutOfRangeException(nameof(scope), scope, "Неизвестный доказательный статус документа.");
        }

        Title = RequireText(title, nameof(title));
        Scope = scope;
        Abstract = RequireText(@abstract, nameof(@abstract));
        Theorem = RequireText(theorem, nameof(theorem));
        Definitions = CopyLines(definitions, nameof(definitions));
        Axioms = CopyLines(axioms, nameof(axioms));
        Limitations = CopyLines(limitations, nameof(limitations));
    }

    /// <summary>Gets the document title.</summary>
    public string Title { get; }

    /// <summary>Gets the declared proof scope.</summary>
    public RicisProofScope Scope { get; }

    /// <summary>Gets the concise purpose statement.</summary>
    public string Abstract { get; }

    /// <summary>Gets the theorem or finite claim.</summary>
    public string Theorem { get; }

    /// <summary>Gets immutable copies of the stated definitions.</summary>
    public IReadOnlyList<string> Definitions { get; }

    /// <summary>Gets immutable copies of the named axioms or external premises.</summary>
    public IReadOnlyList<string> Axioms { get; }

    /// <summary>Gets immutable copies of the stated proof limitations.</summary>
    public IReadOnlyList<string> Limitations { get; }

    private static IReadOnlyList<string> CopyLines(IEnumerable<string> values, string parameterName)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        var copied = values.Select(value => RequireText(value, parameterName)).ToArray();
        return Array.AsReadOnly(copied);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Текстовый элемент proof-документа не может быть пустым.", parameterName);
        }

        return value;
    }
}
