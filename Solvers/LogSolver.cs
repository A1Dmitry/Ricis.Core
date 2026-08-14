using System.Linq.Expressions;
using Ricis.Core.Solvers.ZeroSolver;

namespace Ricis.Core.Solvers;

/// <summary>
///     Log solver: ðàñïîçíà¸ò Math.Log(x) == 0 => x = 1.
///     Ðåàëèçóåò FindTrigonometricRoots/FindFirstRoot pattern ñîâìåñòèìî ñ ZeroSolverUtils.
/// </summary>
public static class LogSolver
{
    // Âîçâðàùàåò âñå òî÷íûå êîðíè äëÿ expr = 0, îòíîñèòåëüíî ïåðåäàííîãî ïàðàìåòðà.
    // Ïîääåðæèâàåò ñëó÷àè âèäà Log(x) (íàòèâíî) è âëîæåííûå âûçîâû, åñëè íàéä¸ì ÿâíûé ïàðàìåòð.
    public static ICollection<Root> FindRoots(this Expression expr, ParameterExpression parameter)
    {
        var roots = new List<Root>();
        if (parameter == null)
        {
            return roots;
        }

        // Ïðîñòàÿ ôîðìà: Log(x)
        if (expr is MethodCallExpression call && call.Method.DeclaringType == typeof(Math) &&
            call.Method.Name == "Log" && call.Arguments.Count == 1)
        {
            var arg = call.Arguments[0];
            // åñëè àðãóìåíò — èìåííî èñêîìûé ïàðàìåòð
            if (arg is ParameterExpression p && p == parameter)
            {
                // ln(x) = 0 => x = 1
                roots.Add(new Root(parameter, 1.0));
                return roots;
            }

            // Для составного аргумента log(g(x)) = 0 необходимо решить
            // g(x) - 1 = 0, а не приписывать параметру значение 1.
            if (arg.FindFirstParameter() == parameter)
            {
                return Expression.Subtract(arg, RicisType.InfinityOne).FindExactRoots(parameter);
            }
        }

        // Ïîïûòêà íàéòè Log(...) âíóòðè áîëåå ñëîæíîãî âûðàæåíèÿ
        var finder = new LogFinder();
        finder.Visit(expr);
        // åñëè âíóòðè íàøëè Log(u) ãäå u ñîäåðæèò ïàðàìåòð — ðåøàåì u = 1
        var inner = finder.FoundLogArgument;
        if (inner is null)
        {
            return roots;
        }

        // Èñïîëüçóåì UniversalZeroSolver ÷òîáû íàéòè êîðíè inner == 1 => inner-1 == 0
        var eqExpr = Expression.Subtract(inner, RicisType.InfinityOne);
        var innerRoots = eqExpr.FindExactRoots(parameter);
        roots.AddRange(innerRoots);

        return roots;
    }

    // Ñîâìåñòèìûé àäàïòåð äëÿ SingularitySolver: âîçâðàùàåò ïåðâûé root (param,double)? èñïîëüçóÿ ZeroSolverUtils
    public static (ParameterExpression, double)? Solve(this Expression expr)
    {
        return ZeroSolverUtils.FindFirstRootFromFindRoots(FindRoots, expr);
    }

    private class LogFinder : ExpressionVisitor
    {
        public Expression FoundLogArgument { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (FoundLogArgument == null
                && node.Method.DeclaringType == typeof(Math)
                && node.Method.Name == "Log"
                && node.Arguments.Count == 1)
            {
                FoundLogArgument = node.Arguments[0];
                return node;
            }

            return base.VisitMethodCall(node);
        }
    }
}