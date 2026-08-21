using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Ricis.Core.Logging;

/// <summary>Semantic visibility of one classified event.</summary>
public enum RicisReportVisibility
{
    /// <summary>Full technical trace visible only to the text diagnostic report.</summary>
    TechnicalTrace,
    /// <summary>Semantically selected content suitable for an academic report.</summary>
    Academic,
    /// <summary>Non-fatal condition requiring controlled visibility.</summary>
    Warning,
    /// <summary>Observed exception requiring cause and handling classification.</summary>
    Exception,
}

/// <summary>Stable semantic category derived from sender and event metadata.</summary>
public enum RicisSemanticEventKind
{
    /// <summary>Start, completion or orchestration lifecycle event.</summary>
    Lifecycle,
    /// <summary>Semantically selected proof or derivation event.</summary>
    ProofStep,
    /// <summary>Internal before/after transformation event.</summary>
    TechnicalTransformation,
    /// <summary>Non-fatal warning event.</summary>
    Warning,
    /// <summary>Exception observed and classified by a stage.</summary>
    HandledException,
    /// <summary>Event without a registered semantic mapping.</summary>
    Unclassified,
}

/// <summary>Immutable sender metadata used by report-specific classifiers.</summary>
public sealed record RicisSenderDescriptor(string TypeName, string ShortName);

/// <summary>Classified event; templates receive this model, never RicisLogEntry.</summary>
public sealed record RicisSemanticEvent(
    RicisLogEntry Source,
    RicisSenderDescriptor Sender,
    RicisSemanticEventKind Kind,
    RicisReportVisibility Visibility,
    string Phase,
    string PublicMessage,
    IReadOnlyDictionary<string, string> Attributes);

/// <summary>Text-log model containing the complete technical event stream.</summary>
public sealed record RicisTextReportModel(
    string Title,
    string RunStatus,
    IReadOnlyList<RicisTextReportRow> Rows);

/// <summary>One diagnostic text-log row, including Trace and exception internals.</summary>
public sealed record RicisTextReportRow(
    long Sequence,
    string Severity,
    string Sender,
    string EventCode,
    string Phase,
    string Message,
    string Before,
    string After,
    string ExceptionType,
    string ExceptionTrace);

/// <summary>Academic report model containing only semantically selected public steps.</summary>
public sealed record RicisAcademicReportModel(
    string Title,
    string Status,
    IReadOnlyList<RicisAcademicStep> Steps,
    IReadOnlyList<string> Limitations,
    string Conclusion);

/// <summary>Public academic step; technical Trace snapshots are intentionally absent.</summary>
public sealed record RicisAcademicStep(
    int Number,
    string Phase,
    string Rule,
    string Message,
    string Status);

/// <summary>Classifies events by sender, event code, attributes, message and exception payload.</summary>
public sealed class RicisSemanticEventClassifier
{
    /// <summary>Classifies one immutable event using sender, code, severity, attributes and payload.</summary>
    public RicisSemanticEvent Classify(RicisLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var sender = new RicisSenderDescriptor(entry.StageType, GetShortName(entry.StageType));
        var phase = GetAttribute(entry, "phaseName") ?? GetAttribute(entry, "ruleFamily") ?? sender.ShortName;
        var kind = ClassifyKind(entry);
        var visibility = entry.Severity switch
        {
            RicisLogSeverity.Trace => RicisReportVisibility.TechnicalTrace,
            RicisLogSeverity.Exception => RicisReportVisibility.Exception,
            RicisLogSeverity.Warning => RicisReportVisibility.Warning,
            _ when kind == RicisSemanticEventKind.ProofStep => RicisReportVisibility.Academic,
            _ => RicisReportVisibility.Academic,
        };
        var publicMessage = GetAttribute(entry, "publicMessage") ?? entry.Message;
        return new RicisSemanticEvent(entry, sender, kind, visibility, phase, publicMessage, entry.Attributes);
    }

    /// <summary>Classifies an ordered event sequence without executing any proof logic.</summary>
    public IReadOnlyList<RicisSemanticEvent> Classify(IReadOnlyList<RicisLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.Select(Classify).ToArray();
    }

    private static RicisSemanticEventKind ClassifyKind(RicisLogEntry entry)
    {
        if (entry.Severity == RicisLogSeverity.Trace)
        {
            return RicisSemanticEventKind.TechnicalTransformation;
        }

        if (entry.Severity == RicisLogSeverity.Exception)
        {
            return RicisSemanticEventKind.HandledException;
        }

        if (entry.Severity == RicisLogSeverity.Warning)
        {
            return RicisSemanticEventKind.Warning;
        }

        if (entry.EventCode.Contains("PROOF", StringComparison.Ordinal) ||
            entry.EventCode.Contains("PHASE_COMPLETE", StringComparison.Ordinal) ||
            entry.EventCode.Contains("SYSTEM_", StringComparison.Ordinal) ||
            entry.EventCode.Contains("PIECEWISE_", StringComparison.Ordinal))
        {
            return RicisSemanticEventKind.ProofStep;
        }

        if (entry.EventCode.EndsWith("START", StringComparison.Ordinal) ||
            entry.EventCode.EndsWith("COMPLETE", StringComparison.Ordinal) ||
            entry.EventCode.Contains("PIPELINE", StringComparison.Ordinal))
        {
            return RicisSemanticEventKind.Lifecycle;
        }

        return RicisSemanticEventKind.Unclassified;
    }

    private static string GetAttribute(RicisLogEntry entry, string key) =>
        entry.Attributes.TryGetValue(key, out var value) ? value : null;

    private static string GetShortName(string typeName)
    {
        var plus = typeName.LastIndexOf('+');
        var dot = typeName.LastIndexOf('.');
        return typeName[(Math.Max(plus, dot) + 1)..];
    }
}

/// <summary>Builds independent report models from classified events.</summary>
public sealed class RicisSemanticReportModelFactory
{
    private readonly RicisSemanticEventClassifier _classifier;

    /// <summary>Initializes the report model factory with an optional classifier strategy.</summary>
    public RicisSemanticReportModelFactory(RicisSemanticEventClassifier classifier = null) =>
        _classifier = classifier ?? new RicisSemanticEventClassifier();

    /// <summary>Builds a complete technical model, including Trace and exception details.</summary>
    public RicisTextReportModel BuildText(IReadOnlyList<RicisLogEntry> entries)
    {
        var events = _classifier.Classify(entries);
        return new RicisTextReportModel(
            "RICIS technical log",
            "diagnostic",
            events.Select(eventItem => new RicisTextReportRow(
                eventItem.Source.Sequence,
                eventItem.Source.Severity.ToString(),
                eventItem.Sender.ShortName,
                eventItem.Source.EventCode,
                eventItem.Phase,
                eventItem.Source.Message,
                eventItem.Source.BeforeExpression,
                eventItem.Source.AfterExpression,
                eventItem.Source.ExceptionType,
                eventItem.Source.ExceptionTrace)).ToArray());
    }

    /// <summary>Builds a public academic model and excludes technical Trace snapshots.</summary>
    public RicisAcademicReportModel BuildAcademic(IReadOnlyList<RicisLogEntry> entries)
    {
        var events = _classifier.Classify(entries)
            .Where(eventItem => eventItem.Visibility == RicisReportVisibility.Academic &&
                                eventItem.Kind is RicisSemanticEventKind.ProofStep or RicisSemanticEventKind.Warning)
            .ToArray();
        var steps = events.Select((eventItem, index) => new RicisAcademicStep(
            index + 1,
            eventItem.Phase,
            GetAttribute(eventItem, "ruleFamily") ?? eventItem.Kind.ToString(),
            eventItem.PublicMessage,
            eventItem.Kind == RicisSemanticEventKind.Warning ? "warning" : "accepted")).ToArray();
        var limitations = _classifier.Classify(entries)
            .Where(eventItem => eventItem.Kind == RicisSemanticEventKind.HandledException)
            .Select(eventItem => $"{eventItem.Sender.ShortName}: {eventItem.PublicMessage}")
            .ToArray();
        return new RicisAcademicReportModel(
            "RICIS academic report",
            "semantic report; not a kernel proof",
            steps,
            limitations,
            steps.Length == 0 ? "No public proof steps were classified." : "The classified public proof steps are reported above.");
    }

    private static string GetAttribute(RicisSemanticEvent eventItem, string key) =>
        eventItem.Attributes.TryGetValue(key, out var value) ? value : null;
}

/// <summary>Resolves named external template files; templates cannot access runtime objects.</summary>
public interface IRicisReportTemplateSource
{
    /// <summary>Loads one named template using specific-to-neutral culture fallback.</summary>
    string Get(string templateName, string cultureName);
}

/// <summary>File-backed template source with specific-to-neutral culture fallback.</summary>
public sealed class RicisFileReportTemplateSource : IRicisReportTemplateSource
{
    private readonly string _rootDirectory;

    /// <summary>Initializes a file-backed source rooted at an external template directory.</summary>
    public RicisFileReportTemplateSource(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory)));
    }

    /// <summary>Loads a culture-specific file template or throws a controlled error.</summary>
    public string Get(string templateName, string cultureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        var candidates = new[]
        {
            Path.Combine(_rootDirectory, $"{templateName}.{cultureName}.template"),
            Path.Combine(_rootDirectory, $"{templateName}.{cultureName.Split('-')[0]}.template"),
            Path.Combine(_rootDirectory, $"{templateName}.template"),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
        }

        throw new InvalidOperationException($"Template '{templateName}' is not available for culture '{cultureName}'.");
    }
}

/// <summary>Embedded resource template source retained for package-embedded deployments.</summary>
public sealed class RicisEmbeddedReportTemplateSource : IRicisReportTemplateSource
{
    private readonly Assembly _assembly;
    private readonly string _rootNamespace;

    /// <summary>Initializes a resource-backed template source.</summary>
    public RicisEmbeddedReportTemplateSource(Assembly assembly = null, string rootNamespace = "Ricis.Core.Logging.Templates")
    {
        _assembly = assembly ?? typeof(RicisEmbeddedReportTemplateSource).Assembly;
        _rootNamespace = rootNamespace;
    }

    /// <summary>Loads a culture-specific embedded template or throws a controlled error.</summary>
    public string Get(string templateName, string cultureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        var candidates = new[]
        {
            $"{_rootNamespace}.{templateName}.{cultureName}.template",
            $"{_rootNamespace}.{templateName}.{cultureName.Split('-')[0]}.template",
            $"{_rootNamespace}.{templateName}.template",
        };
        foreach (var resourceName in candidates)
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        throw new InvalidOperationException($"Template '{templateName}' is not available for culture '{cultureName}'.");
    }
}

/// <summary>Small deterministic template engine for scalar values and typed list blocks.</summary>
public sealed class RicisSafeReportTemplateRenderer
{
    private static readonly Regex EachBlock = new("\\{\\{#each (?<name>[A-Za-z0-9_.]+)\\}\\}(?<body>.*?)\\{\\{/each\\}\\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex IfBlock = new("\\{\\{#if (?<name>[A-Za-z0-9_.]+)\\}\\}(?<body>.*?)\\{\\{/if\\}\\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Scalar = new("\\{\\{(?<name>[A-Za-z0-9_.]+)\\}\\}", RegexOptions.Compiled);

    /// <summary>Renders a restricted template using scalar values and the allowlisted Rows collection.</summary>
    public string RenderText(string template, IReadOnlyDictionary<string, string> values, IReadOnlyList<IReadOnlyDictionary<string, string>> rows, string collectionName = "Rows")
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(rows);
        var conditionallyRendered = IfBlock.Replace(template, match =>
        {
            var key = match.Groups["name"].Value;
            return values.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.Ordinal)
                ? match.Groups["body"].Value
                : string.Empty;
        });
        var rendered = EachBlock.Replace(conditionallyRendered, match =>
        {
            if (!string.Equals(match.Groups["name"].Value, collectionName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported template collection '{match.Groups["name"].Value}'.");
            }

            var body = match.Groups["body"].Value;
            return string.Join(string.Empty, rows.Select(row => Scalar.Replace(body, scalar =>
            {
                var key = scalar.Groups["name"].Value;
                return key.StartsWith("this.", StringComparison.Ordinal) && row.TryGetValue(key[5..], out var value)
                    ? value
                    : scalar.Value;
            })));
        });
        return Scalar.Replace(rendered, match =>
        {
            var key = match.Groups["name"].Value;
            return values.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    /// <summary>Renders the Text model through a named external template.</summary>
    public string RenderTextModel(RicisTextReportModel model, string template)
    {
        ArgumentNullException.ThrowIfNull(model);
        var rows = model.Rows.Select(row => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Sequence"] = row.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Severity"] = row.Severity,
            ["Sender"] = row.Sender,
            ["EventCode"] = row.EventCode,
            ["Phase"] = row.Phase,
            ["Message"] = row.Message,
            ["Before"] = row.Before ?? string.Empty,
            ["After"] = row.After ?? string.Empty,
            ["ExceptionType"] = row.ExceptionType ?? string.Empty,
        }).ToArray();
        return RenderText(template, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Title"] = model.Title,
            ["RunStatus"] = model.RunStatus,
        }, rows);
    }

    /// <summary>Renders the Academic model through a named external template.</summary>
    public string RenderAcademicModel(RicisAcademicReportModel model, string template)
    {
        ArgumentNullException.ThrowIfNull(model);
        var steps = model.Steps.Select(step => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Number"] = step.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Phase"] = step.Phase,
            ["Rule"] = step.Rule,
            ["Message"] = step.Message,
            ["Status"] = step.Status,
        }).ToArray();
        var limitations = model.Limitations.Select(value => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["this"] = value,
        }).ToArray();
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Title"] = model.Title,
            ["Status"] = model.Status,
            ["Conclusion"] = model.Conclusion,
        };
        var limitationText = string.Join(string.Empty, limitations.Select(value => $"- {value["this"]}\n"));
        var templateWithoutLimitations = template.Replace("{{#each Limitations}}- {{this}}\n{{/each}}", limitationText, StringComparison.Ordinal);
        return RenderText(templateWithoutLimitations, values, steps, "Steps");
    }
}
