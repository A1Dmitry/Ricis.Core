using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

internal static class RicisMatrixExpressionSuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("MEX01: матрица хранит общую сигнатуру элементов", StoresCommonSignature),
        ("MEX02: determinant 2x2 строится как RICIS expression", BuildsTwoByTwoDeterminant),
        ("MEX03: нулевая матрица распознаётся структурно", RecognizesStructuralZero),
        ("MEX04: determinant отклоняет матрицу не 2x2", RejectsNonTwoByTwoDeterminant),
        ("MEX05: determinant 3x3 нормализует треугольную матрицу в 1", BuildsThreeByThreeDeterminant),
        ("API18: matrix exposes immutable row view", ExposesRows),
    ];

    private static void StoresCommonSignature()
    {
        var matrix = CreateJacobianMatrix();
        Require(matrix.RowCount == 2 && matrix.ColumnCount == 2, "Якобианная матрица должна быть 2×2.");
        Require(matrix[0, 0].Parameters.Count == 2, "Элементы должны иметь общую двумерную сигнатуру.");
    }

    private static void BuildsTwoByTwoDeterminant()
    {
        var determinant = CreateJacobianMatrix().Determinant2x2();
        Require(determinant.ReturnType == typeof(double), "Определитель должен возвращать scalar T.");
        Require(determinant.ToString().Contains("1"), "Для треугольного якобиана в записи должен присутствовать единичный результат.");
    }

    private static void RecognizesStructuralZero()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var zero = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        var matrix = new RicisMatrixExpression<double>([[zero, zero], [zero, zero]]);
        Require(matrix.IsStructuralZero(), "Матрица из нулевых RICIS-элементов должна быть структурным нулём.");
    }

    private static void RejectsNonTwoByTwoDeterminant()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var one = Expression.Lambda<Func<double, double, double>>(Expression.Constant(1.0), x, y);
        var matrix = new RicisMatrixExpression<double>([[one, one, one], [one, one, one]]);
        RegressionAssertions.Expect<InvalidOperationException>(() => matrix.Determinant2x2(), "Определитель 2×2 должен отклонять матрицу 2×3.");
    }

    private static void BuildsThreeByThreeDeterminant()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var z = Expression.Parameter(typeof(double), "z");
        var one = Expression.Lambda<Func<double, double, double, double>>(Expression.Constant(1.0), x, y, z);
        var zero = Expression.Lambda<Func<double, double, double, double>>(Expression.Constant(0.0), x, y, z);
        var firstOffDiagonal = Expression.Lambda<Func<double, double, double, double>>(Expression.Multiply(Expression.Constant(2.0), y), x, y, z);
        var secondOffDiagonal = Expression.Lambda<Func<double, double, double, double>>(Expression.Constant(2.0), x, y, z);
        var matrix = new RicisMatrixExpression<double>([
            [one, firstOffDiagonal, zero],
            [zero, one, secondOffDiagonal],
            [zero, zero, one]
        ]);
        var determinant = matrix.Determinant3x3();
        Require(determinant.Body.IsOne(), $"Треугольный determinant 3×3 должен дать 1, получено: {determinant}");
    }

    private static void ExposesRows()
    {
        var matrix = CreateJacobianMatrix();

        RegressionAssertions.Require(matrix.Rows.Count == matrix.RowCount, "Rows должен отражать точное число строк матрицы.");
        RegressionAssertions.Require(
            matrix.Rows[0].Count == matrix.ColumnCount && ReferenceEquals(matrix.Rows[0][0], matrix[0, 0]),
            "Rows должен предоставлять исходные lambda coordinates без потери структуры.");
    }

    private static RicisMatrixExpression<double> CreateJacobianMatrix()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var pPrime = Expression.Add(Expression.Multiply(Expression.Constant(3.0), Expression.Multiply(y, y)), Expression.Constant(2.0));
        var one = Expression.Lambda<Func<double, double, double>>(Expression.Constant(1.0), x, y);
        var zero = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        var derivative = Expression.Lambda<Func<double, double, double>>(pPrime, x, y);
        return new RicisMatrixExpression<double>([[one, derivative], [zero, one]]);
    }


    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
