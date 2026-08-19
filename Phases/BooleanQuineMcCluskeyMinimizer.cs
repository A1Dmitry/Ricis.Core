using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Phases;

internal static class BooleanQuineMcCluskeyMinimizer
{
    private const int MaxVariables = 6;

    public static Expression TryMinimize(Expression expression)
    {
        if (!TryCollectVariables(expression, out var variables) || variables.Count > MaxVariables)
        {
            return expression;
        }

        var assignmentCount = 1 << variables.Count;
        var minterms = new List<int>();
        for (var assignment = 0; assignment < assignmentCount; assignment++)
        {
            if (Evaluate(expression, variables, assignment))
            {
                minterms.Add(assignment);
            }
        }

        if (minterms.Count == 0) return Expression.Constant(false);
        if (minterms.Count == assignmentCount) return Expression.Constant(true);

        var primes = BuildPrimeImplicants(minterms, variables.Count);
        var selected = SelectCover(primes, minterms);
        return BuildDnf(selected, variables);
    }

    private static bool TryCollectVariables(Expression expression, out List<ParameterExpression> variables)
    {
        var collected = new List<ParameterExpression>();
        var valid = Visit(expression);
        variables = collected;
        return valid;

        bool Visit(Expression node)
        {
            switch (node)
            {
                case ConstantExpression { Value: bool }:
                    return true;
                case ParameterExpression parameter when parameter.Type == typeof(bool):
                    if (!collected.Contains(parameter)) collected.Add(parameter);
                    return true;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Not &&
                                                unary.Method is null &&
                                                unary.Type == typeof(bool) &&
                                                unary.Operand.Type == typeof(bool):
                    return Visit(unary.Operand);
                case BinaryExpression binary when binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse &&
                                                   binary.Method is null &&
                                                   binary.Type == typeof(bool) &&
                                                   binary.Left.Type == typeof(bool) &&
                                                   binary.Right.Type == typeof(bool) &&
                                                   !binary.IsLifted &&
                                                   !binary.IsLiftedToNull:
                    return Visit(binary.Left) && Visit(binary.Right);
                default:
                    return false;
            }
        }
    }

    private static int VariableIndex(IReadOnlyList<ParameterExpression> variables, ParameterExpression parameter)
    {
        for (var index = 0; index < variables.Count; index++)
        {
            if (ReferenceEquals(variables[index], parameter)) return index;
        }

        throw new InvalidOperationException("Validated Boolean parameter was not found in the variable list.");
    }

    private static bool Evaluate(Expression expression, IReadOnlyList<ParameterExpression> variables, int assignment) =>
        expression switch
        {
            ConstantExpression { Value: bool value } => value,
            ParameterExpression parameter => (assignment & (1 << VariableIndex(variables, parameter))) != 0,
            UnaryExpression { NodeType: ExpressionType.Not, Operand: var operand } => !Evaluate(operand, variables, assignment),
            BinaryExpression { NodeType: ExpressionType.AndAlso, Left: var left, Right: var right } =>
                Evaluate(left, variables, assignment) && Evaluate(right, variables, assignment),
            BinaryExpression { NodeType: ExpressionType.OrElse, Left: var left, Right: var right } =>
                Evaluate(left, variables, assignment) || Evaluate(right, variables, assignment),
            _ => throw new InvalidOperationException("Expression was not validated as a pure Boolean tree.")
        };

    private static List<Implicant> BuildPrimeImplicants(IReadOnlyList<int> minterms, int variableCount)
    {
        var current = minterms.Select(value => new Implicant(value, 0, [value])).ToList();
        var primes = new List<Implicant>();

        while (current.Count > 0)
        {
            var combined = new bool[current.Count];
            var next = new Dictionary<(int Value, int Mask), Implicant>();
            for (var i = 0; i < current.Count; i++)
            {
                for (var j = i + 1; j < current.Count; j++)
                {
                    if (!TryCombine(current[i], current[j], out var result)) continue;
                    combined[i] = true;
                    combined[j] = true;
                    next[(result.Value, result.Mask)] = result;
                }
            }

            for (var i = 0; i < current.Count; i++)
            {
                if (!combined[i]) primes.Add(current[i]);
            }

            current = next.Values.OrderBy(item => item.Mask).ThenBy(item => item.Value).ToList();
        }

        return primes
            .Distinct()
            .OrderByDescending(item => item.LiteralCount(variableCount))
            .ThenBy(item => item.Mask)
            .ThenBy(item => item.Value)
            .ToList();
    }

    private static bool TryCombine(Implicant left, Implicant right, out Implicant result)
    {
        result = default;
        if (left.Mask != right.Mask) return false;
        var difference = (left.Value ^ right.Value) & ~left.Mask;
        if (difference == 0 || (difference & (difference - 1)) != 0) return false;
        result = new Implicant(left.Value & ~difference, left.Mask | difference,
            left.Minterms.Concat(right.Minterms).Distinct().OrderBy(value => value).ToArray());
        return true;
    }

    private static List<Implicant> SelectCover(IReadOnlyList<Implicant> primes, IReadOnlyList<int> minterms)
    {
        var remaining = new HashSet<int>(minterms);
        var selected = new List<Implicant>();

        while (remaining.Count > 0)
        {
            var candidate = primes
                .Where(prime => remaining.Any(prime.Covers))
                .OrderByDescending(prime => remaining.Count(prime.Covers))
                .ThenBy(prime => prime.Mask)
                .ThenBy(prime => prime.Value)
                .First();
            selected.Add(candidate);
            remaining.RemoveWhere(candidate.Covers);
        }

        return selected;
    }

    private static Expression BuildDnf(IReadOnlyList<Implicant> implicants, IReadOnlyList<ParameterExpression> variables)
    {
        Expression result = null;
        foreach (var implicant in implicants)
        {
            Expression term = null;
            for (var index = 0; index < variables.Count; index++)
            {
                if ((implicant.Mask & (1 << index)) != 0) continue;
                Expression literal = (implicant.Value & (1 << index)) != 0
                    ? variables[index]
                    : Expression.Not(variables[index]);
                term = term is null ? literal : Expression.AndAlso(term, literal);
            }

            term ??= Expression.Constant(true);
            result = result is null ? term : Expression.OrElse(result, term);
        }

        return result ?? Expression.Constant(false);
    }

    private readonly record struct Implicant(int Value, int Mask, IReadOnlyList<int> Minterms)
    {
        public bool Covers(int minterm) => (minterm & ~Mask) == Value;
        public int LiteralCount(int variableCount) => variableCount - BitOperations.PopCount((uint)Mask);
    }
}
