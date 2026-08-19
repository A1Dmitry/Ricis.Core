using System;

internal static class RegressionAssertions
{
    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Expect<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    public static void AssertClose(
        double actual,
        double expected,
        double tolerance,
        Func<string> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);
        if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException(messageFactory());
        }
    }
}
