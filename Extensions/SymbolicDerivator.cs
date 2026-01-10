using System.Linq.Expressions;

namespace Ricis.Core.Extensions;

// <summary>
/// Реализация символьного дифференцирования для SP3 (L'Hôpital's Rule).
/// Расширяет Expression методами взятия производной.
/// </summary>
public static class SymbolicDerivator
{
    public static Expression Derive(this Expression expr, ParameterExpression param)
    {
        return Simplify(DeriveRecursive(expr, param));
    }

    private static Expression DeriveRecursive(Expression expr, ParameterExpression param)
    {
        if (expr is ConstantExpression) return Expression.Constant(0.0);

        if (expr is ParameterExpression p)
        {
            // FIX: Сравниваем по имени, так как экземпляры могут отличаться
            return (p == param || p.Name == param.Name) ? Expression.Constant(1.0) : Expression.Constant(0.0);
        }

        if (expr is UnaryExpression unary)
        {
            if (unary.NodeType == ExpressionType.Negate)
                return Expression.Negate(DeriveRecursive(unary.Operand, param));
            if (unary.NodeType == ExpressionType.Convert)
                return DeriveRecursive(unary.Operand, param);
        }

        if (expr is BinaryExpression binary)
        {
            var u = binary.Left;
            var v = binary.Right;
            var du = DeriveRecursive(u, param);
            var dv = DeriveRecursive(v, param);

            switch (binary.NodeType)
            {
                case ExpressionType.Add: return Expression.Add(du, dv);
                case ExpressionType.Subtract: return Expression.Subtract(du, dv);
                case ExpressionType.Multiply:
                    return Expression.Add(Expression.Multiply(du, v), Expression.Multiply(u, dv));
                case ExpressionType.Divide:
                    var num = Expression.Subtract(Expression.Multiply(du, v), Expression.Multiply(u, dv));
                    var den = Expression.Multiply(v, v);
                    return Expression.Divide(num, den);
            }
        }

        if (expr is MethodCallExpression call) return DeriveMethod(call, param);

        return Expression.Constant(0.0);
    }

    private static Expression DeriveMethod(MethodCallExpression node, ParameterExpression param)
    {
        var method = node.Method.Name;
        // FIX: Снимаем Convert с аргумента перед рекурсией
        var arg = Unwrap(node.Arguments[0]);
        var dArg = DeriveRecursive(arg, param);

        Expression derivative = null;

        switch (method)
        {
            case "Sin": derivative = Expression.Call(typeof(Math).GetMethod("Cos"), arg); break;
            case "Cos": derivative = Expression.Negate(Expression.Call(typeof(Math).GetMethod("Sin"), arg)); break;
            case "Tan":
                var cos = Expression.Call(typeof(Math).GetMethod("Cos"), arg);
                derivative = Expression.Divide(Expression.Constant(1.0), Expression.Multiply(cos, cos));
                break;
            case "Exp": derivative = node; break;
            case "Log": derivative = Expression.Divide(Expression.Constant(1.0), arg); break;
            case "Sinh": derivative = Expression.Call(typeof(Math).GetMethod("Cosh"), arg); break;
            case "Cosh": derivative = Expression.Call(typeof(Math).GetMethod("Sinh"), arg); break;
            case "Sqrt":
                derivative = Expression.Divide(Expression.Constant(1.0), Expression.Multiply(Expression.Constant(2.0), node));
                break;
            case "Pow":
                if (node.Arguments.Count > 1 && TryGetDoubleConstant(node.Arguments[1], out double n))
                {
                    var newPower = Expression.Call(typeof(Math).GetMethod("Pow"), arg, Expression.Constant(n - 1));
                    derivative = Expression.Multiply(Expression.Constant(n), newPower);
                }
                break;
        }

        if (derivative != null) return Expression.Multiply(derivative, dArg);
        return Expression.Constant(0.0);
    }

    private static Expression Unwrap(Expression ex)
    {
        if (ex.NodeType == ExpressionType.Convert && ex is UnaryExpression u) return Unwrap(u.Operand);
        return ex;
    }

    private static bool TryGetDoubleConstant(Expression expr, out double value)
    {
        value = 0.0;
        expr = Unwrap(expr);
        if (expr is ConstantExpression c && c.Value != null)
        {
            try { value = Convert.ToDouble(c.Value); return true; } catch { }
        }
        return false;
    }

    private static Expression Simplify(Expression expr)
    {
        if (expr is BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Multiply)
            {
                if (IsZero(b.Left) || IsZero(b.Right)) return Expression.Constant(0.0);
                if (IsOne(b.Left)) return Simplify(b.Right);
                if (IsOne(b.Right)) return Simplify(b.Left);
            }
            if (b.NodeType == ExpressionType.Add)
            {
                if (IsZero(b.Left)) return Simplify(b.Right);
                if (IsZero(b.Right)) return Simplify(b.Left);
            }
            if (b.NodeType == ExpressionType.Subtract)
            {
                if (IsZero(b.Right)) return Simplify(b.Left);
            }
        }
        return expr;
    }

    private static bool IsZero(Expression e) => e is ConstantExpression c && Convert.ToDouble(c.Value) == 0.0;
    private static bool IsOne(Expression e) => e is ConstantExpression c && Convert.ToDouble(c.Value) == 1.0;
}