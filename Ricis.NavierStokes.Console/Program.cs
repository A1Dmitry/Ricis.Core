using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

namespace Ricis.NavierStokes.ConsoleApp;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Any(argument => argument is "--help" or "-h"))
        {
            PrintHelp();
            return 0;
        }

        if (args.Length > 0 && !args.Contains("--stationary-vortex", StringComparer.Ordinal))
        {
            Console.Error.WriteLine("Неизвестный аргумент. Используйте --help.");
            return 2;
        }

        var velocity = new RicisVectorField3(
            (x, y, z, t) => -y,
            (x, y, z, t) => x,
            (x, y, z, t) => 0.0);
        Expression<Func<double, double, double, double, double>> pressure =
            (x, y, z, t) => ((x * x) + (y * y)) / 2.0;
        var proof = new StringBuilder();

        try
        {
            var result = velocity.ProveNavierStokesIdentity(pressure, 1.0, proof);
            Console.WriteLine(proof.ToString());
            Console.WriteLine($"Сертификат RICIS: несжимаемость и остаток = 0 → {result.IsCertified}.");
            return result.IsCertified ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Proof-сценарий не сертифицирован: {exception.Message}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Ricis.NavierStokes.Console");
        Console.WriteLine("  --stationary-vortex   вывести proof-документ для u=(-y,x,0), p=(x²+y²)/2, ν=1 (аргумент по умолчанию)");
        Console.WriteLine("  --help, -h            вывести эту справку");
    }
}
