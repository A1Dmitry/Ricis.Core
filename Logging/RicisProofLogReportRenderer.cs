using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ricis.Core.Logging;

/// <summary>
/// Presentation format for a typed proof-log report. This enum is deliberately
/// separate from <c>RicisProofDocumentFormat</c>: it renders audit events and
/// does not change the proof-document contract.
/// </summary>
public enum RicisProofLogFormat
{
    /// <summary>Machine-readable JSON audit record.</summary>
    Json,

    /// <summary>LaTeX appendix suitable for a technical proof report.</summary>
    Latex,

    /// <summary>Lean-oriented comment report; it is not a Lean theorem.</summary>
    Lean,
}

/// <summary>Renders one canonical sequence of proof-log events.</summary>
public interface IRicisProofLogRenderer
{
    /// <summary>Gets the format handled by this renderer.</summary>
    RicisProofLogFormat Format { get; }

    /// <summary>Renders an immutable ordered proof-log snapshot.</summary>
    string Render(IReadOnlyList<RicisLogEntry> entries);
}

/// <summary>
/// Single dispatch point for proof-log reports. Renderer implementations only
/// consume immutable entries; they never invoke visitors, handlers, or delegates.
/// </summary>
public static partial class RicisProofLogReportRenderer
{
    private static readonly IReadOnlyDictionary<RicisProofLogFormat, IRicisProofLogRenderer> Renderers =
        new Dictionary<RicisProofLogFormat, IRicisProofLogRenderer>
        {
            [RicisProofLogFormat.Json] = new JsonRenderer(),
            [RicisProofLogFormat.Latex] = new LatexRenderer(),
            [RicisProofLogFormat.Lean] = new LeanRenderer(),
        };

    /// <summary>Renders an immutable log snapshot through the requested report adapter.</summary>
    public static string Render(IReadOnlyList<RicisLogEntry> entries, RicisProofLogFormat format)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Неизвестный формат proof-лога.");
        }

        return Renderers[format].Render(ValidateOrdered(entries));
    }

    private static IReadOnlyList<RicisLogEntry> ValidateOrdered(IReadOnlyList<RicisLogEntry> entries)
    {
        long previous = 0;
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (entry.Sequence <= previous)
            {
                throw new ArgumentException("Proof-log entries должны иметь строго возрастающую sequence.", nameof(entries));
            }

            previous = entry.Sequence;
        }

        return entries;
    }

    private sealed class JsonRenderer : IRicisProofLogRenderer
    {
        public RicisProofLogFormat Format => RicisProofLogFormat.Json;

        public string Render(IReadOnlyList<RicisLogEntry> entries)
        {
            var document = new RicisProofLogJsonDocument(
                "ricis-proof-log/v1",
                false,
                entries.Select(RicisProofLogJsonEntry.From).ToArray());
            return JsonSerializer.Serialize(
                document,
                RicisProofLogJsonContext.Default.RicisProofLogJsonDocument);
        }
    }

    private sealed class LatexRenderer : IRicisProofLogRenderer
    {
        public RicisProofLogFormat Format => RicisProofLogFormat.Latex;

        public string Render(IReadOnlyList<RicisLogEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("\\section*{RICIS typed proof log}");
            builder.AppendLine("\\textbf{Status:} audit trace only; not an external formal proof.");
            builder.AppendLine();
            builder.AppendLine("\\begin{tabular}{r l l p{0.48\\linewidth}}");
            builder.AppendLine("\\hline");
            builder.AppendLine(@"Seq. & Severity & Stage & Message \\ ");
            builder.AppendLine("\\hline");
            foreach (var entry in entries)
            {
                builder.Append(entry.Sequence).Append(" & ")
                    .Append(EscapeLatex(entry.Severity.ToString())).Append(" & ")
                    .Append(EscapeLatex(entry.StageType)).Append(" & ")
                    .Append(EscapeLatex(entry.Message)).AppendLine(@" \\ ");
            }

            builder.AppendLine("\\hline");
            builder.AppendLine("\\end{tabular}");
            foreach (var entry in entries.Where(entry => entry.Severity == RicisLogSeverity.Trace))
            {
                builder.AppendLine();
                builder.Append("\\paragraph{Trace ").Append(entry.Sequence).Append(" — ")
                    .Append(EscapeLatex(entry.StageType)).AppendLine("}");
                builder.Append("\\texttt{before: ").Append(EscapeLatex(entry.BeforeExpression ?? string.Empty)).AppendLine("}");
                builder.Append("\\texttt{after: ").Append(EscapeLatex(entry.AfterExpression ?? string.Empty)).AppendLine("}");
            }

            AppendLatexExceptions(builder, entries);
            return builder.ToString();
        }
    }

    private sealed class LeanRenderer : IRicisProofLogRenderer
    {
        public RicisProofLogFormat Format => RicisProofLogFormat.Lean;

        public string Render(IReadOnlyList<RicisLogEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("/-");
            builder.AppendLine("RICIS typed proof-log report");
            builder.AppendLine("Status: NOT KERNEL VERIFIED. This is an audit comment; no Lean declaration is generated.");
            foreach (var entry in entries)
            {
                builder.Append("[").Append(entry.Sequence).Append("] ")
                    .Append(entry.Severity).Append(" ")
                    .Append(EscapeLeanComment(entry.StageType)).Append(" :: ")
                    .AppendLine(EscapeLeanComment(entry.Message));
                if (entry.Severity == RicisLogSeverity.Trace)
                {
                    builder.Append("  before: ").AppendLine(EscapeLeanComment(entry.BeforeExpression ?? string.Empty));
                    builder.Append("  after: ").AppendLine(EscapeLeanComment(entry.AfterExpression ?? string.Empty));
                }

                if (entry.Severity == RicisLogSeverity.Exception)
                {
                    builder.Append("  exception: ").AppendLine(EscapeLeanComment(entry.ExceptionType ?? string.Empty));
                    builder.Append("  trace: ").AppendLine(EscapeLeanComment(entry.ExceptionTrace ?? string.Empty));
                }
            }

            builder.AppendLine("-/");
            return builder.ToString();
        }
    }

    private sealed record RicisProofLogJsonDocument(
        string Schema,
        bool KernelVerification,
        IReadOnlyList<RicisProofLogJsonEntry> Entries);

    private sealed record RicisProofLogJsonEntry(
        long Sequence,
        DateTimeOffset TimestampUtc,
        string Severity,
        string EventCode,
        string Message,
        string StageType,
        IReadOnlyDictionary<string, string> Attributes,
        string BeforeExpression,
        string AfterExpression,
        string ExceptionType,
        string ExceptionTrace)
    {
        public static RicisProofLogJsonEntry From(RicisLogEntry entry) => new(
            entry.Sequence,
            entry.TimestampUtc,
            entry.Severity.ToString(),
            entry.EventCode,
            entry.Message,
            entry.StageType,
            entry.Attributes,
            entry.BeforeExpression,
            entry.AfterExpression,
            entry.ExceptionType,
            entry.ExceptionTrace);
    }

    [JsonSerializable(typeof(RicisProofLogJsonDocument))]
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    private partial class RicisProofLogJsonContext : JsonSerializerContext
    {
    }

    private static void AppendLatexExceptions(StringBuilder builder, IReadOnlyList<RicisLogEntry> entries)
    {
        foreach (var entry in entries.Where(entry => entry.Severity == RicisLogSeverity.Exception))
        {
            builder.AppendLine();
            builder.Append("\\paragraph{Exception ").Append(entry.Sequence).AppendLine("}");
            builder.Append("\\textbf{Type:} ").Append(EscapeLatex(entry.ExceptionType ?? string.Empty)).AppendLine("\\\\");
            builder.Append("\\texttt{").Append(EscapeLatex(entry.ExceptionTrace ?? string.Empty)).AppendLine("}");
        }
    }

    private static string EscapeLatex(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value ?? string.Empty)
        {
            builder.Append(character switch
            {
                '\\' => "\\textbackslash{}",
                '{' => "\\{",
                '}' => "\\}",
                '$' => "\\$",
                '&' => "\\&",
                '#' => "\\#",
                '%' => "\\%",
                '_' => "\\_",
                '^' => "\\textasciicircum{}",
                '~' => "\\textasciitilde{}",
                '\n' => "\\newline ",
                '\r' => string.Empty,
                _ => character.ToString(),
            });
        }

        return builder.ToString();
    }

    private static string EscapeLeanComment(string value) =>
        (value ?? string.Empty).Replace("-/", "- /", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
