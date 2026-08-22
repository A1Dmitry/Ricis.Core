namespace Ricis.Core.Proofs;

using Ricis.Core.Resources;

/// <summary>
/// Represents a named normative axiom or derived axiom step that is explicitly
/// declared in a RICIS proof document before the expression-tree derivation.
/// </summary>
public sealed class RicisProofAxiomStep
{
    /// <summary>
    /// Initializes a named normative proof step.
    /// </summary>
    /// <param name="ruleId">The stable normative identifier, such as <c>ID-01</c>.</param>
    /// <param name="title">The concise name of the applied rule.</param>
    /// <param name="statement">The exact formal consequence supplied by the rule.</param>
    /// <exception cref="ArgumentException">Thrown when any textual component is null, empty, or whitespace.</exception>
    public RicisProofAxiomStep(string ruleId, string title, string statement)
    {
        RuleId = RequireText(ruleId, nameof(ruleId));
        Title = RequireText(title, nameof(title));
        Statement = RequireText(statement, nameof(statement));
    }

    /// <summary>Gets the stable normative identifier.</summary>
    public string RuleId { get; }

    /// <summary>Gets the concise title of the rule.</summary>
    public string Title { get; }

    /// <summary>Gets the exact formal consequence of the rule.</summary>
    public string Statement { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.e444ee8cd714"), parameterName);
        }

        return value;
    }
}
