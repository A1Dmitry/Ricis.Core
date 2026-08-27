using System.Numerics;

namespace Ricis.Numerics.Factorization;

/// <summary>
/// Immutable exact evidence produced by the numeric Fermat baseline.
/// </summary>
/// <param name="N">The supplied positive odd integer.</param>
/// <param name="P">The reconstructed smaller factor.</param>
/// <param name="Q">The reconstructed greater-or-equal factor.</param>
/// <param name="X">The exact Fermat midpoint.</param>
/// <param name="Y">The exact Fermat half-gap.</param>
/// <param name="Delta">The exact square delta <c>X²-N=Y²</c>.</param>
/// <param name="InputBitLength">The exact bit length of <paramref name="N"/>.</param>
/// <param name="MaximumFactorBitLength">The maximum exact bit length of the reconstructed factors.</param>
/// <param name="FactorScale">The power-of-two scale matching <paramref name="MaximumFactorBitLength"/>.</param>
public sealed record FermatFactorizationResult(
    BigInteger N,
    BigInteger P,
    BigInteger Q,
    BigInteger X,
    BigInteger Y,
    BigInteger Delta,
    int InputBitLength,
    int MaximumFactorBitLength,
    BigInteger FactorScale);
