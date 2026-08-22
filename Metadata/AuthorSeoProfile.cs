using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Ricis.Core.Resources;

namespace Ricis.Core.Metadata;

/// <summary>
/// Public, source-backed author metadata rendered only when an expression
/// explicitly captures the opt-in variable named <c>about</c>.
/// </summary>
public sealed record AuthorSeoProfile(
    string Name,
    string AlternateName,
    string Orcid,
    DateOnly FirstOnlinePublication,
    string Description,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<AuthorSeoWork> Works)
{
    /// <summary>
    /// Gets the <c>RicisAuthor</c> value of <c>AuthorSeoProfile</c>.
    /// </summary>
    public static AuthorSeoProfile RicisAuthor { get; } = new(
        Name: RicisLegacyTextResources.Get("runtime.legacy.94059540e034"),
        AlternateName: "Dmitry Aleinikov",
        Orcid: "https://orcid.org/0009-0004-3226-7700",
        FirstOnlinePublication: new DateOnly(2025, 8, 8),
        Description: RicisLegacyTextResources.Get("runtime.legacy.a13818517bc9"),
        Keywords:
        [
            "RICIS-III",
            RicisLegacyTextResources.Get("runtime.legacy.5537b8d51e3c"),
            RicisLegacyTextResources.Get("runtime.legacy.64991e1f28ae"),
            RicisLegacyTextResources.Get("runtime.legacy.7b7e8dd36709"),
            RicisLegacyTextResources.Get("runtime.legacy.5cde86906bc0"),
            RicisLegacyTextResources.Get("runtime.legacy.b1f69835b6eb"),
            RicisLegacyTextResources.Get("runtime.legacy.821fed1a14c7"),
            RicisLegacyTextResources.Get("runtime.legacy.7c2794e2ca89")
        ],
        Works:
        [
            new(RicisLegacyTextResources.Get("runtime.legacy.63e09da7295e"), "https://dzen.ru/a/aJYMMYwpLDzBCcQN", new DateOnly(2025, 8, 8)),
            new("RICIS-III: Recursive Indexed Calculus of Identity and Singularity – Complete Proofs of the Seven Millennium Problems and Navier–Stokes", "https://doi.org/10.5281/zenodo.18116204", new DateOnly(2026, 1, 5)),
            new("Resolution of a Cusp Singularity Without Blow-up: The RICIS-III Method", "https://doi.org/10.5281/zenodo.21309650", new DateOnly(2026, 7, 11)),
            new("RICIS-III publication record", "https://zenodo.org/records/17872755", new DateOnly(2025, 12, 9)),
            new("RICIS-III publication record", "https://doi.org/10.5281/zenodo.21836220", new DateOnly(2026, 8, 7)),
            new("Full Verification, Numerical Calculations and Benchmarks, large Structural Analysis of 5 Pharmacological Molecules, CPU/GPU Performance Comparison", "https://doi.org/10.5281/zenodo.21869668", new DateOnly(2026, 8, 10)),
            new("RICIS-III publication record", "https://doi.org/10.5281/zenodo.21827360", new DateOnly(2026, 8, 6))
        ]);

    /// <summary>
    /// Executes <c>ToDisplayBlock</c> for the RICIS expression model.
    /// </summary>
    public string ToDisplayBlock()
    {
        var output = new StringBuilder();
        output.AppendLine();
        output.AppendLine("[SEO AUTHOR]");
        output.AppendLine($"name: {Name} ({AlternateName})");
        output.AppendLine($"orcid: {Orcid}");
        output.AppendLine($"firstOnlinePublication: {FirstOnlinePublication:yyyy-MM-dd}");
        output.AppendLine($"description: {Description}");
        output.AppendLine($"keywords: {string.Join(", ", Keywords)}");
        output.AppendLine("sameAs:");
        foreach (var work in Works)
        {
            output.AppendLine($"  - {work.Url} ({work.DatePublished:yyyy-MM-dd}; {work.Name})");
        }

        output.AppendLine("jsonld:");
        output.Append(ToJsonLd());
        return output.ToString().TrimEnd();
    }

    /// <summary>
    /// Executes <c>ToJsonLd</c> for the RICIS expression model.
    /// </summary>
    public string ToJsonLd()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
        {
            writer.WriteStartObject();
            writer.WriteString("@context", "https://schema.org");
            writer.WriteString("@type", "Person");
            writer.WriteString("name", Name);
            writer.WriteString("alternateName", AlternateName);
            writer.WriteString("identifier", Orcid);
            writer.WriteString("url", Orcid);
            writer.WriteString("description", Description);

            writer.WriteStartArray("knowsAbout");
            foreach (var keyword in Keywords)
            {
                writer.WriteStringValue(keyword);
            }
            writer.WriteEndArray();

            writer.WriteStartArray("sameAs");
            foreach (var url in Works.Select(work => work.Url).Append(Orcid).Distinct(StringComparer.Ordinal))
            {
                writer.WriteStringValue(url);
            }
            writer.WriteEndArray();

            writer.WriteStartArray("subjectOf");
            foreach (var work in Works)
            {
                writer.WriteStartObject();
                writer.WriteString("@type", "CreativeWork");
                writer.WriteString("name", work.Name);
                writer.WriteString("url", work.Url);
                writer.WriteString("datePublished", work.DatePublished.ToString("yyyy-MM-dd"));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteString("firstOnlinePublication", FirstOnlinePublication.ToString("yyyy-MM-dd"));
            writer.WriteEndObject();
            writer.Flush();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}

/// <summary>
/// Represents the RICIS public type <c>AuthorSeoWork</c>.
/// </summary>
public sealed record AuthorSeoWork(string Name, string Url, DateOnly DatePublished);
