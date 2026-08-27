namespace Ricis.WebAssembly.Models;

/// <summary>
/// Defines operations exposed by the RICIS HTTP API.
/// </summary>
public enum RicisOperation
{
    /// <summary>
    /// Simplifies a single expression through the RICIS phase pipeline.
    /// </summary>
    Simplify,

    /// <summary>
    /// Produces the symbolic derivative of a single expression.
    /// </summary>
    Derivative,

    /// <summary>
    /// Parses and processes semicolon-separated expressions as a structural system.
    /// </summary>
    System
}

/// <summary>
/// Represents the JSON request accepted by the RICIS Web API.
/// </summary>
public sealed record RicisExpressionRequest(string Expression);

/// <summary>
/// Represents a successful single-expression response returned by the RICIS Web API.
/// </summary>
public sealed record RicisExpressionResponse(
    string Source,
    string Operation,
    string Parsed,
    string Ricis);

/// <summary>
/// Represents a successful expression-system response returned by the RICIS Web API.
/// </summary>
public sealed record RicisExpressionSystemResponse(
    string Source,
    int Count,
    string System,
    IReadOnlyList<string> Expressions,
    IReadOnlyList<string> RicisExpressions);

/// <summary>
/// Represents a controlled error response returned by the RICIS Web API.
/// </summary>
public sealed record RicisApiErrorResponse(string Error, int? Position = null);

/// <summary>
/// Represents a displayed RICIS processing result independently of endpoint-specific DTOs.
/// </summary>
public sealed record RicisWorkspaceResult(
    RicisOperation Operation,
    string Source,
    string Parsed,
    IReadOnlyList<string> RicisExpressions,
    string? System);
