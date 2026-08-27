using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Resources;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class RicisRuntimeResourcesTests
{
    [TestMethod]
    public void RuntimeResources_RussianCulture_ReturnsRussianMessage()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
            Assert.AreEqual("Факториал определён только для неотрицательных целых чисел.", RicisRuntimeResources.FactorialNegative);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [TestMethod]
    public void RuntimeResources_ProjectCultures_ReturnLocalizedMessages()
    {
        var original = CultureInfo.CurrentUICulture;
        var expected = new Dictionary<string, string>
        {
            ["fr-CA"] = "Erreur de fournisseur inconnue.",
            ["de-DE"] = "Unbekannter Providerfehler.",
            ["hi-IN"] = "अज्ञात provider त्रुटि।",
            ["ms-MY"] = "Ralat penyedia tidak diketahui.",
        };
        try
        {
            foreach (var pair in expected)
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(pair.Key);
                Assert.AreEqual(pair.Value, RicisRuntimeResources.UnknownProviderError, pair.Key);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [TestMethod]
    public void RuntimeResources_UnsupportedCulture_FallsBackToEnglish()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
            Assert.AreEqual("Unknown provider error.", RicisRuntimeResources.UnknownProviderError);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [TestMethod]
    public void RuntimeResources_UnresolvedNode_FormatsNodeType()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            StringAssert.Contains(RicisRuntimeResources.UnresolvedRicisNode("ExampleNode"), "ExampleNode");
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
