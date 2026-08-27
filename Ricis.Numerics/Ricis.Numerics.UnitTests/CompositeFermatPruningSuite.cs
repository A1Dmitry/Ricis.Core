using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Numerics.Factorization;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class CompositeFermatPruningSuite
{
    [TestMethod("CFP-01: exact-square geometry has zero gap")]
    public void ExactSquareGeometryHasZeroGap()
    {
        var geometry = FermatStartGeometry.Create(new BigInteger(10201));

        Assert.AreEqual(FermatGeometryStatus.ExactSquare, geometry.Status);
        Assert.AreEqual(new BigInteger(101), geometry.FloorRoot);
        Assert.AreEqual(new BigInteger(101), geometry.StartX);
        Assert.AreEqual(BigInteger.Zero, geometry.StartDelta);
        Assert.AreEqual(BigInteger.Zero, geometry.GeometricSpan);
    }

    [DataTestMethod]
    [DataRow("5959", "77", "78", "125")]
    [DataRow("10403", "101", "102", "1")]
    [TestMethod("CFP-02: two-intersection geometry preserves exact root certificate")]
    public void TwoIntersectionGeometryPreservesExactRootCertificate(
        string encodedN,
        string encodedRoot,
        string encodedStartX,
        string encodedDelta)
    {
        var n = BigInteger.Parse(encodedN, System.Globalization.CultureInfo.InvariantCulture);
        var geometry = FermatStartGeometry.Create(n);

        Assert.AreEqual(FermatGeometryStatus.TwoIntersections, geometry.Status);
        Assert.AreEqual(BigInteger.Parse(encodedRoot, System.Globalization.CultureInfo.InvariantCulture), geometry.FloorRoot);
        Assert.AreEqual(BigInteger.Parse(encodedStartX, System.Globalization.CultureInfo.InvariantCulture), geometry.StartX);
        Assert.AreEqual(BigInteger.Parse(encodedDelta, System.Globalization.CultureInfo.InvariantCulture), geometry.StartDelta);
        Assert.IsTrue(geometry.FloorRoot * geometry.FloorRoot <= n);
        Assert.IsTrue((geometry.FloorRoot + BigInteger.One) * (geometry.FloorRoot + BigInteger.One) > n);
        Assert.IsTrue(geometry.GeometricSpan > BigInteger.Zero);
    }

    [TestMethod("CFP-03: start geometry rejects invalid composite search input")]
    public void StartGeometryRejectsInvalidCompositeSearchInput()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatStartGeometry.Create(BigInteger.Zero));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatStartGeometry.Create(new BigInteger(-1)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatStartGeometry.Create(new BigInteger(10)));
    }

    [TestMethod("CFP-04: profile factory rejects an undefined ordering or negative offset")]
    public void ProfileFactoryRejectsInvalidParameters()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatPruningProfile.Create((FermatSearchOrdering)99, BigInteger.Zero));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatPruningProfile.Create(FermatSearchOrdering.FermatCoordinates, -BigInteger.One));
    }

    [TestMethod("CFP-05: profile factory preserves selected ordering and explicit offset")]
    public void ProfileFactoryPreservesSelectedOrderingAndOffset()
    {
        var profile = FermatPruningProfile.Create(FermatSearchOrdering.TangentLowerFactor, new BigInteger(2));

        Assert.AreEqual(FermatSearchOrdering.TangentLowerFactor, profile.Ordering);
        Assert.AreEqual(new BigInteger(2), profile.TangentBandOffset);
    }

    [TestMethod("CFP-06: tangent band emits only odd lower factor candidates with step two")]
    public void TangentBandEmitsOnlyOddLowerFactorCandidatesWithStepTwo()
    {
        var geometry = FermatStartGeometry.Create(new BigInteger(5959));
        var profile = FermatPruningProfile.Create(FermatSearchOrdering.TangentLowerFactor, new BigInteger(2));
        var band = FermatTangentBand.Create(geometry, profile, new BigInteger(80));
        var candidates = band.Enumerate(FermatFactorCandidateOrientation.LowerFactor).ToArray();

        CollectionAssert.Contains(candidates, new BigInteger(59));
        Assert.IsTrue(candidates.All(candidate => !candidate.IsEven));
        Assert.IsTrue(candidates.Zip(candidates.Skip(1)).All(pair => pair.Second - pair.First == 2));
        Assert.AreEqual(new BigInteger(candidates.Length), band.LowerCandidateCount);
        Assert.AreEqual(new BigInteger(2), band.Step);
    }

    [TestMethod("CFP-07: tangent band rejects an undefined orientation")]
    public void TangentBandRejectsUndefinedOrientation()
    {
        var geometry = FermatStartGeometry.Create(new BigInteger(5959));
        var band = FermatTangentBand.Create(geometry, FermatPruningProfile.Default, new BigInteger(80));

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => band.Enumerate((FermatFactorCandidateOrientation)99).ToArray());
    }

    [TestMethod("CFP-08: Fermat coordinate composite search reconstructs canonical fixture")]
    public void FermatCoordinateCompositeSearchReconstructsCanonicalFixture()
    {
        var result = CompositeFermatSearch.Search(new BigInteger(5959));

        Assert.IsTrue(result.HasFactorization);
        Assert.IsNotNull(result.Factorization);
        Assert.AreEqual(new BigInteger(59), result.Factorization.P);
        Assert.AreEqual(new BigInteger(101), result.Factorization.Q);
        Assert.AreEqual(new BigInteger(5959), result.Factorization.P * result.Factorization.Q);
        Assert.AreEqual(FermatSearchOutcome.FactorFound, result.Trace.Outcome);
        Assert.AreEqual(FermatSearchOrdering.FermatCoordinates, result.Trace.Ordering);
        Assert.IsTrue(result.Trace.ExactRootChecks > BigInteger.Zero);
    }

    [TestMethod("CFP-09: tangent lower factor search reaches factor with parity step two")]
    public void TangentLowerFactorSearchReachesFactorWithParityStepTwo()
    {
        var profile = FermatPruningProfile.Create(FermatSearchOrdering.TangentLowerFactor, new BigInteger(2));
        var result = CompositeFermatSearch.Search(new BigInteger(5959), profile);

        Assert.IsTrue(result.HasFactorization);
        Assert.IsNotNull(result.Factorization);
        Assert.AreEqual(new BigInteger(59), result.Factorization.P);
        Assert.AreEqual(new BigInteger(101), result.Factorization.Q);
        Assert.AreEqual(new BigInteger(2), result.Trace.ParityStep);
        Assert.IsTrue(result.Trace.DivisibilityChecks > BigInteger.Zero);
        Assert.IsTrue(result.Trace.PFactorCandidates > BigInteger.Zero);
    }

    [TestMethod("CFP-10: exact-square composite path certifies without candidate loop")]
    public void ExactSquareCompositePathCertifiesWithoutCandidateLoop()
    {
        var result = CompositeFermatSearch.Search(new BigInteger(10201));

        Assert.IsTrue(result.HasFactorization);
        Assert.IsNotNull(result.Factorization);
        Assert.AreEqual(new BigInteger(101), result.Factorization.P);
        Assert.AreEqual(new BigInteger(101), result.Factorization.Q);
        Assert.AreEqual(FermatSearchOutcome.ExactSquare, result.Trace.Outcome);
        Assert.AreEqual(BigInteger.Zero, result.Trace.InitialCandidates);
        Assert.AreEqual(BigInteger.Zero, result.Trace.DivisibilityChecks);
        Assert.AreEqual(BigInteger.One, result.Trace.FinalReconstruction);
    }

    [TestMethod("CFP-11: effective span expands a smaller geometric span to the BFR-2 calculation")]
    public void EffectiveSpanExpandsSmallerGeometricSpanToBfr2Calculation()
    {
        var result = CompositeFermatSearch.Search(new BigInteger(10403));

        Assert.IsTrue(result.Trace.CalculatedSpan > result.Trace.GeometricSpan);
        Assert.IsTrue(result.Trace.RangeExpansionApplied);
        Assert.AreEqual(result.Trace.CalculatedSpan, result.Trace.EffectiveSpan);
        Assert.IsTrue(result.Trace.EffectiveEndX >= result.Trace.Geometry.StartX + result.Trace.CalculatedSpan);
    }

    [TestMethod("CFP-12: residue tables retain every exact square residue")]
    public void ResidueTablesRetainEveryExactSquareResidue()
    {
        for (var y = 0; y < 64; y++)
        {
            Assert.IsTrue(SquareResidueFilters.CouldBeSquareModulo64(new BigInteger(y * y)));
        }

        foreach (var modulus in new[] { 7, 31, 127 })
        {
            for (var y = 0; y < modulus; y++)
            {
                var square = new BigInteger(y * y);
                var accepted = modulus switch
                {
                    7 => SquareResidueFilters.CouldBeSquareModulo7(square),
                    31 => SquareResidueFilters.CouldBeSquareModulo31(square),
                    127 => SquareResidueFilters.CouldBeSquareModulo127(square),
                    _ => false,
                };
                Assert.IsTrue(accepted, $"square {y}² must pass modulus {modulus}");
            }
        }
    }

    [TestMethod("CFP-13: trace stages are monotone and retain exact integer reporting data")]
    public void TraceStagesAreMonotoneAndRetainExactIntegerReportingData()
    {
        var trace = CompositeFermatSearch.Search(new BigInteger(5959)).Trace;

        Assert.IsTrue(trace.InitialCandidates >= trace.AfterRelativeBounds);
        Assert.IsTrue(trace.AfterRelativeBounds >= trace.AfterParity);
        Assert.IsTrue(trace.AfterParity >= trace.AfterBitMask);
        Assert.IsTrue(trace.AfterBitMask >= trace.AfterCrtResidues);
        Assert.IsTrue(trace.AfterCrtResidues >= trace.ExactRootChecks);
        Assert.IsTrue(trace.ExactRootChecks >= trace.ExactSquares);
        Assert.IsTrue(trace.ExactSquares >= trace.FinalReconstruction);
        Assert.AreEqual(trace.AfterCrtResidues, trace.RemainingFractionNumerator);
        Assert.AreEqual(trace.InitialCandidates, trace.RemainingFractionDenominator);
    }

    [TestMethod("CFP-14: fixed-width root gateway preserves floor certificate on fixture")]
    public void FixedWidthRootGatewayPreservesFloorCertificateOnFixture()
    {
        var value = new BigInteger(5959);
        var root = FixedWidthFermatRoot.Floor(value);

        Assert.AreEqual(new BigInteger(77), root);
        Assert.IsTrue(root * root <= value);
        Assert.IsTrue((root + BigInteger.One) * (root + BigInteger.One) > value);
    }

    [TestMethod("CFP-15: tangent upper factor ordering reaches symmetric Q candidate")]
    public void TangentUpperFactorOrderingReachesSymmetricQCandidate()
    {
        var profile = FermatPruningProfile.Create(FermatSearchOrdering.TangentUpperFactor, new BigInteger(2));
        var result = CompositeFermatSearch.Search(new BigInteger(5959), profile);

        Assert.IsTrue(result.HasFactorization);
        Assert.IsNotNull(result.Factorization);
        Assert.AreEqual(new BigInteger(101), result.Factorization.Q);
        Assert.IsTrue(result.Trace.QFactorCandidates > BigInteger.Zero);
        Assert.IsTrue(result.Trace.DivisibilityChecks > BigInteger.Zero);
    }

    [TestMethod("CFP-16A: tangent trace uses selected orientation count without losing total band count")]
    public void TangentTraceUsesSelectedOrientationCountWithoutLosingTotalBandCount()
    {
        var lower = CompositeFermatSearch.Search(
            new BigInteger(5959),
            FermatPruningProfile.Create(FermatSearchOrdering.TangentLowerFactor, new BigInteger(2)));
        var upper = CompositeFermatSearch.Search(
            new BigInteger(5959),
            FermatPruningProfile.Create(FermatSearchOrdering.TangentUpperFactor, new BigInteger(2)));

        Assert.AreEqual(lower.Trace.PFactorCandidates, lower.Trace.InitialCandidates);
        Assert.AreEqual(upper.Trace.QFactorCandidates, upper.Trace.InitialCandidates);
        Assert.AreEqual(lower.Trace.PFactorCandidates + lower.Trace.QFactorCandidates,
            lower.Trace.TangentBandCandidateCount);
        Assert.AreEqual(upper.Trace.PFactorCandidates + upper.Trace.QFactorCandidates,
            upper.Trace.TangentBandCandidateCount);
    }

    [TestMethod("CFP-16: geometry-dominant span is retained without calculated extension")]
    public void GeometryDominantSpanIsRetainedWithoutCalculatedExtension()
    {
        var trace = CompositeFermatSearch.Search(new BigInteger(5959)).Trace;

        Assert.IsTrue(trace.GeometricSpan > trace.CalculatedSpan);
        Assert.IsFalse(trace.RangeExpansionApplied);
        Assert.AreEqual(trace.GeometricSpan, trace.EffectiveSpan);
    }

    [TestMethod("CFP-17: bounded not-found is not a global factorization conclusion")]
    public void BoundedNotFoundIsNotAGlobalFactorizationConclusion()
    {
        var profile = FermatPruningProfile.Create(FermatSearchOrdering.TangentLowerFactor, BigInteger.Zero);
        var result = CompositeFermatSearch.Search(new BigInteger(101), profile);

        Assert.IsFalse(result.HasFactorization);
        Assert.IsNull(result.Factorization);
        Assert.AreEqual(FermatSearchOutcome.NotFoundWithinDeclaredProfile, result.Trace.Outcome);
    }

    [TestMethod("CFP-18: residue filters reject known non-square values")]
    public void ResidueFiltersRejectKnownNonSquareValues()
    {
        Assert.IsFalse(SquareResidueFilters.CouldBeSquareModulo64(new BigInteger(2)));
        Assert.IsFalse(SquareResidueFilters.CouldBeSquareModulo7(new BigInteger(3)));
        Assert.IsFalse(SquareResidueFilters.CouldBeSquareModulo31(new BigInteger(3)));
        Assert.IsFalse(SquareResidueFilters.CouldBeSquareModulo127(new BigInteger(3)));
    }

    [TestMethod("CFP-19: composite search rejects invalid N before any bounded geometry is created")]
    public void CompositeSearchRejectsInvalidNBeforeBoundedGeometryIsCreated()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CompositeFermatSearch.Search(BigInteger.One));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => CompositeFermatSearch.Search(new BigInteger(10), FermatPruningProfile.Default));
    }

    [TestMethod("CFP-20: new public result contracts are sealed and have no writable properties")]
    public void NewPublicResultContractsAreSealedAndHaveNoWritableProperties()
    {
        var types = new[]
        {
            typeof(FermatStartGeometry),
            typeof(FermatPruningProfile),
            typeof(FermatTangentBand),
            typeof(FermatRegionTrace),
            typeof(CompositeFermatSearchResult),
        };

        foreach (var type in types)
        {
            Assert.IsTrue(type.IsSealed, $"{type.Name} must be sealed.");
            Assert.IsTrue(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(property => !property.CanWrite),
                $"{type.Name} must not expose writable public state.");
        }
    }
}
