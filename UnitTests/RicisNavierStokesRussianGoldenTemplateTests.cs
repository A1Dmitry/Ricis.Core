using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Logging;

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
        StringAssert.Contains(russian, "Граница доказательств");
        StringAssert.Contains(english, "Evidence boundary");
        foreach (var placeholder in new[] { "{{Title}}", "{{Abstracts}}", "{{#each Sections}}", "{{AppendixSections}}", "{{TechnicalAppendix}}" })
        {
            StringAssert.Contains(russian, placeholder);
            StringAssert.Contains(english, placeholder);
        }
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
