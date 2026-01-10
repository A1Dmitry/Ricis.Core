using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Solvers;

namespace Ricis.Core.Simplifiers;

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

    // --- SP3: Реализация правила Лопиталя ---
    private double ApplyLHopital(Expression num, Expression den, ParameterExpression param, double point)
    {
        try
        {
            var currNum = num;
            var currDen = den;

            for (int i = 0; i < 3; i++) // Максимум 3 итерации (производные)
            {
                // Используем Extension Method из SymbolicDerivator
                currNum = currNum.Derive(param);
                currDen = currDen.Derive(param);

                var nVal = currNum.EvaluateAtPoint(point, param.Name);
                var dVal = currDen.EvaluateAtPoint(point, param.Name);

                if (double.IsNaN(nVal) || double.IsNaN(dVal)) return double.NaN;

                // Если знаменатель != 0, предел найден
                if (Math.Abs(dVal) > 1e-10)
                {
                    return nVal / dVal;
                }

                // Если знаменатель 0, а числитель != 0 -> Полюс (бесконечность)
                if (Math.Abs(nVal) > 1e-10)
                {
                    return double.NaN; // Лопиталь не применим, это не 0/0
                }

                // Иначе 0/0 -> продолжаем брать производные
            }
        }
        catch
        {
            // ignored
        }

        return double.NaN;
    }

    private Expression SimplifyDivision(Expression numerator, Expression denominator)
    {
        var tempSingularities = new List<InfinityExpression>();

        // 1. Полиномиальные корни
        var polyRoots = denominator.SolveRoots();
        foreach (var root in polyRoots)
        {
            // Игнорируем корни, которые сами по себе NaN
            if (double.IsNaN(root.value)) continue;

            // Вычисляем значение числителя в точке корня
            var numVal = numerator.EvaluateAtPoint(root.value, root.expr.Name);

            // Если числитель не вычислим — пропускаем
            if (double.IsNaN(numVal)) continue;

            // --- SP3 INTEGRATION START ---
            // Если числитель тоже 0 (ситуация 0/0), пробуем правило Лопиталя
            if (Math.Abs(numVal) < 1e-10)
            {
                var limit = ApplyLHopital(numerator, denominator, root.expr, root.value);
                if (!double.IsNaN(limit))
                {
                    // Устранимая сингулярность найдена! Возвращаем предел как константу.
                    // Для простых случаев (один корень) это идеальное решение.
                    return Expression.Constant(limit);
                }
            }
            // --- SP3 INTEGRATION END ---

            numerator.AddSingularityIfValid(root.expr, root.value, tempSingularities);
        }

        // 2. Тригонометрические корни
        var trigRoot = TrigSolver.Solve(denominator);
        if (trigRoot.HasValue)
        {
            var (param, value) = trigRoot.Value;

            if (!double.IsNaN(value))
            {
                var numVal = numerator.EvaluateAtPoint(value, param.Name);

                // --- SP3 INTEGRATION START ---
                // Проверка 0/0 для тригонометрии (например, sin(x)/x)
                if (Math.Abs(numVal) < 1e-10)
                {
                    var limit = ApplyLHopital(numerator, denominator, param, value);
                    if (!double.IsNaN(limit))
                    {
                        return Expression.Constant(limit);
                    }
                }
                // --- SP3 INTEGRATION END ---

                numerator.AddSingularityIfValid(param, value, tempSingularities);
            }
        }

        // 3. Фолбэк для трансцендентных (Exp, Log, Sqrt...)
        if (tempSingularities.Count == 0 && denominator.IsTranscendentalCandidate())
        {
            // Пока оставляем пустым
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
            .Where(r => !double.IsNaN(r.Value))
            .GroupBy(r => Math.Round(r.Value, 4))
            .Select(g => g.First())
            .OrderBy(r => r.Value)
            .ToList();

        if (allRoots.Count == 0) return Expression.Divide(numerator, denominator);

        return InfinityExpression.CreateLazy(primaryIndex, allRoots);
    }
}