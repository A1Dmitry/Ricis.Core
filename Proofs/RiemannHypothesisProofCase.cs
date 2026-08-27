using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Extensions;

namespace Ricis.Core.Proofs;

/// <summary>
/// Orchestrates the Riemann critical-line proof case without placing analytic
/// zeta-function assumptions inside the generic RICIS proof engine.
/// </summary>
/// <remarks>
/// This case proves the conditional algebraic consequence of a reflected pair:
/// <c>sigma+mirrorSigma=1</c> and <c>sigma-mirrorSigma=0</c> imply
/// <c>sigma=1/2</c>. The analytic obligations remain explicit and unresolved
/// until a typed complex zeta model supplies them.
/// </remarks>
public sealed class RiemannHypothesisProofCase : RicisProofCase
{
    private static readonly string[] MissingAnalyticObligations =
    [
        "Complex scalar model with Re, Im and conjugation",
        "Zeta(s) or completed Xi(s) symbolic definition",
        "NontrivialZero(s) predicate with pole and trivial-zero exclusions",
        "Analytic continuation over the critical strip",
        "Functional equation transferring zero membership from s to 1-s",
        "Universal bridge from every nontrivial zero to the reflected pair"
    ];

    private readonly IReadOnlyList<Expression<Func<double, double, bool>>> constraints;
    private readonly Expression<Func<double, double, bool>> claim;

    /// <summary>
    /// Initializes a conditional Riemann proof case.
    /// </summary>
    /// <param name="constraints">Formal domain restrictions for the reflected pair.</param>
    /// <param name="claim">The coordinate claim, normally <c>sigma=1/2</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public RiemannHypothesisProofCase(
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        this.constraints = constraints.ToArray();
        this.claim = claim;
    }

    /// <inheritdoc />
    public override string Name => "Riemann Hypothesis — conditional critical-line case";

    /// <inheritdoc />
    public override IReadOnlyList<string> UnresolvedObligations => MissingAnalyticObligations;

    /// <summary>
    /// Gets the single derived predicate produced by the last <see cref="RicisProofCase.Run"/> call.
    /// </summary>
    public Expression<Func<double, double, bool>> DerivedClaim { get; private set; } = null!;

    /// <inheritdoc />
    protected override RicisProofCaseResult Execute(ICollection<RicisProofMonitorEntry> events)
    {
        foreach (var obligation in MissingAnalyticObligations)
        {
            AddMonitor("ANALYTIC", "OPEN", obligation);
        }

        AddMonitor("ID-01..ID-06", "START", "Delegating reflected-pair elimination to the generic RICIS proof extension.");
        var document = new StringBuilder();
        var derived = constraints.ProveTypeIdentityCriticalLine(claim, document);
        DerivedClaim = derived;
        AddMonitor("ID-01..ID-06", "PASS", $"Derived {derived} as a conditional algebraic consequence.");
        AddMonitor("SCOPE", "CONDITIONAL", "Analytic obligations are explicit and are not silently promoted to axioms.");

        return new RicisProofCaseResult(
            "ConditionalTheorem",
            derived.ToString(),
            document.ToString());
    }
}
