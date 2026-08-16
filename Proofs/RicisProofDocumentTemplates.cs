using System.Text;
using System.Text.Json;
using System.Linq.Expressions;

namespace Ricis.Core.Proofs;

/// <summary>
/// Renders non-academic textual representations of an already-derived RICIS
/// proof. This layer never evaluates hypotheses or changes expression trees.
/// </summary>
internal static class RicisProofDocumentTemplates
{
    /// <summary>
    /// Renders a non-academic proof template selected by <paramref name="format"/>.
    /// </summary>
    internal static string Render(
        RicisProofDocumentFormat format,
        RicisProofDocumentProfile profile,
        string derivation,
        LambdaExpression derived)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(derived);

        return format switch
        {
            RicisProofDocumentFormat.Log => RenderLog(profile, derivation, derived),
            RicisProofDocumentFormat.Lean => throw new RicisUnsupportedLeanProofShapeException(
                "Generic C# expression tree cannot be emitted as a verified Lean theorem. " +
                "Use RicisLeanTemplate.Render(StructuredData, RequestedRows) for a supported structured Lean bridge."),
            RicisProofDocumentFormat.Json => RenderJson(profile, derivation, derived),
            RicisProofDocumentFormat.Academic => throw new ArgumentException(
                "Academic document rendering is provided by the existing academic template.", nameof(format)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Неизвестный формат proof-документа."),
        };
    }

    private static string RenderLog(
        RicisProofDocumentProfile profile,
        string derivation,
        LambdaExpression derived)
    {
        var builder = new StringBuilder();
        builder.Append("[RICIS ").Append(profile.Scope).Append("] ").AppendLine(profile.Title);
        builder.Append("abstract: ").AppendLine(profile.Abstract);
        builder.Append("theorem: ").AppendLine(profile.Theorem);
        builder.Append("derived: ").AppendLine(derived.ToString());
        AppendLogValues(builder, "definition", profile.Definitions);
        AppendLogValues(builder, "axiom", profile.Axioms);
        foreach (var step in profile.NormativeSteps)
        {
            builder.Append("normative-step[").Append(step.RuleId).Append("]: ")
                .Append(step.Title).Append(" — ").AppendLine(step.Statement);
        }

        AppendLogValues(builder, "limitation", profile.Limitations);
        builder.AppendLine("trace:");
        foreach (var line in derivation.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.Append("  ").AppendLine(line);
        }

        return builder.ToString();
    }

    private static string RenderLeanScaffold(
        RicisProofDocumentProfile profile,
        string derivation,
        LambdaExpression derived)
    {
        var builder = new StringBuilder();
        builder.AppendLine("/-");
        builder.AppendLine("RICIS proof-document export: Lean scaffold");
        builder.AppendLine("Status: documentation export only; arbitrary C# expression trees are not Lean-checked by this output.");
        builder.Append("Title: ").AppendLine(EscapeLeanComment(profile.Title));
        builder.Append("Scope: ").AppendLine(profile.Scope.ToString());
        builder.Append("Theorem: ").AppendLine(EscapeLeanComment(profile.Theorem));
        builder.Append("Derived expression: ").AppendLine(EscapeLeanComment(derived.ToString()));
        builder.AppendLine("Normative steps:");
        foreach (var step in profile.NormativeSteps)
        {
            builder.Append("- ").Append(step.RuleId).Append(": ")
                .AppendLine(EscapeLeanComment(step.Statement));
        }

        builder.AppendLine("RICIS trace:");
        foreach (var line in derivation.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.Append("- ").AppendLine(EscapeLeanComment(line));
        }

        builder.AppendLine("-/");
        builder.AppendLine("namespace Ricis.Generated");
        builder.AppendLine();
        builder.AppendLine("-- Add domain-specific Lean definitions and theorem statements before formal verification.");
        builder.AppendLine("end Ricis.Generated");
        return builder.ToString();
    }

    private static string RenderJson(
        RicisProofDocumentProfile profile,
        string derivation,
        LambdaExpression derived)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", RicisProofDocumentFormat.Json.ToString());
            writer.WriteString("title", profile.Title);
            writer.WriteString("scope", profile.Scope.ToString());
            writer.WriteString("abstract", profile.Abstract);
            writer.WriteString("theorem", profile.Theorem);
            WriteStringArray(writer, "definitions", profile.Definitions);
            WriteStringArray(writer, "axioms", profile.Axioms);
            writer.WriteStartArray("normativeSteps");
            foreach (var step in profile.NormativeSteps)
            {
                writer.WriteStartObject();
                writer.WriteString("ruleId", step.RuleId);
                writer.WriteString("title", step.Title);
                writer.WriteString("statement", step.Statement);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteStringArray(writer, "limitations", profile.Limitations);
            writer.WriteString("derivation", derivation);
            writer.WriteString("derived", derived.ToString());
            writer.WriteString("limitationsNotice", "The document records a finite derivation or conditional theorem only; external premises are not evaluated.");
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteStringArray(Utf8JsonWriter writer, string propertyName, IReadOnlyList<string> values)
    {
        writer.WriteStartArray(propertyName);
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void AppendLogValues(StringBuilder builder, string key, IReadOnlyList<string> values)
    {
        foreach (var value in values)
        {
            builder.Append(key).Append(": ").AppendLine(value);
        }
    }

    private static string EscapeLeanComment(string value) => value.Replace("-}", "- }", StringComparison.Ordinal);
}
