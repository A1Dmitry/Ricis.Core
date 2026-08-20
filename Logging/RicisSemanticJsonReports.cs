using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ricis.Core.Logging;

/// <summary>Versioned machine-readable semantic report, independent from the technical journal.</summary>
public sealed record RicisJsonReportDocument(
    string Schema,
    string ReportType,
    bool KernelVerification,
    string Status,
    IReadOnlyList<RicisJsonReportEvent> Events);

/// <summary>One public semantic event projected for JSON consumers.</summary>
public sealed record RicisJsonReportEvent(
    long Sequence,
    string Kind,
    string Visibility,
    string Sender,
    string Phase,
    string EventCode,
    string Message,
    string Status,
    string ExceptionType,
    string HandlingStatus,
    string PublicCause);

/// <summary>Builds a versioned JSON model without exposing the raw RicisLogEntry payload.</summary>
public sealed class RicisJsonReportModelFactory
{
    private readonly RicisSemanticEventClassifier _classifier;

    /// <summary>Initializes a JSON model factory with an optional semantic classifier.</summary>
    public RicisJsonReportModelFactory(RicisSemanticEventClassifier classifier = null) =>
        _classifier = classifier ?? new RicisSemanticEventClassifier();

    /// <summary>Projects classified events into a deterministic public JSON document model.</summary>
    public RicisJsonReportDocument Build(IReadOnlyList<RicisLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var events = _classifier.Classify(entries)
            .Select(Project)
            .ToArray();
        return new RicisJsonReportDocument(
            "ricis-semantic-report/v1",
            "json-semantic",
            false,
            "classified",
            events);
    }

    private static RicisJsonReportEvent Project(RicisSemanticEvent eventItem)
    {
        var source = eventItem.Source;
        var publicCause = GetAttribute(eventItem, "publicMessage") ??
                          (eventItem.Kind == RicisSemanticEventKind.HandledException ? source.Message : null);
        var handlingStatus = GetAttribute(eventItem, "handlingStatus");
        var status = eventItem.Kind switch
        {
            RicisSemanticEventKind.Warning => "warning",
            RicisSemanticEventKind.HandledException => "exception",
            RicisSemanticEventKind.Unclassified => "unclassified",
            _ => "accepted",
        };
        return new RicisJsonReportEvent(
            source.Sequence,
            ToWireName(eventItem.Kind),
            ToWireName(eventItem.Visibility),
            eventItem.Sender.ShortName,
            eventItem.Phase,
            source.EventCode,
            eventItem.PublicMessage,
            status,
            eventItem.Kind == RicisSemanticEventKind.HandledException ? source.ExceptionType : null,
            handlingStatus,
            publicCause);
    }

    private static string GetAttribute(RicisSemanticEvent eventItem, string key) =>
        eventItem.Attributes.TryGetValue(key, out var value) ? value : null;

    private static string ToWireName<TEnum>(TEnum value) =>
        value.ToString() switch
        {
            "TechnicalTransformation" => "technical-transformation",
            "HandledException" => "handled-exception",
            "Unclassified" => "unclassified",
            "ProofStep" => "proof-step",
            "Lifecycle" => "lifecycle",
            "Warning" => "warning",
            "Exception" => "exception",
            "TechnicalTrace" => "technical-trace",
            "Academic" => "academic",
            _ => value.ToString().ToLowerInvariant(),
        };
}

/// <summary>Serializes only the semantic JSON document model, never the raw journal.</summary>
public sealed class RicisJsonReportSerializer
{
    private static readonly JsonSerializerOptions Options = RicisSemanticJsonContext.Default.Options;

    /// <summary>Serializes one semantic JSON document deterministically.</summary>
    public string Serialize(RicisJsonReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Validate(document);
        return JsonSerializer.Serialize(document, RicisSemanticJsonContext.Default.RicisJsonReportDocument);
    }

    private static void Validate(RicisJsonReportDocument document)
    {
        if (!string.Equals(document.Schema, "ricis-semantic-report/v1", StringComparison.Ordinal))
        {
            throw new ArgumentException("Unsupported RICIS semantic JSON schema.", nameof(document));
        }

        long previous = 0;
        foreach (var eventItem in document.Events)
        {
            ArgumentNullException.ThrowIfNull(eventItem);
            if (eventItem.Sequence <= previous)
            {
                throw new ArgumentException("JSON semantic events must be strictly sequence ordered.", nameof(document));
            }

            previous = eventItem.Sequence;
        }
    }
}

[JsonSerializable(typeof(RicisJsonReportDocument))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class RicisSemanticJsonContext : JsonSerializerContext
{
}
