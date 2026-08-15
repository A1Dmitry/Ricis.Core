using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents an immutable matrix of delayed scalar RICIS expressions.
/// Matrix entries share one parameter signature and remain ordinary scalar RICIS lambdas.
/// </summary>
/// <typeparam name="T">The scalar result type of every matrix entry.</typeparam>
public sealed class RicisMatrixExpression<T>
    where T : INumber<T>
{
    private readonly ReadOnlyCollection<ReadOnlyCollection<LambdaExpression>> _rows;

    /// <summary>
    /// Initializes a non-empty rectangular matrix from delayed scalar expressions.
    /// </summary>
    /// <param name="rows">The matrix rows, each containing scalar lambda expressions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the matrix is empty, ragged, or has incompatible entry signatures.</exception>
    public RicisMatrixExpression(IEnumerable<IEnumerable<LambdaExpression>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var copiedRows = rows.Select(row =>
        {
            ArgumentNullException.ThrowIfNull(row);
            return Array.AsReadOnly(row.ToArray());
        }).ToArray();

        if (copiedRows.Length == 0 || copiedRows[0].Count == 0)
            throw new ArgumentException("RICIS-матрица обязана иметь положительное число строк и столбцов.", nameof(rows));
        if (copiedRows.Any(row => row.Count != copiedRows[0].Count))
            throw new ArgumentException("Строки RICIS-матрицы должны иметь одинаковую длину.", nameof(rows));

        ValidateEntries(copiedRows);
        _rows = Array.AsReadOnly(copiedRows);
    }

    /// <summary>
    /// Gets the number of matrix rows.
    /// </summary>
    public int RowCount => _rows.Count;

    /// <summary>
    /// Gets the number of matrix columns.
    /// </summary>
    public int ColumnCount => _rows[0].Count;

    /// <summary>
    /// Gets the delayed entry at the specified zero-based row and column.
    /// </summary>
    public LambdaExpression this[int row, int column] => _rows[row][column];

    /// <summary>
    /// Gets an immutable view of the matrix rows.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<LambdaExpression>> Rows => _rows;

    /// <summary>
    /// Computes the normalized symbolic determinant of a 2×2 RICIS matrix.
    /// </summary>
    /// <returns>A delayed scalar lambda for <c>a·d−b·c</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the matrix is not 2×2.</exception>
    public LambdaExpression Determinant2x2()
    {
        if (RowCount != 2 || ColumnCount != 2)
            throw new InvalidOperationException("Determinant2x2 требует матрицу ровно 2×2.");

        var parameters = CreateParameters(this[0, 0].Parameters);
        var a = Rebind(this[0, 0], parameters);
        var b = Rebind(this[0, 1], parameters);
        var c = Rebind(this[1, 0], parameters);
        var d = Rebind(this[1, 1], parameters);
        return Normalize(Expression.Subtract(Expression.Multiply(a, d), Expression.Multiply(b, c)), parameters);
    }

    /// <summary>
    /// Computes the normalized symbolic determinant of a 3×3 RICIS matrix.
    /// </summary>
    /// <returns>A delayed scalar lambda expanded by the internal permutation formula.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the matrix is not 3×3.</exception>
    public LambdaExpression Determinant3x3()
    {
        if (RowCount != 3 || ColumnCount != 3)
            throw new InvalidOperationException("Determinant3x3 требует матрицу ровно 3×3.");

        var parameters = CreateParameters(this[0, 0].Parameters);
        var a = Rebind(this[0, 0], parameters);
        var b = Rebind(this[0, 1], parameters);
        var c = Rebind(this[0, 2], parameters);
        var d = Rebind(this[1, 0], parameters);
        var e = Rebind(this[1, 1], parameters);
        var f = Rebind(this[1, 2], parameters);
        var g = Rebind(this[2, 0], parameters);
        var h = Rebind(this[2, 1], parameters);
        var i = Rebind(this[2, 2], parameters);

        var positive = Add(
            Add(Multiply(a, Subtract(Multiply(e, i), Multiply(f, h))),
                Multiply(b, Subtract(Multiply(f, g), Multiply(d, i)))),
            Multiply(c, Subtract(Multiply(d, h), Multiply(e, g))));
        return Normalize(positive, parameters);

        Expression Zero() => Expression.Constant(T.Zero, typeof(T));
        Expression Multiply(Expression left, Expression right)
        {
            if (left.IsZero() || right.IsZero()) return Zero();
            if (left.IsOne()) return right;
            if (right.IsOne()) return left;
            return Expression.Multiply(left, right);
        }
        Expression Add(Expression left, Expression right)
        {
            if (left.IsZero()) return right;
            if (right.IsZero()) return left;
            return Expression.Add(left, right);
        }
        Expression Subtract(Expression left, Expression right)
        {
            if (right.IsZero()) return left;
            if (left.IsZero()) return Expression.Negate(right);
            return Expression.Subtract(left, right);
        }
    }

    /// <summary>
    /// Determines whether every matrix entry is a structural RICIS zero.
    /// </summary>
    public bool IsStructuralZero() => _rows.All(row => row.All(entry => entry.Body.IsZero()));

    /// <summary>
    /// Returns the matrix as rows of delayed coordinate records.
    /// </summary>
    public override string ToString() => $"[{string.Join("; ", _rows.Select(row => string.Join(", ", row)))}]";

    private static void ValidateEntries(IReadOnlyList<IReadOnlyList<LambdaExpression>> rows)
    {
        var first = rows[0][0];
        if (first.ReturnType != typeof(T)) throw new ArgumentException("Матрица содержит выражение с неверным типом результата.");
        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < rows[row].Count; column++)
            {
                var entry = rows[row][column];
                if (entry.ReturnType != typeof(T) || entry.Parameters.Count != first.Parameters.Count)
                    throw new ArgumentException("Все элементы матрицы должны иметь одинаковый тип результата и сигнатуру.");
                for (var parameter = 0; parameter < first.Parameters.Count; parameter++)
                    if (entry.Parameters[parameter].Type != first.Parameters[parameter].Type)
                        throw new ArgumentException("Все элементы матрицы должны иметь одинаковые типы параметров.");
            }
        }
    }

    private static ParameterExpression[] CreateParameters(IReadOnlyList<ParameterExpression> source) =>
        source.Select(parameter => Expression.Parameter(parameter.Type, parameter.Name ?? "x")).ToArray();

    private static Expression Rebind(LambdaExpression source, IReadOnlyList<ParameterExpression> target) =>
        new ParameterRebindVisitor(source.Parameters, target).Visit(source.Body)
        ?? throw new InvalidOperationException("Не удалось переназначить параметры матричного элемента.");

    private static LambdaExpression Normalize(Expression body, IReadOnlyList<ParameterExpression> parameters)
    {
        var lambda = Expression.Lambda(body, parameters);
        return RicisPhasePipeline.Simplify(lambda) as LambdaExpression
            ?? throw new InvalidOperationException("RICIS не сохранил матричный expression tree.");
    }

    private sealed class ParameterRebindVisitor : ExpressionVisitor
    {
        private readonly IReadOnlyList<ParameterExpression> _source;
        private readonly IReadOnlyList<ParameterExpression> _target;
        public ParameterRebindVisitor(IReadOnlyList<ParameterExpression> source, IReadOnlyList<ParameterExpression> target)
        {
            _source = source; _target = target;
        }
        protected override Expression VisitExtension(Expression node) => RicisSpecialExpressionRebinder.Rebind(node, Visit);
        protected override Expression VisitParameter(ParameterExpression node)
        {
            for (var i = 0; i < _source.Count; i++) if (ReferenceEquals(_source[i], node)) return _target[i];
            return base.VisitParameter(node);
        }
    }
}
