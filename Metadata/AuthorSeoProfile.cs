using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

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
    public static AuthorSeoProfile RicisAuthor { get; } = new(
        Name: "Дмитрий Алейников",
        AlternateName: "Dmitry Aleinikov",
        Orcid: "https://orcid.org/0009-0004-3226-7700",
        FirstOnlinePublication: new DateOnly(2025, 8, 8),
        Description: "Независимый исследователь и автор публикаций о RICIS-III, формальной математике, структурном разрешении сингулярностей и вычислительных приложениях.",
        Keywords:
        [
            "RICIS-III",
            "формальная математика",
            "формальная верификация",
            "индексированные бесконечности",
            "типизированные нули",
            "разрешение сингулярностей",
            "алгебраическая геометрия",
            "вычислительный анализ"
        ],
        Works:
        [
            new("Как мы переопределили бесконечность, устранили деление на ноль и переписали теорему Коши", "https://dzen.ru/a/aJYMMYwpLDzBCcQN", new DateOnly(2025, 8, 8)),
            new("RICIS-III: Recursive Indexed Calculus of Identity and Singularity – Complete Proofs of the Seven Millennium Problems and Navier–Stokes", "https://doi.org/10.5281/zenodo.18116204", new DateOnly(2026, 1, 5)),
            new("Resolution of a Cusp Singularity Without Blow-up: The RICIS-III Method", "https://doi.org/10.5281/zenodo.21309650", new DateOnly(2026, 7, 11)),
            new("RICIS-III publication record", "https://zenodo.org/records/17872755", new DateOnly(2025, 12, 9)),
            new("RICIS-III publication record", "https://doi.org/10.5281/zenodo.21836220", new DateOnly(2026, 8, 7)),
            new("Full Verification, Numerical Calculations and Benchmarks, large Structural Analysis of 5 Pharmacological Molecules, CPU/GPU Performance Comparison", "https://doi.org/10.5281/zenodo.21869668", new DateOnly(2026, 8, 10)),
            new("RICIS-III publication record", "https://doi.org/10.5281/zenodo.21827360", new DateOnly(2026, 8, 6))
        ]);

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

public sealed record AuthorSeoWork(string Name, string Url, DateOnly DatePublished);
