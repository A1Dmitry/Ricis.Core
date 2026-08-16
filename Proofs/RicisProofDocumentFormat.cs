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
    /// Emits a Lean-oriented documentation scaffold. It records the RICIS
    /// derivation in comments and explicitly does not claim that arbitrary C#
    /// expression trees have been checked by Lean.
    /// </summary>
    Lean,

    /// <summary>
    /// Emits a machine-readable JSON representation of the proof document.
    /// </summary>
    Json,
}
