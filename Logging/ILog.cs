using System.Collections.ObjectModel;

namespace Ricis.Core.Logging;

/// <summary>
/// Severity of an immutable RICIS proof-log entry.
/// </summary>
public enum RicisLogSeverity
{
    /// <summary>Normal lifecycle information.</summary>
    Info,

    /// <summary>A non-fatal condition that must remain visible in the report.</summary>
    Warning,

    /// <summary>An exception observed by a stage before it is rethrown or handled.</summary>
    Exception,

    /// <summary>A deterministic before/after transformation attempt.</summary>
    Trace,
}

/// <summary>
/// Immutable canonical event captured during proof execution. Renderers consume
/// this value object and never rerun the proof pipeline.
/// </summary>
public sealed class RicisLogEntry
{
    /// <summary>Initializes one immutable proof-log event.</summary>
    public RicisLogEntry(
        long sequence,
        DateTimeOffset timestampUtc,
        RicisLogSeverity severity,
        string eventCode,
        string message,
        string stageType,
        IReadOnlyDictionary<string, string> attributes = null,
        string beforeExpression = null,
        string afterExpression = null,
        string exceptionType = null,
        string exceptionTrace = null)
    {
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Последовательность событий должна быть положительной.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Неизвестная severity журнала.");
        }

        Sequence = sequence;
        TimestampUtc = timestampUtc;
        Severity = severity;
        EventCode = RequireText(eventCode, nameof(eventCode));
        Message = RequireText(message, nameof(message));
        StageType = RequireText(stageType, nameof(stageType));
        Attributes = CopyAttributes(attributes);
        BeforeExpression = beforeExpression;
        AfterExpression = afterExpression;
        ExceptionType = exceptionType;
        ExceptionTrace = exceptionTrace;
    }

    /// <summary>Gets the monotonic sequence number assigned by the shared journal.</summary>
    public long Sequence { get; }

    /// <summary>Gets the UTC timestamp of capture.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>Gets event severity.</summary>
    public RicisLogSeverity Severity { get; }

    /// <summary>Gets a stable machine-readable event code.</summary>
    public string EventCode { get; }

    /// <summary>Gets the human-readable event message.</summary>
    public string Message { get; }

    /// <summary>Gets the actual source-stage CLR type name.</summary>
    public string StageType { get; }

    /// <summary>Gets immutable renderer-safe attributes.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>Gets an optional display snapshot of the expression before a trace stage.</summary>
    public string BeforeExpression { get; }

    /// <summary>Gets an optional display snapshot of the expression after a trace stage.</summary>
    public string AfterExpression { get; }

    /// <summary>Gets an optional exception CLR type.</summary>
    public string ExceptionType { get; }

    /// <summary>Gets an optional exception message and stack trace snapshot.</summary>
    public string ExceptionTrace { get; }

    private static IReadOnlyDictionary<string, string> CopyAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in attributes)
        {
            copy.Add(RequireText(pair.Key, nameof(attributes)), pair.Value ?? string.Empty);
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Текстовый элемент proof-лога не может быть пустым.", parameterName);
        }

        return value;
    }
}

/// <summary>
/// Typed facade over a shared proof event journal. <typeparamref name="TStage"/>
/// is the actual visitor, handler, solver, or orchestration type that emitted an
/// event; it is never a document-format discriminator.
/// </summary>
public interface ILog<TStage>
{
    /// <summary>Records normal stage information.</summary>
    void Info(string eventCode, string message, IReadOnlyDictionary<string, string> attributes = null);

    /// <summary>Records a non-fatal stage warning.</summary>
    void Warning(string eventCode, string message, IReadOnlyDictionary<string, string> attributes = null);

    /// <summary>Records an exception before the caller rethrows or handles it.</summary>
    void Exception(string eventCode, System.Exception error, string message = null, IReadOnlyDictionary<string, string> attributes = null);

    /// <summary>
    /// Records a deterministic transformation attempt with renderer-safe before
    /// and after expression snapshots.
    /// </summary>
    void Trace(
        string eventCode,
        string message,
        string beforeExpression,
        string afterExpression,
        IReadOnlyDictionary<string, string> attributes = null);

    /// <summary>Creates a typed child facade backed by the same canonical journal.</summary>
    ILog<TNextStage> For<TNextStage>();

    /// <summary>Returns a sequence-ordered immutable snapshot of all shared events.</summary>
    IReadOnlyList<RicisLogEntry> Snapshot();
}
