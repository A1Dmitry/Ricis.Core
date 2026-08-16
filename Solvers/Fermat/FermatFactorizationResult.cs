using System.Numerics;

namespace Ricis.Core.Solvers.Fermat;

/// <summary>
/// Contains the exact structural result of an N-only Fermat factorization.
/// </summary>
public sealed record FermatFactorizationResult(
    BigInteger N,
    BigInteger P,
    BigInteger Q,
    BigInteger X,
    BigInteger Y,
    BigInteger Delta,
    int InputBitLength,
    int FactorBitLength,
    BigInteger FactorScale);
