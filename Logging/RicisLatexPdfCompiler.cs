using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Ricis.Core.Logging;

/// <summary>Immutable options for a request-scoped LaTeX-to-PDF compiler run.</summary>
public sealed record RicisLatexPdfCompileOptions(
    string Engine = "pdflatex",
    int PassCount = 2,
    int TimeoutMillisecondsPerPass = 120_000,
    int MaxEvidenceCharacters = 8_192);

/// <summary>Technical compilation evidence kept separate from an academic semantic report.</summary>
public sealed record RicisLatexPdfCompilationEvidence(
    string Engine,
    int PassCount,
    IReadOnlyList<int> ExitCodes,
    IReadOnlyList<string> BoundedPassLogs);

/// <summary>Output of a request-scoped LaTeX-to-PDF compilation.</summary>
public sealed record RicisLatexPdfCompileResult(
    string PdfPath,
    string LatexPath,
    RicisLatexPdfCompilationEvidence Evidence);

/// <summary>
/// Compiles an already rendered LaTeX document to PDF. The class does not inspect a Lean artifact,
/// persist requester data, or merge compiler diagnostics into an academic report.
/// </summary>
public sealed class RicisLatexPdfCompiler
{
    private static readonly IReadOnlySet<string> SupportedEngines = new HashSet<string>(StringComparer.Ordinal)
    {
        "pdflatex",
        "lualatex",
    };

    private readonly RicisSemanticLatexTemplateRenderer _renderer;

    /// <summary>Creates a compiler with an optional semantic LaTeX renderer for model-to-PDF convenience calls.</summary>
    public RicisLatexPdfCompiler(RicisSemanticLatexTemplateRenderer renderer = null) =>
        _renderer = renderer ?? new RicisSemanticLatexTemplateRenderer();

    /// <summary>Renders a semantic report through an external template and compiles the resulting LaTeX to PDF.</summary>
    public RicisLatexPdfCompileResult CompileSemanticReport(
        RicisLatexReportViewModel model,
        string template,
        string outputDirectory,
        RicisLatexPdfCompileOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);
        return CompileLatex(model.DocumentId, _renderer.Render(model, template), outputDirectory, options ?? OptionsForTemplate(template));
    }

    /// <summary>Writes caller-owned LaTeX source to the supplied output directory and executes the configured two-pass compiler.</summary>
    public RicisLatexPdfCompileResult CompileLatex(
        string documentId,
        string latexSource,
        string outputDirectory,
        RicisLatexPdfCompileOptions options = null)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("A document identifier is required.", nameof(documentId));
        }

        ArgumentNullException.ThrowIfNull(latexSource);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        }

        var effective = options ?? new RicisLatexPdfCompileOptions();
        ValidateOptions(effective);
        Directory.CreateDirectory(outputDirectory);

        var fileStem = ToSafeFileStem(documentId);
        var latexPath = Path.Combine(outputDirectory, fileStem + ".tex");
        File.WriteAllText(latexPath, latexSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var exitCodes = new List<int>();
        var evidence = new List<string>();
        for (var pass = 0; pass < effective.PassCount; pass++)
        {
            var result = RunCompiler(effective.Engine, latexPath, outputDirectory, effective.TimeoutMillisecondsPerPass);
            exitCodes.Add(result.ExitCode);
            evidence.Add(Bound(result.StandardOutput + Environment.NewLine + result.StandardError, effective.MaxEvidenceCharacters));
            if (result.TimedOut)
            {
                throw new TimeoutException($"LaTeX compiler '{effective.Engine}' timed out on pass {pass + 1}.");
            }

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"LaTeX compiler '{effective.Engine}' failed on pass {pass + 1} with exit code {result.ExitCode}.");
            }
        }

        var pdfPath = Path.Combine(outputDirectory, fileStem + ".pdf");
        if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0)
        {
            throw new InvalidOperationException("LaTeX compiler completed without a non-empty PDF artifact.");
        }

        return new RicisLatexPdfCompileResult(
            pdfPath,
            latexPath,
            new RicisLatexPdfCompilationEvidence(effective.Engine, effective.PassCount, exitCodes, evidence));
    }

    private static (int ExitCode, bool TimedOut, string StandardOutput, string StandardError) RunCompiler(
        string engine,
        string latexPath,
        string outputDirectory,
        int timeoutMilliseconds)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = engine,
                WorkingDirectory = outputDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.StartInfo.ArgumentList.Add("-interaction=nonstopmode");
        process.StartInfo.ArgumentList.Add("-halt-on-error");
        process.StartInfo.ArgumentList.Add(latexPath);
        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        var timedOut = !process.WaitForExit(timeoutMilliseconds);
        if (timedOut)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }

        return (timedOut ? -1 : process.ExitCode, timedOut, standardOutput, standardError);
    }

    private static RicisLatexPdfCompileOptions OptionsForTemplate(string template)
    {
        var match = Regex.Match(template, @"^%\s*RICIS-ENGINE:\s*(?<engine>[a-z]+)\s*$", RegexOptions.Multiline);
        return match.Success
            ? new RicisLatexPdfCompileOptions(Engine: match.Groups["engine"].Value)
            : new RicisLatexPdfCompileOptions();
    }

    private static void ValidateOptions(RicisLatexPdfCompileOptions options)
    {
        if (!SupportedEngines.Contains(options.Engine))
        {
            throw new ArgumentOutOfRangeException(nameof(options.Engine), "Only pdflatex and lualatex are supported.");
        }

        if (options.PassCount is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(options.PassCount), "LaTeX pass count must be between one and three.");
        }

        if (options.TimeoutMillisecondsPerPass <= 0 || options.MaxEvidenceCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "LaTeX compiler timeouts and evidence bounds must be positive.");
        }
    }

    private static string ToSafeFileStem(string documentId)
    {
        var normalized = new string(documentId.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "ricis-report" : normalized;
    }

    private static string Bound(string value, int maxCharacters) =>
        value.Length <= maxCharacters ? value : value[..maxCharacters];
}
