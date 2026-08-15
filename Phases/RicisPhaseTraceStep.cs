using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Core.Phases;

/// <summary>
/// Represents one deterministic transformation attempt of the normative RICIS
/// pipeline. The step stores the complete before and after expression trees,
/// the phase name, and the family of rules that governed the attempt.
/// </summary>
public sealed class RicisPhaseTraceStep
{
    /// <summary>
    /// Initializes one pipeline trace step.
    /// </summary>
    public RicisPhaseTraceStep(
        string phaseName,
        string ruleFamily,
        Expression before,
        Expression after,
        bool wasSkipped)
    {
        PhaseName = phaseName ?? throw new ArgumentNullException(nameof(phaseName));
        RuleFamily = ruleFamily ?? throw new ArgumentNullException(nameof(ruleFamily));
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after ?? throw new ArgumentNullException(nameof(after));
        WasSkipped = wasSkipped;
    }

    /// <summary>
    /// Gets the ordered normative phase that attempted a transformation.
    /// </summary>
    public string PhaseName { get; }

    /// <summary>
    /// Gets the RICIS axiom family or structural rule family governing the phase.
    /// </summary>
    public string RuleFamily { get; }

    /// <summary>
    /// Gets the expression tree before this phase.
    /// </summary>
    public Expression Before { get; }

    /// <summary>
    /// Gets the expression tree after this phase.
    /// </summary>
    public Expression After { get; }

    /// <summary>
    /// Gets whether the phase was deliberately skipped because its certified
    /// double-domain precondition was unavailable for the current expression.
    /// </summary>
    public bool WasSkipped { get; }

    /// <summary>
    /// Gets whether the phase changed the structural RICIS expression.
    /// A deliberately skipped phase is never reported as a transformation.
    /// </summary>
    public bool Changed => !WasSkipped && !Before.AreEqual(After);
}
