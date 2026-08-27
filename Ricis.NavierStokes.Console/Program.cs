using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Resources;
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
            Console.Error.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.ac92022f5033"));
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
            Console.WriteLine(RicisLegacyTextResources.Format("runtime.legacy.f57195db7e4a", ("result.IsCertified", result.IsCertified)));
            return result.IsCertified ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(RicisLegacyTextResources.Format("runtime.legacy.88a55d591990", ("exception.Message", exception.Message)));
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Ricis.NavierStokes.Console");
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.3cbc5ad69169"));
        Console.WriteLine(RicisLegacyTextResources.Get("runtime.legacy.fbcb0fd793ec"));
    }
}
