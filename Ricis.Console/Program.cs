using System.Linq.Expressions;
using System.Text;
using Ricis.Core;
using Ricis.Core.Resources;
using Ricis.Core.Extensions;
using Ricis.Core.Metadata;
using Ricis.Core.Rationals;
using Ricis.Core.Phases;
using Ricis.Core.Proofs;

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

        if (args.Length > 0 && string.Equals(args[0], "--system-proof-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunSystemProofDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--riemann-proof-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunRiemannProofDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--lean-doc-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunLeanDocumentDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--lean-a6-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunLeanA6Demo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--jacobian-proof-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunJacobianProofDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--jacobian-proof-latex", StringComparison.OrdinalIgnoreCase))
        {
            return RunJacobianProofLatex();
        }

        if (args.Length > 0 && string.Equals(args[0], "--jacobian-proof-lean", StringComparison.OrdinalIgnoreCase))
        {
            return RunJacobianProofLean();
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

        if (args.Length > 0 && string.Equals(args[0], "--public-api-demo", StringComparison.OrdinalIgnoreCase))
        {
            return RunPublicApiDemo();
        }

        if (args.Length > 0 && string.Equals(args[0], "--expr", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.c9ff425fb2bd"));
                return 2;
            }

            return ProcessInput(string.Join(' ', args[1..])) ? 0 : 1;
        }

        // A non-option argument is a one-shot lambda expression or a semicolon-separated system.
        // Each expression is parsed and returned without entering the interactive ReadLine loop.
        if (args.Length > 0)
        {
            if (args[0].StartsWith("-", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"Неизвестный аргумент: {args[0]}. Используйте --help через интерактивный режим или --expr.");
                return 2;
            }

            return ProcessInput(string.Join(' ', args)) ? 0 : 1;
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

            ProcessInput(input);
        }
    }

    private static bool ProcessInput(string input)
    {
        var parts = input.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return ProcessExpression(parts[0]);
        }

        if (parts.Any(string.IsNullOrWhiteSpace))
        {
            Console.Error.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.e10a8da85bae"));
            return false;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Система RICIS: {parts.Length} выражений.");
        Console.ResetColor();

        var success = true;
        for (var index = 0; index < parts.Length; index++)
        {
            Console.WriteLine($"\n[{index + 1}/{parts.Length}] {parts[index]}");
            success &= ProcessExpression(parts[index]);
        }

        return success;
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
            Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.75af99602ada"));
            Console.ResetColor();
            Console.WriteLine($"  {source}");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.cf7f24147dc2"));
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
            Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.9b50323547f5"));
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
            Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.f36de4c64c02"));
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
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.047640924d4c"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.124fe58dd0a0"));

        foreach (var item in cases)
        {
            var derivative = item.Source.DxDt();
            var executeRicis = derivative.Compile();
            Console.WriteLine();
            Console.WriteLine($"F(t) = {item.Name}");
            Console.WriteLine($"  Исходное дерево: {item.Source}");
            Console.WriteLine($"  RICIS dF/dt:    {derivative}");
            Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.963f5d19d876"));

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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.814013884998"));
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

    private static int RunLeanDocumentDemo()
    {
        var document = RicisLeanTemplate.Render(
            new RicisLeanStructuredData(),
            new RicisLeanRequestedRows([RicisLeanProofRow.Id06ReflectedExactHalf]));
        Console.Write(document.Source);
        return 0;
    }

    private static int RunLeanA6Demo()
    {
        var document = RicisLeanTemplate.Render(
            new RicisLeanStructuredData(),
            new RicisLeanRequestedRows([RicisLeanProofRow.A6IndexedZeroInfinityBridge]));
        Console.Write(document.Source);
        return 0;
    }

    private static int RunJacobianProofDemo()
    {
        var scenario = RicisJacobianProofScenario.Create();
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.75cb2e2ae9d9"));
        Console.WriteLine($"Проверка lambda-тезиса: {scenario.ScalarProof.Proof.IsVerified}");
        Console.WriteLine($"Lambda-условий: {scenario.ScalarProof.Proof.Conditions.Count}; ограничений: {scenario.ScalarProof.Proof.Constraints.Count}");
        Console.WriteLine($"Typed trace entries: {scenario.ScalarProof.Trace.Count}");
        Console.WriteLine($"Structural singularity: {scenario.Jacobian.IsStructuralSingular}; A6 payload entries: {scenario.A6Payload.Count}");
        Console.WriteLine();
        Console.WriteLine(scenario.ScalarProof.GetDocument(RicisProofDocumentFormat.Json));
        return scenario.ScalarProof.Proof.IsVerified && scenario.Jacobian.IsStructuralSingular ? 0 : 1;
    }

    private static int RunJacobianProofLatex()
    {
        Console.Write(RicisJacobianProofScenario.Create().LatexSource);
        return 0;
    }

    private static int RunJacobianProofLean()
    {
        Console.Write(RicisJacobianProofScenario.Create().CombinedLeanSource);
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.90bdfaf1ceae"));
        Console.WriteLine($"Исходный тезис: {claim}");
        Console.WriteLine($"Производное выражение: {derived}");
        Console.WriteLine();
        Console.WriteLine(protocol.ToString());
        Console.WriteLine($"Проверка производного дерева при x=2: {derived.Compile()(2.0):G17}");
        return 0;
    }

    private static int RunSystemProofDemo()
    {
        Expression<Func<double, double, bool>>[] equations =
        [
            (x, y) => x + y == 5.0,
            (x, y) => x - y == 1.0,
        ];
        Expression<Func<double, double, bool>>[] constraints =
        [
            (x, y) => x >= 0.0 && y >= 0.0,
        ];
        Expression<Func<double, double, bool>> claim = (x, y) => x == 3.0;
        var protocol = new StringBuilder();
        var derived = equations.Prove(constraints, claim, protocol);

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.0adfe53f1bd6"));
        Console.WriteLine("  x + y = 5");
        Console.WriteLine("  x - y = 1");
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.c2f0f60dd597"));
        Console.WriteLine();
        Console.WriteLine(protocol.ToString());
        Console.WriteLine($"Проверка производного выражения: (x,y)=(3,2) → {derived.Compile()(3.0, 2.0)}; (2,3) → {derived.Compile()(2.0, 3.0)}");
        return 0;
    }

    private static int RunRiemannProofDemo()
    {
        Expression<Func<double, double, bool>>[] constraints =
        [
            (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0,
            (sigma, mirrorSigma) => mirrorSigma > 0.0 && mirrorSigma < 1.0,
        ];
        var exactHalf = Expression.Lambda<Func<double>>(
            Expression.Divide(Expression.Constant(1.0), Expression.Constant(2.0)));
        var sigma = Expression.Parameter(typeof(double), "sigma");
        var mirrorSigma = Expression.Parameter(typeof(double), "mirrorSigma");
        var claim = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(sigma, exactHalf.Body),
            sigma,
            mirrorSigma);
        var proofCase = new RiemannHypothesisProofCase(constraints, claim);
        var result = proofCase.Run();

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.fafa7294967f"));
        foreach (var entry in proofCase.Monitor)
        {
            Console.WriteLine($"[{entry.Status}] {entry.Stage}: {entry.Message}");
        }
        Console.WriteLine();
        Console.WriteLine(result.Document);
        var derived = proofCase.DerivedClaim
            ?? throw new InvalidOperationException("RH proof case did not produce a derived claim.");
        Console.WriteLine($"Статус: {result.Status}; производное выражение: {result.DerivedExpression}");
        Console.WriteLine($"Проверка производного выражения: (0.5,0.5) → {derived.Compile()(0.5, 0.5)}; (0.4,0.6) → {derived.Compile()(0.4, 0.6)}");
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.83ca1bbabd59"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.39bc002d3bd8"));
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.a528f2b07dcb"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.fe050f75a8da"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.ffff260af1e2"));
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.49681bb299cc"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.7d6f0f3dd89e"));
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.476fb2b3767c"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.82b7fa8525bd"));
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.74cae0371260"));
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.cc814e93871c"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.d1057e1d7090"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.13aa954183f4"));
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

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.40a0d1485ccf"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.908259110c38"));
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

    private static int RunPublicApiDemo()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var exactExpression = Expression.Add(Expression.Multiply(x, Expression.Constant(2.0)), Expression.Constant(1.0));
        var exactOk = exactExpression.TryEvaluate("x", Rational.Create(3), out var exactValue) && exactValue == Rational.Create(7);
        var quarter = CircleSectors.FromRadians(Math.PI / 2);
        var polar = PolarConverter.ExactSinCos(new Rational(1, 4));
        var collapsedSin = PolarConverter.TryCollapseTrig(nameof(Math.Sin), Math.PI / 2);
        var zero = NumericConstants.ZeroOf(typeof(int));
        var one = NumericConstants.OneOf(typeof(int));
        var left = new RicisType("Space");
        var right = new RicisType("Time");
        var tuple = RicisType.CreateTuple(right, left);
        var checks = new[]
        {
            (Name: "ExactEvaluator", Passed: exactOk),
            (Name: "CircleSectors", Passed: quarter.Fraction == new Rational(1, 4)),
            (Name: "PolarConverter", Passed: polar.sin == 1.0 && polar.cos == 0.0 && collapsedSin is ConstantExpression { Value: double value } && value == 1.0),
            (Name: "NumericConstants", Passed: zero.Value is int zeroValue && zeroValue == 0 && one.Value is int oneValue && oneValue == 1),
            (Name: "RicisType", Passed: tuple.Signature == "Tuple<Space,Time>" && RicisType.Operate(left, right, "*").Signature == "(Space*Time)"),
        };

        Console.WriteLine("RICIS public API utility demo:");
        Console.WriteLine($"  ExactEvaluator: {exactValue}");
        Console.WriteLine($"  CircleSectors: {quarter} / {quarter.InSectors(4)}");
        Console.WriteLine($"  PolarConverter: sin={polar.sin:G6}, cos={polar.cos:G6}");
        Console.WriteLine($"  NumericConstants: zero={zero.Value}, one={one.Value}");
        Console.WriteLine($"  RicisType tuple: {tuple}");
        foreach (var check in checks)
        {
            Console.WriteLine($"  {check.Name}: {(check.Passed ? "PASS" : "FAIL")}");
        }

        return checks.All(check => check.Passed) ? 0 : 1;
    }

    private static int RunAuthorSeoDemo()
    {
        // `about` is deliberately captured from the outer scope. The pipeline
        // detects that closure member and adds SEO metadata only to ToString.
        var about = AuthorSeoProfile.RicisAuthor;
        Expression<Func<double, double>> source = x => about != null ? x + 1 : x + 1;
        var derived = (Expression<Func<double, double>>)RicisPhasePipeline.Simplify(source);

        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.640615de9bc9"));
        Console.WriteLine($"  {source}");
        Console.WriteLine();
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.2b385a2b6882"));
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
            ("x => derivative(x ^ 3)", 2, 12),
            ("x => integral(x + 1, 5)", 2, 15),
            ("x => sum(x, 1)", 2, 3),
            ("x => compoundInterest(100, 10, 2)", 0, 121),
            ("x => distance(x, 5)", 2, 3),
            ("x => max(x, 0)", -2, 0),
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
            ? RicisLegacyTextResources.Get("runtime.legacy.14737b5f2c10")
            : $"Парсер: сбоев {failures}.");
        return failures == 0 ? 0 : 1;
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.d222cfd65123"));
        Console.ResetColor();
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.d04bc9ac037b"));
        Console.WriteLine();
    }

    private static void PrintHelp()
    {
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.7f2330d2bbc8"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.ffcd489e92b9"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.416d7d14588d"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.93bace57b4c8"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.8099ef03dba9"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.6ccb906783b8"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.f7d335bd5913"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.71ebe4b7d311"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.cc439dc3fc96"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.1f4a267baf74"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.4b1d29984c30"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.c89502be3567"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.f2f2fd6bfe9d"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.e17e26ee0562"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.e74ed7b0bca3"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.4481989d8c91"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.1d89bb7c27e3"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.9e25cdd21553"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.a96ae4768a3d"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.500cb2e02d86"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.96433f2f26aa"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.89a4282f9a92"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.f542732f1557"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.544a20f19ad7"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.762723409760"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.bc0190f24dca"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.92cb71bfe04f"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.47b9480a6fbd"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.262f67ae6e94"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.783f50c5c083"));
        Console.WriteLine();
        PrintExamples();
    }

    private static void PrintExamples()
    {
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.b575ce0c3c75"));
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
