using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;

internal static class RicisComplexSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("CPLX01: AsComplex — вещественная функция получает нулевую Im-компоненту", LiftRealFunction),
        ("CPLX02: AsComplex — Re и Im связываются с одним параметром", PairRebindsParameters),
        ("CPLX03: Conjugate — меняет знак Im без вычисления Re", ConjugateNegatesImaginaryPart),
        ("CPLX04: Add/Subtract — сохраняют покомпонентную комплексную алгебру", AddAndSubtractComponents),
        ("CPLX05: Multiply — реализует (ac−bd)+i(ad+bc)", MultiplyComponents),
        ("CPLX06: SquaredNorm — сохраняет точность BigInteger", SquaredNormPreservesBigInteger),
        ("CPLX07: Norm — строит sqrt(Re²+Im²) как дерево Math.Sqrt", DoubleNormBuildsSqrtTree),
    ];

    private static void LiftRealFunction()
    {
        Expression<Func<double, double>> real = x => x + 1.0;
        var complex = real.AsComplex();

        Require(Math.Abs(complex.Re().Compile()(2.0) - 3.0) < 1e-12,
            "Re(F+i·0) должен быть исходной вещественной функцией.");
        Require(complex.Im().Compile()(-100.0) == 0.0,
            "Im(F+i·0) должен быть типизированным нулём.");
    }

    private static void PairRebindsParameters()
    {
        Expression<Func<double, double>> real = x => x + 1.0;
        Expression<Func<double, double>> imaginary = y => 2.0 * y;
        var complex = real.AsComplex(imaginary);

        Require(ReferenceEquals(complex.Re().Parameters[0], complex.Im().Parameters[0]),
            "Комплексные компоненты обязаны иметь общий параметр expression tree.");
        Require(Math.Abs(complex.Im().Compile()(3.0) - 6.0) < 1e-12,
            "Привязка параметра мнимой части должна сохранить её значение.");
    }

    private static void ConjugateNegatesImaginaryPart()
    {
        Expression<Func<double, double>> real = x => x + 1.0;
        Expression<Func<double, double>> imaginary = x => 2.0 * x;
        var conjugate = real.AsComplex(imaginary).Conjugate();

        Require(Math.Abs(conjugate.Re().Compile()(3.0) - 4.0) < 1e-12,
            "Сопряжение не должно изменять Re-компоненту.");
        Require(Math.Abs(conjugate.Im().Compile()(3.0) + 6.0) < 1e-12,
            "Сопряжение должно инвертировать Im-компоненту.");
    }

    private static void AddAndSubtractComponents()
    {
        Expression<Func<double, double>> realA = x => x + 1.0;
        Expression<Func<double, double>> imaginaryA = x => 2.0;
        Expression<Func<double, double>> realB = y => y - 1.0;
        Expression<Func<double, double>> imaginaryB = y => 3.0;
        var first = realA.AsComplex(imaginaryA);
        var second = realB.AsComplex(imaginaryB);
        var sum = first.Add(second);
        var difference = first.Subtract(second);

        Require(Math.Abs(sum.Re().Compile()(2.0) - 4.0) < 1e-12 &&
                Math.Abs(sum.Im().Compile()(2.0) - 5.0) < 1e-12,
            "Сумма комплексных функций должна складывать компоненты.");
        Require(Math.Abs(difference.Re().Compile()(2.0) - 2.0) < 1e-12 &&
                Math.Abs(difference.Im().Compile()(2.0) + 1.0) < 1e-12,
            "Разность комплексных функций должна вычитать компоненты.");
    }

    private static void MultiplyComponents()
    {
        Expression<Func<double, double>> realA = x => x + 1.0;
        Expression<Func<double, double>> imaginaryA = x => 2.0;
        Expression<Func<double, double>> realB = y => y - 1.0;
        Expression<Func<double, double>> imaginaryB = y => 3.0;
        var product = realA.AsComplex(imaginaryA).Multiply(realB.AsComplex(imaginaryB));

        Require(Math.Abs(product.Re().Compile()(2.0) + 3.0) < 1e-12,
            $"Re((3+2i)(1+3i)) должно быть −3, получено {product.Re().Compile()(2.0)}.");
        Require(Math.Abs(product.Im().Compile()(2.0) - 11.0) < 1e-12,
            $"Im((3+2i)(1+3i)) должно быть 11, получено {product.Im().Compile()(2.0)}.");
    }

    private static void SquaredNormPreservesBigInteger()
    {
        Expression<Func<BigInteger, BigInteger>> real = value => value;
        Expression<Func<BigInteger, BigInteger>> imaginary = value => BigInteger.One;
        var squaredNorm = real.AsComplex(imaginary).SquaredNorm();
        var input = BigInteger.Parse("12345678901234567890123456789012345678901234567890");
        var actual = squaredNorm.Compile()(input);
        var expected = (input * input) + BigInteger.One;

        Require(actual == expected,
            "Квадрат нормы должен вычисляться в BigInteger без потери точности или преобразования к double.");
    }

    private static void DoubleNormBuildsSqrtTree()
    {
        Expression<Func<double, double>> real = x => 3.0;
        Expression<Func<double, double>> imaginary = x => 4.0;
        var norm = real.AsComplex(imaginary).Norm();

        Require(norm.Body is MethodCallExpression { Method.Name: nameof(Math.Sqrt) },
            $"Norm должен сохранить явный вызов Math.Sqrt, получено {norm.Body.NodeType}.");
        Require(Math.Abs(norm.Compile()(0.0) - 5.0) < 1e-12,
            $"Норма 3+4i должна быть 5, получено {norm.Compile()(0.0)}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
