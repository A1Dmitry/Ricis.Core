// SingularitySolver.cs (финальная версия)

using System.Linq.Expressions;
using Ricis.Core.Polynomial;

namespace Ricis.Core.Solvers;

public static class SingularitySolver
{
    public static List<(ParameterExpression expr, double value)> SolveRoots(this Expression denominator)
    {
        var roots = new HashSet<(ParameterExpression, double)>();

        // 1. Аналитический поиск
        CollectRoots(denominator, roots);

        // 2. Численный фолбэк (L25)
        if (roots.Count != 0 || !IsTranscendentalComposite(denominator))
        {
            return roots.ToList();
        }

        var param = FindParameter(denominator);
        if (param == null)
        {
            return roots.ToList();
        }

        var numericalRoots = denominator.FindNumericalRoots(param);
        foreach (var root in numericalRoots)
        {
            roots.Add((root.Parameter, root.DoubleValue));
        }

        return roots.ToList();
    }

    private static void CollectRoots(Expression expr, HashSet<(ParameterExpression Parameter, double Value)> roots)
    {
        // 1. Попытка распарсить как полином (ax^2 + bx + c)
        var quad = expr.ParseQuadratic();
        if (quad.HasValue)
        {
            var (param, a, b, c) = quad.Value;

            // Квадратное: ax^2 + bx + c = 0
            if (Math.Abs(a) > 1e-10)
            {
                var discriminant = b * b - 4 * a * c;
                if (discriminant >= -1e-10)
                {
                    var sqrtD = Math.Sqrt(Math.Max(0, discriminant));
                    roots.Add((param, (-b + sqrtD) / (2 * a)));
                    roots.Add((param, (-b - sqrtD) / (2 * a)));
                }
                return;
            }
            // Линейное: bx + c = 0 => x = -c/b
            else if (Math.Abs(b) > 1e-10)
            {
                roots.Add((param, -c / b));
                return;
            }
        }

        // 2. Структурный разбор (если парсер не справился или для специфических форм)
        switch (expr)
        {
            case ParameterExpression p:
                roots.Add((p, 0.0));
                break;

            case BinaryExpression bin:
                if (bin.NodeType == ExpressionType.Multiply)
                {
                    CollectRoots(bin.Left, roots);
                    CollectRoots(bin.Right, roots);
                }
                // Явная обработка вычитания (Subtract)
                else if (bin.NodeType == ExpressionType.Subtract)
                {
                    // Случай: C - x (например, 1 - x)
                    if (bin.Left is ConstantExpression cLeft && bin.Right is ParameterExpression pRight)
                    {
                        if (TryGetDouble(cLeft, out var val))
                        {
                            roots.Add((pRight, val));
                        }
                    }
                    // Случай: x - C
                    else if (bin.Left is ParameterExpression pLeft && bin.Right is ConstantExpression cRight)
                    {
                        if (TryGetDouble(cRight, out var val))
                        {
                            roots.Add((pLeft, val));
                        }
                    }
                    // Случай: x^n - 1 (если не взялось парсером)
                    else if (bin.Right is ConstantExpression constRight &&
                        constRight.Value is double rightVal &&
                        Math.Abs(rightVal - 1.0) < double.Epsilon)
                    {
                        if (TryExtractPower(bin.Left, out var baseExpr, out var exponent))
                        {
                            if (baseExpr is ParameterExpression param && exponent > 1)
                            {
                                roots.Add((param, 1.0));
                                if (exponent % 2 == 0)
                                {
                                    roots.Add((param, -1.0));
                                }
                            }
                        }
                    }
                }
                // Явная обработка сложения (Add)
                else if (bin.NodeType == ExpressionType.Add)
                {
                    // x + C = 0 => x = -C
                    if (bin.Left is ParameterExpression pLeft && bin.Right is ConstantExpression cRight)
                    {
                        if (TryGetDouble(cRight, out var val))
                        {
                            roots.Add((pLeft, -val));
                        }
                    }
                }
                break;

            case MethodCallExpression call when call.Method.Name == "Log":
                if (call.Arguments.Count == 1 && call.Arguments[0] is ParameterExpression paramLog)
                {
                    roots.Add((paramLog, 1.0));
                }
                break;
        }
    }

    // --- Хелперы ---
    private static bool TryGetDouble(ConstantExpression c, out double val)
    {
        val = 0.0;
        if (c?.Value == null)
        {
            return false;
        }

        try { val = Convert.ToDouble(c.Value); return true; } catch { return false; }
    }

    private static bool IsTranscendentalComposite(Expression expr)
    {
        bool hasTrig = false;
        bool hasArithmetic = false;
        new ExpressionTraverser(node =>
        {
            if (node is MethodCallExpression call && call.Method.DeclaringType == typeof(Math))
            {
                hasTrig = true;
            }
            else if (node is BinaryExpression)
            {
                hasArithmetic = true;
            }
        }).Visit(expr);
        return hasTrig && hasArithmetic;
    }

    private static ParameterExpression FindParameter(Expression expr)
    {
        ParameterExpression found = null;
        new ExpressionTraverser(node => { if (found == null && node is ParameterExpression p)
            {
                found = p;
            }
        }).Visit(expr);
        return found;
    }

    private static bool TryExtractPower(Expression expr, out Expression baseExpr, out int exponent)
    {
        baseExpr = null; exponent = 0;
        if (expr is MethodCallExpression pow && pow.Method.Name == "Pow" && pow.Arguments.Count == 2)
        {
            baseExpr = pow.Arguments[0];
            if (pow.Arguments[1] is ConstantExpression c && c.Value is double d) { exponent = (int)d; return true; }
        }
        return false;
    }
}