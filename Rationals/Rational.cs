using System.Numerics;
using DivideByZeroException = System.DivideByZeroException;

namespace Ricis.Core.Rationals;

/// <summary>
/// Represents the RICIS public type <c>Rational</c>.
/// </summary>
public readonly struct Rational : IEquatable<Rational>
{
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    /// <summary>
    /// Gets the <c>Zero</c> value of <c>Rational</c>.
    /// </summary>
    public static readonly Rational Zero = new(0);
    /// <summary>
    /// Gets the <c>One</c> value of <c>Rational</c>.
    /// </summary>
    public static readonly Rational One = new(1);

    /// <summary>
    /// Gets the <c>Numerator</c> value of <c>Rational</c>.
    /// </summary>
    public BigInteger Numerator { get; }
    /// <summary>
    /// Gets the <c>Denominator</c> value of <c>Rational</c>.
    /// </summary>
    public BigInteger Denominator { get; } // always > 0

    /// <summary>
    /// Performs the <c>operator -</c> operation.
    /// </summary>
    public static Rational operator -(Rational a)
    {
        if (a.Numerator.IsZero)
        {
            return a; // -0 = 0
        }

        return new Rational(-a.Numerator, a.Denominator);
    }

    /// <summary>
    /// Initializes a new instance of <c>Rational</c>.
    /// </summary>
    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
        {
            throw new DivideByZeroException();
        }

        if (denominator.Sign < 0)
        {
            numerator = BigInteger.Negate(numerator);
            denominator = BigInteger.Negate(denominator);
        }

        var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    /// <summary>
    /// Initializes a new instance of <c>Rational</c>.
    /// </summary>
    public Rational(BigInteger integer) : this(integer, BigInteger.One)
    {
    }

    /// <summary>
    /// Gets the <c>IsZero</c> value of <c>Rational</c>.
    /// </summary>
    public bool IsZero => Numerator.IsZero;

    /// <summary>
    /// Executes <c>Create</c> for the RICIS expression model.
    /// </summary>
    public static Rational Create(long value)
    {
        return new Rational(value);
    }

    /// <summary>
    /// Executes <c>FromDecimal</c> for the RICIS expression model.
    /// </summary>
    public static Rational FromDecimal(decimal d)
    {
        var bits = decimal.GetBits(d);
        var lo = (uint)bits[0];
        var mid = (uint)bits[1];
        var hi = (uint)bits[2];
        var flags = (uint)bits[3];
        var sign = (flags & 0x80000000) != 0 ? -1 : 1;
        var scale = (flags >> 16) & 0x7F;

        var numerator = ((BigInteger)hi << 64) | ((BigInteger)mid << 32) | lo;
        numerator *= sign;
        var denominator = BigInteger.Pow(10, (int)scale);
        return new Rational(numerator, denominator);
    }

    /// <summary>
    /// Executes <c>ToDouble</c> for the RICIS expression model.
    /// </summary>
    public double ToDouble()
    {
        return (double)Numerator / (double)Denominator;
    }

    /// <summary>
    /// Performs the <c>operator +</c> operation.
    /// </summary>
    public static Rational operator +(Rational a, Rational b)
    {
        return new Rational(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    }

    /// <summary>
    /// Performs the <c>operator -</c> operation.
    /// </summary>
    public static Rational operator -(Rational a, Rational b)
    {
        return new Rational(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    }

    /// <summary>
    /// Performs the <c>operator *</c> operation.
    /// </summary>
    public static Rational operator *(Rational a, Rational b)
    {
        return new Rational(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
    }

    /// <summary>
    /// Performs the <c>operator /</c> operation.
    /// </summary>
    public static Rational operator /(Rational a, Rational b)
    {
        if (b.Numerator.IsZero)
        {
            throw new DivideByZeroException();
        }

        return new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Denominator.IsOne ? Numerator.ToString() : $"{Numerator}/{Denominator}";
    }

    /// <summary>
    /// Executes <c>Equals</c> for the RICIS expression model.
    /// </summary>
    public bool Equals(Rational other)
    {
        return Numerator.Equals(other.Numerator) && Denominator.Equals(other.Denominator);
    }


    /// <summary>
    /// Executes <c>Floor</c> for the RICIS expression model.
    /// </summary>
    public static Rational Floor(Rational r)
    {
        var floored = r.Numerator / r.Denominator;
        if (r.Numerator < 0 && r.Numerator % r.Denominator != 0)
        {
            floored -= BigInteger.One;
        }

        return new Rational(floored);
    }

    /// <summary>
    /// Performs the <c>operator &lt;</c> operation.
    /// </summary>
    public static bool operator <(Rational left, Rational right)
    {
        return left.Numerator * right.Denominator < right.Numerator * left.Denominator;
    }

    /// <summary>
    /// Performs the <c>operator ==</c> operation.
    /// </summary>
    public static bool operator ==(Rational left, Rational right)
    {
        return left.Numerator * right.Denominator == right.Numerator * left.Denominator;
    }

    /// <summary>
    /// Performs the <c>operator !=</c> operation.
    /// </summary>
    public static bool operator !=(Rational left, Rational right)
    {
        return !(left == right);
    }


    /// <summary>
    /// Performs the <c>operator &gt;</c> operation.
    /// </summary>
    public static bool operator >(Rational left, Rational right)
    {
        return left.Numerator * right.Denominator > right.Numerator * left.Denominator;
    }

    /// <summary>
    /// Performs the <c>operator &lt;=</c> operation.
    /// </summary>
    public static bool operator <=(Rational left, Rational right)
    {
        return left.Numerator * right.Denominator <= right.Numerator * left.Denominator;
    }

    /// <summary>
    /// Performs the <c>operator &gt;=</c> operation.
    /// </summary>
    public static bool operator >=(Rational left, Rational right)
    {
        return left.Numerator * right.Denominator >= right.Numerator * left.Denominator;
    }

    /// <summary>
    /// Converts a value through <c>implicit operator Rational</c>.
    /// </summary>
    public static implicit operator Rational(int value)
    {
        return new Rational(value, BigInteger.One);
    }

    /// <summary>
    /// Converts a value through <c>implicit operator Rational</c>.
    /// </summary>
    public static implicit operator Rational(long value)
    {
        return new Rational(value, BigInteger.One);
    }

    /// <summary>
    /// Converts a value through <c>implicit operator Rational</c>.
    /// </summary>
    public static implicit operator Rational(BigInteger value)
    {
        return new Rational(value, BigInteger.One);
    }

    // === ﬂ¬Õ¿ﬂ  ŒÕ¬≈–—»ﬂ ¬ DOUBLE (‰Îˇ fallback Ë ‚˚‚Ó‰‡) ===
    /// <summary>
    /// Converts a value through <c>explicit operator double</c>.
    /// </summary>
    public static explicit operator double(Rational r)
    {
        return r.ToDouble();
    }

    // === Œœ≈–¿“Œ–€ — INT ===
    /// <summary>
    /// Performs the <c>operator +</c> operation.
    /// </summary>
    public static Rational operator +(Rational a, int b)
    {
        return a + new Rational(b);
    }

    /// <summary>
    /// Performs the <c>operator +</c> operation.
    /// </summary>
    public static Rational operator +(int a, Rational b)
    {
        return new Rational(a) + b;
    }

    /// <summary>
    /// Performs the <c>operator -</c> operation.
    /// </summary>
    public static Rational operator -(Rational a, int b)
    {
        return a - new Rational(b);
    }

    /// <summary>
    /// Performs the <c>operator -</c> operation.
    /// </summary>
    public static Rational operator -(int a, Rational b)
    {
        return new Rational(a) - b;
    }

    /// <summary>
    /// Performs the <c>operator *</c> operation.
    /// </summary>
    public static Rational operator *(Rational a, int b)
    {
        return a * new Rational(b);
    }

    /// <summary>
    /// Performs the <c>operator *</c> operation.
    /// </summary>
    public static Rational operator *(int a, Rational b)
    {
        return new Rational(a) * b;
    }

    /// <summary>
    /// Performs the <c>operator /</c> operation.
    /// </summary>
    public static Rational operator /(Rational a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException();
        }

        return a / new Rational(b);
    }

    /// <summary>
    /// Performs the <c>operator /</c> operation.
    /// </summary>
    public static Rational operator /(int a, Rational b)
    {
        if (b.IsZero)
        {
            throw new DivideByZeroException();
        }

        return new Rational(a) / b;
    }

    //// === ”Õ¿–Õ€… Ã»Õ”— ===
    //public static Rational operator -(Rational a) => new Rational(-a.Numerator, a.Denominator);

    // === —–¿¬Õ≈Õ»≈ — INT (ÂÒÎË ÌÛÊÌÓ) ===
    /// <summary>
    /// Performs the <c>operator ==</c> operation.
    /// </summary>
    public static bool operator ==(Rational a, int b)
    {
        return a == new Rational(b);
    }

    /// <summary>
    /// Performs the <c>operator ==</c> operation.
    /// </summary>
    public static bool operator ==(int a, Rational b)
    {
        return new Rational(a) == b;
    }

    /// <summary>
    /// Performs the <c>operator !=</c> operation.
    /// </summary>
    public static bool operator !=(Rational a, int b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Performs the <c>operator !=</c> operation.
    /// </summary>
    public static bool operator !=(int a, Rational b)
    {
        return !(a == b);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is Rational rational && Equals(rational);
    }
}