namespace Ricis.Core.Logging;

/// <summary>
/// Thread-safe in-memory canonical journal for one RICIS proof run. Generic
/// child facades share this store, so a sequence captures proof orchestration
/// and every visitor/handler stage without requiring parallel text buffers.
/// </summary>
public sealed class RicisProofLog<TStage> : ILog<TStage>
{
    private readonly RicisProofLogJournal _journal;

    /// <summary>Initializes an empty proof journal for <typeparamref name="TStage"/>.</summary>
    public RicisProofLog()
        : this(new RicisProofLogJournal())
    {
    }

    internal RicisProofLog(RicisProofLogJournal journal)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
    }

    /// <inheritdoc />
    public void Info(string eventCode, string message, IReadOnlyDictionary<string, string> attributes = null) =>
        _journal.Append<TStage>(RicisLogSeverity.Info, eventCode, message, attributes);

    /// <inheritdoc />
    public void Warning(string eventCode, string message, IReadOnlyDictionary<string, string> attributes = null) =>
        _journal.Append<TStage>(RicisLogSeverity.Warning, eventCode, message, attributes);

    /// <inheritdoc />
    public void Exception(string eventCode, Exception error, string message = null, IReadOnlyDictionary<string, string> attributes = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        _journal.Append<TStage>(
            RicisLogSeverity.Exception,
            eventCode,
            message ?? error.Message,
            attributes,
            exceptionType: error.GetType().FullName ?? error.GetType().Name,
            exceptionTrace: error.ToString());
    }

    /// <inheritdoc />
    public void Trace(
        string eventCode,
        string message,
        string beforeExpression,
        string afterExpression,
        IReadOnlyDictionary<string, string> attributes = null) =>
        _journal.Append<TStage>(
            RicisLogSeverity.Trace,
            eventCode,
            message,
            attributes,
            beforeExpression,
            afterExpression);

    /// <inheritdoc />
    public ILog<TNextStage> For<TNextStage>() => new RicisProofLog<TNextStage>(_journal);

    /// <inheritdoc />
    public IReadOnlyList<RicisLogEntry> Snapshot() => _journal.Snapshot();
}

/// <summary>Non-generic store shared by all typed facades of one proof run.</summary>
internal sealed class RicisProofLogJournal
{
    private readonly object _gate = new();
    private readonly List<RicisLogEntry> _entries = [];
    private long _nextSequence;

    public void Append<TSourceStage>(
        RicisLogSeverity severity,
        string eventCode,
        string message,
        IReadOnlyDictionary<string, string> attributes,
        string beforeExpression = null,
        string afterExpression = null,
        string exceptionType = null,
        string exceptionTrace = null)
    {
        lock (_gate)
        {
            var entry = new RicisLogEntry(
                sequence: checked(++_nextSequence),
                timestampUtc: DateTimeOffset.UtcNow,
                severity: severity,
                eventCode: eventCode,
                message: message,
                stageType: typeof(TSourceStage).FullName ?? typeof(TSourceStage).Name,
                attributes: attributes,
                beforeExpression: beforeExpression,
                afterExpression: afterExpression,
                exceptionType: exceptionType,
                exceptionTrace: exceptionTrace);
            _entries.Add(entry);
        }
    }

    public IReadOnlyList<RicisLogEntry> Snapshot()
    {
        lock (_gate)
        {
            return Array.AsReadOnly(_entries.ToArray());
        }
    }
}
