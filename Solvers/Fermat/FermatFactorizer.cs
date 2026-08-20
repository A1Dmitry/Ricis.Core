using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using Ricis.Core.Proofs;

namespace Ricis.Core.Solvers.Fermat;

/// <summary>
/// Finds a balanced factor pair from N using the difference-of-squares identity.
/// The only mathematical input is N; factors are never supplied by the caller.
/// </summary>
public static class FermatFactorizer
{
    // Squares modulo 64 are exactly { 0, 1, 4, 9, 16, 17, 25, 33, 36, 41, 49, 57 }.
    // This is an exact necessary condition, used only to skip impossible root checks.
    private const ulong SquareResidueModulo64Mask =
        (1UL << 0) | (1UL << 1) | (1UL << 4) | (1UL << 9) |
        (1UL << 16) | (1UL << 17) | (1UL << 25) | (1UL << 33) |
        (1UL << 36) | (1UL << 41) | (1UL << 49) | (1UL << 57);

    /// <summary>
    /// Solves <c>x² - N = y²</c> and returns <c>P=x-y</c>, <c>Q=x+y</c>.
    /// The search starts at the exact integer ceiling of sqrt(N) and advances
    /// the deferred difference by <c>Δ(x+1)=Δ(x)+2x+1</c>.
    /// </summary>
    public static FermatFactorizationResult Solve(BigInteger n)
    {
        if (n <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "N must be positive.");
        }

        var x = IntegerSqrtCeiling(n);
        var delta = x * x - n;
        while (true)
        {
            if (CouldBePerfectSquare(delta))
            {
                var y = IntegerSqrtExact(delta);
                if (y >= 0 && y * y == delta)
                {
                    var p = x - y;
                    var q = x + y;
                    if (p > 1 && q > 1 && p * q == n)
                    {
            var inputBits = checked((int)n.GetBitLength());
            var factorBits = checked((int)Math.Max(p.GetBitLength(), q.GetBitLength()));
            var scale = BigInteger.One << factorBits;
                        return new FermatFactorizationResult(
                            n, p, q, x, y, delta, inputBits, factorBits, scale);
                    }
                }
            }

            delta += (2 * x) + 1;
            x++;
        }
    }

    /// <summary>
    /// Solves the N-only Fermat system and renders the solver-created derivation
    /// through the shared proof-document renderer.
    /// </summary>
    /// <param name="n">The only numeric input to the factorization.</param>
    /// <param name="profile">Metadata and explicit proof boundaries for the document.</param>
    /// <param name="format">The requested document format, including <see cref="RicisProofDocumentFormat.Log"/>.</param>
    /// <param name="document">The output buffer receiving the rendered document.</param>
    /// <returns>The recovered factorization result.</returns>
    public static FermatFactorizationResult ProveDocument(
        BigInteger n,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(document);
        var documentConstructor = RicisProofDocumentTemplates.ResolveFactory(format);
        var result = Solve(n);
        var derivation = new StringBuilder();
        derivation.AppendLine("N-only input: N is the sole supplied value.");
        derivation.AppendLine($"Search: x := ceil(sqrt(N)) = {result.X}.");
        derivation.AppendLine($"Difference recurrence: Δ(x+1) = Δ(x) + 2x + 1.");
        derivation.AppendLine($"Found: Δ({result.X}) = {result.Delta} = {result.Y}².");
        derivation.AppendLine($"Substitution: P := x−y = {result.P}; Q := x+y = {result.Q}.");
        derivation.AppendLine($"Reconstruction check: P·Q = {result.P * result.Q} = N.");
        derivation.AppendLine($"Dynamic normalization: inputBits={result.InputBitLength}, factorBits={result.FactorBitLength}, scale=2^{result.FactorBitLength}.");

        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var derived = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(x, Expression.Constant((double)result.X)), x, y);
        var rendered = documentConstructor(profile, derivation.ToString(), derived);
        document.Append(rendered);
        return result;
    }

    private static bool CouldBePerfectSquare(BigInteger value)
    {
        var residue = (int)(value & 63);
        return ((SquareResidueModulo64Mask >> residue) & 1UL) != 0;
    }

    private static BigInteger IntegerSqrtCeiling(BigInteger value)
    {
        var floor = IntegerSqrtExact(value);
        return floor * floor == value ? floor : floor + 1;
    }

    private static BigInteger IntegerSqrtExact(BigInteger value)
    {
        if (value < 0) return -1;
        if (value < 2) return value;

        // The initial value is a proven upper bound: 2^ceil(bitLength(value)/2) ≥ √value.
        // Newton's decreasing integer iteration converges to floor(√value). Unlike binary search,
        // it avoids a full-width square multiplication for every candidate in the Fermat loop.
        var root = BigInteger.One << checked((int)((value.GetBitLength() + 1) / 2));
        while (true)
        {
            var next = (root + (value / root)) >> 1;
            if (next >= root)
            {
                return root;
            }

            root = next;
        }
    }
}
