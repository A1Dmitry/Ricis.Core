using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ricis.Core.UnitTests;

/// <summary>
/// MSTest adapter for the canonical RICIS regression catalog.
/// Test bodies remain owned by <see cref="RicisRegressionTestCatalog"/> and are
/// executed unchanged by both the console runner and Test Explorer.
/// </summary>
[TestClass]
public sealed class RicisRegressionCatalogMSTestAdapter
{
    public static IEnumerable<object[]> Cases() =>
        RicisRegressionTestCatalog.Tests.Select(test => new object[] { test.Name, test.Body });

    public static string DisplayName(MethodInfo _, object[] data) => (string)data[0];

    [DataTestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Cases), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(DisplayName))]
    public void CanonicalRegressionCase(string _, Action body)
    {
        body();
    }
}
