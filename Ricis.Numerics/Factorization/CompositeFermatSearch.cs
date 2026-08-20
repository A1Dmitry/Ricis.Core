using System.Numerics;

namespace Ricis.Numerics.Factorization;

/// <summary>
/// Performs bounded Composite Fermat searches with exact geometry, retained/rejected evidence and fail-closed certificates.
/// This type does not claim complete or constant-time factorization.
/// </summary>
public static class CompositeFermatSearch
{
    /// <summary>
    /// Performs the deterministic BFR-2 Fermat-coordinate profile for a positive odd N-only input.
    /// </summary>
    /// <param name="n">The positive odd value to search, restricted to the fixed-width composite path.</param>
    /// <returns>Exact certificate when found, otherwise a bounded not-found result with trace evidence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid input or an unsupported fixed-width BFR-2 boundary.</exception>
    public static CompositeFermatSearchResult Search(BigInteger n) => Search(n, FermatPruningProfile.Default);

    /// <summary>
    /// Performs the selected BFR-2 Fermat-coordinate or inner tangent P/Q profile for a positive odd N-only input.
    /// </summary>
    /// <param name="n">The positive odd value to search, restricted to the fixed-width composite path.</param>
    /// <param name="profile">Public non-answer search profile.</param>
    /// <returns>Exact certificate when found, otherwise a bounded not-found result with trace evidence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid input or an unsupported fixed-width BFR-2 boundary.</exception>
    public static CompositeFermatSearchResult Search(BigInteger n, FermatPruningProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (n <= BigInteger.One || n.IsEven)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "Composite search requires an odd N greater than one.");
        }

        var geometry = FermatStartGeometry.Create(n);
        var context = CreateContext(geometry, profile);
        if (geometry.Status == FermatGeometryStatus.ExactSquare)
        {
            return ExactSquareResult(context);
        }

        return profile.Ordering switch
        {
            FermatSearchOrdering.FermatCoordinates => SearchFermatCoordinates(context),
            FermatSearchOrdering.TangentLowerFactor => SearchTangentCandidates(context, FermatFactorCandidateOrientation.LowerFactor),
            FermatSearchOrdering.TangentUpperFactor => SearchTangentCandidates(context, FermatFactorCandidateOrientation.UpperFactor),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), "The profile ordering is undefined."),
        };
    }

    private static CompositeSearchContext CreateContext(FermatStartGeometry geometry, FermatPruningProfile profile)
    {
        // x² < 9N/8 is represented exactly as x² <= floor((9N-1)/8).
        var bfrUpperSquare = geometry.N + ((geometry.N - BigInteger.One) / 8);
        var bfrUpperX = FixedWidthFermatRoot.Floor(bfrUpperSquare);
        var calculatedSpan = BigInteger.Max(BigInteger.Zero, bfrUpperX - geometry.StartX);
        var effectiveSpan = BigInteger.Max(geometry.GeometricSpan, calculatedSpan);
        var rawEnd = geometry.StartX + effectiveSpan;
        var effectiveEnd = FirstFermatParityAtOrAbove(rawEnd, geometry.N);
        var band = FermatTangentBand.Create(geometry, profile, effectiveEnd);

        return new CompositeSearchContext(
            geometry,
            profile,
            calculatedSpan,
            effectiveSpan,
            effectiveEnd,
            geometry.GeometricSpan < calculatedSpan,
            band);
    }

    private static CompositeFermatSearchResult ExactSquareResult(CompositeSearchContext context)
    {
        var certificate = CreateCertificate(context.Geometry.N, context.Geometry.FloorRoot, context.Geometry.FloorRoot);
        var trace = CreateTrace(
            context,
            FermatSearchOutcome.ExactSquare,
            tangentCount: BigInteger.Zero,
            lowerCount: BigInteger.Zero,
            upperCount: BigInteger.Zero,
            divisibilityChecks: BigInteger.Zero,
            initial: BigInteger.Zero,
            afterBounds: BigInteger.Zero,
            afterParity: BigInteger.Zero,
            parityRejected: BigInteger.Zero,
            afterMask: BigInteger.Zero,
            maskRejected: BigInteger.Zero,
            afterCrt: BigInteger.Zero,
            rejected7: BigInteger.Zero,
            rejected31: BigInteger.Zero,
            rejected127: BigInteger.Zero,
            rootChecks: BigInteger.Zero,
            exactSquares: BigInteger.Zero,
            reconstructions: BigInteger.One);
        return new CompositeFermatSearchResult(certificate, trace);
    }

    private static CompositeFermatSearchResult SearchFermatCoordinates(CompositeSearchContext context)
    {
        var initial = context.EffectiveEndX - context.Geometry.StartX + BigInteger.One;
        var firstCandidate = FirstFermatParityAtOrAbove(context.Geometry.StartX, context.Geometry.N);
        var afterParity = CountStepTwo(firstCandidate, context.EffectiveEndX);
        var parityRejected = initial - afterParity;
        var afterMask = BigInteger.Zero;
        var afterCrt = BigInteger.Zero;
        var maskRejected = BigInteger.Zero;
        var rejected7 = BigInteger.Zero;
        var rejected31 = BigInteger.Zero;
        var rejected127 = BigInteger.Zero;
        var rootChecks = BigInteger.Zero;
        var exactSquares = BigInteger.Zero;

        for (var x = firstCandidate; x <= context.EffectiveEndX; x += 2)
        {
            var delta = x * x - context.Geometry.N;
            if (!SquareResidueFilters.CouldBeSquareModulo64(delta))
            {
                maskRejected++;
                continue;
            }

            afterMask++;
            if (!SquareResidueFilters.CouldBeSquareModulo7(delta))
            {
                rejected7++;
                continue;
            }

            if (!SquareResidueFilters.CouldBeSquareModulo31(delta))
            {
                rejected31++;
                continue;
            }

            if (!SquareResidueFilters.CouldBeSquareModulo127(delta))
            {
                rejected127++;
                continue;
            }

            afterCrt++;
            rootChecks++;
            var y = FixedWidthFermatRoot.Floor(delta);
            if (y * y != delta)
            {
                continue;
            }

            exactSquares++;
            var p = x - y;
            var q = x + y;
            if (p <= BigInteger.One || q <= BigInteger.One || p * q != context.Geometry.N)
            {
                continue;
            }

            var trace = CreateTrace(
                context,
                FermatSearchOutcome.FactorFound,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                initial,
                initial,
                afterParity,
                parityRejected,
                afterMask,
                maskRejected,
                afterCrt,
                rejected7,
                rejected31,
                rejected127,
                rootChecks,
                exactSquares,
                BigInteger.One);
            return new CompositeFermatSearchResult(CreateCertificate(context.Geometry.N, p, q), trace);
        }

        return new CompositeFermatSearchResult(
            null,
            CreateTrace(
                context,
                FermatSearchOutcome.NotFoundWithinDeclaredProfile,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                initial,
                initial,
                afterParity,
                parityRejected,
                afterMask,
                maskRejected,
                afterCrt,
                rejected7,
                rejected31,
                rejected127,
                rootChecks,
                exactSquares,
                BigInteger.Zero));
    }

    private static CompositeFermatSearchResult SearchTangentCandidates(
        CompositeSearchContext context,
        FermatFactorCandidateOrientation orientation)
    {
        var tangentCount = context.Band.CandidateCount;
        var selectedCount = orientation == FermatFactorCandidateOrientation.LowerFactor
            ? context.Band.LowerCandidateCount
            : context.Band.UpperCandidateCount;
        var divisibilityChecks = BigInteger.Zero;

        foreach (var candidate in context.Band.Enumerate(orientation))
        {
            divisibilityChecks++;
            if (context.Geometry.N % candidate != BigInteger.Zero)
            {
                continue;
            }

            var complement = context.Geometry.N / candidate;
            if (candidate <= BigInteger.One || complement <= BigInteger.One || candidate * complement != context.Geometry.N)
            {
                continue;
            }

            var trace = CreateTrace(
                context,
                FermatSearchOutcome.FactorFound,
                tangentCount,
                context.Band.LowerCandidateCount,
                context.Band.UpperCandidateCount,
                divisibilityChecks,
                tangentCount,
                tangentCount,
                tangentCount,
                BigInteger.Zero,
                tangentCount,
                BigInteger.Zero,
                tangentCount,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.One);
            return new CompositeFermatSearchResult(CreateCertificate(context.Geometry.N, candidate, complement), trace);
        }

        return new CompositeFermatSearchResult(
            null,
            CreateTrace(
                context,
                FermatSearchOutcome.NotFoundWithinDeclaredProfile,
                tangentCount,
                context.Band.LowerCandidateCount,
                context.Band.UpperCandidateCount,
                divisibilityChecks,
                tangentCount,
                tangentCount,
                tangentCount,
                BigInteger.Zero,
                tangentCount,
                BigInteger.Zero,
                tangentCount,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero,
                BigInteger.Zero));
    }

    private static FermatRegionTrace CreateTrace(
        CompositeSearchContext context,
        FermatSearchOutcome outcome,
        BigInteger tangentCount,
        BigInteger lowerCount,
        BigInteger upperCount,
        BigInteger divisibilityChecks,
        BigInteger initial,
        BigInteger afterBounds,
        BigInteger afterParity,
        BigInteger parityRejected,
        BigInteger afterMask,
        BigInteger maskRejected,
        BigInteger afterCrt,
        BigInteger rejected7,
        BigInteger rejected31,
        BigInteger rejected127,
        BigInteger rootChecks,
        BigInteger exactSquares,
        BigInteger reconstructions) =>
        new(new FermatRegionTraceData
        {
            Geometry = context.Geometry,
            Profile = context.Profile,
            Ordering = context.Profile.Ordering,
            Outcome = outcome,
            GeometricSpan = context.Geometry.GeometricSpan,
            CalculatedSpan = context.CalculatedSpan,
            EffectiveSpan = context.EffectiveSpan,
            EffectiveEndX = context.EffectiveEndX,
            RangeExpansionApplied = context.RangeExpansionApplied,
            TangentBandOffset = context.Profile.TangentBandOffset,
            TangentBandCandidateCount = tangentCount,
            PFactorCandidates = lowerCount,
            QFactorCandidates = upperCount,
            ParityStep = 2,
            DivisibilityChecks = divisibilityChecks,
            InitialCandidates = initial,
            AfterRelativeBounds = afterBounds,
            AfterParity = afterParity,
            ParityRejected = parityRejected,
            AfterBitMask = afterMask,
            MaskRejected = maskRejected,
            AfterCrtResidues = afterCrt,
            CrtModulo7Rejected = rejected7,
            CrtModulo31Rejected = rejected31,
            CrtModulo127Rejected = rejected127,
            ExactRootChecks = rootChecks,
            ExactSquares = exactSquares,
            FinalReconstruction = reconstructions,
        });

    private static FermatFactorizationResult CreateCertificate(BigInteger n, BigInteger first, BigInteger second)
    {
        var p = BigInteger.Min(first, second);
        var q = BigInteger.Max(first, second);
        var x = (p + q) / 2;
        var y = (q - p) / 2;
        var delta = x * x - n;
        if (p <= BigInteger.One || q <= BigInteger.One || p * q != n || y * y != delta)
        {
            throw new InvalidOperationException("An exact composite certificate could not be reconstructed.");
        }

        var inputBits = checked((int)n.GetBitLength());
        var factorBits = checked((int)BigInteger.Max(p.GetBitLength(), q.GetBitLength()));
        return new FermatFactorizationResult(n, p, q, x, y, delta, inputBits, factorBits, BigInteger.One << factorBits);
    }

    private static BigInteger FirstFermatParityAtOrAbove(BigInteger value, BigInteger n)
    {
        var mustBeEven = n % 4 != BigInteger.One;
        return value.IsEven == mustBeEven ? value : value + BigInteger.One;
    }

    private static BigInteger CountStepTwo(BigInteger first, BigInteger last) =>
        first > last ? BigInteger.Zero : ((last - first) / 2) + BigInteger.One;

    private sealed record CompositeSearchContext(
        FermatStartGeometry Geometry,
        FermatPruningProfile Profile,
        BigInteger CalculatedSpan,
        BigInteger EffectiveSpan,
        BigInteger EffectiveEndX,
        bool RangeExpansionApplied,
        FermatTangentBand Band);
}
