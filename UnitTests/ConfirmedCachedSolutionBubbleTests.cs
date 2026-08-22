using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.CachedSolutions;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class ConfirmedCachedSolutionBubbleTests
{
    [TestMethod]
    public void StartupCatalogProducesFullyPopulatedConfirmed3DMapBubbles()
    {
        var bubbles = DefaultCachedSolutions.CreateIndex().ConfirmedBubbles;

        Assert.AreEqual(3, bubbles.Count);
        foreach (var bubble in bubbles)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.DisplayName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.Formula));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.ClassicalResult));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.RicisResult));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.SourceTest));
            Assert.IsTrue(Uri.TryCreate(bubble.SourceUrl, UriKind.Absolute, out _),
                $"Invalid source URL for {bubble.Id}.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.Explanation));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bubble.Notes));
            Assert.AreEqual("confirmed", bubble.Status);
            Assert.IsTrue(bubble.Tags.Count > 0);
        }
    }
}
