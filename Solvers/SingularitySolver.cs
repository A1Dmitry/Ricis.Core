// SingularitySolver.cs (финальная версия)

using System.Linq.Expressions;
using Ricis.Core.Polynomial;

namespace Ricis.Core.Solvers;

/// <summary>
/// Represents the RICIS public type <c>SingularitySolver</c>.
/// </summary>
public static class SingularitySolver
{
    /// <summary>
    /// Executes <c>SolveRoots</c> for the RICIS expression model.
    /// </summary>
    public static List<(ParameterExpression expr, double value)> SolveRoots(this Expression denominator)
    {
        var roots = new HashSet<(ParameterExpression, double)>();

        // SP2/SP4: preserve exact structural factors first. This prevents a
        // broad numerical tolerance from merging close but distinct factors.
        CollectRoots(denominator, roots);
        if (roots.Count != 0)
        {
            return roots.ToList();
        }

        var parameter = FindParameter(denominator);
        // Then use the common polynomial/numerical solver for forms that are
        // not directly decomposed, such as x²−4 or 1−Tan(x).
        if (parameter is not null)
        {
            foreach (var root in PolynomialZeroSolver.FindRoots(denominator, parameter))
            {
                roots.Add((root.Parameter, root.DoubleValue));
            }
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
            if (Math.Abs(a) > double.Epsilon)
            {
                var discriminant = b * b - 4 * a * c;
                if (discriminant >= -double.Epsilon)
                {
                    var sqrtD = Math.Sqrt(Math.Max(0, discriminant));
                    roots.Add((param, (-b + sqrtD) / (2 * a)));
                    roots.Add((param, (-b - sqrtD) / (2 * a)));
                }
                return;
            }
            // Линейное: bx + c = 0 => x = -c/b
            else if (Math.Abs(b) > double.Epsilon)
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

            case BinaryExpression { NodeType: ExpressionType.Power } power when
                power.Right is ConstantExpression exponent && TryGetDouble(exponent, out var powerValue) && powerValue > 0:
                // A positive real power vanishes exactly where its base vanishes.
                CollectRoots(power.Left, roots);
                break;

            case MethodCallExpression { Method.Name: "Pow", Arguments.Count: 2 } pow when
                TryGetPositiveConstant(pow.Arguments[1], out _):
                CollectRoots(pow.Arguments[0], roots);
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

            case MethodCallExpression call when call.Method.Name is "Sin" or "Cos" or "Tan" &&
                                                call.Arguments.Count == 1 && call.Arguments[0] is ParameterExpression:
                // Preserve the established principal-key contract for a pure
                // trigonometric denominator. Composite arguments fall through
                // to the certified numerical solver and retain their root set.
                var trigRoot = TrigSolver.Solve(call);
                if (trigRoot.HasValue)
                {
                    roots.Add(trigRoot.Value);
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
    private static bool TryGetPositiveConstant(Expression expression, out double value)
    {
        if (expression is ConstantExpression constant && TryGetDouble(constant, out value))
        {
            return value > 0;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Divide } ratio &&
            ratio.Left is ConstantExpression numerator && TryGetDouble(numerator, out var numeratorValue) &&
            ratio.Right is ConstantExpression denominator && TryGetDouble(denominator, out var denominatorValue) &&
            denominatorValue != 0)
        {
            value = numeratorValue / denominatorValue;
            return double.IsFinite(value) && value > 0;
        }

        value = 0;
        return false;
    }

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
        var hasTrig = false;
        var hasArithmetic = false;
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