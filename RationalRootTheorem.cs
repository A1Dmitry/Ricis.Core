using System.Numerics;

namespace Ricis.Core;

internal class RationalRootTheorem
{
    public static List<Rational> GetPossibleRoots(Dictionary<int, Rational> coeffs)
    {
        // ИСПРАВЛЕНИЕ: Берем коэффициент с минимальной степенью (обычно 0)
        // Dictionary не гарантирует порядок, Values.First() — это русская рулетка.
        var minDegree = coeffs.Keys.Min();
        var constant = coeffs[minDegree];

        var maxDegree = coeffs.Keys.Max();
        var leading = coeffs[maxDegree];

        // Если свободный член 0 (например x^2 - 5x), то 0 - корень, 
        // но теорема ищет ненулевые рациональные корни для приведенного.
        // Для x-5 minDegree=0, constant=-5.
        if (constant.IsZero) return new List<Rational>();

        var pFactors = Factorize(constant.Numerator);
        var qFactors = Factorize(leading.Denominator);

        var candidates = new HashSet<Rational>();

        foreach (var p in pFactors)
        foreach (var q in qFactors)
        {
            candidates.Add(new Rational(p, q));
            candidates.Add(new Rational(BigInteger.Negate(p), q));
        }

        return candidates.OrderBy(r => r.ToDouble()).ToList();
    }

    private static List<BigInteger> Factorize(BigInteger n)
    {
        n = BigInteger.Abs(n);
        var factors = new List<BigInteger> { BigInteger.One };

        if (n.IsZero) return factors; // Защита

        for (BigInteger i = 2; i * i <= n; i++)
            if (n % i == 0)
            {
                factors.Add(i);
                while (n % i == 0) n /= i;
            }

        if (n > 1) factors.Add(n);

        return factors;
    }
}