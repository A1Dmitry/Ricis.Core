using System.Linq.Expressions;

namespace Ricis.Core.Solvers.ZeroSolver;

public static class TrigonometricZeroSolver
{
    public static List<Root> FindTrigonometricRoots(this Expression expr, ParameterExpression param)
    {
        var roots = new List<Root>();

        if (expr is not MethodCallExpression call ||
            call.Method.DeclaringType != typeof(Math) ||
            call.Arguments.Count != 1) return roots;
        var arg = call.Arguments[0];

        // Извлекаем линейный коэффициент: k*x + b
        var linear = arg.ExtractLinear(param);
        if (linear == null)
        {
            return roots;
        }

        var valueTuple = linear.Value;
        var multiplier = valueTuple.multiplier;
        var offset = valueTuple.offset;

        var baseAngle = call.Method.Name switch
        {
            "Sin" => 0.0 + Math.PI, // sin(θ) = 0 ⇒ θ = kπ (главное — π)
            "Cos" => Math.PI / 2, // cos(θ) = 0 ⇒ θ = π/2 + kπ
            "Tan" => Math.PI, // tan(θ) = 0 ⇒ θ = kπ (но tan имеет полюса!)
            _ => double.NaN
        };

        if (double.IsNaN(baseAngle))
        {
            return roots;
        }

        // Главное значение (k=0)
        var theta = baseAngle - offset;
        if (!(Math.Abs(multiplier) > 0)) return roots;
        var x = theta / multiplier;
        roots.Add(new Root(param, x));

        return roots;
    }
}