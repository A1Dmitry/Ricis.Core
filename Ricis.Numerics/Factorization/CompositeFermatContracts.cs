using System.Collections.Generic;
using System.Numerics;

namespace Ricis.Numerics.Factorization;

/// <summary>Classifies the exact start intersection of the Fermat curve.</summary>
public enum FermatGeometryStatus
{
    /// <summary>The N-only input is an exact square and has zero Fermat gap.</summary>
    ExactSquare,

    /// <summary>The next integral x-coordinate has two symmetric real intersections.</summary>
    TwoIntersections,
}

/// <summary>Chooses the bounded candidate ordering recorded by a composite trace.</summary>
public enum FermatSearchOrdering
{
    /// <summary>Scans parity-aligned Fermat x coordinates.</summary>
    FermatCoordinates,

    /// <summary>Scans lower P candidates on the inner tangent band.</summary>
    TangentLowerFactor,

    /// <summary>Scans upper Q candidates on the inner tangent band.</summary>
    TangentUpperFactor,
}

/// <summary>Chooses one symmetric P/Q tangent-band side.</summary>
public enum FermatFactorCandidateOrientation
{
    /// <summary>Lower P side.</summary>
    LowerFactor,

    /// <summary>Upper Q side.</summary>
    UpperFactor,
}

/// <summary>Describes the terminal status of one bounded composite search.</summary>
public enum FermatSearchOutcome
{
    /// <summary>An exact square supplied the zero-gap certificate.</summary>
    ExactSquare,

    /// <summary>An exact P·Q=N reconstruction was found.</summary>
    FactorFound,

    /// <summary>The declared bounded region was exhausted without a certificate.</summary>
    NotFoundWithinDeclaredProfile,
}

/// <summary>Immutable exact start geometry derived exclusively from a positive odd N.</summary>
public sealed record FermatStartGeometry
{
    private FermatStartGeometry(
        BigInteger n,
        BigInteger floorRoot,
        BigInteger startX,
        BigInteger startDelta,
        BigInteger geometricSpan,
        FermatGeometryStatus status)
    {
        N = n;
        FloorRoot = floorRoot;
        StartX = startX;
        StartDelta = startDelta;
        GeometricSpan = geometricSpan;
        Status = status;
    }

    /// <summary>Gets the validated N-only input.</summary>
    public BigInteger N { get; }

    /// <summary>Gets B=floor(sqrt(N)).</summary>
    public BigInteger FloorRoot { get; }

    /// <summary>Gets B for zero gap or B+1 for a two-intersection start.</summary>
    public BigInteger StartX { get; }

    /// <summary>Gets StartX²-N.</summary>
    public BigInteger StartDelta { get; }

    /// <summary>Gets ceil(2*sqrt(StartDelta)), the integral two-intersection separation.</summary>
    public BigInteger GeometricSpan { get; }

    /// <summary>Gets the zero-gap or two-intersection status.</summary>
    public FermatGeometryStatus Status { get; }

    /// <summary>
    /// Creates the exact zero-gap/two-intersection start state with a fixed-width root primitive.
    /// </summary>
    /// <param name="n">A positive odd input of at most 2048 bits.</param>
    /// <returns>Immutable exact start geometry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when input is non-positive, even or wider than 2048 bits.</exception>
    public static FermatStartGeometry Create(BigInteger n)
    {
        if (n <= 0 || n.IsEven)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "N must be positive and odd.");
        }

        var floorRoot = FixedWidthFermatRoot.Floor(n);
        var square = floorRoot * floorRoot;
        var next = floorRoot + BigInteger.One;
        if (square > n || next * next <= n)
        {
            throw new InvalidOperationException("The floor-root certificate is invalid.");
        }

        if (square == n)
        {
            return new FermatStartGeometry(n, floorRoot, floorRoot, BigInteger.Zero, BigInteger.Zero, FermatGeometryStatus.ExactSquare);
        }

        var startDelta = next * next - n;
        var doubledDistanceSquared = 4 * startDelta;
        var separationFloor = FixedWidthFermatRoot.Floor(doubledDistanceSquared);
        var geometricSpan = separationFloor * separationFloor == doubledDistanceSquared
            ? separationFloor
            : separationFloor + BigInteger.One;
        return new FermatStartGeometry(n, floorRoot, next, startDelta, geometricSpan, FermatGeometryStatus.TwoIntersections);
    }
}

/// <summary>
/// Immutable public N-only profile. It contains ordering and visual band offset, never P, Q or an answer.
/// </summary>
public sealed record FermatPruningProfile
{
    private FermatPruningProfile(FermatSearchOrdering ordering, BigInteger tangentBandOffset)
    {
        Ordering = ordering;
        TangentBandOffset = tangentBandOffset;
    }

    /// <summary>Gets the selected bounded search ordering.</summary>
    public FermatSearchOrdering Ordering { get; }

    /// <summary>Gets the non-negative inner-band offset reported as geometry evidence.</summary>
    public BigInteger TangentBandOffset { get; }

    /// <summary>Gets the deterministic BFR-2 Fermat-coordinate default.</summary>
    public static FermatPruningProfile Default { get; } = new(FermatSearchOrdering.FermatCoordinates, 2);

    /// <summary>
    /// Creates an immutable public profile without accepting hidden factor information.
    /// </summary>
    /// <param name="ordering">The candidate ordering.</param>
    /// <param name="tangentBandOffset">A non-negative integral visual band offset.</param>
    /// <returns>An immutable profile.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for undefined ordering or negative offset.</exception>
    public static FermatPruningProfile Create(FermatSearchOrdering ordering, BigInteger tangentBandOffset)
    {
        if (!Enum.IsDefined(ordering))
        {
            throw new ArgumentOutOfRangeException(nameof(ordering), "The search ordering is undefined.");
        }

        if (tangentBandOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tangentBandOffset), "The tangent-band offset must be non-negative.");
        }

        return new FermatPruningProfile(ordering, tangentBandOffset);
    }
}

/// <summary>Immutable odd P/Q candidate band inside the approved BFR-2 region.</summary>
public sealed record FermatTangentBand
{
    private FermatTangentBand(
        BigInteger lowerFirst,
        BigInteger lowerLast,
        BigInteger upperFirst,
        BigInteger upperLast,
        BigInteger yLimit,
        BigInteger effectiveEndX,
        BigInteger offset)
    {
        LowerFirst = lowerFirst;
        LowerLast = lowerLast;
        UpperFirst = upperFirst;
        UpperLast = upperLast;
        YLimit = yLimit;
        EffectiveEndX = effectiveEndX;
        Offset = offset;
        LowerCandidateCount = Count(lowerFirst, lowerLast);
        UpperCandidateCount = Count(upperFirst, upperLast);
    }

    /// <summary>Gets the first lower P candidate, or one for an empty side.</summary>
    public BigInteger LowerFirst { get; }

    /// <summary>Gets the final lower P candidate, or zero for an empty side.</summary>
    public BigInteger LowerLast { get; }

    /// <summary>Gets the first upper Q candidate, or one for an empty side.</summary>
    public BigInteger UpperFirst { get; }

    /// <summary>Gets the final upper Q candidate, or zero for an empty side.</summary>
    public BigInteger UpperLast { get; }

    /// <summary>Gets the required odd-candidate step of two.</summary>
    public BigInteger Step => 2;

    /// <summary>Gets the integral BFR-2 y limit used to form this band.</summary>
    public BigInteger YLimit { get; }

    /// <summary>Gets the fail-safe effective x endpoint.</summary>
    public BigInteger EffectiveEndX { get; }

    /// <summary>Gets the explicit profile offset recorded with the band.</summary>
    public BigInteger Offset { get; }

    /// <summary>Gets lower P candidate count.</summary>
    public BigInteger LowerCandidateCount { get; }

    /// <summary>Gets upper Q candidate count.</summary>
    public BigInteger UpperCandidateCount { get; }

    /// <summary>Gets both symmetric sides’ candidate count.</summary>
    public BigInteger CandidateCount => LowerCandidateCount + UpperCandidateCount;

    /// <summary>
    /// Creates the full P/Q band from exact BFR-2 y constraints and a fail-safe effective boundary.
    /// </summary>
    /// <param name="geometry">Validated exact geometry.</param>
    /// <param name="profile">N-only public profile.</param>
    /// <param name="effectiveEndX">Inclusive x endpoint no smaller than the start x coordinate.</param>
    /// <returns>Immutable parity-ready band.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required contract is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when endpoint precedes start geometry.</exception>
    public static FermatTangentBand Create(FermatStartGeometry geometry, FermatPruningProfile profile, BigInteger effectiveEndX)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(profile);
        if (effectiveEndX < geometry.StartX)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveEndX), "The effective endpoint must not precede the start geometry.");
        }

        var byN = FixedWidthFermatRoot.Floor((geometry.N - BigInteger.One) / 8);
        var bySlope = (effectiveEndX - BigInteger.One) / 3;
        var yLimit = BigInteger.Min(byN, bySlope);

        var lowerFirst = FirstOddAtOrAbove(BigInteger.Max(new BigInteger(3), geometry.StartX - yLimit));
        var lowerLast = LastOddAtOrBelow(geometry.FloorRoot);
        if (lowerFirst > lowerLast)
        {
            lowerFirst = BigInteger.One;
            lowerLast = BigInteger.Zero;
        }

        var upperFirst = FirstOddAtOrAbove(geometry.FloorRoot);
        var upperLast = LastOddAtOrBelow(effectiveEndX + yLimit);
        if (upperFirst > upperLast)
        {
            upperFirst = BigInteger.One;
            upperLast = BigInteger.Zero;
        }

        return new FermatTangentBand(lowerFirst, lowerLast, upperFirst, upperLast, yLimit, effectiveEndX, profile.TangentBandOffset);
    }

    /// <summary>
    /// Enumerates exactly one odd P/Q side in ascending increments of two.
    /// </summary>
    /// <param name="orientation">Lower P or upper Q side.</param>
    /// <returns>Candidate sequence with an exact parity invariant.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an undefined orientation.</exception>
    public IEnumerable<BigInteger> Enumerate(FermatFactorCandidateOrientation orientation)
    {
        if (!Enum.IsDefined(orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(orientation), "The factor orientation is undefined.");
        }

        var start = orientation == FermatFactorCandidateOrientation.LowerFactor ? LowerFirst : UpperFirst;
        var end = orientation == FermatFactorCandidateOrientation.LowerFactor ? LowerLast : UpperLast;
        for (var candidate = start; candidate <= end; candidate += 2)
        {
            yield return candidate;
        }
    }

    private static BigInteger FirstOddAtOrAbove(BigInteger value) => value.IsEven ? value + BigInteger.One : value;
    private static BigInteger LastOddAtOrBelow(BigInteger value) => value.IsEven ? value - BigInteger.One : value;
    private static BigInteger Count(BigInteger first, BigInteger last) => first > last ? BigInteger.Zero : ((last - first) / 2) + BigInteger.One;
}

/// <summary>Immutable all-layer evidence for a bounded Composite Fermat run.</summary>
public sealed class FermatRegionTrace
{
    internal FermatRegionTrace(FermatRegionTraceData data)
    {
        Geometry = data.Geometry;
        Profile = data.Profile;
        Ordering = data.Ordering;
        Outcome = data.Outcome;
        GeometricSpan = data.GeometricSpan;
        CalculatedSpan = data.CalculatedSpan;
        EffectiveSpan = data.EffectiveSpan;
        EffectiveEndX = data.EffectiveEndX;
        RangeExpansionApplied = data.RangeExpansionApplied;
        TangentBandOffset = data.TangentBandOffset;
        TangentBandCandidateCount = data.TangentBandCandidateCount;
        PFactorCandidates = data.PFactorCandidates;
        QFactorCandidates = data.QFactorCandidates;
        ParityStep = data.ParityStep;
        DivisibilityChecks = data.DivisibilityChecks;
        InitialCandidates = data.InitialCandidates;
        AfterRelativeBounds = data.AfterRelativeBounds;
        AfterParity = data.AfterParity;
        ParityRejected = data.ParityRejected;
        AfterBitMask = data.AfterBitMask;
        MaskRejected = data.MaskRejected;
        AfterCrtResidues = data.AfterCrtResidues;
        CrtModulo7Rejected = data.CrtModulo7Rejected;
        CrtModulo31Rejected = data.CrtModulo31Rejected;
        CrtModulo127Rejected = data.CrtModulo127Rejected;
        ExactRootChecks = data.ExactRootChecks;
        ExactSquares = data.ExactSquares;
        FinalReconstruction = data.FinalReconstruction;
    }

    /// <summary>Gets exact start geometry.</summary>
    public FermatStartGeometry Geometry { get; }
    /// <summary>Gets the public profile.</summary>
    public FermatPruningProfile Profile { get; }
    /// <summary>Gets the ordering used.</summary>
    public FermatSearchOrdering Ordering { get; }
    /// <summary>Gets terminal bounded-search status.</summary>
    public FermatSearchOutcome Outcome { get; }
    /// <summary>Gets two-intersection span.</summary>
    public BigInteger GeometricSpan { get; }
    /// <summary>Gets BFR-2 calculated span.</summary>
    public BigInteger CalculatedSpan { get; }
    /// <summary>Gets max(geometric, calculated).</summary>
    public BigInteger EffectiveSpan { get; }
    /// <summary>Gets the effective inclusive x endpoint.</summary>
    public BigInteger EffectiveEndX { get; }
    /// <summary>Gets whether calculation expanded local geometry.</summary>
    public bool RangeExpansionApplied { get; }
    /// <summary>Gets explicit tangent offset.</summary>
    public BigInteger TangentBandOffset { get; }
    /// <summary>Gets both tangent sides’ candidate count.</summary>
    public BigInteger TangentBandCandidateCount { get; }
    /// <summary>Gets lower P candidate count.</summary>
    public BigInteger PFactorCandidates { get; }
    /// <summary>Gets upper Q candidate count.</summary>
    public BigInteger QFactorCandidates { get; }
    /// <summary>Gets the parity step.</summary>
    public BigInteger ParityStep { get; }
    /// <summary>Gets tangent-path exact divisibility checks.</summary>
    public BigInteger DivisibilityChecks { get; }
    /// <summary>Gets candidates before ordered filter layers.</summary>
    public BigInteger InitialCandidates { get; }
    /// <summary>Gets candidates after relative bounds.</summary>
    public BigInteger AfterRelativeBounds { get; }
    /// <summary>Gets candidates after parity filtering.</summary>
    public BigInteger AfterParity { get; }
    /// <summary>Gets parity rejects.</summary>
    public BigInteger ParityRejected { get; }
    /// <summary>Gets candidates after mod-64 filtering.</summary>
    public BigInteger AfterBitMask { get; }
    /// <summary>Gets mod-64 rejects.</summary>
    public BigInteger MaskRejected { get; }
    /// <summary>Gets candidates after CRT filters.</summary>
    public BigInteger AfterCrtResidues { get; }
    /// <summary>Gets first-stage modulus-7 rejects.</summary>
    public BigInteger CrtModulo7Rejected { get; }
    /// <summary>Gets second-stage modulus-31 rejects.</summary>
    public BigInteger CrtModulo31Rejected { get; }
    /// <summary>Gets third-stage modulus-127 rejects.</summary>
    public BigInteger CrtModulo127Rejected { get; }
    /// <summary>Gets exact root checks.</summary>
    public BigInteger ExactRootChecks { get; }
    /// <summary>Gets certified square deltas.</summary>
    public BigInteger ExactSquares { get; }
    /// <summary>Gets successful P·Q=N reconstructions.</summary>
    public BigInteger FinalReconstruction { get; }
    /// <summary>Gets exact remaining-region numerator.</summary>
    public BigInteger RemainingFractionNumerator => AfterCrtResidues;
    /// <summary>Gets exact remaining-region denominator.</summary>
    public BigInteger RemainingFractionDenominator => InitialCandidates;
}

/// <summary>Immutable bounded composite result that never confuses not-found with a global conclusion.</summary>
public sealed class CompositeFermatSearchResult
{
    internal CompositeFermatSearchResult(FermatFactorizationResult? factorization, FermatRegionTrace trace)
    {
        Factorization = factorization;
        Trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

    /// <summary>Gets an exact certificate only if one was reconstructed.</summary>
    public FermatFactorizationResult? Factorization { get; }
    /// <summary>Gets full immutable geometry and filtering evidence.</summary>
    public FermatRegionTrace Trace { get; }
    /// <summary>Gets whether a final exact factorization certificate exists.</summary>
    public bool HasFactorization => Factorization is not null;
}

internal sealed class FermatRegionTraceData
{
    public required FermatStartGeometry Geometry { get; init; }
    public required FermatPruningProfile Profile { get; init; }
    public required FermatSearchOrdering Ordering { get; init; }
    public required FermatSearchOutcome Outcome { get; init; }
    public required BigInteger GeometricSpan { get; init; }
    public required BigInteger CalculatedSpan { get; init; }
    public required BigInteger EffectiveSpan { get; init; }
    public required BigInteger EffectiveEndX { get; init; }
    public required bool RangeExpansionApplied { get; init; }
    public required BigInteger TangentBandOffset { get; init; }
    public required BigInteger TangentBandCandidateCount { get; init; }
    public required BigInteger PFactorCandidates { get; init; }
    public required BigInteger QFactorCandidates { get; init; }
    public required BigInteger ParityStep { get; init; }
    public required BigInteger DivisibilityChecks { get; init; }
    public required BigInteger InitialCandidates { get; init; }
    public required BigInteger AfterRelativeBounds { get; init; }
    public required BigInteger AfterParity { get; init; }
    public required BigInteger ParityRejected { get; init; }
    public required BigInteger AfterBitMask { get; init; }
    public required BigInteger MaskRejected { get; init; }
    public required BigInteger AfterCrtResidues { get; init; }
    public required BigInteger CrtModulo7Rejected { get; init; }
    public required BigInteger CrtModulo31Rejected { get; init; }
    public required BigInteger CrtModulo127Rejected { get; init; }
    public required BigInteger ExactRootChecks { get; init; }
    public required BigInteger ExactSquares { get; init; }
    public required BigInteger FinalReconstruction { get; init; }
}
