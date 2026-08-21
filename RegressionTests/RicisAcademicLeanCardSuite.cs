using Ricis.Core.Metadata;
using Ricis.Core.Proofs;

/// <summary>Adversarial QA for the author-oriented academic Lean card artifact.</summary>
public static class RicisAcademicLeanCardSuite
{
    /// <summary>Gets the academic card regression cases.</summary>
    public static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("ACL01: author metadata содержит SEO, ORCID и даты из source card", AuthorMetadataIsComplete),
        ("ACL02: academic card graph начинается автором и достигает RICIS III", CardGraphIsAuthorOriented),
        ("ACL03: Lean artifact содержит реальный proof stack и trust boundary", LeanArtifactUsesCheckedStack),
    ];

    private static void AuthorMetadataIsComplete()
    {
        var author = AuthorSeoProfile.RicisAuthor;
        Require(author.Name == "Дмитрий Алейников" &&
                author.AlternateName == "Dmitry Aleinikov" &&
                author.Orcid == "https://orcid.org/0009-0004-3226-7700" &&
                author.FirstOnlinePublication == new DateOnly(2025, 8, 8) &&
                author.Keywords.Count > 0 &&
                author.Works.Count > 0 &&
                author.Works.All(work => Uri.TryCreate(work.Url, UriKind.Absolute, out _)),
            "Author card должен содержать имя, alternate name, ORCID, дату, SEO keywords и source links.");
    }

    private static void CardGraphIsAuthorOriented()
    {
        var path = Path.Combine(ProjectRoot(), "FormalVerification", "Lean", "Artifacts", "academic", "RicisIII_AcademicAuthorExpansion.lean");
        var source = File.ReadAllText(path);
        var authorIndex = source.IndexOf("AUTHOR-SEO", StringComparison.Ordinal);
        var centralIndex = source.IndexOf("RICIS-III\n", StringComparison.Ordinal);
        Require(authorIndex >= 0 && centralIndex > authorIndex &&
                source.Contains("AUTHOR-SEO → RICIS-III-PUBLICATION", StringComparison.Ordinal) &&
                source.Contains("RICIS-CONCRETE-ROOT-TO-LEAF → RICIS-III", StringComparison.Ordinal),
            "Academic card должен разворачиваться от автора по provenance links до central RICIS III node.");
    }

    private static void LeanArtifactUsesCheckedStack()
    {
        var path = Path.Combine(ProjectRoot(), "FormalVerification", "Lean", "Artifacts", "academic", "RicisIII_AcademicAuthorExpansion.lean");
        var source = File.ReadAllText(path);
        Require(source.Contains("RicisIdentity.id06_exact_half", StringComparison.Ordinal) &&
                source.Contains("author_card_reaches_ricis_iii", StringComparison.Ordinal) &&
                source.Contains("academic_target_from_proof_stack", StringComparison.Ordinal) &&
                !source.Contains("sorry", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("admit", StringComparison.OrdinalIgnoreCase),
            "Academic Lean artifact должен использовать checked ID-01–ID-06 stack без sorry/admit.");
    }

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ricis.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Не найден корень Ricis.Core проекта.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
