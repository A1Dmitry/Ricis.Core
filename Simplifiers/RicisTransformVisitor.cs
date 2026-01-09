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
        // Временный список для сбора отдельных сингулярностей (как у вас сейчас)
        var tempSingularities = new List<InfinityExpression>();

        // 1. Полиномиальные корни (через SingularitySolver, который теперь включает численный метод)
        var polyRoots = denominator.SolveRoots();
        foreach (var root in polyRoots)
        {
            numerator.AddSingularityIfValid(root.expr, root.value, tempSingularities);
        }

        // 2. Тригонометрические корни (Простые)
        var trigRoot = TrigSolver.Solve(denominator);
        if (trigRoot.HasValue)
        {
            var (param, value) = trigRoot.Value;
            numerator.AddSingularityIfValid(param, value, tempSingularities);
        }

        // 3. Фолбэк для трансцендентных (если SingularitySolver не справился, хотя он должен)
        if (tempSingularities.Count == 0 && denominator.IsTranscendentalCandidate())
        {
            var param = denominator.FindParameter();
            if (param != null)
            {
                // Возвращаем абстрактную бесконечность (без конкретного корня)
                return InfinityExpression.CreateLazy(denominator, param, double.NaN);
            }
        }

        // Если ничего не нашли - возвращаем деление
        if (tempSingularities.Count == 0)
        {
            return Expression.Divide(numerator, denominator);
        }

        // --- ФИНАЛЬНАЯ СБОРКА (RICIS-III) ---

        // Если нашли одну - возвращаем её (она уже InfinityExpression)
        if (tempSingularities.Count == 1)
        {
            return tempSingularities[0];
        }

        // Если нашли МНОГО - объединяем их в один InfinityExpression со списком корней.
        // Берем Индекс (Идентичность) из первой сингулярности (они все от одного знаменателя).
        var primaryIndex = tempSingularities[0].Numerator;

        // Собираем все корни из всех найденных сингулярностей
        var allRoots = tempSingularities
            .SelectMany(s => s.Roots)
            .GroupBy(r => Math.Round(r.Value, 4)) // Группируем корни, совпадающие до 4 знака
            .Select(g => g.First())               // Берем один из группы
            .OrderBy(r => r.Value)
            .ToList(); 

        // Создаем Монолит
        return InfinityExpression.CreateLazy(primaryIndex, allRoots);
    }

    // Проверка: содержит ли выражение трансцендентные функции в сложной структуре
   

    

    

    

    
}
