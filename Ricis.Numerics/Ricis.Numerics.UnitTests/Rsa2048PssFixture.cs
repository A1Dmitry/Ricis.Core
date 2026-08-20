#nullable enable

using System.Numerics;
using System.Security.Cryptography;
using Ricis.Numerics.Cryptography;

namespace Ricis.Numerics.UnitTests;

internal static class Rsa2048PssFixture
{
    private const string PHex = "F2B473CC41EA736A9A505131B791874689DAEA3FE816BA3D532B851A48239B81A2067F386EA801997971B07FB35538C6165FE418BFD7985DBC0A167C14F36CD38D277AABAE29A8DA6D7A45BBE1B4F009089EF798C5DDDDBA9B9436A40BFFE2972B524A1BC0F6FE91EF7FBFAE68AB2EEB078A8394261A8AEBA876F0291315D3DF";
    private const string QHex = "E9F57E7340E91796E045DCC68545E6F66F237F1761EFD853F50A3B0D871CBAE8C1188C50794F45D8D663B14CEFD3246CA28BD94108AE4F44725D5E7B9D70DBFA6F502ECC1C598A9B51D1EB6BD069056DDFA1AF152871E539FAC824FD41B658851EC462D4458C93B139CAE18F31984A80DA7148529B2CF8C69F693F3A1E406C45";
    private const string DHex = "1E4731D202A0B9527001C5AA6A63DF7B01AF8D6F4C14E8637C21CF428B4B3365E3A32B4F926D0D1494D4DB1EC8877C7DCF8BB0032EE1F9CAD58F6C1B8D7DBDB0614BF42429E0AC8CA555E2FEBE966F536AD7DA4928BE73EDDF62F6C9DB4E8F3D42D33BFFFF8865B70AAF0E1D6C6297D11986AAB4FDBAEFFDE72CD84E77BB56AFCCD3FAB67B279E41DFBEB7BB62D03BDEAB6C3EF9C2E6F2C4DA7DFB3CA1FBAD65D45948FFBED40D5667EC08526E1CE2E663D1A90589897AA066CD643777F8FCB8A4D96F2803E12FDAA62A4C779454F9B753C4D5FF543547D18AC6148E6E97C98E2D20776E72F520D6A594468B6A4C04572B042406A71913F75BAE066BD9AB7E45";
    private const string DpHex = "7D71595C9C41227059DBE36B6FF6AED57D91017C106816D075794BED5E95D0DD3AB262F4F6F4AD06F72714D39C0C133107057EEE6FD16DF61CA7962181EEA333E084243A31E56459A936066EEA64CB9FCFADE2493B13C67399D00C41D3D5E8F6BB34680B5A3C0F2DBC7CE9C4AA62B7F850487E63872236E5408860EEC9E7813F";
    private const string DqHex = "BA57237DDC874A988ACC5A096C00BEF22C96D314E6964770A74C9CB82B9300737DC875896AF56EA6442B66FDE64DFF46DF380FB3B29C52DA2B549E7A4A6DA76791DD0548E09398C818A4DFA3217D642B9CE084388FDC173CD4B7306EDE35C3CF53300B9F123DE32C56E17641BCB8952E87CD8E52ECF126BBDC1FD19212A23119";
    private const string InverseQHex = "0FE486DAE6BFAB032ED372FDB0EA1C81F42E2E1AEA6566C6862F57DDCC968FBD0D08F7BE6346A4A962C90F01C50B2E680E60ECBDCFB3F1B0B7800238209B08AAC3F85FC4FC2B46B5EF4747E88370F1C8EEC5B1DF54191C1CF1435491A05B5EB1499909722EDD9148E03C412B67B22685066E4CB22A0691588293769DBB4AF2AF";
    private const string ExponentHex = "010001";
    private const string ModulusHex = "DDCEFBF9D0121F2D32BF09B86A35D761A7D02AEDBBF2CB05181B87A722C1824DC25835FD80BEC1B043CD8A2DE13EF5AEDB5303FA880B0BBDC3D426C11D819E9B003480CAEC611D50F95F245D59EB8E72F427F5EB08FF7FD97B9E98F46FEFE53FB206F06597536D25D14120CD6ACA05BE31A432C119E17A6FD5A6B6C94FD8A7EFEA670EF7AD63DF72089D00F49D43E74035B57C8998BF99E6D86EC3A3ED312EA3DBF1C553C7F41DF6552645620C0EA73E8101E4A2D4C1B753B36FDB2B420F55C08ED6A80B9509249F720D95B89BE741B82C9B08150646876CD3F80A828D02D59878D8862BAAAF148E9D103347F4F7E516921CA86F9BA0B87D896E44EE74042F1B";

    internal static BigInteger P => ReadUnsigned(PHex);
    internal static BigInteger Q => ReadUnsigned(QHex);
    internal static BigInteger Modulus => P * Q;
    internal static ULong2048 Modulus2048 => ULong2048.FromBigInteger(Modulus);
    internal static ULong2048 PublicExponent => ULong2048.FromBigInteger(ReadUnsigned(ExponentHex));
    internal static Rsa2048PublicKey PublicKey => new(Modulus2048, PublicExponent);

    internal static void AssertArithmeticFixture()
    {
        Assert.AreNotEqual(P, Q);
        Assert.AreEqual(1024, P.GetBitLength());
        Assert.AreEqual(1024, Q.GetBitLength());
        AssertStrongProbablePrime(P);
        AssertStrongProbablePrime(Q);
        Assert.AreEqual(2048, Modulus.GetBitLength());
        Assert.AreEqual(BigInteger.Zero, Modulus % P);
        Assert.AreEqual(BigInteger.Zero, Modulus % Q);
        Assert.AreEqual(ReadUnsigned(ModulusHex), Modulus);
    }

    internal static byte[] SignPssSha256(ReadOnlySpan<byte> message)
    {
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            P = Convert.FromHexString(PHex),
            Q = Convert.FromHexString(QHex),
            D = Convert.FromHexString(DHex),
            DP = Convert.FromHexString(DpHex),
            DQ = Convert.FromHexString(DqHex),
            InverseQ = Convert.FromHexString(InverseQHex),
            Exponent = Convert.FromHexString(ExponentHex),
            Modulus = Convert.FromHexString(ModulusHex)
        });
        return rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    internal static bool VerifyWithBcl(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Exponent = Convert.FromHexString(ExponentHex),
            Modulus = Convert.FromHexString(ModulusHex)
        });
        return rsa.VerifyData(message, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    private static void AssertStrongProbablePrime(BigInteger candidate)
    {
        var bases = new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 };
        Assert.IsTrue(candidate > 2 && !candidate.IsEven, "Expected a positive odd test-prime candidate.");
        var d = candidate - 1;
        var s = 0;
        while (d.IsEven)
        {
            d >>= 1;
            s++;
        }

        foreach (var baseValue in bases)
        {
            var x = BigInteger.ModPow(baseValue, d, candidate);
            if (x == BigInteger.One || x == candidate - 1) continue;

            var passes = false;
            for (var round = 1; round < s; round++)
            {
                x = (x * x) % candidate;
                if (x == candidate - 1)
                {
                    passes = true;
                    break;
                }
            }

            Assert.IsTrue(passes, $"Candidate failed Miller-Rabin base {baseValue}.");
        }
    }

    private static BigInteger ReadUnsigned(string hex) => new(Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
}
