using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class LeanAgentAccessTests
{
    [TestMethod]
    public void AgentKnowledgeContractExposesLeanArtifactsThroughManifest()
    {
        var root = ProjectRoot();
        var leanRoot = Path.Combine(root, "FormalVerification", "Lean");
        var manifestPath = Path.Combine(leanRoot, "Artifacts", "manifest.json");
        var readmePath = Path.Combine(leanRoot, "README.md");
        var toolchainPath = Path.Combine(leanRoot, "lean-toolchain");

        Assert.IsTrue(File.Exists(manifestPath));
        Assert.IsTrue(File.Exists(readmePath));
        Assert.IsTrue(File.Exists(toolchainPath));
        Assert.IsTrue(File.ReadAllText(readmePath).Contains("lake env lean", StringComparison.Ordinal));
        Assert.IsFalse(string.IsNullOrWhiteSpace(File.ReadAllText(toolchainPath)));

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var artifacts = document.RootElement.GetProperty("artifacts");
        Assert.IsTrue(artifacts.GetArrayLength() > 0);

        foreach (var artifact in artifacts.EnumerateArray())
        {
            var source = artifact.GetProperty("source").GetString();
            var sourcePath = Path.Combine(root, source!.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(sourcePath), $"Lean artifact is not accessible: {source}");

            var knowledge = artifact.GetProperty("knowledgeSource");
            Assert.IsTrue(knowledge.GetProperty("mandatoryForModelStudy").GetBoolean(), source);
            Assert.AreEqual("mandatory-project-knowledge-source",
                knowledge.GetProperty("role").GetString(), source);
        }
    }

    [TestMethod]
    public void LeanArtifactsAreSafeKnowledgeSourcesWithoutSorryOrAdmit()
    {
        var root = ProjectRoot();
        var artifactDirectory = Path.Combine(root, "FormalVerification", "Lean", "Artifacts");
        var sources = Directory.EnumerateFiles(artifactDirectory, "*.lean", SearchOption.AllDirectories).ToArray();

        Assert.IsTrue(sources.Length > 0);
        foreach (var source in sources)
        {
            var text = File.ReadAllText(source);
            Assert.IsFalse(text.Contains("sorry", StringComparison.OrdinalIgnoreCase), source);
            Assert.IsFalse(text.Contains("admit", StringComparison.OrdinalIgnoreCase), source);
        }
    }

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ricis.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Ricis.Core solution root was not found.");
    }
}
