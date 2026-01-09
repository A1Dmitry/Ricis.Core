// --- 3. IDENTITY (∞_0) ---
using Ricis.Core;
using System.Linq.Expressions;
using Ricis.Core.Expressions;

public sealed class ZeroInfinityExpression : InfinityExpression
{
    // DRY FIX: Используем глобальную константу из RicisType
    public override Expression Numerator => RicisType.InfinityZero;

    public override bool CanReduce => false;

    public ZeroInfinityExpression(List<(ParameterExpression, double)> roots)
        : base(roots) { }

    

    public override string ToString() => FormatInfinity("0", Roots);
}