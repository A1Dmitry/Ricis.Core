using System.Security.Cryptography;
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
        ("ACL04: provenance marker одинаково обрабатывает LF и CRLF", ProvenanceMarkerMatchingIsNewlineStable),
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
        var projectRoot = ProjectRoot();
        var path = Path.Combine(projectRoot, "FormalVerification", "Lean", "Artifacts", "academic", "RicisIII_AcademicAuthorExpansion.lean");
        TraceArtifact("ACL02", projectRoot, path);
        var exists = File.Exists(path);
        Console.WriteLine($"[ACL-TRACE] ACL02 fileExists={exists}");
        var source = exists ? NormalizeLineEndings(File.ReadAllText(path)) : string.Empty;
        var authorIndex = source.IndexOf("AUTHOR-SEO", StringComparison.Ordinal);
        var centralIndex = source.IndexOf("RICIS-III\n", StringComparison.Ordinal);
        var authorPublicationLink = source.Contains("AUTHOR-SEO → RICIS-III-PUBLICATION", StringComparison.Ordinal);
        var rootCentralLink = source.Contains("RICIS-CONCRETE-ROOT-TO-LEAF → RICIS-III", StringComparison.Ordinal);
        Console.WriteLine($"[ACL-TRACE] ACL02 authorIndex={authorIndex} centralIndex={centralIndex} authorPublicationLink={authorPublicationLink} rootCentralLink={rootCentralLink}");
        Require(exists && authorIndex >= 0 && centralIndex > authorIndex && authorPublicationLink && rootCentralLink,
            "Academic card должен разворачиваться от автора по provenance links до central RICIS III node.");
    }

    private static void ProvenanceMarkerMatchingIsNewlineStable()
    {
        const string marker = "RICIS-III\n";
        const string lfSource = "AUTHOR-SEO → RICIS-III-PUBLICATION\nRICIS-III\n";
        var crlfSource = lfSource.Replace("\n", "\r\n", StringComparison.Ordinal);
        var lfNormalized = NormalizeLineEndings(lfSource);
        var crlfNormalized = NormalizeLineEndings(crlfSource);
        var lfIndex = lfNormalized.IndexOf(marker, StringComparison.Ordinal);
        var crlfIndex = crlfNormalized.IndexOf(marker, StringComparison.Ordinal);
        Console.WriteLine($"[ACL-TRACE] ACL04 lfIndex={lfIndex} crlfIndex={crlfIndex} lfLength={lfNormalized.Length} crlfLength={crlfNormalized.Length}");
        Require(lfIndex >= 0 && crlfIndex == lfIndex,
            "Academic provenance marker должен одинаково находиться в LF и CRLF source.");
    }

    private static string NormalizeLineEndings(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static void LeanArtifactUsesCheckedStack()
    {
        var projectRoot = ProjectRoot();
        var path = Path.Combine(projectRoot, "FormalVerification", "Lean", "Artifacts", "academic", "RicisIII_AcademicAuthorExpansion.lean");
        TraceArtifact("ACL03", projectRoot, path);
        var exists = File.Exists(path);
        var source = exists ? File.ReadAllText(path) : string.Empty;
        var id06 = source.Contains("RicisIdentity.id06_exact_half", StringComparison.Ordinal);
        var graphTheorem = source.Contains("author_card_reaches_ricis_iii", StringComparison.Ordinal);
        var targetTheorem = source.Contains("academic_target_from_proof_stack", StringComparison.Ordinal);
        var hasSorry = source.Contains("sorry", StringComparison.OrdinalIgnoreCase);
        var hasAdmit = source.Contains("admit", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"[ACL-TRACE] ACL03 fileExists={exists} id06={id06} graphTheorem={graphTheorem} targetTheorem={targetTheorem} hasSorry={hasSorry} hasAdmit={hasAdmit}");
        Require(exists && id06 && graphTheorem && targetTheorem && !hasSorry && !hasAdmit,
            "Academic Lean artifact должен использовать checked ID-01–ID-06 stack без sorry/admit.");
    }

    private static void TraceArtifact(string scenario, string projectRoot, string path)
    {
        var exists = File.Exists(path);
        var bytes = exists ? File.ReadAllBytes(path) : Array.Empty<byte>();
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        Console.WriteLine($"[ACL-TRACE] scenario={scenario} projectRoot={projectRoot}");
        Console.WriteLine($"[ACL-TRACE] path={path} exists={exists} bytes={bytes.Length} sha256={hash}");
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
