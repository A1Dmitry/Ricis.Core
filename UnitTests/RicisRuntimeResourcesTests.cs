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
    public void RuntimeResources_UnsupportedCulture_FallsBackToEnglish()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-CA");
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
