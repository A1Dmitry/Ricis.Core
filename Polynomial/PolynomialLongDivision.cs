using System.Linq.Expressions;
using Ricis.Core.Rationals;

namespace Ricis.Core.Polynomial;

public static class PolynomialLongDivision
{
    public static Expression TryDivide(this Expression numerator, Expression denominator, ParameterExpression param)
    {
        var numCollector = new PolynomialCoefficientCollector(param);
        numCollector.Visit(numerator);

        // ИСПРАВЛЕНИЕ:
        if (!numCollector.IsPolynomial || numCollector.Coefficients.Count == 0)
        {
            return null;
        }

        var denCollector = new PolynomialCoefficientCollector(param);
        denCollector.Visit(denominator);

        if (!denCollector.IsPolynomial || denCollector.Coefficients.Count == 0)
        {
            return null;
        }

        // Используем SortedDictionary с обратным порядком для удобства
        var dividend = new SortedDictionary<int, Rational>(numCollector.Coefficients,
            Comparer<int>.Create((x, y) => y.CompareTo(x)));
        var divisor = new SortedDictionary<int, Rational>(denCollector.Coefficients,
            Comparer<int>.Create((x, y) => y.CompareTo(x)));

        if (divisor.Count == 0)
        {
            return null;
        }

        var divisorDegree = divisor.Keys.First(); // максимальная степень
        var leadingDivisor = divisor[divisorDegree];
        if (leadingDivisor.IsZero)
        {
            return null;
        }

        var quotient = Divide(dividend, divisor, divisorDegree, leadingDivisor);
        if (quotient == null)
        {
            return null;
        }

        return BuildExpressionFromCoefficients(quotient, param);
    }

    private static Dictionary<int, Rational> Divide(
        SortedDictionary<int, Rational> dividend,
        SortedDictionary<int, Rational> divisor,
        int divisorDegree,
        Rational leadingDivisor)
    {
        var quotient = new Dictionary<int, Rational>();
        var remainder = new SortedDictionary<int, Rational>(dividend, Comparer<int>.Create((x, y) => y.CompareTo(x)));

        while (remainder.Count > 0 && remainder.Keys.First() >= divisorDegree)
        {
            var currentDegree = remainder.Keys.First();
            var leadingDividend = remainder[currentDegree];

            var termCoeff = leadingDividend / leadingDivisor;
            var termDegree = currentDegree - divisorDegree;

            quotient[termDegree] = termCoeff;

            foreach (var (degB, coeffB) in divisor)
            {
                var degResult = degB + termDegree;
                var subtract = termCoeff * coeffB;

                if (remainder.TryGetValue(degResult, out var current))
                {
                    var newCoeff = current - subtract;
                    if (newCoeff.IsZero)
                    {
                        remainder.Remove(degResult);
                    }
                    else
                    {
                        remainder[degResult] = newCoeff;
                    }
                }
                else
                {
                    remainder[degResult] = -subtract;
                }
            }
        }

        // КРИТИЧНО: строгий запрет на неточное деление
        if (remainder.Count > 0)
        {
            return null; // любой остаток — деление неточное
        }

        return quotient;
    }

    private static Expression BuildExpressionFromCoefficients(Dictionary<int, Rational> coeffs, ParameterExpression param)
    {
        if (coeffs == null || coeffs.Count == 0)
        {
            return RicisType.InfinityZero;
        }

        Expression result = null;

        // Сортируем по убыванию степени для правильного порядка (опционально, но красиво)
        foreach (var kv in coeffs.OrderByDescending(k => k.Key))
        {
            var degree = kv.Key;
            var coeff = kv.Value;

            // Пропускаем нулевые коэффициенты — они не влияют на полином
            if (coeff.IsZero)
            {
                continue;
            }

            Expression coeffExpr = ConstantFromRational(coeff);

            Expression term;

            if (degree == 0)
            {
                term = coeffExpr;
            }
            else if (degree == 1)
            {
                term = Expression.Multiply(coeffExpr, param);
            }
            else
            {
                // Строим x^degree = x * x * ... * x (degree раз)
                Expression power = param;
                for (var i = 1; i < degree; i++)
                {
                    power = Expression.Multiply(power, param);
                }
                term = Expression.Multiply(coeffExpr, power);
            }

            // Накопление: result + term
            result = result == null ? term : Expression.Add(result, term);
        }

        // Если после фильтрации ничего не осталось — возвращаем 0
        return result ?? RicisType.InfinityZero;
    }

    private static ConstantExpression ConstantFromRational(Rational r)
    {
        // Rational.ToDouble() уже есть в твоём проекте
        var value = r.ToDouble();
        return Expression.Constant(value, typeof(double));
    }
}