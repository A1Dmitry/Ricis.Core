namespace Ricis.Core.Proofs;

/// <summary>
/// Selects the presentation template used to render a RICIS proof document.
/// The selected format changes only the textual representation; it never
/// changes the symbolic derivation, its expression tree, or its proof scope.
/// </summary>
public enum RicisProofDocumentFormat
{
    /// <summary>
    /// Emits a compact line-oriented diagnostic protocol.
    /// </summary>
    Log,

    /// <summary>
    /// Emits the full academic Markdown document with definitions, stated
    /// premises, proof scope, trace, result, and limitations.
    /// </summary>
    Academic,

    /// <summary>
    /// Emits a machine-readable JSON representation of the proof document.
    /// </summary>
    Json,

    /// <summary>
    /// Emits a LaTeX proof document generated from the same RICIS derivation.
    /// </summary>
    Latex,

    /// <summary>
    /// Requests a Lean document for a proof shape. Generic C# expression trees
    /// are rejected unless a supported structured Lean bridge is selected through
    /// <see cref="RicisLeanTemplate"/>.
    /// </summary>
    Lean,
}
