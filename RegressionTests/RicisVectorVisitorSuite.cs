using System.Linq.Expressions;
using Ricis.Core.Expressions;

internal static class RicisVectorVisitorSuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("VVIS01: Visitor доказывает G∘F=Id₃", ProvesOuterInnerIdentity),
        ("VVIS02: Visitor доказывает F∘G=Id₃", ProvesInnerOuterIdentity),
        ("VVIS03: Visitor отклоняет ложное векторное тождество", RejectsFalseIdentity),
        ("VVIS04: Visitor оставляет residual как vector expression", KeepsResidualAsVector),
    ];

    private static void ProvesOuterInnerIdentity()
    {
        var (f, g) = CreatePair();
        var composition = RicisVectorExpression<double>.Compose(g, f);
        var proof = new RicisVectorExpressionVisitor<double>().ProveIdentity(composition, new RicisVectorExpressionVisitor<double>().Identity(composition));
        Require(proof.IsProved && proof.Residual.IsStructuralZero(), $"G∘F должен дать 0⃗: {proof}");
    }

    private static void ProvesInnerOuterIdentity()
    {
        var (f, g) = CreatePair();
        var composition = RicisVectorExpression<double>.Compose(f, g);
        var visitor = new RicisVectorExpressionVisitor<double>();
        var proof = visitor.ProveIdentity(composition, visitor.Identity(composition));
        Require(proof.IsProved && proof.Residual.IsStructuralZero(), $"F∘G должен дать 0⃗: {proof}");
    }

    private static void RejectsFalseIdentity()
    {
        var (f, _) = CreatePair();
        var visitor = new RicisVectorExpressionVisitor<double>();
        var proof = visitor.ProveIdentity(f, visitor.Identity(f));
        Require(!proof.IsProved, "Невыраженное отображение F не должно отождествляться с Id₃.");
    }

    private static void KeepsResidualAsVector()
    {
        var (f, _) = CreatePair();
        var visitor = new RicisVectorExpressionVisitor<double>();
        var residual = visitor.Residual(f, visitor.Identity(f));
        Require(residual.Dimension == 3 && residual.ParameterCount == 3, "Residual должен оставаться 3D vector expression.");
    }

    private static (RicisVectorExpression<double> F, RicisVectorExpression<double> G) CreatePair()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var z = Expression.Parameter(typeof(double), "z");
        var b = Expression.Add(Expression.Multiply(Expression.Constant(2.0), z), Expression.Constant(3.0));
        var a = Expression.Add(Expression.Multiply(y, y), Expression.Constant(1.0));
        var shiftedY = Expression.Subtract(y, b);
        var shiftedA = Expression.Add(Expression.Multiply(shiftedY, shiftedY), Expression.Constant(1.0));
        var f = new RicisVectorExpression<double>([
            Expression.Lambda<Func<double, double, double, double>>(Expression.Add(x, a), x, y, z),
            Expression.Lambda<Func<double, double, double, double>>(Expression.Add(y, b), x, y, z),
            Expression.Lambda<Func<double, double, double, double>>(z, x, y, z)
        ]);
        var g = new RicisVectorExpression<double>([
            Expression.Lambda<Func<double, double, double, double>>(Expression.Subtract(x, shiftedA), x, y, z),
            Expression.Lambda<Func<double, double, double, double>>(Expression.Subtract(y, b), x, y, z),
            Expression.Lambda<Func<double, double, double, double>>(z, x, y, z)
        ]);
        return (f, g);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
