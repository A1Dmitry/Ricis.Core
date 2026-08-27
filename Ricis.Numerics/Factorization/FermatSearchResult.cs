using System.Numerics;

namespace Ricis.Numerics.Factorization;

/// <summary>
/// Immutable measured evidence for one N-only Fermat search with bit-mask square-residue pruning.
/// </summary>
/// <param name="Factorization">The exact reconstructed factorization.</param>
/// <param name="CandidatePoints">The total parity-aligned Fermat candidates visited.</param>
/// <param name="MaskRejected">Candidates rejected before exact root extraction by a square-residue bit mask.</param>
/// <param name="MaskPassed">Candidates that passed the bit mask and reached an exact square-root check.</param>
/// <param name="ExactSquareRootChecks">The exact integer square-root checks performed.</param>
public sealed record FermatSearchResult(
    FermatFactorizationResult Factorization,
    BigInteger CandidatePoints,
    BigInteger MaskRejected,
    BigInteger MaskPassed,
    BigInteger ExactSquareRootChecks);
