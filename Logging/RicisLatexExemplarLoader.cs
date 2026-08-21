using System.Text.Json;

namespace Ricis.Core.Logging;

/// <summary>
/// Loads an external recursive proof exemplar into an immutable semantic LaTeX ViewModel.
/// The loader is data-only: it cannot execute expressions, classify runtime logs or promote evidence status.
/// </summary>
public sealed class RicisLatexExemplarLoader
{
    /// <summary>Loads and validates one UTF-8 external recursive proof exemplar.</summary>
    public RicisLatexReportViewModel Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        RequireObject(root, "root");
        return new RicisLatexReportViewModel(
            ReadRequiredString(root, "documentId"),
            ReadRequiredString(root, "title"),
            ReadRequiredString(root, "statusKey"),
            ReadRequiredString(root, "evidenceBoundary"),
            false,
            ReadSections(ReadRequiredArray(root, "sections")));
    }

    private static IReadOnlyList<RicisLatexSectionViewModel> ReadSections(JsonElement sections)
    {
        var result = new List<RicisLatexSectionViewModel>();
        foreach (var section in sections.EnumerateArray())
        {
            RequireObject(section, "section");
            var kindText = ReadRequiredString(section, "kind");
            if (!Enum.TryParse<RicisLatexSectionKind>(kindText, ignoreCase: false, out var kind))
            {
                throw new InvalidOperationException($"Unsupported semantic LaTeX section kind '{kindText}'.");
            }

            result.Add(new RicisLatexSectionViewModel(
                ReadRequiredString(section, "sectionId"),
                kind,
                ReadRequiredString(section, "heading"),
                ReadRequiredString(section, "body"),
                ReadOptionalString(section, "equation"),
                ReadRequiredString(section, "evidenceStatus"),
                ReadClaims(ReadOptionalArray(section, "claims")),
                ReadProofSteps(ReadOptionalArray(section, "proofSteps")),
                ReadValidationRows(ReadOptionalArray(section, "validationRows")),
                ReadSections(ReadOptionalArray(section, "children"))));
        }

        return result;
    }

    private static IReadOnlyList<RicisLatexClaimViewModel> ReadClaims(JsonElement claims)
    {
        var result = new List<RicisLatexClaimViewModel>();
        foreach (var claim in claims.EnumerateArray())
        {
            RequireObject(claim, "claim");
            result.Add(new RicisLatexClaimViewModel(
                ReadRequiredString(claim, "claimId"),
                ReadRequiredString(claim, "statement"),
                ReadRequiredString(claim, "evidenceStatus"),
                ReadRequiredString(claim, "evidenceBoundary")));
        }

        return result;
    }

    private static IReadOnlyList<RicisLatexProofStepViewModel> ReadProofSteps(JsonElement steps)
    {
        var result = new List<RicisLatexProofStepViewModel>();
        foreach (var step in steps.EnumerateArray())
        {
            RequireObject(step, "proof step");
            if (!step.TryGetProperty("number", out var number) || number.ValueKind != JsonValueKind.Number || !number.TryGetInt32(out var value) || value <= 0)
            {
                throw new InvalidOperationException("A semantic LaTeX proof step requires a positive integer number.");
            }

            result.Add(new RicisLatexProofStepViewModel(
                value,
                ReadRequiredString(step, "ruleId"),
                ReadRequiredString(step, "phase"),
                ReadRequiredString(step, "statement"),
                ReadRequiredString(step, "status")));
        }

        return result;
    }

    private static IReadOnlyList<RicisLatexValidationRowViewModel> ReadValidationRows(JsonElement rows)
    {
        var result = new List<RicisLatexValidationRowViewModel>();
        foreach (var row in rows.EnumerateArray())
        {
            RequireObject(row, "validation row");
            result.Add(new RicisLatexValidationRowViewModel(
                ReadRequiredString(row, "case"),
                ReadRequiredString(row, "condition"),
                ReadRequiredString(row, "resolution"),
                ReadRequiredString(row, "evidenceStatus")));
        }

        return result;
    }

    private static JsonElement ReadRequiredArray(JsonElement owner, string propertyName)
    {
        if (!owner.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Semantic LaTeX exemplar requires array '{propertyName}'.");
        }

        return value;
    }

    private static JsonElement ReadOptionalArray(JsonElement owner, string propertyName) =>
        owner.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value
            : EmptyArray;

    private static JsonElement EmptyArray => JsonDocument.Parse("[]").RootElement.Clone();

    private static string ReadRequiredString(JsonElement owner, string propertyName)
    {
        var value = ReadOptionalString(owner, propertyName);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Semantic LaTeX exemplar requires non-empty string '{propertyName}'.");
    }

    private static string ReadOptionalString(JsonElement owner, string propertyName) =>
        owner.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static void RequireObject(JsonElement value, string role)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Semantic LaTeX {role} must be a JSON object.");
        }
    }
}
