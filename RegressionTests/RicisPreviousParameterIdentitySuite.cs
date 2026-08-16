using Ricis.Core.Proofs;

internal static class RicisPreviousParameterIdentitySuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PREV01: равные ответы F(x)=F(x−1) дают (F(x)/F(x−1))-1 = 0", EqualResponsesProduceZero),
        ("PREV02: нулевые ответы проходят через RICIS 0_F/0_F = 1 и дают 0", ZeroResponseUsesRicisBridge),
        ("PREV03: разные ответы F(x) и F(x−1) не принимаются как тождественные", DifferentResponsesAreRejected),
    ];

    private static void EqualResponsesProduceZero()
    {
        var proofCase = new RicisPreviousParameterIdentityProofCase(_ => 7.0);
        var result = proofCase.Run();

        Require(result.DerivedExpression.EndsWith("=> 0", StringComparison.Ordinal),
            $"Ожидался нулевой результат, получено {result.DerivedExpression}.");
        Require(proofCase.Monitor.Any(entry => entry.Stage == "PARAMETERS" && entry.Status == "PASS") &&
                proofCase.Monitor.Any(entry => entry.Stage == "IDENTITY" && entry.Status == "PASS") &&
                proofCase.Monitor.Any(entry => entry.Stage == "RATIO" && entry.Status == "PASS"),
            "Должны быть явно зафиксированы x, x−1, identity premise и ratio reduction.");
        Require(result.Document.Contains("F(x)", StringComparison.Ordinal) &&
                result.Document.Contains("F(x−1)", StringComparison.Ordinal),
            "Proof document обязан различать F(x) и F(x−1).");
    }

    private static void ZeroResponseUsesRicisBridge()
    {
        var proofCase = new RicisPreviousParameterIdentityProofCase(_ => 0.0);
        var result = proofCase.Run();

        Require(result.DerivedExpression.EndsWith("=> 0", StringComparison.Ordinal),
            $"RICIS bridge 0_F/0_F должен дать 0 после вычитания единицы, получено {result.DerivedExpression}.");
    }

    private static void DifferentResponsesAreRejected()
    {
        var proofCase = new RicisPreviousParameterIdentityProofCase(x => x);
        try
        {
            proofCase.Run();
            throw new InvalidOperationException("F(u)=u не должна объявляться равной F(x−1) без equality premise.");
        }
        catch (ArgumentException)
        {
            Require(proofCase.Monitor.Any(entry => entry.Stage == "IDENTITY" && entry.Status == "BLOCKED"),
                "Отклонение различных F(x) и F(x−1) должно быть видно в monitor.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
