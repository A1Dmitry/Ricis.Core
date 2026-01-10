using System.Linq.Expressions;

namespace Ricis.Core.Polynomial
{
    /// <summary>
    /// Robust quadratic extractor: walks the expression, collects additive terms,
    /// for each term extracts coefficient and degree (power of the single parameter).
    /// Returns (parameter, a, b, c) for a*x^2 + b*x + c if found (otherwise null).
    /// </summary>
   
    public static class PolynomialParser
    {
        public static (ParameterExpression, double a, double b, double c)? ParseQuadratic(this Expression expr)
        {
            if (expr == null) return null;

            var terms = new Dictionary<int, double>();
            ParameterExpression variable = null;

            var additiveTerms = FlattenAddSubtract(expr);

            foreach (var term in additiveTerms)
            {
                if (TryExtractMonomial(term, ref variable, out var degree, out var coeff))
                {
                    terms.TryAdd(degree, 0.0);
                    terms[degree] += coeff;
                }
                else
                {
                    return null;
                }
            }

            var a = terms.GetValueOrDefault(2, 0.0);
            var b = terms.GetValueOrDefault(1, 0.0);
            var c = terms.GetValueOrDefault(0, 0.0);

            if (variable == null) return null;
            if (Math.Abs(a) < double.Epsilon && Math.Abs(b) < double.Epsilon && Math.Abs(c) < double.Epsilon) return null;

            return (variable, a, b, c);
        }

        private static List<Expression> FlattenAddSubtract(Expression expr)
        {
            var list = new List<Expression>();
            void Recur(Expression e, double sign = 1.0)
            {
                if (e == null) return;
                if (e.NodeType != ExpressionType.Add)
                {
                    switch (e.NodeType)
                    {
                        case ExpressionType.Subtract:
                            {
                                var b = (BinaryExpression)e;
                                Recur(b.Left, sign);
                                Recur(b.Right, -sign);
                                return;
                            }
                    }
                    list.Add(sign == -1.0 ? Expression.Multiply(Expression.Constant(-1.0), e) : e);
                }
                else
                {
                    var b = (BinaryExpression)e;
                    Recur(b.Left, sign);
                    Recur(b.Right, sign);
                    return;
                }
            }
            Recur(expr);
            return list;
        }

        private static bool TryExtractMonomial(Expression expr, ref ParameterExpression variable, out int degree, out double coefficient)
        {
            degree = 0;
            coefficient = 1.0;

            var factors = new List<Expression>();
            CollectMultiplicativeFactors(expr, factors);

            foreach (var f in factors)
            {
                // FIX: Снимаем Convert
                var unwrapped = Unwrap(f);

                if (unwrapped is ConstantExpression c)
                {
                    if (!TryGetNumericConstant(c, out var v)) return false;
                    coefficient *= v;
                    continue;
                }

                if (unwrapped is ParameterExpression p)
                {
                    if (variable == null) variable = p;
                    else if (variable != p && variable.Name != p.Name) return false;
                    degree += 1;
                    continue;
                }

                // FIX: Поддержка Math.Pow(x, n)
                if (unwrapped is MethodCallExpression mce && mce.Method.Name == "Pow")
                {
                    var arg0 = Unwrap(mce.Arguments[0]);
                    var arg1 = Unwrap(mce.Arguments[1]);

                    if (arg0 is ParameterExpression p2)
                    {
                        if (variable == null) variable = p2;
                        else if (variable != p2 && variable.Name != p2.Name) return false;

                        if (arg1 is ConstantExpression c2 && TryGetNumericConstant(c2, out var powVal))
                        {
                            if (Math.Abs(powVal % 1) < double.Epsilon && powVal >= 0)
                            {
                                degree += (int)powVal;
                                continue;
                            }
                        }
                    }
                    return false; // Pow с неверными аргументами
                }

                if (unwrapped is BinaryExpression be && be.NodeType == ExpressionType.Multiply)
                {
                    if (!TryExtractMonomial(f, ref variable, out var d2, out var c2)) return false;
                    degree += d2;
                    coefficient *= c2;
                    continue;
                }

                return false;
            }

            return true;
        }

        private static void CollectMultiplicativeFactors(Expression expr, List<Expression> outFactors)
        {
            if (expr == null) return;
            if (expr.NodeType == ExpressionType.Multiply)
            {
                var b = (BinaryExpression)expr;
                CollectMultiplicativeFactors(b.Left, outFactors);
                CollectMultiplicativeFactors(b.Right, outFactors);
                return;
            }
            outFactors.Add(expr);
        }

        private static bool TryGetNumericConstant(ConstantExpression c, out double value)
        {
            value = 0.0;
            if (c.Value == null) return false;
            try { value = Convert.ToDouble(c.Value); return true; } catch { return false; }
        }

        private static Expression Unwrap(Expression ex)
        {
            if (ex.NodeType == ExpressionType.Convert && ex is UnaryExpression u) return Unwrap(u.Operand);
            return ex;
        }
    }
}
