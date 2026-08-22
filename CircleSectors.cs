using Ricis.Core.Rationals;
using System.Numerics;
using Ricis.Core.Resources;

namespace Ricis.Core;

/// <summary>
/// Represents the RICIS public type <c>CircleSectors</c>.
/// </summary>
public readonly struct CircleSectors
{
    /// <summary>
    /// Gets the <c>Fraction</c> value of <c>CircleSectors</c>.
    /// </summary>
    public Rational Fraction { get; }

    private CircleSectors(Rational fraction)
    {
        // Используем метод Floor из Rational (добавь его, если нет)
        var floor = Rational.Floor(fraction);
        Fraction = fraction - floor;
        if (Fraction < Rational.Zero)
        {
            Fraction += Rational.One;
        }
    }

    /// <summary>
    /// Executes <c>FromRadians</c> for the RICIS expression model.
    /// </summary>
    public static CircleSectors FromRadians(double radians, int maxDenominator = 100)
    {
        if (double.IsNaN(radians) || double.IsInfinity(radians))
        {
            throw new ArgumentException("Invalid angle");
        }

        var normalized = (radians % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
        var fractionDouble = normalized / (2 * Math.PI);

        var best = BestRationalApproximation(fractionDouble, maxDenominator);

        return new CircleSectors(best);
    }

    private static Rational BestRationalApproximation(double x, int maxDenominator)
    {
        var absX = Math.Abs(x);
        switch (absX)
        {
            case 0:
                return Rational.Zero;
            case 1:
                return Rational.One;
        }
        

        var value = absX;
        var a0 = (BigInteger)Math.Floor(value);
        var f = value - (double)a0;

        BigInteger p0 = a0, q0 = BigInteger.One;
        BigInteger p1 = BigInteger.One, q1 = BigInteger.Zero;

        var pBest = p0;
        var qBest = q0;

        var bestError = Math.Abs(value - (double)pBest / (double)qBest);

        while (q0 <= maxDenominator)
        {
            if (Math.Abs(f) < 1e-12)
            {
                break;
            }

            var reciprocal = 1.0 / f;
            var a = (BigInteger)Math.Floor(reciprocal);

            var pNew = a * p0 + p1;
            var qNew = a * q0 + q1;

            if (qNew > maxDenominator)
            {
                break;
            }

            var approx = (double)pNew / (double)qNew;
            var error = Math.Abs(value - approx);

            if (error < bestError)
            {
                bestError = error;
                pBest = pNew;
                qBest = qNew;
            }

            p1 = p0;
            q1 = q0;
            p0 = pNew;
            q0 = qNew;

            f = reciprocal - (double)a;
        }

        var result = new Rational(pBest, qBest);
        return x < 0 ? -result : result;
    }

    /// <summary>
    /// Executes <c>InSectors</c> for the RICIS expression model.
    /// </summary>
    public string InSectors(int totalSectors)
    {
        if (totalSectors <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSectors));
        }

        var sectorsPassed = Fraction * Rational.Create(totalSectors);
        var whole = Rational.Floor(sectorsPassed);
        var frac = sectorsPassed - whole;

        if (frac.IsZero)
        {
            if (whole.IsZero)
            {
                return RicisLegacyTextResources.Get("runtime.legacy.e35833b2044a");
            }

            if (whole == Rational.Create(totalSectors))
            {
                return RicisLegacyTextResources.Get("runtime.legacy.b21098a6244b");
            }

            return RicisLegacyTextResources.Format("runtime.legacy.29d21c34f591", ("whole", whole), ("totalSectors", totalSectors));
        }

        return RicisLegacyTextResources.Format("runtime.legacy.4becb3af5ca9", ("sectorsPassed", sectorsPassed), ("totalSectors", totalSectors));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Fraction.IsZero)
        {
            return RicisLegacyTextResources.Get("runtime.legacy.2f76f05bc135");
        }

        if (Fraction.Denominator.IsOne)
        {
            return RicisLegacyTextResources.Format("runtime.legacy.63165961aeb4", ("Fraction.Numerator", Fraction.Numerator));
        }

        return RicisLegacyTextResources.Format("runtime.legacy.0015d6e3591f", ("Fraction", Fraction));
    }
}