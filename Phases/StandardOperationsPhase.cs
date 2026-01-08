// StandardOperationsPhase.cs

using System.Linq.Expressions;

namespace Ricis.Core.Phases;

/// <summary>
/// Phase 5: Standard operations (по RICIS 7.3)
/// Применяет безопасные стандартные упрощения после RICIS
/// DRY-подход: минимальный визитор только для очевидных случаев
/// Не трогает ∞_F, Monolith и другие кастомные узлы
/// </summary>
public static class StandardOperationsPhase
{
    public static Expression Apply(Expression expr)
    {
        if (expr == null)
        {
            return null;
        }

        return new StandardOperationsVisitor().Visit(expr);
    }

   
}