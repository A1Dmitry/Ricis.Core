if (args.Length == 1 && string.Equals(args[0], "--list-tests", StringComparison.Ordinal))
{
    foreach (var (name, _) in RicisRegressionTestCatalog.Tests)
    {
        Console.WriteLine(name);
    }

    return;
}

var failures = new List<string>();

foreach (var (name, body) in RicisRegressionTestCatalog.Tests)
{
    try
    {
        body();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"FAIL: {name}\n  {ex}");
        Console.WriteLine(failures[^1]);
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"\n{failures.Count} regression test(s) failed.");
    Environment.Exit(1);
}

Console.WriteLine($"\nAll {RicisRegressionTestCatalog.Tests.Count} regression tests passed.");
