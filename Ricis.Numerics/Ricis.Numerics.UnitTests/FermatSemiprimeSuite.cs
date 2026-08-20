using System.Numerics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Numerics.Factorization;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class FermatSemiprimeSuite
{
    [TestMethod("FERMAT-01: migrated baseline factors canonical odd semiprime")]
    public void MigratedBaselineFactorsCanonicalOddSemiprime()
    {
        var result = FermatFactorizer.Solve(new BigInteger(5959));

        Assert.AreEqual(new BigInteger(5959), result.N);
        Assert.AreEqual(new BigInteger(59), result.P);
        Assert.AreEqual(new BigInteger(101), result.Q);
        Assert.AreEqual(result.Y * result.Y, result.Delta);
        Assert.AreEqual(result.N, result.P * result.Q);
    }

    [TestMethod("FERMAT-02: mask-pruned N-only search publishes exact work counters")]
    public void MaskPrunedNOnlySearchPublishesExactWorkCounters()
    {
        var search = FermatFactorizer.Search(new BigInteger(5959));

        Assert.AreEqual(new BigInteger(5959), search.Factorization.N);
        Assert.AreEqual(search.CandidatePoints, search.MaskRejected + search.MaskPassed);
        Assert.AreEqual(search.MaskPassed, search.ExactSquareRootChecks);
        Assert.IsTrue(search.MaskRejected > BigInteger.Zero);
        Assert.IsTrue(search.ExactSquareRootChecks > BigInteger.Zero);
    }

    [TestMethod("FERMAT-03: migrated baseline rejects non-positive or even input")]
    public void MigratedBaselineRejectsInvalidInput()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatFactorizer.Solve(BigInteger.Zero));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => FermatFactorizer.Solve(new BigInteger(10)));
    }

    [DataTestMethod]
    [DataRow("0", "0")]
    [DataRow("1", "1")]
    [DataRow("2", "1")]
    [DataRow("3", "1")]
    [DataRow("4", "2")]
    [DataRow("5959", "77")]
    [DataRow("1000000", "1000")]
    [TestMethod("FERMAT-04: shift root returns exact floor on boundary matrix")]
    public void ShiftRootReturnsExactFloorOnBoundaryMatrix(string encodedValue, string encodedExpected)
    {
        var value = BigInteger.Parse(encodedValue, System.Globalization.CultureInfo.InvariantCulture);
        var expected = BigInteger.Parse(encodedExpected, System.Globalization.CultureInfo.InvariantCulture);
        var root = FermatFactorizer.IntegerSquareRootFloorByShift(value);

        Assert.AreEqual(expected, root);
        Assert.IsTrue(root * root <= value);
        Assert.IsTrue((root + BigInteger.One) * (root + BigInteger.One) > value);
    }

    [TestMethod("FERMAT-05: shift root consumes a 2048-bit perfect square exactly")]
    public void ShiftRootConsumes2048BitPerfectSquareExactly()
    {
        var expected = (BigInteger.One << 1023) + (BigInteger.One << 511) + 17;
        var square = expected * expected;
        var root = FermatFactorizer.IntegerSquareRootFloorByShift(square);

        Assert.AreEqual(expected, root);
        Assert.AreEqual(square, root * root);
    }

    [TestMethod("FERMAT-06: shift root one-bit correction preserves floor certificate around square")]
    public void ShiftRootOneBitCorrectionPreservesFloorCertificateAroundSquare()
    {
        var baseRoot = (BigInteger.One << 1024) + 31337;
        var square = baseRoot * baseRoot;
        var below = FermatFactorizer.IntegerSquareRootFloorByShift(square - BigInteger.One);
        var exact = FermatFactorizer.IntegerSquareRootFloorByShift(square);
        var above = FermatFactorizer.IntegerSquareRootFloorByShift(square + BigInteger.One);

        Assert.AreEqual(baseRoot - BigInteger.One, below);
        Assert.AreEqual(baseRoot, exact);
        Assert.AreEqual(baseRoot, above);
    }

    [TestMethod("SEMIPRIME-01: N-only BigInteger constructor recovers validated canonical pair")]
    public void NOnlyBigIntegerConstructorRecoversCanonicalPair()
    {
        var value = new Semiprime<BigInteger>(new BigInteger(5959));

        Assert.AreEqual(new BigInteger(5959), value.N);
        Assert.AreEqual(new BigInteger(59), value.P);
        Assert.AreEqual(new BigInteger(101), value.Q);
    }

    [TestMethod("SEMIPRIME-02: pair constructor canonicalizes reversed prime pair")]
    public void PairConstructorCanonicalizesReversedPair()
    {
        var value = new Semiprime<BigInteger>(new BigInteger(101), new BigInteger(59));

        Assert.AreEqual(new BigInteger(59), value.P);
        Assert.AreEqual(new BigInteger(101), value.Q);
        Assert.AreEqual(new BigInteger(5959), value.N);
    }

    [TestMethod("SEMIPRIME-03: pair constructor admits odd prime square")]
    public void PairConstructorAdmitsOddPrimeSquare()
    {
        var value = new Semiprime<BigInteger>(new BigInteger(101), new BigInteger(101));

        Assert.AreEqual(new BigInteger(101), value.P);
        Assert.AreEqual(new BigInteger(101), value.Q);
        Assert.AreEqual(new BigInteger(10201), value.N);
    }

    [TestMethod("SEMIPRIME-04: constructors reject invalid domain values fail-closed")]
    public void ConstructorsRejectInvalidDomainValuesFailClosed()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new Semiprime<BigInteger>(BigInteger.One));
        Assert.ThrowsException<ArgumentException>(() => _ = new Semiprime<BigInteger>(new BigInteger(10)));
        Assert.ThrowsException<ArgumentException>(() => _ = new Semiprime<BigInteger>(new BigInteger(101)));
        Assert.ThrowsException<ArgumentException>(() => _ = new Semiprime<BigInteger>(new BigInteger(9), new BigInteger(101)));
        Assert.ThrowsException<ArgumentException>(() => _ = new Semiprime<BigInteger>(new BigInteger(2), new BigInteger(101)));
    }

    [TestMethod("SEMIPRIME-05: derived Fermat coordinates reconstruct factors exactly")]
    public void DerivedFermatCoordinatesReconstructFactorsExactly()
    {
        var value = new Semiprime<BigInteger>(new BigInteger(59), new BigInteger(101));

        Assert.AreEqual(new BigInteger(42), value.Gap);
        Assert.AreEqual(new BigInteger(80), value.FermatX);
        Assert.AreEqual(new BigInteger(21), value.FermatY);
        Assert.AreEqual(value.N, value.FermatX * value.FermatX - value.FermatY * value.FermatY);
        Assert.AreEqual(value.P, value.FermatX - value.FermatY);
        Assert.AreEqual(value.Q, value.FermatX + value.FermatY);
    }

    [TestMethod("SEMIPRIME-06: immutable public surface has only validated constructors")]
    public void ImmutablePublicSurfaceHasOnlyValidatedConstructors()
    {
        var type = typeof(Semiprime<BigInteger>);
        Assert.IsTrue(type.IsSealed);
        Assert.IsTrue(type.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(property => !property.CanWrite));
        CollectionAssert.AreEqual(
            new[] { 1, 2 },
            type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(constructor => constructor.GetParameters().Length)
                .Order()
                .ToArray());
        Assert.IsTrue(typeof(SemiprimeBase<BigInteger>)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field => field.Name is "number" or "smallerFactor" or "greaterFactor")
            .All(field => field.IsInitOnly));
    }

    [TestMethod("SEMIPRIME-07: canonical reduction expression has exact immutable scalar shape")]
    public void CanonicalReductionExpressionHasExactImmutableScalarShape()
    {
        var semiprime = new Semiprime<BigInteger>(new BigInteger(59), new BigInteger(101));
        var expression = semiprime.CanonicalReductionExpression;

        Assert.AreEqual(typeof(Func<BigInteger, BigInteger>), expression.Type);
        Assert.IsInstanceOfType<BinaryExpression>(expression.Body);
        var divide = (BinaryExpression)expression.Body;
        Assert.AreEqual(System.Linq.Expressions.ExpressionType.Divide, divide.NodeType);
        Assert.AreEqual(typeof(BigInteger), divide.Type);
    }

    [TestMethod("SEMIPRIME-08: Int2048 participates through generic INumber contract")]
    public void Int2048ParticipatesThroughGenericContract()
    {
        var value = new Semiprime<Int2048>(new Int2048(101), new Int2048(59));

        Assert.AreEqual(new Int2048(59), value.P);
        Assert.AreEqual(new Int2048(101), value.Q);
        Assert.AreEqual(new Int2048(5959), value.N);
        Assert.AreEqual(value.N, value.FermatX * value.FermatX - value.FermatY * value.FermatY);
    }

    [TestMethod("SEMIPRIME-09: ULong2048 participates through generic exact-operator contract")]
    public void ULong2048ParticipatesThroughGenericContract()
    {
        var value = new Semiprime<ULong2048>(new ULong2048(101), new ULong2048(59));

        Assert.AreEqual(new ULong2048(59), value.P);
        Assert.AreEqual(new ULong2048(101), value.Q);
        Assert.AreEqual(new ULong2048(5959), value.N);
        Assert.AreEqual(value.N, value.FermatX * value.FermatX - value.FermatY * value.FermatY);
    }

    [TestMethod("SEMIPRIME-10: ULong2048 standard identity contracts map to canonical values")]
    public void ULong2048StandardIdentityContractsMapToCanonicalValues()
    {
        Assert.AreEqual(ULong2048.Zero, ULong2048.AdditiveIdentity);
        Assert.AreEqual(ULong2048.One, ULong2048.MultiplicativeIdentity);
    }
}
