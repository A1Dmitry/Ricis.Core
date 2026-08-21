using System.Globalization;
using System.Text;
using Ricis.Core.Resources;

namespace Ricis.Core.Logging;

/// <summary>Semantic kind of one recursive LaTeX document section.</summary>
public enum RicisLatexSectionKind
{
    /// <summary>General narrative or contextual section.</summary>
    Narrative,
    /// <summary>Definition or declared foundation.</summary>
    Definition,
    /// <summary>Declared axiom or protocol group.</summary>
    AxiomGroup,
    /// <summary>Classified derivation section.</summary>
    Derivation,
    /// <summary>Claim whose verification status is stated explicitly.</summary>
    Claim,
    /// <summary>Lemma rendered as a typed supporting statement.</summary>
    Lemma,
    /// <summary>Type or consistency validation table.</summary>
    Validation,
    /// <summary>Reference or terminology appendix.</summary>
    Appendix,
}

/// <summary>Immutable claim projection rendered in a semantic LaTeX report.</summary>
public sealed record RicisLatexClaimViewModel(
    string ClaimId,
    string Statement,
    string EvidenceStatus,
    string EvidenceBoundary);

/// <summary>Immutable proof-step projection rendered in a semantic LaTeX report.</summary>
public sealed record RicisLatexProofStepViewModel(
    int Number,
    string RuleId,
    string Phase,
    string Statement,
    string Status);

/// <summary>Immutable validation-row projection rendered in a semantic LaTeX report.</summary>
public sealed record RicisLatexValidationRowViewModel(
    string Case,
    string Condition,
    string Resolution,
    string EvidenceStatus);

/// <summary>Presentation style for one semantic section in an academic LaTeX document.</summary>
public enum RicisLatexSectionPresentation
{
    /// <summary>Numbered section selected from the recursive depth.</summary>
    Numbered,
    /// <summary>Unnumbered front-matter or closing section that remains listed in the table of contents.</summary>
    Unnumbered,
    /// <summary>Appendix section after the document's epilogue.</summary>
    Appendix,
}

/// <summary>Immutable bilingual or multilingual abstract block.</summary>
public sealed record RicisLatexAbstractViewModel(string Language, string Label, string Body);

/// <summary>
/// Recursive MVVM section. The model carries semantic presentation data only and
/// deliberately excludes runtime visitors, ILog instances, raw journal entries and Trace snapshots.
/// </summary>
public sealed record RicisLatexSectionViewModel
{
    /// <summary>Creates a validated immutable recursive section.</summary>
    public RicisLatexSectionViewModel(
        string sectionId,
        RicisLatexSectionKind kind,
        string heading,
        string body,
        string equation = "",
        string evidenceStatus = "declared",
        IReadOnlyList<RicisLatexClaimViewModel> claims = null,
        IReadOnlyList<RicisLatexProofStepViewModel> proofSteps = null,
        IReadOnlyList<RicisLatexValidationRowViewModel> validationRows = null,
        IReadOnlyList<RicisLatexSectionViewModel> children = null,
        RicisLatexSectionPresentation presentation = RicisLatexSectionPresentation.Numbered)
    {
        SectionId = Require(sectionId, nameof(sectionId));
        Kind = kind;
        Heading = Require(heading, nameof(heading));
        Body = body ?? string.Empty;
        Equation = equation ?? string.Empty;
        EvidenceStatus = Require(evidenceStatus, nameof(evidenceStatus));
        Claims = Copy(claims);
        ProofSteps = Copy(proofSteps);
        ValidationRows = Copy(validationRows);
        Children = Copy(children);
        Presentation = presentation;
    }

    /// <summary>Stable recursive section identifier.</summary>
    public string SectionId { get; }

    /// <summary>Semantic section kind.</summary>
    public RicisLatexSectionKind Kind { get; }

    /// <summary>Stable heading/resource key or pre-localized display heading.</summary>
    public string Heading { get; }

    /// <summary>Already classified semantic body.</summary>
    public string Body { get; }

    /// <summary>Optional declared presentation equation.</summary>
    public string Equation { get; }

    /// <summary>Evidence status that the template renders without escalation.</summary>
    public string EvidenceStatus { get; }

    /// <summary>Claims declared within this section.</summary>
    public IReadOnlyList<RicisLatexClaimViewModel> Claims { get; }

    /// <summary>Public proof steps declared within this section.</summary>
    public IReadOnlyList<RicisLatexProofStepViewModel> ProofSteps { get; }

    /// <summary>Validation rows declared within this section.</summary>
    public IReadOnlyList<RicisLatexValidationRowViewModel> ValidationRows { get; }

    /// <summary>Recursive child sections.</summary>
    public IReadOnlyList<RicisLatexSectionViewModel> Children { get; }

    /// <summary>Academic section presentation style.</summary>
    public RicisLatexSectionPresentation Presentation { get; }

    private static string Require(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("A semantic LaTeX field is required.", parameterName);

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values) =>
        Array.AsReadOnly((values ?? Array.Empty<T>()).ToArray());
}

/// <summary>
/// Immutable root ViewModel for an external semantic LaTeX template. Trace is absent unless the explicit
/// technical appendix flag is selected by the report caller.
/// </summary>
public sealed record RicisLatexReportViewModel
{
    /// <summary>Creates a validated immutable semantic LaTeX report model.</summary>
    public RicisLatexReportViewModel(
        string documentId,
        string title,
        string statusKey,
        string evidenceBoundary,
        bool includeTechnicalAppendix,
        IReadOnlyList<RicisLatexSectionViewModel> sections,
        IReadOnlyList<string> technicalAppendixRows = null,
        RicisLatexAuthorAttributionViewModel authorAttribution = null,
        string subtitle = "",
        IReadOnlyList<RicisLatexAbstractViewModel> abstracts = null,
        string conclusion = "",
        string conclusionHeading = "",
        IReadOnlyList<string> conclusionSteps = null,
        string epilogue = "",
        string epilogueHeading = "",
        IReadOnlyList<string> epilogueSteps = null,
        bool includeTableOfContents = false)
    {
        DocumentId = Require(documentId, nameof(documentId));
        Title = Require(title, nameof(title));
        StatusKey = Require(statusKey, nameof(statusKey));
        EvidenceBoundary = Require(evidenceBoundary, nameof(evidenceBoundary));
        IncludeTechnicalAppendix = includeTechnicalAppendix;
        Sections = Array.AsReadOnly((sections ?? Array.Empty<RicisLatexSectionViewModel>()).ToArray());
        TechnicalAppendixRows = Array.AsReadOnly((technicalAppendixRows ?? Array.Empty<string>()).ToArray());
        AuthorAttribution = authorAttribution;
        Subtitle = subtitle ?? string.Empty;
        Abstracts = Array.AsReadOnly((abstracts ?? Array.Empty<RicisLatexAbstractViewModel>()).ToArray());
        Conclusion = conclusion ?? string.Empty;
        ConclusionHeading = conclusionHeading ?? string.Empty;
        ConclusionSteps = Array.AsReadOnly((conclusionSteps ?? Array.Empty<string>()).ToArray());
        Epilogue = epilogue ?? string.Empty;
        EpilogueHeading = epilogueHeading ?? string.Empty;
        EpilogueSteps = Array.AsReadOnly((epilogueSteps ?? Array.Empty<string>()).ToArray());
        IncludeTableOfContents = includeTableOfContents;
    }

    /// <summary>Stable report identifier.</summary>
    public string DocumentId { get; }

    /// <summary>Report title supplied by the caller or a resource resolver.</summary>
    public string Title { get; }

    /// <summary>Stable semantic status key.</summary>
    public string StatusKey { get; }

    /// <summary>Human-readable evidence boundary.</summary>
    public string EvidenceBoundary { get; }

    /// <summary>Whether the caller explicitly requested the technical appendix.</summary>
    public bool IncludeTechnicalAppendix { get; }

    /// <summary>Recursive public sections.</summary>
    public IReadOnlyList<RicisLatexSectionViewModel> Sections { get; }

    /// <summary>Technical trace rows supplied only for an explicit appendix.</summary>
    public IReadOnlyList<string> TechnicalAppendixRows { get; }

    /// <summary>Optional public author attribution; requester identity is deliberately absent.</summary>
    public RicisLatexAuthorAttributionViewModel AuthorAttribution { get; }

    /// <summary>Optional academic subtitle rendered below the title.</summary>
    public string Subtitle { get; }

    /// <summary>Ordered bilingual or multilingual abstract blocks.</summary>
    public IReadOnlyList<RicisLatexAbstractViewModel> Abstracts { get; }

    /// <summary>Optional conclusion rendered before the epilogue.</summary>
    public string Conclusion { get; }

    /// <summary>Optional source-specific conclusion heading.</summary>
    public string ConclusionHeading { get; }

    /// <summary>Optional ordered conclusion items.</summary>
    public IReadOnlyList<string> ConclusionSteps { get; }

    /// <summary>Optional unnumbered epilogue rendered after the conclusion.</summary>
    public string Epilogue { get; }

    /// <summary>Optional source-specific epilogue heading.</summary>
    public string EpilogueHeading { get; }

    /// <summary>Optional ordered epilogue items.</summary>
    public IReadOnlyList<string> EpilogueSteps { get; }

    /// <summary>Whether the external template renders a table of contents.</summary>
    public bool IncludeTableOfContents { get; }

    private static string Require(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("A semantic LaTeX field is required.", parameterName);
}

/// <summary>Builds a dedicated recursive LaTeX ViewModel from classified public events.</summary>
public sealed class RicisSemanticLatexReportModelFactory
{
    private readonly RicisSemanticEventClassifier _classifier;

    /// <summary>Initializes the LaTeX model factory with an optional classifier strategy.</summary>
    public RicisSemanticLatexReportModelFactory(RicisSemanticEventClassifier classifier = null) =>
        _classifier = classifier ?? new RicisSemanticEventClassifier();

    /// <summary>
    /// Builds a public semantic report. Trace is excluded by default and can be represented only by
    /// an explicitly requested technical appendix.
    /// </summary>
    public RicisLatexReportViewModel Build(
        IReadOnlyList<RicisLogEntry> entries,
        string documentId,
        string title,
        string evidenceBoundary,
        bool includeTechnicalAppendix = false,
        RicisLatexAuthorAttributionViewModel authorAttribution = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var classified = _classifier.Classify(entries);
        var publicEvents = classified
            .Where(item => item.Visibility == RicisReportVisibility.Academic &&
                           item.Kind is RicisSemanticEventKind.ProofStep or RicisSemanticEventKind.Warning)
            .ToArray();
        var steps = publicEvents.Select((item, index) => new RicisLatexProofStepViewModel(
            index + 1,
            GetAttribute(item, "ruleFamily") ?? item.Kind.ToString(),
            item.Phase,
            item.PublicMessage,
            item.Kind == RicisSemanticEventKind.Warning ? "warning" : "accepted")).ToArray();
        var limitations = classified
            .Where(item => item.Kind == RicisSemanticEventKind.HandledException)
            .Select((item, index) => new RicisLatexClaimViewModel(
                $"limitation-{index + 1}",
                item.PublicMessage,
                GetAttribute(item, "handlingStatus") ?? "handled",
                item.Sender.ShortName))
            .ToArray();
        var sections = new[]
        {
            new RicisLatexSectionViewModel(
                "public-derivation",
                RicisLatexSectionKind.Derivation,
                "public-derivation",
                "classified-public-events",
                evidenceStatus: "semantic-report",
                proofSteps: steps),
            new RicisLatexSectionViewModel(
                "limitations",
                RicisLatexSectionKind.Claim,
                "limitations",
                "public-boundary-and-handled-exceptions",
                evidenceStatus: "semantic-report",
                claims: limitations),
        };
        var appendixRows = includeTechnicalAppendix
            ? classified.Where(item => item.Visibility == RicisReportVisibility.TechnicalTrace)
                .Select(DescribeTechnicalTrace)
                .ToArray()
            : Array.Empty<string>();
        return new RicisLatexReportViewModel(
            documentId,
            title,
            "semantic-report-not-kernel-proof",
            evidenceBoundary,
            includeTechnicalAppendix,
            sections,
            appendixRows,
            authorAttribution);
    }

    private static string GetAttribute(RicisSemanticEvent item, string key) =>
        item.Attributes.TryGetValue(key, out var value) ? value : null;

    private static string DescribeTechnicalTrace(RicisSemanticEvent item) =>
        $"{item.Source.Sequence.ToString(CultureInfo.InvariantCulture)} | {item.Sender.ShortName} | {item.Source.EventCode} | {item.Source.Message} | before={item.Source.BeforeExpression ?? string.Empty} | after={item.Source.AfterExpression ?? string.Empty}";
}

/// <summary>Renders a restricted, already escaped projection through an external LaTeX template.</summary>
public sealed class RicisSemanticLatexTemplateRenderer
{
    private readonly RicisSafeReportTemplateRenderer _renderer;

    /// <summary>Initializes the renderer with an optional restricted template engine.</summary>
    public RicisSemanticLatexTemplateRenderer(RicisSafeReportTemplateRenderer renderer = null) =>
        _renderer = renderer ?? new RicisSafeReportTemplateRenderer();

    /// <summary>Renders a recursive semantic model without exposing runtime log or proof objects to a template.</summary>
    public string Render(RicisLatexReportViewModel model, string template)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);
        var resources = new RicisSemanticReportResources(RicisSemanticReportResources.GetTemplateLocale(template));
        var sections = BuildSectionRows(model.Sections.Where(section => section.Presentation != RicisLatexSectionPresentation.Appendix).ToArray(), resources);
        var appendixSections = BuildSectionRows(model.Sections.Where(section => section.Presentation == RicisLatexSectionPresentation.Appendix).ToArray(), resources);
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DocumentId"] = Escape(model.DocumentId),
            ["Title"] = Escape(model.Title),
            ["StatusKey"] = Escape(model.StatusKey),
            ["EvidenceBoundaryLabel"] = Escape(resources.EvidenceBoundaryLabel),
            ["SemanticStatusLabel"] = Escape(resources.SemanticStatusLabel),
            ["Subtitle"] = Escape(model.Subtitle),
            ["Abstracts"] = RenderAbstracts(model.Abstracts, resources),
            ["TableOfContentsIncluded"] = model.IncludeTableOfContents ? "true" : "false",
            ["EvidenceBoundary"] = EscapeParagraphs(model.EvidenceBoundary),
            ["ConclusionIncluded"] = string.IsNullOrWhiteSpace(model.Conclusion) ? "false" : "true",
            ["ConclusionHeading"] = Escape(string.IsNullOrWhiteSpace(model.ConclusionHeading) ? resources.ConclusionHeading : model.ConclusionHeading),
            ["Conclusion"] = EscapeParagraphs(model.Conclusion),
            ["ConclusionSteps"] = RenderTextList(model.ConclusionSteps),
            ["EpilogueIncluded"] = string.IsNullOrWhiteSpace(model.Epilogue) ? "false" : "true",
            ["EpilogueHeading"] = Escape(string.IsNullOrWhiteSpace(model.EpilogueHeading) ? resources.EpilogueHeading : model.EpilogueHeading),
            ["Epilogue"] = EscapeParagraphs(model.Epilogue),
            ["EpilogueSteps"] = RenderTextList(model.EpilogueSteps),
            ["AppendixSections"] = RenderSectionRows(appendixSections),
            ["AuthorIncluded"] = model.AuthorAttribution?.IsIncluded == true ? "true" : "false",
            ["AuthorMode"] = Escape(model.AuthorAttribution?.Mode.ToString() ?? string.Empty),
            ["AuthorDisplayName"] = Escape(model.AuthorAttribution?.DisplayName ?? string.Empty),
            ["AuthorAlternateName"] = Escape(model.AuthorAttribution?.AlternateName ?? string.Empty),
            ["AuthorOrcid"] = Escape(model.AuthorAttribution?.Orcid ?? string.Empty),
            ["AuthorDescription"] = EscapeParagraphs(model.AuthorAttribution?.Description ?? string.Empty),
            ["AuthorKeywords"] = Escape(string.Join(", ", model.AuthorAttribution?.Keywords ?? Array.Empty<string>())),
            ["AuthorWorks"] = RenderAuthorWorks(model.AuthorAttribution?.Works ?? Array.Empty<RicisLatexAuthorWorkViewModel>()),
            ["TechnicalAppendix"] = model.IncludeTechnicalAppendix ? RenderTechnicalAppendix(model.TechnicalAppendixRows, resources) : string.Empty,
        };
        return _renderer.RenderText(template, values, sections, "Sections");
    }

    private static string RenderSectionRows(IReadOnlyList<IReadOnlyDictionary<string, string>> rows) =>
        string.Join(Environment.NewLine, rows.Select(row =>
            $"{row["Opening"]}{Environment.NewLine}\\textit{{{row["Status"]}}}\\\\{Environment.NewLine}{row["Body"]}{Environment.NewLine}{row["Equation"]}{Environment.NewLine}{row["Claims"]}{Environment.NewLine}{row["ProofSteps"]}{Environment.NewLine}{row["ValidationRows"]}"));

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildSectionRows(IReadOnlyList<RicisLatexSectionViewModel> roots, RicisSemanticReportResources resources)
    {
        var flattened = Flatten(roots);
        var rows = new List<IReadOnlyDictionary<string, string>>();
        var appendixStarted = false;
        foreach (var section in flattened)
        {
            var isFirstAppendix = section.Section.Presentation == RicisLatexSectionPresentation.Appendix && !appendixStarted;
            appendixStarted |= section.Section.Presentation == RicisLatexSectionPresentation.Appendix;
            rows.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Opening"] = RenderSectionOpening(section.Section, section.Depth, isFirstAppendix),
                ["Body"] = RenderSectionBody(section.Section),
                ["Equation"] = RenderEquation(section.Section.Equation),
                ["Status"] = Escape(section.Section.EvidenceStatus),
                ["Claims"] = RenderClaims(section.Section.Claims, resources),
                ["ProofSteps"] = RenderProofSteps(section.Section.ProofSteps, section.Section.Kind == RicisLatexSectionKind.Claim),
                ["ValidationRows"] = RenderValidationRows(section.Section.ValidationRows, resources),
            });
        }

        return rows;
    }

    private static IReadOnlyList<(RicisLatexSectionViewModel Section, int Depth)> Flatten(IReadOnlyList<RicisLatexSectionViewModel> roots)
    {
        var rows = new List<(RicisLatexSectionViewModel Section, int Depth)>();
        foreach (var root in roots)
        {
            Visit(root, 0, rows);
        }

        return rows;
    }

    private static void Visit(RicisLatexSectionViewModel section, int depth, ICollection<(RicisLatexSectionViewModel Section, int Depth)> rows)
    {
        rows.Add((section, depth));
        foreach (var child in section.Children)
        {
            Visit(child, depth + 1, rows);
        }
    }

    private static string GetSectionCommand(int depth) => depth switch
    {
        0 => "\\section",
        1 => "\\subsection",
        2 => "\\subsubsection",
        _ => "\\paragraph",
    };

    private static string GetContentsLevel(int depth) => depth switch
    {
        0 => "section",
        1 => "subsection",
        2 => "subsubsection",
        _ => "paragraph",
    };

    private static string RenderSectionOpening(RicisLatexSectionViewModel section, int depth, bool isFirstAppendix)
    {
        if (section.Kind == RicisLatexSectionKind.Lemma)
        {
            return string.Empty;
        }

        var command = GetSectionCommand(depth);
        var heading = Escape(section.Heading);
        return section.Presentation switch
        {
            RicisLatexSectionPresentation.Unnumbered =>
                $"{command}*{{{heading}}}{Environment.NewLine}\\addcontentsline{{toc}}{{{GetContentsLevel(depth)}}}{{{heading}}}",
            RicisLatexSectionPresentation.Appendix =>
                $"{(isFirstAppendix ? "\\appendix" + Environment.NewLine : string.Empty)}{command}{{{heading}}}",
            _ => $"{command}{{{heading}}}",
        };
    }

    private static string RenderSectionBody(RicisLatexSectionViewModel section)
    {
        var body = EscapeParagraphs(section.Body);
        return section.Kind switch
        {
            RicisLatexSectionKind.Definition => $"\\begin{{definition}}{body}\\end{{definition}}",
            RicisLatexSectionKind.AxiomGroup => $"\\begin{{axiom}}{body}\\end{{axiom}}",
            RicisLatexSectionKind.Lemma => $"\\begin{{lemma}}[{Escape(section.Heading)}]{body}\\end{{lemma}}",
            _ => body,
        };
    }

    private static string RenderEquation(string equation) =>
        string.IsNullOrWhiteSpace(equation)
            ? string.Empty
            : $"\\[{Environment.NewLine}\\texttt{{{Escape(equation)}}}{Environment.NewLine}\\]";

    private static string RenderClaims(IReadOnlyList<RicisLatexClaimViewModel> claims, RicisSemanticReportResources resources) =>
        string.Join(Environment.NewLine, claims.Select(claim =>
            $"\\begin{{theorem}}[{Escape(claim.ClaimId)} --- {Escape(claim.EvidenceStatus)}]{EscapeParagraphs(claim.Statement)}\\end{{theorem}}{Environment.NewLine}\\noindent\\textit{{{Escape(resources.ClaimEvidenceBoundaryLabel)}}} {EscapeParagraphs(claim.EvidenceBoundary)}"));

    private static string RenderProofSteps(IReadOnlyList<RicisLatexProofStepViewModel> steps, bool useProofEnvironment) =>
        steps.Count == 0
            ? string.Empty
            : (useProofEnvironment ? "\\begin{proof}" : string.Empty) +
              "\\begin{enumerate}" + Environment.NewLine +
              string.Join(Environment.NewLine, steps.Select(step =>
                  $"\\item \\textbf{{{Escape(step.RuleId)}}} [{Escape(step.Status)}] {Escape(step.Phase)}: {EscapeParagraphs(step.Statement)}")) +
              Environment.NewLine + "\\end{enumerate}" +
              (useProofEnvironment ? "\\end{proof}" : string.Empty);

    private static string RenderAbstracts(IReadOnlyList<RicisLatexAbstractViewModel> abstracts, RicisSemanticReportResources resources) =>
        string.Join(Environment.NewLine, abstracts.Select(abstractBlock =>
            $"\\begin{{otherlanguage}}{{{GetLatexLanguage(abstractBlock.Language, resources)}}}{Environment.NewLine}" +
            $"\\renewcommand{{\\abstractname}}{{{Escape(abstractBlock.Label)}}}{Environment.NewLine}" +
            $"\\begin{{abstract}}{Environment.NewLine}{EscapeParagraphs(abstractBlock.Body)}{Environment.NewLine}\\end{{abstract}}{Environment.NewLine}\\end{{otherlanguage}}"));

    private static string GetLatexLanguage(string language, RicisSemanticReportResources resources) => language switch
    {
        "ru-RU" => "russian",
        "en-US" => "english",
        "fr-CA" => "english",
        "de-DE" => "english",
        "hi-IN" => "hindi",
        "ms-MY" => "english",
        _ => throw new InvalidOperationException(resources.UnsupportedAbstractLanguage(language)),
    };

    private static string RenderTextList(IReadOnlyList<string> values) =>
        values.Count == 0
            ? string.Empty
            : "\\begin{enumerate}" + Environment.NewLine +
              string.Join(Environment.NewLine, values.Select(value => $"\\item {EscapeParagraphs(value)}")) +
              Environment.NewLine + "\\end{enumerate}";

    private static string RenderValidationRows(IReadOnlyList<RicisLatexValidationRowViewModel> rows, RicisSemanticReportResources resources) =>
        rows.Count == 0
            ? string.Empty
            : "\\begin{tabular}{|p{0.20\\linewidth}|p{0.24\\linewidth}|p{0.30\\linewidth}|p{0.16\\linewidth}|}\\hline" + Environment.NewLine +
              $"{Escape(resources.ValidationHeaderCase)} & {Escape(resources.ValidationHeaderCondition)} & {Escape(resources.ValidationHeaderResolution)} & {Escape(resources.ValidationHeaderStatus)} \\\\ \\hline" + Environment.NewLine +
              string.Join(Environment.NewLine, rows.Select(row =>
                  $"{Escape(row.Case)} & {Escape(row.Condition)} & {Escape(row.Resolution)} & {Escape(row.EvidenceStatus)} \\\\ \\hline")) +
              Environment.NewLine + "\\end{tabular}";

    private static string RenderAuthorWorks(IReadOnlyList<RicisLatexAuthorWorkViewModel> works) =>
        works.Count == 0
            ? string.Empty
            : "\\begin{itemize}" +
              string.Join(string.Empty, works.Select(work =>
                  $"\\item {Escape(work.Name)} ({Escape(work.DatePublished)}): \\texttt{{{Escape(work.Url)}}}")) +
              "\\end{itemize}";

    private static string RenderTechnicalAppendix(IReadOnlyList<string> rows, RicisSemanticReportResources resources) =>
        rows.Count == 0
            ? string.Empty
            : $"\\appendix\\section*{{{Escape(resources.TechnicalAppendixHeading)}}}\\begin{{itemize}}" +
              string.Join(string.Empty, rows.Select(row => $"\\item \\texttt{{{Escape(row)}}}")) +
              "\\end{itemize}";

    private static string EscapeParagraphs(string value)
    {
        var paragraphs = (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.None);
        return string.Join("\\par ", paragraphs.Select(Escape));
    }

    private static string Escape(string value)
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
                '∞' => "\\ensuremath{\\infty}",
                '→' => "\\ensuremath{\\to}",
                '·' => "\\ensuremath{\\cdot}",
                '−' => "-",
                '≤' => "\\ensuremath{\\le}",
                '≥' => "\\ensuremath{\\ge}",
                '≠' => "\\ensuremath{\\ne}",
                '≡' => "\\ensuremath{\\equiv}",
                '—' => "---",
                '–' => "--",
                '\n' => " ",
                _ => character.ToString(),
            });
        }

        return builder.ToString();
    }
}
