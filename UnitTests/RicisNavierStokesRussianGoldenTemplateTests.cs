using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Logging;
using Ricis.Core.Resources;

namespace Ricis.Core.UnitTests;

/// <summary>
/// Golden tests for the Russian academic template. The original source fixes the expected document form;
/// semantic evidence overlays are asserted explicitly rather than hidden by a text-normalization shortcut.
/// </summary>
[TestClass]
public sealed class RicisNavierStokesRussianGoldenTemplateTests
{
    [TestMethod]
    public void RussianTemplate_ModelDerivedFromNavierStokesSource_PreservesGoldenAcademicSkeleton()
    {
        var root = FindProjectRoot();
        var sourcePath = Path.Combine(root, "Knowledge", "LaTexExamples", "NavierStokes-Ricis.structural-exemplar.tex");
        var exemplarPath = Path.Combine(root, "Logging", "Templates", "navier-stokes-ricis.exemplar.json");
        var templateDirectory = Path.Combine(root, "Logging", "Templates");
        var expectedSource = File.ReadAllText(sourcePath);
        var model = new RicisLatexExemplarLoader().Load(exemplarPath);
        var actual = new RicisSemanticLatexTemplateRenderer().Render(
            model,
            new RicisFileReportTemplateSource(templateDirectory).Get("latex", "ru-RU"));

        var expectedHeadings = ExtractAcademicHeadings(expectedSource);
        var actualHeadings = ExtractAcademicHeadings(actual);

        Assert.IsTrue(expectedSource.Contains("Глобальная гладкость уравнений Навье-Стокса в 3D", StringComparison.Ordinal));
        Assert.IsTrue(actual.Contains("Глобальная гладкость уравнений Навье-Стокса в 3D", StringComparison.Ordinal));
        Assert.AreEqual(2, CountToken(expectedSource, "\\begin{abstract}"));
        Assert.AreEqual(2, CountToken(actual, "\\begin{abstract}"));
        Assert.IsTrue(expectedSource.Contains("\\tableofcontents", StringComparison.Ordinal));
        Assert.IsTrue(actual.Contains("\\tableofcontents", StringComparison.Ordinal));

        AssertHeadingsAppearInOrder(expectedHeadings, actualHeadings);
        foreach (var environment in new[] { "definition", "axiom", "theorem", "proof", "tabular" })
        {
            StringAssert.Contains(expectedSource, $"\\begin{{{environment}}}");
            StringAssert.Contains(actual, $"\\begin{{{environment}}}");
        }

        StringAssert.Contains(actual, "Граница доказательств");
        StringAssert.Contains(actual, "Deferred");
        StringAssert.Contains(actual, "KernelChecked");
        Assert.IsFalse(actual.Contains("before=", StringComparison.Ordinal));
        Assert.IsFalse(actual.Contains("after=", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RussianAndEnglishTemplates_AreIndependentExternalAssetsWithTheSameSafeContract()
    {
        var root = FindProjectRoot();
        var source = new RicisFileReportTemplateSource(Path.Combine(root, "Logging", "Templates"));
        var russian = source.Get("latex", "ru-RU");
        var english = source.Get("latex", "en-US");

        Assert.AreNotEqual(russian, english, "ru-RU и en-US должны быть независимыми external templates.");
        foreach (var placeholder in new[] { "{{Title}}", "{{Abstracts}}", "{{#each Sections}}", "{{EvidenceBoundaryLabel}}", "{{SemanticStatusLabel}}", "{{AppendixSections}}", "{{TechnicalAppendix}}" })
        {
            StringAssert.Contains(russian, placeholder);
            StringAssert.Contains(english, placeholder);
        }
    }

    [TestMethod]
    public void EnglishTemplate_EnglishSourceProjection_ContainsNoCyrillicCharacters()
    {
        var root = FindProjectRoot();
        var templateDirectory = Path.Combine(root, "Logging", "Templates");
        var model = new RicisLatexExemplarLoader().Load(
            Path.Combine(templateDirectory, "navier-stokes-ricis.en-US.exemplar.json"));
        var document = new RicisSemanticLatexTemplateRenderer().Render(
            model,
            new RicisFileReportTemplateSource(templateDirectory).Get("latex", "en-US"));

        Assert.IsFalse(Regex.IsMatch(document, "[\\p{IsCyrillic}]"),
            "English template and English source projection must not render Cyrillic text.");
        StringAssert.Contains(document, "Global Smoothness of the 3D Navier-Stokes Equations");
        StringAssert.Contains(document, "Evidence boundary");
        StringAssert.Contains(document, "Theorem and proof");
        StringAssert.Contains(document, "Appendix: RICIS glossary");
    }

    [TestMethod]
    public void SemanticReportResources_ResolveAllCoverageLocalesAndFallbackToEnglish()
    {
        var expectedLabels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["en-US"] = "Evidence boundary",
            ["ru-RU"] = "Граница доказательств",
            ["fr-CA"] = "Limite de la preuve",
            ["de-DE"] = "Beweisgrenze",
            ["hi-IN"] = "प्रमाण-सीमा",
            ["ms-MY"] = "Sempadan bukti",
        };

        foreach (var (locale, expected) in expectedLabels)
        {
            var resources = new RicisSemanticReportResources(locale);
            Assert.AreEqual(expected, resources.EvidenceBoundaryLabel, locale);
            Assert.IsFalse(string.IsNullOrWhiteSpace(resources.SemanticStatusLabel), locale);
            Assert.IsFalse(string.IsNullOrWhiteSpace(resources.TechnicalAppendixHeading), locale);
        }

        Assert.AreEqual("Evidence boundary", new RicisSemanticReportResources("unsupported-XY").EvidenceBoundaryLabel);
    }

    [TestMethod]
    public void LatexPdfCompiler_TwoPassDocument_CreatesPdfAndKeepsCompilerEvidenceSeparate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ricis-latex-pdf-unit-" + Guid.NewGuid().ToString("N"));
        try
        {
            const string latex = "\\documentclass{article}\\begin{document}RICIS PDF compiler unit test.\\end{document}";
            var runner = new SuccessfulLatexProcessRunner();
            var result = new RicisLatexPdfCompiler(processRunner: runner).CompileLatex(
                "pdf-unit-test",
                latex,
                outputDirectory,
                new RicisLatexPdfCompileOptions(PassCount: 2, TimeoutMillisecondsPerPass: 30_000, MaxEvidenceCharacters: 512));

            Assert.IsTrue(File.Exists(result.PdfPath));
            Assert.AreEqual(2, runner.CallCount);
            Assert.AreEqual(2, result.Evidence.PassCount);
            CollectionAssert.AreEqual(new[] { 0, 0 }, result.Evidence.ExitCodes.ToArray());
            Assert.AreEqual(2, result.Evidence.BoundedPassLogs.Count);
            Assert.IsTrue(result.Evidence.BoundedPassLogs.All(log => log.Length <= 512));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("ExternalDependency")]
    public void LatexPdfCompiler_SystemPdflatex_WhenAvailable_CreatesPdfAndKeepsCompilerEvidenceSeparate()
    {
        if (!IsExecutableAvailable("pdflatex"))
        {
            Assert.Inconclusive("SKIPPED_EXTERNAL_DEPENDENCY: pdflatex is not available on PATH; hermetic compiler contracts remain covered.");
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), "ricis-latex-system-pdf-unit-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = new RicisLatexPdfCompiler().CompileLatex(
                "system-pdf-unit-test",
                "\\documentclass{article}\\begin{document}RICIS system PDF compiler integration test.\\end{document}",
                outputDirectory,
                new RicisLatexPdfCompileOptions(PassCount: 2, TimeoutMillisecondsPerPass: 30_000, MaxEvidenceCharacters: 512));

            Assert.IsTrue(File.Exists(result.PdfPath));
            Assert.AreEqual(2, result.Evidence.PassCount);
            CollectionAssert.AreEqual(new[] { 0, 0 }, result.Evidence.ExitCodes.ToArray());
            Assert.AreEqual(2, result.Evidence.BoundedPassLogs.Count);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    public void LatexPdfCompiler_UnavailableEngine_ReturnsTypedExceptionWithBoundedTechnicalEvidence()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ricis-latex-unavailable-unit-" + Guid.NewGuid().ToString("N"));
        try
        {
            var exception = Assert.ThrowsException<RicisLatexPdfCompilerUnavailableException>(() =>
                new RicisLatexPdfCompiler(processRunner: new UnavailableLatexProcessRunner()).CompileLatex(
                    "unavailable-unit-test",
                    "\\documentclass{article}\\begin{document}x\\end{document}",
                    outputDirectory,
                    new RicisLatexPdfCompileOptions(PassCount: 2, MaxEvidenceCharacters: 32)));

            Assert.AreEqual("pdflatex", exception.Evidence.Engine);
            Assert.AreEqual(1, exception.Evidence.PassCount);
            CollectionAssert.AreEqual(new[] { -127 }, exception.Evidence.ExitCodes.ToArray());
            CollectionAssert.AreEqual(new[] { "compiler-unavailable" }, exception.Evidence.BoundedPassLogs.ToArray());
            Assert.IsFalse(exception.Message.Contains("\\documentclass", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SystemRicisLatexProcessRunner_MissingExecutable_ReturnsControlledLaunchFailure()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ricis-latex-runner-unit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var result = new SystemRicisLatexProcessRunner().Run(
                "pdflatex-unit-missing-executable",
                Path.Combine(outputDirectory, "input.tex"),
                outputDirectory,
                1_000);

            Assert.IsTrue(result.LaunchFailed);
            Assert.AreEqual(-127, result.ExitCode);
            Assert.IsFalse(result.TimedOut);
            Assert.AreEqual("compiler-unavailable", result.StandardError);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void CountryLocaleManifest_ResolvesConfirmedCoverageAndExposesAllDeclaredTemplates()
    {
        var root = FindProjectRoot();
        var templateDirectory = Path.Combine(root, "Logging", "Templates");
        var resolver = RicisReportLocaleResolver.Load(Path.Combine(templateDirectory, "ricis-country-locale-coverage.exemplar.json"));

        Assert.AreEqual("en-US", resolver.Resolve("US").Locale);
        Assert.AreEqual("fr-CA", resolver.Resolve("CA", "fr-CA").Locale);
        Assert.AreEqual("en-US", resolver.Resolve("CA", "unsupported-XY").Locale);
        Assert.AreEqual("de-DE", resolver.Resolve("DE").Locale);
        Assert.AreEqual("hi-IN", resolver.Resolve("IN").Locale);
        Assert.AreEqual("ms-MY", resolver.Resolve("MY").Locale);
        Assert.AreEqual("en-US", resolver.Resolve("XX", "de-DE").Locale);

        var source = new RicisFileReportTemplateSource(templateDirectory);
        foreach (var locale in new[] { "en-US", "ru-RU", "fr-CA", "de-DE", "hi-IN", "ms-MY" })
        {
            StringAssert.Contains(source.Get("latex", locale), "% RICIS-LOCALE: " + locale);
        }
    }

    [TestMethod]
    public void ConfirmedLocaleExemplars_RenderThroughCultureSpecificResources()
    {
        var root = FindProjectRoot();
        var templateDirectory = Path.Combine(root, "Logging", "Templates");
        var source = new RicisFileReportTemplateSource(templateDirectory);
        var renderer = new RicisSemanticLatexTemplateRenderer();
        var expectedBoundaryLabels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["en-US"] = "Evidence boundary",
            ["fr-CA"] = "Limite de la preuve",
            ["de-DE"] = "Beweisgrenze",
            ["hi-IN"] = "प्रमाण-सीमा",
            ["ms-MY"] = "Sempadan bukti",
        };

        foreach (var (locale, boundaryLabel) in expectedBoundaryLabels)
        {
            var model = new RicisLatexExemplarLoader().Load(
                Path.Combine(templateDirectory, $"navier-stokes-ricis.{locale}.exemplar.json"));
            var document = renderer.Render(model, source.Get("latex", locale));
            StringAssert.Contains(document, boundaryLabel, locale);
            StringAssert.Contains(document, "Deferred", locale);
            Assert.IsFalse(document.Contains("before=", StringComparison.Ordinal), locale);
            Assert.IsFalse(document.Contains("after=", StringComparison.Ordinal), locale);
        }
    }

    [TestMethod]
    public void SemanticLatexRenderer_PublicLabelsComeFromResourcesNotHardcodedSource()
    {
        var root = FindProjectRoot();
        var rendererSource = File.ReadAllText(Path.Combine(root, "Logging", "RicisSemanticLatexReports.cs"));
        foreach (var publicLabel in new[]
                 {
                     "Evidence boundary", "Граница доказательств", "Case & Condition & Resolution & Status",
                     "Случай & Условие & Разрешение & Статус", "Technical appendix",
                 })
        {
            Assert.IsFalse(rendererSource.Contains(publicLabel, StringComparison.Ordinal), publicLabel);
        }

        var resourcesSource = File.ReadAllText(Path.Combine(root, "Resources", "RicisSemanticReportStrings.resx"));
        StringAssert.Contains(resourcesSource, "EvidenceBoundaryLabel");
        StringAssert.Contains(resourcesSource, "TechnicalAppendixHeading");
    }

    private sealed class SuccessfulLatexProcessRunner : IRicisLatexProcessRunner
    {
        public int CallCount { get; private set; }

        public RicisLatexProcessResult Run(string engine, string latexPath, string outputDirectory, int timeoutMilliseconds)
        {
            CallCount++;
            File.WriteAllBytes(Path.ChangeExtension(latexPath, ".pdf"), new byte[] { 0x25, 0x50, 0x44, 0x46 });
            return new RicisLatexProcessResult(0, TimedOut: false, LaunchFailed: false, "pass-ok", string.Empty);
        }
    }

    private sealed class UnavailableLatexProcessRunner : IRicisLatexProcessRunner
    {
        public RicisLatexProcessResult Run(string engine, string latexPath, string outputDirectory, int timeoutMilliseconds) =>
            new(-127, TimedOut: false, LaunchFailed: true, string.Empty, "compiler-unavailable");
    }

    private static IReadOnlyList<string> ExtractAcademicHeadings(string latex)
    {
        var matches = Regex.Matches(latex, @"\\(?:sub)*section\*?\{(?<heading>[^}]*)\}");
        return matches
            .Select(match => Normalize(match.Groups["heading"].Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private static void AssertHeadingsAppearInOrder(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var cursor = 0;
        foreach (var heading in expected)
        {
            while (cursor < actual.Count && !string.Equals(actual[cursor], heading, StringComparison.Ordinal))
            {
                cursor++;
            }

            Assert.IsTrue(cursor < actual.Count, $"Golden Russian heading '{heading}' отсутствует или нарушает source order.");
            cursor++;
        }
    }

    private static string Normalize(string value)
    {
        var normalized = value
            .Replace("\\bomega", " omega ", StringComparison.Ordinal)
            .Replace("\\nabla", " nabla ", StringComparison.Ordinal)
            .Replace("\\cdot", " dot ", StringComparison.Ordinal)
            .Replace("---", "-", StringComparison.Ordinal)
            .Replace("--", "-", StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, "[${}()]", " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }

    private static int CountToken(string value, string token) =>
        Regex.Matches(value, Regex.Escape(token)).Count;

    private static bool IsExecutableAvailable(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return false;
        }

        var extensions = OperatingSystem.IsWindows()
            ? new[] { string.Empty, ".exe", ".cmd", ".bat" }
            : new[] { string.Empty };
        var pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return pathEntries.Any(directory => extensions.Any(extension => File.Exists(Path.Combine(directory, executable + extension))));
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ricis.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Не найден корень Ricis.Core проекта.");
    }
}
