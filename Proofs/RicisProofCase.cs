namespace Ricis.Core.Proofs;

/// <summary>
/// Defines the orchestration boundary for a proof case. The base class owns
/// monitoring and output state; a derived case owns its domain-specific premises
/// and the proof protocol it delegates to the RICIS engine.
/// </summary>
public abstract class RicisProofCase
{
    private readonly List<RicisProofMonitorEntry> monitor = [];

    /// <summary>
    /// Gets the human-readable proof-case name.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the monitored proof events in execution order.
    /// </summary>
    public IReadOnlyList<RicisProofMonitorEntry> Monitor => monitor;

    /// <summary>
    /// Gets the unresolved domain obligations that are outside the generic RICIS algebra engine.
    /// </summary>
    public abstract IReadOnlyList<string> UnresolvedObligations { get; }

    /// <summary>
    /// Gets the proof result and monitored output after <see cref="Run"/> completes.
    /// </summary>
    public RicisProofCaseResult Result { get; private set; } = null!;

    /// <summary>
    /// Executes the proof case once and returns its independent result.
    /// </summary>
    /// <returns>The derived expression, rendered document and monitoring snapshot.</returns>
    public RicisProofCaseResult Run()
    {
        monitor.Clear();
        AddMonitor("CASE", "START", Name);
        Result = Execute(monitor);
        AddMonitor("CASE", "END", Result.Status);
        return Result;
    }

    /// <summary>
    /// Adds a monitored event for a derived proof case.
    /// </summary>
    /// <param name="stage">Stable stage identifier.</param>
    /// <param name="status">Stage status such as <c>OPEN</c>, <c>PASS</c> or <c>BLOCKED</c>.</param>
    /// <param name="message">Human-readable stage message.</param>
    protected void AddMonitor(string stage, string status, string message) =>
        monitor.Add(new RicisProofMonitorEntry(stage, status, message));

    /// <summary>
    /// Performs domain-specific proof orchestration while preserving the base monitoring contract.
    /// </summary>
    /// <param name="events">The mutable event sink owned by the base class.</param>
    /// <returns>The independent result for this case.</returns>
    protected abstract RicisProofCaseResult Execute(ICollection<RicisProofMonitorEntry> events);
}

/// <summary>
/// Represents one monitored event emitted by a proof case.
/// </summary>
/// <param name="Stage">Stable stage identifier.</param>
/// <param name="Status">Stage status.</param>
/// <param name="Message">Human-readable stage message.</param>
public sealed record RicisProofMonitorEntry(string Stage, string Status, string Message);

/// <summary>
/// Contains the output of a specialized proof case.
/// </summary>
/// <param name="Status">Proof status, normally <c>ConditionalTheorem</c> for cases with external obligations.</param>
/// <param name="DerivedExpression">The independent expression tree derived by RICIS.</param>
/// <param name="Document">The rendered proof document.</param>
public sealed record RicisProofCaseResult(
    string Status,
    string DerivedExpression,
    string Document);
