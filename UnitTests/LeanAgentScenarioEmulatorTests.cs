using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.AgentSimulation;
using Ricis.Core.Expressions;

namespace Ricis.Core.UnitTests;

[TestClass]
public sealed class LeanAgentScenarioEmulatorTests
{

    [TestMethod]
    public void EmulatorUsesRicisCoreForQuadraticPoleInsteadOfClassicalCancellation()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var formula = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(1d), Expression.Subtract(Expression.Power(x, Expression.Constant(2d)), Expression.Constant(4d))),
            x);
        var scenario = new LeanAgentAcademicScenario(
            "academic-quadratic-pole",
            "Квадратичный полюс",
            formula,
            "Классическая дробь не определена при x=−2 и x=2",
            "Сохранить оба сингулярных ключа при глубокой редукции.");

        var result = CreateEmulator().Run(scenario);

        Assert.IsTrue(result.RicisResult.Body is InfinityExpression infinity && infinity.Roots.Count == 2,
            $"Expected two indexed RICIS roots, got {result.RicisResult}.");
        Assert.IsTrue(result.Trace.Any(entry => entry.Stage == "RICIS.Core" && entry.Action.Contains("Фаза 2", StringComparison.Ordinal)));
        EmitTrace(result);
    }

    [TestMethod]
    public void EmulatorKeepsOscillatoryAcademicSingularityDeferred()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var reciprocal = Expression.Divide(Expression.Constant(1d), x);
        var formula = Expression.Lambda<Func<double, double>>(
            Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, reciprocal),
            x);
        var scenario = new LeanAgentAcademicScenario(
            "academic-oscillatory-limit",
            "Осциллирующая сингулярность sin(1/x)",
            formula,
            "Классический двусторонний предел при x→0 не существует",
            "Не подменять осцилляцию ложным числом; индексировать сингулярный аргумент.");

        var result = CreateEmulator().Run(scenario);

        Assert.IsTrue(result.RicisResult.Body is MethodCallExpression
        {
            Method.Name: nameof(Math.Sin),
            Arguments: [InfinityExpression]
        }, $"Expected Sin around indexed infinity, got {result.RicisResult}.");
        Assert.IsTrue(result.Trace.Any(entry => entry.Action == "Deep reduction complete" && entry.Verified));
        EmitTrace(result);
    }

    private static LeanAgentScenarioEmulator CreateEmulator() => new(ProjectRoot());

    private static void EmitTrace(LeanAgentScenarioResult result)
    {
        foreach (var entry in result.Trace)
        {
            Console.WriteLine($"[AGENT-TRACE] {result.Scenario.Id} #{entry.Sequence} | {entry.Stage} | {entry.Action} | {entry.Details} | {entry.Before} => {entry.After} | verified={entry.Verified}");
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
