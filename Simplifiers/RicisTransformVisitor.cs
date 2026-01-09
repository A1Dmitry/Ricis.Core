using System.Linq.Expressions;
using Ricis.Core;
using Ricis.Core.Expressions;
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
            AddSingularityIfValid(numerator, denominator, root.expr, root.value, tempSingularities);
        }

        // 2. Тригонометрические корни (Простые)
        var trigRoot = TrigSolver.Solve(denominator);
        if (trigRoot.HasValue)
        {
            var (param, value) = trigRoot.Value;
            AddSingularityIfValid(numerator, denominator, param, value, tempSingularities);
        }

        // 3. Фолбэк для трансцендентных (если SingularitySolver не справился, хотя он должен)
        if (tempSingularities.Count == 0 && IsTranscendentalCandidate(denominator))
        {
            var param = FindParameter(denominator);
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
    private bool IsTranscendentalCandidate(Expression expr)
    {
        var hasTrig = false;
        var isComplex = false;

        // Простой обход дерева выражения
        new ExpressionTraverser(node =>
        {
            if (node is MethodCallExpression call)
            {
                if (call.Method.DeclaringType == typeof(Math))
                {
                    var name = call.Method.Name;
                    if (name == "Cos" || name == "Sin" || name == "Tan" ||
                        name == "Cosh" || name == "Sinh" || name == "Tanh")
                    {
                        hasTrig = true;
                    }
                }
            }
            else if (node is BinaryExpression)
            {
                isComplex = true; // Есть операции (+, -, *)
            }
        }).Visit(expr);

        return hasTrig && isComplex;
    }

    // Хелпер для поиска параметра (x)
    private ParameterExpression FindParameter(Expression expr)
    {
        ParameterExpression found = null;
        IExpressionVisitor visitor = new ExpressionTraverser(node =>
        {
            if (found == null && node is ParameterExpression p)
            {
                found = p;
            }
        });
        visitor.Visit(expr);
        return found;
    }

    

    private void AddSingularityIfValid(
        Expression numerator,
        Expression denominator,
        ParameterExpression param,
        double value,
        List<InfinityExpression> singularities)
    {
        var numAtRoot = EvaluateAtPoint(numerator, param.Name, value);

        InfinityExpression infinity;
        if (numAtRoot == 0.0) // 0/0 DETECTED
        {
            // --- FIX: Индекс 0 для состояния 0/0 ---
            var indexZero = RicisType.InfinityZero;
            infinity = InfinityExpression.CreateLazy(indexZero, param, value);
        }
        else
        {
            // Полюс C/0 -> Индекс C (числитель)
            infinity = InfinityExpression.CreateLazy(numerator, param, value);
        }

        singularities.Add(infinity);
    }

    private static double EvaluateAtPoint(Expression expr, string paramName, double value)
    {
        try
        {
            var visitor = new SubstitutionVisitor(paramName, value);
            var substituted = visitor.Visit(expr);
            var lambda = Expression.Lambda<Func<double>>(Expression.Convert(substituted, typeof(double)));
            return lambda.Compile()();
        }
        catch
        {
            return 1.0;
        }
    }
}
