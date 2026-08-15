using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Extensions;
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

        if (args.Length > 0 && string.Equals(args[0], "--derivative-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunDerivativeDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--structural-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunStructuralFunctionDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--integral-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunIntegralDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--sum-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunSumDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--proof-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunProofOperationsDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--academic-proof-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunAcademicProofDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--continuous-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunContinuousSugarDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--complex-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunComplexDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--interest-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunCompoundInterestDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--analytic-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunAnalyticSugarDemo();
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

    private static int RunDerivativeDemo()
    {
        var cases = new (string Name, Expression<Func<double, double>> Source,
                         Func<double, double> ClassicalDerivative, double[] Points)[]
        {
            ("t³", t => t * t * t, t => 3.0 * t * t, [-2.0, 0.0, 3.0]),
            ("sin(t²)+t³", t => Math.Sin(t * t) + Math.Pow(t, 3.0),
                t => 2.0 * t * Math.Cos(t * t) + 3.0 * t * t, [-1.0, 0.0, 2.0]),
            ("t²·sin(t)", t => (t * t) * Math.Sin(t),
                t => 2.0 * t * Math.Sin(t) + t * t * Math.Cos(t), [-1.0, 0.5, 2.0])
        };

        var allMatch = true;
        Console.WriteLine("Сравнение формальной производной RICIS с известной классической формулой:");
        Console.WriteLine("Лимиты и Лопиталь не используются; сравниваются исполненные производные.");

        foreach (var item in cases)
        {
            var derivative = item.Source.DxDt();
            var executeRicis = derivative.Compile();
            Console.WriteLine();
            Console.WriteLine($"F(t) = {item.Name}");
            Console.WriteLine($"  Исходное дерево: {item.Source}");
            Console.WriteLine($"  RICIS dF/dt:    {derivative}");
            Console.WriteLine("  t                 RICIS               классика            |Δ|");

            foreach (var point in item.Points)
            {
                var ricis = executeRicis(point);
                var classical = item.ClassicalDerivative(point);
                var delta = Math.Abs(ricis - classical);
                var match = !double.IsNaN(ricis) && !double.IsInfinity(ricis) && delta <= 1e-12;
                allMatch &= match;
                Console.WriteLine($"  {point,8:G4}  {ricis,20:G17}  {classical,20:G17}  {delta,8:G3} {(match ? "OK" : "FAIL")}");
            }
        }

        return allMatch ? 0 : 1;
    }

    private static int RunProofOperationsDemo()
    {
        Expression<Func<double, double>> f = x => x + 1.0;
        Expression<Func<double, double>> g = y => y - 1.0;
        Expression<Func<double, double>> identity = z => z / z;

        var composition = f.Compose(g);
        var application = f.At(g);
        var difference = f.Difference(f);
        var ratio = identity.Ratio(identity);
        var product = f.Product(g);

        Console.WriteLine("Доказательные expression-операции RICIS:");
        Console.WriteLine($"F(x):              {f}");
        Console.WriteLine($"G(y):              {g}");
        Console.WriteLine($"Compose(F, G):     {composition}");
        Console.WriteLine($"At(F, G):          {application}");
        Console.WriteLine($"Difference(F, F):  {difference}");
        Console.WriteLine($"Ratio(I, I):       {ratio}");
        Console.WriteLine($"Product(F, G):     {product}");
        Console.WriteLine();
        Console.WriteLine($"Compose при x=3:   {composition.Compile()(3.0):G17}");
        Console.WriteLine($"Product при x=3:   {product.Compile()(3.0):G17}");
        Console.WriteLine($"Difference при x=0:{difference.Compile()(0.0):G17}");
        Console.WriteLine($"Ratio при x=0:     {ratio.Compile()(0.0):G17}");
        return 0;
    }

    private static int RunAcademicProofDemo()
    {
        Expression<Func<double, bool>>[] conditions =
        [
            x => x >= -10.0,
        ];
        Expression<Func<double, bool>>[] constraints =
        [
            x => x != 5.0,
        ];
        Expression<Func<double, double>> claim = x => ((x * x) - 25.0) / (x - 5.0);
        var protocol = new StringBuilder();
        var derived = conditions.Prove(constraints, claim, protocol);

        Console.WriteLine("Академический доказательный протокол RICIS:");
        Console.WriteLine($"Исходный тезис: {claim}");
        Console.WriteLine($"Производное выражение: {derived}");
        Console.WriteLine();
        Console.WriteLine(protocol.ToString());
        Console.WriteLine($"Проверка производного дерева при x=2: {derived.Compile()(2.0):G17}");
        return 0;
    }

    private static int RunAnalyticSugarDemo()
    {
        Expression<Func<double, double>> shifted = x => x + 1.0;
        var sin = shifted.Sin();
        var exponential = shifted.Exp();
        var logarithm = shifted.Log();
        var squareRoot = shifted.Sqrt();
        var cube = shifted.Pow(3.0);
        var cubeDerivative = cube.DxDt();
        var hyperbolic = shifted.Tanh();

        Console.WriteLine("Аналитический математический сахар RICIS:");
        Console.WriteLine("  Каждый результат — явный Math.* expression-узел над нормализованной лямбдой; исходные делегаты не вызываются при построении.");
        Console.WriteLine($"F(x):          {shifted}");
        Console.WriteLine($"Sin(F):        {sin}");
        Console.WriteLine($"Exp(F):        {exponential}");
        Console.WriteLine($"Log(F):        {logarithm}");
        Console.WriteLine($"Sqrt(F):       {squareRoot}");
        Console.WriteLine($"Pow(F, 3):     {cube}");
        Console.WriteLine($"d Pow(F,3)/dx:{cubeDerivative}");
        Console.WriteLine($"Tanh(F):       {hyperbolic}");
        Console.WriteLine();
        Console.WriteLine("x       sin(F)      exp(F)      log(F)      sqrt(F)     F³         dF³/dx    tanh(F)");

        foreach (var point in new[] { 0.0, 1.0, 2.0 })
        {
            Console.WriteLine($"{point,4:G}  {sin.Compile()(point),10:G6}  {exponential.Compile()(point),10:G6}  " +
                              $"{logarithm.Compile()(point),10:G6}  {squareRoot.Compile()(point),10:G6}  " +
                              $"{cube.Compile()(point),8:G6}  {cubeDerivative.Compile()(point),9:G6}  {hyperbolic.Compile()(point),9:G6}");
        }

        return 0;
    }

    private static int RunCompoundInterestDemo()
    {
        Expression<Func<double, double>> principal = x => 1000.0 * x;
        Expression<Func<double, double>> rate = y => 2.5 * y;
        Expression<Func<double, double>> periods = z => z;
        var annualThreePeriods = principal.CompoundInterest(rate, 3);
        var deferredPeriods = principal.CompoundInterest(rate, periods);

        Console.WriteLine("Символическая формула сложного процента RICIS:");
        Console.WriteLine("  P = S · (1 + r/100)^n; S, r и при необходимости n остаются отложенными expression tree.");
        Console.WriteLine("  Формула строится символически и не является финансовым прогнозом или рекомендацией.");
        Console.WriteLine();
        Console.WriteLine($"S(x):                 {principal}");
        Console.WriteLine($"r(x), в процентах:    {rate}");
        Console.WriteLine($"P(x), n=3:            {annualThreePeriods}");
        Console.WriteLine($"P(x), n(x)=x:         {deferredPeriods}");
        Console.WriteLine();
        Console.WriteLine("x       S(x)      r(x)      P(x), n=3      P(x), n=x");

        foreach (var point in new[] { 1.0, 2.0, 3.0 })
        {
            Console.WriteLine($"{point,4:G}  {principal.Compile()(point),9:G7}  {rate.Compile()(point),8:G5}  " +
                              $"{annualThreePeriods.Compile()(point),13:G10}  {deferredPeriods.Compile()(point),13:G10}");
        }

        return 0;
    }

    private static int RunComplexDemo()
    {
        Expression<Func<double, double>> real = x => x + 1.0;
        Expression<Func<double, double>> imaginary = x => 2.0;
        Expression<Func<double, double>> otherReal = y => y - 1.0;
        Expression<Func<double, double>> otherImaginary = y => 3.0;

        var first = real.AsComplex(imaginary);
        var second = otherReal.AsComplex(otherImaginary);
        var conjugate = first.Conjugate();
        var product = first.Multiply(second);
        var squaredNorm = first.SquaredNorm();
        var norm = first.Norm();

        Console.WriteLine("Комплексные отложенные функции RICIS:");
        Console.WriteLine("  z(x) хранится как пара expression tree Re(z), Im(z); System.Numerics.Complex и делегаты не используются при построении.");
        Console.WriteLine($"z.Re(x):             {first.Re()}");
        Console.WriteLine($"z.Im(x):             {first.Im()}");
        Console.WriteLine($"conj(z).Re(x):       {conjugate.Re()}");
        Console.WriteLine($"conj(z).Im(x):       {conjugate.Im()}");
        Console.WriteLine($"(z·w).Re(x):         {product.Re()}");
        Console.WriteLine($"(z·w).Im(x):         {product.Im()}");
        Console.WriteLine($"|z|²:                {squaredNorm}");
        Console.WriteLine($"|z|:                 {norm}");
        Console.WriteLine();
        Console.WriteLine("x       Re(z)    Im(z)    Re(z·w)  Im(z·w)  |z|²     |z|");

        foreach (var point in new[] { -1.0, 0.0, 2.0 })
        {
            Console.WriteLine($"{point,4:G}  {first.Re().Compile()(point),8:G5}  {first.Im().Compile()(point),7:G5}  " +
                              $"{product.Re().Compile()(point),9:G5}  {product.Im().Compile()(point),9:G5}  " +
                              $"{squaredNorm.Compile()(point),7:G5}  {norm.Compile()(point),6:G5}");
        }

        return 0;
    }

    private static int RunContinuousSugarDemo()
    {
        Expression<Func<double, double>> f = x => 2.0 * x;
        Expression<Func<double, double>> g = y => y + 1.0;
        Expression<Func<double, double>> lower = z => z - 1.0;
        Expression<Func<double, double>> upper = u => u + 1.0;

        var absolute = f.Abs();
        var minimum = f.Min(g);
        var maximum = f.Max(g);
        var clamped = f.Clamp(lower, upper);
        var positive = f.PositivePart();
        var negative = f.NegativePart();
        var distance = f.Distance(g);

        Console.WriteLine("Непрерывный математический сахар RICIS:");
        Console.WriteLine("  Все результаты — чистые конечные expression tree; делегаты не вызываются при построении.");
        Console.WriteLine($"F(x):              {f}");
        Console.WriteLine($"G(y):              {g}");
        Console.WriteLine($"Abs(F):            {absolute}");
        Console.WriteLine($"Min(F, G):         {minimum}");
        Console.WriteLine($"Max(F, G):         {maximum}");
        Console.WriteLine($"Clamp(F, x-1, x+1):{clamped}");
        Console.WriteLine($"PositivePart(F):   {positive}");
        Console.WriteLine($"NegativePart(F):   {negative}");
        Console.WriteLine($"Distance(F, G):    {distance}");
        Console.WriteLine();
        Console.WriteLine("x       Abs(F)   Min(F,G)  Max(F,G)  Clamp     F⁺        F⁻        |F−G|");

        foreach (var point in new[] { -2.0, 0.0, 2.0 })
        {
            Console.WriteLine($"{point,4:G}  {absolute.Compile()(point),9:G5}  {minimum.Compile()(point),9:G5}  " +
                              $"{maximum.Compile()(point),9:G5}  {clamped.Compile()(point),9:G5}  " +
                              $"{positive.Compile()(point),9:G5}  {negative.Compile()(point),9:G5}  " +
                              $"{distance.Compile()(point),9:G5}");
        }

        return 0;
    }

    private static int RunSumDemo()
    {
        Expression<Func<double, double>> first = x => x + 1.0;
        Expression<Func<double, double>> second = y => y - 1.0;
        var sum = first.Sum(second);

        Console.WriteLine("Структурная Sum RICIS для двух отложенных лямбд:");
        Console.WriteLine($"F(x):       {first}");
        Console.WriteLine($"G(y):       {second}");
        Console.WriteLine($"Sum(F, G):  {sum}");
        Console.WriteLine($"Проверка x=2: {sum.Compile()(2.0):G17}");
        return 0;
    }

    private static int RunIntegralDemo()
    {
        Expression<Func<double, double>> strip = x => x + 1.0;
        Expression<Func<double, double>> deferredWidth = u => u - 1.0;
        var constantRange = strip.Integral(5.0);
        var symbolicRange = strip.Integral(deferredWidth);

        Console.WriteLine("Геометрический Integral RICIS через A6:");
        Console.WriteLine("  0_F · ∞_L → F·L; F и L остаются отложенными деревьями.");
        Console.WriteLine("  Ни предел, ни сумма Римана, ни первообразная не строятся.");
        Console.WriteLine();
        Console.WriteLine($"F(x):              {strip}");
        Console.WriteLine("L:                 5");
        Console.WriteLine($"Integral(F, 5):    {constantRange}");
        Console.WriteLine($"Проверка x=2:      {constantRange.Compile()(2.0):G17}");
        Console.WriteLine();
        Console.WriteLine($"L(x):              {deferredWidth}");
        Console.WriteLine($"Integral(F, L):    {symbolicRange}");
        Console.WriteLine($"Проверка x=3:      {symbolicRange.Compile()(3.0):G17}");
        return 0;
    }

    private static int RunStructuralFunctionDemo()
    {
        var inputs = new[]
        {
            "x => sign(x) / sign(x)",
            "x => clamp(x, -1, 1) / clamp(x, -1, 1)",
            "x => (abs(x) * (x + 1)) / abs(x)",
            "x => mod(x, 2) / mod(x, 2)",
            "x => (x % 2) / (x % 2)"
        };

        Console.WriteLine("Структурное сокращение одинаковых функций по L1/SP2:");
        Console.WriteLine("Классические нулевые точки не вычисляются до тождества F/F → 1.");
        Console.WriteLine();

        var failures = 0;
        foreach (var input in inputs)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"> {input}");
            Console.ResetColor();
            if (!ProcessExpression(input))
            {
                failures++;
            }
        }

        return failures == 0 ? 0 : 1;
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
        Console.WriteLine("  Функции: sin, cos, tan, sinh, cosh, tanh, exp, log, log10, sqrt, abs, sign, clamp, mod, pow.");
        Console.WriteLine("  Допустимы варианты Math.Sin(x) и sin(x); имена регистронезависимы.");
        Console.WriteLine("  Важно: степень записывайте как x^2 или pow(x, 2).");
        Console.WriteLine("  Ввод не компилируется как C# и не может вызывать произвольные методы.");
        Console.WriteLine("  all запускает все поддерживаемые примеры из каталога; в CLI используйте --all.");
        Console.WriteLine("  В CLI --author-seo-demo показывает SEO-блок при захвате внешней переменной about.");
        Console.WriteLine("  В CLI --derivative-demo показывает DxDt() как символьную перестройку без lim и Лопиталя.");
        Console.WriteLine("  В CLI --structural-demo показывает L1/SP2 для sign, clamp, abs и остатка.");
        Console.WriteLine("  В CLI --integral-demo показывает Integral(F, L) как геометрическое применение A6.");
        Console.WriteLine("  В CLI --sum-demo показывает Sum(F, G) для двух отложенных лямбд.");
        Console.WriteLine("  В CLI --proof-demo показывает Compose, At, Difference, Ratio и Product.");
        Console.WriteLine("  В CLI --academic-proof-demo записывает пошаговый академический вывод Prove в StringBuilder.");
        Console.WriteLine("  В CLI --continuous-demo показывает Abs, Min, Max, Clamp, части числа и Distance.");
        Console.WriteLine("  В CLI --complex-demo показывает Re, Im, сопряжение, произведение и норму комплексных функций.");
        Console.WriteLine("  В CLI --interest-demo показывает P=S·(1+r/100)^n как чистое expression-дерево.");
        Console.WriteLine("  В CLI --analytic-demo показывает аналитические Math.*-узлы и производную Pow(F,3).");
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
