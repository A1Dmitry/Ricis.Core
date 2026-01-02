// RicisCore/ExpressionIdentityComparer.cs

using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

public static class ExpressionIdentityComparer
{
    /// <summary>
    /// Строгая проверка самоидентичности двух Expression по RICIS L1_IDENTITY: X = X.
    /// Рекурсивное сравнение по структуре дерева.
    /// </summary>
    public static bool AreSelfIdentical(this Expression a, Expression b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        if (a.NodeType != b.NodeType)
        {
            return false;
        }

        if (a.Type != b.Type)
        {
            return false;
        }

        return a.NodeType switch
        {
            ExpressionType.Constant => ConstantIdentical((ConstantExpression)a, (ConstantExpression)b),
            ExpressionType.Parameter => ParameterIdentical((ParameterExpression)a, (ParameterExpression)b),
            ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply or ExpressionType.Divide
                or ExpressionType.Power or ExpressionType.And or ExpressionType.Or =>
                // все бинарные операции
                BinaryIdentical((BinaryExpression)a, (BinaryExpression)b),
            ExpressionType.Negate or ExpressionType.UnaryPlus or ExpressionType.Convert =>
                // унарные
                UnaryIdentical((UnaryExpression)a, (UnaryExpression)b),
            ExpressionType.Call => MethodCallIdentical((MethodCallExpression)a, (MethodCallExpression)b),
            ExpressionType.Lambda => LambdaIdentical((LambdaExpression)a, (LambdaExpression)b),
            ExpressionType.Extension =>
                // Для наших RICIS-выражений (InfinityExpression и т.д.)
                ExtensionIdentical(a, b),
            _ => false
        };
    }

    private static bool ConstantIdentical(ConstantExpression a, ConstantExpression b)
    {
        return Equals(a.Value, b.Value);
    }

    private static bool ParameterIdentical(ParameterExpression a, ParameterExpression b)
    {
        return a.Name == b.Name && a.Type == b.Type;
    }

    private static bool BinaryIdentical(BinaryExpression a, BinaryExpression b)
    {
        return a.Method == b.Method &&
               AreSelfIdentical(a.Left, b.Left) &&
               AreSelfIdentical(a.Right, b.Right);
    }

    private static bool UnaryIdentical(UnaryExpression a, UnaryExpression b)
    {
        return a.Method == b.Method &&
               AreSelfIdentical(a.Operand, b.Operand);
    }

    private static bool MethodCallIdentical(MethodCallExpression a, MethodCallExpression b)
    {
        if (a.Method != b.Method)
        {
            return false;
        }

        if (!AreSelfIdentical(a.Object, b.Object))
        {
            return false;
        }

        if (a.Arguments.Count != b.Arguments.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Arguments.Count; i++)
        {
            if (!AreSelfIdentical(a.Arguments[i], b.Arguments[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool LambdaIdentical(LambdaExpression a, LambdaExpression b)
    {
        if (a.Parameters.Count != b.Parameters.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Parameters.Count; i++)
        {
            if (!ParameterIdentical(a.Parameters[i], b.Parameters[i]))
            {
                return false;
            }
        }
        return AreSelfIdentical(a.Body, b.Body);
    }

    private static bool ExtensionIdentical(Expression a, Expression b)
    {
        // Для RICIS-расширений (InfinityExpression, BridgedExpression и т.д.)
        // Fallback на ToString() или можно добавить конкретные сравнения
        return a.AreSelfIdentical(b);
    }


}