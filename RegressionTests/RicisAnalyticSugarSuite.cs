using System.Linq.Expressions;
using Ricis.Core.Extensions;

internal static class RicisAnalyticSugarSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("AN01: Sin/Cos/Tan — строят явные Math-узлы", TrigonometricNodes),
        ("AN02: Exp/Log/Log10/Sqrt — сохраняют аналитические значения", ExponentialLogarithmicAndRootValues),
        ("AN03: Sinh/Cosh/Tanh — строят гиперболические узлы", HyperbolicNodes),
        ("AN04: Pow — постоянный показатель совместим с DxDt", ConstantPowerWorksWithDerivative),
        ("AN05: Pow — отложенный показатель связывается с параметром", DeferredPowerRebindsParameter),
        ("AN06: Аналитический сахар — L1 нормализует вход до Math-узла", AnalyticInputUsesPhaseZeroIdentity),
    ];

    private static void TrigonometricNodes()
    {
        Expression<Func<double, double>> identity = x => x;
        var sin = identity.Sin();
        var cos = identity.Cos();
        var tan = identity.Tan();

        Require(sin.Body is MethodCallExpression { Method.Name: nameof(Math.Sin) }, "Sin должен строить Math.Sin-узел.");
        Require(cos.Body is MethodCallExpression { Method.Name: nameof(Math.Cos) }, "Cos должен строить Math.Cos-узел.");
        Require(tan.Body is MethodCallExpression { Method.Name: nameof(Math.Tan) }, "Tan должен строить Math.Tan-узел.");
        Require(Math.Abs(sin.Compile()(Math.PI / 2.0) - 1.0) < 1e-12, "sin(π/2) должен быть 1.");
        Require(Math.Abs(cos.Compile()(0.0) - 1.0) < 1e-12, "cos(0) должен быть 1.");
        Require(Math.Abs(tan.Compile()(0.0)) < 1e-12, "tan(0) должен быть 0.");
    }

    private static void ExponentialLogarithmicAndRootValues()
    {
        Expression<Func<double, double>> identity = x => x;
        var exp = identity.Exp();
        var log = identity.Log();
        var log10 = identity.Log10();
        var sqrt = identity.Sqrt();

        Require(Math.Abs(exp.Compile()(1.0) - Math.E) < 1e-12, "exp(1) должен быть e.");
        Require(Math.Abs(log.Compile()(Math.E) - 1.0) < 1e-12, "log(e) должен быть 1.");
        Require(Math.Abs(log10.Compile()(1000.0) - 3.0) < 1e-12, "log10(1000) должен быть 3.");
        Require(Math.Abs(sqrt.Compile()(81.0) - 9.0) < 1e-12, "sqrt(81) должен быть 9.");
    }

    private static void HyperbolicNodes()
    {
        Expression<Func<double, double>> identity = x => x;
        var sinh = identity.Sinh();
        var cosh = identity.Cosh();
        var tanh = identity.Tanh();

        Require(sinh.Body is MethodCallExpression { Method.Name: nameof(Math.Sinh) }, "Sinh должен строить Math.Sinh-узел.");
        Require(cosh.Body is MethodCallExpression { Method.Name: nameof(Math.Cosh) }, "Cosh должен строить Math.Cosh-узел.");
        Require(tanh.Body is MethodCallExpression { Method.Name: nameof(Math.Tanh) }, "Tanh должен строить Math.Tanh-узел.");
        Require(Math.Abs(sinh.Compile()(0.0)) < 1e-12, "sinh(0) должен быть 0.");
        Require(Math.Abs(cosh.Compile()(0.0) - 1.0) < 1e-12, "cosh(0) должен быть 1.");
        Require(Math.Abs(tanh.Compile()(0.0)) < 1e-12, "tanh(0) должен быть 0.");
    }

    private static void ConstantPowerWorksWithDerivative()
    {
        Expression<Func<double, double>> identity = x => x;
        var cube = identity.Pow(3.0);
        var derivative = cube.DxDt();

        Require(cube.Body is MethodCallExpression { Method.Name: nameof(Math.Pow) }, "Pow(3) должен строить Math.Pow-узел.");
        Require(Math.Abs(cube.Compile()(2.0) - 8.0) < 1e-12, "2^3 должно быть 8.");
        Require(Math.Abs(derivative.Compile()(2.0) - 12.0) < 1e-12,
            $"d(x^3)/dx при x=2 должно быть 12, получено {derivative.Compile()(2.0)}.");
    }

    private static void DeferredPowerRebindsParameter()
    {
        Expression<Func<double, double>> @base = x => x + 1.0;
        Expression<Func<double, double>> exponent = y => y;
        var power = @base.Pow(exponent);

        Require(power.Body is MethodCallExpression { Method.Name: nameof(Math.Pow) },
            "Pow(F,G) должен сохранить явный Math.Pow-узел.");
        Require(Math.Abs(power.Compile()(2.0) - 9.0) < 1e-12,
            $"(x+1)^x при x=2 должно быть 9, получено {power.Compile()(2.0)}.");
    }

    private static void AnalyticInputUsesPhaseZeroIdentity()
    {
        Expression<Func<double, double>> identity = x => x / x;
        var exponential = identity.Exp();

        Require(Math.Abs(exponential.Compile()(0.0) - Math.E) < 1e-12,
            "L1 должен превратить x/x в 1 до построения exp(x/x), включая x=0.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
