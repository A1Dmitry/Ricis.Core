using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Metadata;
using Ricis.Core.Phases;

namespace Ricis.ConsoleApp;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        if (args.Length > 0 && string.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
        {
            return RunSelfTest();
        }

        if (args.Length > 0 && string.Equals(args[0], "--all", StringComparison.OrdinalIgnoreCase))
        {
            return RunAllExamples();
        }

        if (args.Length > 0 && string.Equals(args[0], "--author-seo-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunAuthorSeoDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--expr", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("После --expr требуется строка выражения.");
                return 2;
            }

            return ProcessExpression(string.Join(' ', args[1..])) ? 0 : 1;
        }

        PrintBanner();
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("RICIS> ");
            Console.ResetColor();

            var input = Console.ReadLine();
            if (input is null)
            {
                Console.WriteLine();
                return 0;
            }

            input = input.Trim();
            if (input.Length == 0)
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            if (input.Equals("examples", StringComparison.OrdinalIgnoreCase))
            {
                PrintExamples();
                continue;
            }

            if (input.Equals("selftest", StringComparison.OrdinalIgnoreCase))
            {
                RunSelfTest();
                continue;
            }

            if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                RunAllExamples();
                continue;
            }

            ProcessExpression(input);
        }
    }

    private static bool ProcessExpression(string input)
    {
        try
        {
            var parser = new LambdaTextParser();
            var source = parser.Parse(input);
            var transformed = RicisPhasePipeline.Simplify(source);
            if (transformed is not Expression<Func<double, double>> derived)
            {
                throw new InvalidOperationException(
                    $"Конвейер должен вернуть Expression<Func<double,double>>, получено {transformed.GetType().Name}.");
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Исходная лямбда:");
            Console.ResetColor();
            Console.WriteLine($"  {source}");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Производная RICIS-лямбда:");
            Console.ResetColor();
            Console.WriteLine($"  {derived}");

            TryPrintEvaluation(derived);
            Console.WriteLine();
            return true;
        }
        catch (LambdaParseException error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка разбора: {error.Message}");
            Console.ResetColor();
            PrintPointer(input, error.Position);
            Console.WriteLine("Введите help для списка поддерживаемых операций.");
            Console.WriteLine();
            return false;
        }
        catch (Exception error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка RICIS: {error.Message}");
            Console.ResetColor();
            Console.WriteLine();
            return false;
        }
    }

    private static void TryPrintEvaluation(Expression<Func<double, double>> derived)
    {
        try
        {
            var compiled = derived.Compile();
            var values = new[] { -2.0, -1.0, 0.0, 1.0, 2.0 };
            var results = values.Select(value => $"{value:G} → {compiled(value):G17}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Значения производного дерева:");
            Console.ResetColor();
            Console.WriteLine($"  {string.Join("; ", results)}");
        }
        catch (Exception error)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Производное дерево остаётся символическим и не исполняется как double: {error.Message}");
            Console.ResetColor();
        }
    }

    private static int RunAuthorSeoDemo()
    {
        // `about` is deliberately captured from the outer scope. The pipeline
        // detects that closure member and adds SEO metadata only to ToString.
        var about = AuthorSeoProfile.RicisAuthor;
        Expression<Func<double, double>> source = x => about != null ? x + 1 : x + 1;
        var derived = (Expression<Func<double, double>>)RicisPhasePipeline.Simplify(source);

        Console.WriteLine("Исходная лямбда с захваченным about:");
        Console.WriteLine($"  {source}");
        Console.WriteLine();
        Console.WriteLine("Производная RICIS-лямбда с SEO-профилем автора:");
        Console.WriteLine($"  {derived}");
        Console.WriteLine();
        Console.WriteLine($"Проверка исполнения: x=2 → {derived.Compile()(2):G17}");
        return 0;
    }

    private static int RunAllExamples()
    {
        var parser = new LambdaTextParser();
        var failures = 0;
        Console.WriteLine($"Пакетный прогон: {ExampleCatalog.All.Count} входных выражений.");
        Console.WriteLine();

        foreach (var example in ExampleCatalog.All)
        {
            try
            {
                var source = parser.Parse(example.Input);
                var transformed = RicisPhasePipeline.Simplify(source);
                Console.WriteLine($"{example.Id,-3} | {example.Title}");
                Console.WriteLine($"  input:   {source}");
                Console.WriteLine($"  RICIS:   {transformed}");
            }
            catch (Exception error)
            {
                failures++;
                Console.WriteLine($"{example.Id,-3} | {example.Title}");
                Console.WriteLine($"  ERROR:   {error.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? $"Пакетный прогон завершён: {ExampleCatalog.All.Count}/{ExampleCatalog.All.Count} выражений обработано."
            : $"Пакетный прогон завершён: сбоев {failures} из {ExampleCatalog.All.Count}.");
        return failures == 0 ? 0 : 1;
    }

    private static int RunSelfTest()
    {
        var parser = new LambdaTextParser();
        var checks = new (string Text, double Input, double Expected)[]
        {
            ("x => x + 5", 5, 10),
            ("sin(pi / 2)", 0, 1),
            ("x => pow(x, 2) + 1", 3, 10),
            ("x => exp(x) - 1", 0, 0),
        };

        var failures = 0;
        foreach (var check in checks)
        {
            try
            {
                var value = parser.Parse(check.Text).Compile()(check.Input);
                if (Math.Abs(value - check.Expected) > 1e-12)
                {
                    throw new InvalidOperationException($"Ожидалось {check.Expected:G17}, получено {value:G17}.");
                }

                Console.WriteLine($"PASS: parser — {check.Text}");
            }
            catch (Exception error)
            {
                failures++;
                Console.Error.WriteLine($"FAIL: parser — {check.Text}: {error.Message}");
            }
        }

        Console.WriteLine(failures == 0
            ? "Парсер: все встроенные проверки успешно пройдены."
            : $"Парсер: сбоев {failures}.");
        return failures == 0 ? 0 : 1;
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("RICIS Console — интерактивный разбор и преобразование лямбд");
        Console.ResetColor();
        Console.WriteLine("Введите help для синтаксиса, examples для каталога, all для пакетного прогона, exit для выхода.");
        Console.WriteLine();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Поддерживаемый безопасный синтаксис:");
        Console.WriteLine("  [x =>] выражение");
        Console.WriteLine("  Операторы: +, -, *, /, ^ и круглые скобки.");
        Console.WriteLine("  Константы: pi, e.");
        Console.WriteLine("  Функции: sin, cos, tan, sinh, cosh, tanh, exp, log, log10, sqrt, abs, pow.");
        Console.WriteLine("  Допустимы варианты Math.Sin(x) и sin(x); имена регистронезависимы.");
        Console.WriteLine("  Важно: степень записывайте как x^2 или pow(x, 2).");
        Console.WriteLine("  Ввод не компилируется как C# и не может вызывать произвольные методы.");
        Console.WriteLine("  all запускает все поддерживаемые примеры из каталога; в CLI используйте --all.");
        Console.WriteLine("  В CLI --author-seo-demo показывает SEO-блок при захвате внешней переменной about.");
        Console.WriteLine();
        PrintExamples();
    }

    private static void PrintExamples()
    {
        Console.WriteLine("Примеры:");
        foreach (var example in ExampleCatalog.All)
        {
            Console.WriteLine($"  {example.Id}: {example.Input} — {example.Title}");
        }

        Console.WriteLine();
    }

    private static void PrintPointer(string input, int position)
    {
        Console.WriteLine($"  {input}");
        Console.WriteLine($"  {new string(' ', Math.Clamp(position, 0, input.Length))}^");
    }
}
