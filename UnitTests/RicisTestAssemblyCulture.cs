using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.MethodLevel)]

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class RicisTestAssemblyCulture
{
    [AssemblyInitialize]
    public static void Initialize(TestContext _)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
    }
}
