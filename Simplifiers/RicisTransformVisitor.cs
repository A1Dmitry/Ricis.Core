using System.Linq.Expressions;
using Ricis.Core;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;
using Ricis.Core.Solvers;

public class RicisTransformVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Divide)
        {
            return SimplifyDivision(node.Left, node.Right);
        }
        return base.VisitBinary(node);
    }

    private Expression SimplifyDivision(Expression numerator, Expression denominator)
    {
        var tempSingularities = new List<InfinityExpression>();

        // 1. Полиномиальные корни
        var polyRoots = denominator.SolveRoots();
        foreach (var root in polyRoots)
        {
            // FIX: Игнорируем корни, которые сами по себе NaN
            if (double.IsNaN(root.value)) continue;

            // FIX: Проверяем числитель. Теперь EvaluateAtPoint вернет NaN при ошибке.
            var numVal = numerator.EvaluateAtPoint(root.value, root.expr.Name);

            // Если числитель не вычислим в этой точке — это не наш случай, пропускаем
            if (double.IsNaN(numVal)) continue;

            numerator.AddSingularityIfValid(root.expr, root.value, tempSingularities);
        }

        // 2. Тригонометрические корни
        var trigRoot = TrigSolver.Solve(denominator);
        if (trigRoot.HasValue)
        {
            var (param, value) = trigRoot.Value;
            // FIX: Защита от NaN
            if (!double.IsNaN(value))
            {
                numerator.AddSingularityIfValid(param, value, tempSingularities);
            }
        }

        // 3. Фолбэк для трансцендентных (Exp, Log, Sqrt...)
        if (tempSingularities.Count == 0 && denominator.IsTranscendentalCandidate())
        {
            // Пока оставляем пустым или возвращаем деление, 
            // чтобы не генерировать "x=не число" (NaN) в логах.
        }

        // Если ничего не нашли
        if (tempSingularities.Count == 0)
        {
            return Expression.Divide(numerator, denominator);
        }

        // --- ФИНАЛЬНАЯ СБОРКА ---

        // Если нашли одну
        if (tempSingularities.Count == 1)
        {
            return tempSingularities[0];
        }

        // Если нашли много — собираем Монолит
        var primaryIndex = tempSingularities[0].Numerator;

        var allRoots = tempSingularities
            .SelectMany(s => s.Roots)
            .Where(r => !double.IsNaN(r.Value)) // FIX: Финальная фильтрация мусора
            .GroupBy(r => Math.Round(r.Value, 4))
            .Select(g => g.First())
            .OrderBy(r => r.Value)
            .ToList();

        if (allRoots.Count == 0) return Expression.Divide(numerator, denominator);

        return InfinityExpression.CreateLazy(primaryIndex, allRoots);
    }

    // Проверка: содержит ли выражение трансцендентные функции в сложной структуре









}
