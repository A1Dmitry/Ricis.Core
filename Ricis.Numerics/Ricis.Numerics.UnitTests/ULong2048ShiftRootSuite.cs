using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ricis.Numerics.UnitTests;

[TestClass]
public sealed class ULong2048ShiftRootSuite
{
    [TestMethod("ROOT01: fixed-width root maps zero to zero")]
    public void ZeroMapsToZero()
    {
        Assert.AreEqual(ULong2048.Zero, ULong2048.IntegerSquareRootFloor(ULong2048.Zero));
    }

    [TestMethod("ROOT02: fixed-width root maps one to one")]
    public void OneMapsToOne()
    {
        var root = ULong2048.IntegerSquareRootFloor(ULong2048.One);
        Assert.AreEqual(ULong2048.One, root);
        Assert.AreEqual(ULong2048.One, root * root);
    }

    [TestMethod("ROOT03: fixed-width root floors two and three to one")]
    public void TwoAndThreeFloorToOne()
    {
        Assert.AreEqual(ULong2048.One, ULong2048.IntegerSquareRootFloor(new ULong2048(2)));
        Assert.AreEqual(ULong2048.One, ULong2048.IntegerSquareRootFloor(new ULong2048(3)));
    }

    [TestMethod("ROOT04: fixed-width root accepts small exact squares")]
    public void SmallExactSquaresRoundTrip()
    {
        foreach (var expected in new ulong[] { 2, 3, 8, 244 })
        {
            var value = new ULong2048(expected);
            Assert.AreEqual(value, ULong2048.IntegerSquareRootFloor(value * value));
        }
    }

    [TestMethod("ROOT05: fixed-width root transitions exactly around selected squares")]
    public void OneBitBoundaryCorrectionIsExact()
    {
        foreach (var encodedRoot in new[]
                 {
                     (BigInteger.One << 64) + 7,
                     (BigInteger.One << 512) + 37,
                     (BigInteger.One << 1023) + 17,
                 })
        {
            var square = encodedRoot * encodedRoot;
            var below = ULong2048.IntegerSquareRootFloor(ULong2048.FromBigInteger(square - BigInteger.One));
            var exact = ULong2048.IntegerSquareRootFloor(ULong2048.FromBigInteger(square));
            var above = ULong2048.IntegerSquareRootFloor(ULong2048.FromBigInteger(square + BigInteger.One));

            Assert.AreEqual(ULong2048.FromBigInteger(encodedRoot - BigInteger.One), below);
            Assert.AreEqual(ULong2048.FromBigInteger(encodedRoot), exact);
            Assert.AreEqual(ULong2048.FromBigInteger(encodedRoot), above);
        }
    }

    [TestMethod("ROOT06: fixed-width root returns exact 512 1024 and 2047-bit roots")]
    public void CrossLimbPerfectSquaresRoundTrip()
    {
        foreach (var expected in new[]
                 {
                     (BigInteger.One << 511) + 1,
                     (BigInteger.One << 1023) + (BigInteger.One << 477) + 13,
                     (BigInteger.One << 1023) + (BigInteger.One << 1000) + 17,
                 })
        {
            var root = ULong2048.IntegerSquareRootFloor(ULong2048.FromBigInteger(expected * expected));
            Assert.AreEqual(ULong2048.FromBigInteger(expected), root);
        }
    }

    [TestMethod("ROOT07: fixed-width root handles maximum representable input without upper-square overflow")]
    public void MaximumInputHasExactFloorRoot()
    {
        var expected = (BigInteger.One << 1024) - BigInteger.One;
        var root = ULong2048.IntegerSquareRootFloor(ULong2048.MaxValue);

        Assert.AreEqual(ULong2048.FromBigInteger(expected), root);
        AssertFloorCertificate(ULong2048.MaxValue, root);
    }

    [TestMethod("ROOT08: fixed-width root has floor certificate near 2048-bit maximum")]
    public void NearMaximumNonSquareHasFloorCertificate()
    {
        var input = ULong2048.FromBigInteger(((BigInteger.One << 2048) - 1) - 123_456_789);
        var root = ULong2048.IntegerSquareRootFloor(input);

        AssertFloorCertificate(input, root);
        Assert.AreEqual(FloorRootOracle(input.ToBigInteger()), root.ToBigInteger());
    }

    [TestMethod("ROOT09: deterministic corpus agrees with independent BigInteger oracle")]
    public void DeterministicCorpusAgreesWithOracle()
    {
        for (var bitLength = 1; bitLength <= 2048; bitLength += 67)
        {
            var value = (BigInteger.One << (bitLength - 1)) + (BigInteger.One << Math.Max(0, bitLength / 3)) + bitLength;
            if (value.GetBitLength() > 2048) value = (BigInteger.One << 2048) - bitLength;
            var input = ULong2048.FromBigInteger(value);
            var root = ULong2048.IntegerSquareRootFloor(input);

            Assert.AreEqual(FloorRootOracle(value), root.ToBigInteger(), $"bitLength={bitLength}");
            AssertFloorCertificate(input, root);
        }
    }

    [TestMethod("ROOT10: fixed-width root hot path allocates no managed memory")]
    public void FixedWidthRootHotPathIsAllocationFree()
    {
        var root = ULong2048.FromBigInteger((BigInteger.One << 1023) + (BigInteger.One << 511) + 31);
        var input = root * root;
        _ = ULong2048.IntegerSquareRootFloor(input);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = ULong2048.Zero;
        for (var iteration = 0; iteration < 128; iteration++) result = ULong2048.IntegerSquareRootFloor(input);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.AreEqual(root, result);
        Assert.AreEqual(0L, allocated);
    }

    [TestMethod("FROOT01: fixed-width root accepts exact N-only Fermat delta")]
    public void ExactFermatDeltaIsAccepted()
    {
        var n = new ULong2048(5959);
        var x = new ULong2048(80);
        var delta = (x * x) - n;
        var y = ULong2048.IntegerSquareRootFloor(delta);

        Assert.AreEqual(new ULong2048(441), delta);
        Assert.AreEqual(new ULong2048(21), y);
        Assert.AreEqual(delta, y * y);
    }

    [TestMethod("FROOT02: fixed-width root rejects one-above exact Fermat delta")]
    public void OneAboveExactFermatDeltaIsNotAcceptedAsSquare()
    {
        var delta = new ULong2048(442);
        var y = ULong2048.IntegerSquareRootFloor(delta);

        Assert.AreEqual(new ULong2048(21), y);
        Assert.AreNotEqual(delta, y * y);
    }

    private static void AssertFloorCertificate(ULong2048 input, ULong2048 root)
    {
        var inputOracle = input.ToBigInteger();
        var rootOracle = root.ToBigInteger();
        Assert.IsTrue(rootOracle * rootOracle <= inputOracle);
        Assert.IsTrue((rootOracle + BigInteger.One) * (rootOracle + BigInteger.One) > inputOracle);
    }

    private static BigInteger FloorRootOracle(BigInteger value)
    {
        if (value < 2) return value;
        var root = BigInteger.One << checked((int)((value.GetBitLength() + 1) / 2));
        while (true)
        {
            var next = (root + value / root) >> 1;
            if (next >= root) return root;
            root = next;
        }
    }
}
