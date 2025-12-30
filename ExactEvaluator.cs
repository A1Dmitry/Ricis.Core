using Ricis.Core;
using System.Linq.Expressions;
using System.Numerics;

public static class ExactEvaluator
{
    public static bool TryEvaluate(Expression expr, string paramName, Rational paramValue, out Rational result)
    {
        result = Rational.Zero;
        var visitor = new EvalVisitor(paramName, paramValue);
        return visitor.TryEvaluate(expr, out result);
    }

    private class EvalVisitor : ExpressionVisitor
    {
        private readonly string _paramName;
        private readonly Rational _paramValue;
        private Rational _last;
        private bool _ok;

        public EvalVisitor(string paramName, Rational paramValue)
        {
            _paramName = paramName;
            _paramValue = paramValue;
        }

        public bool TryEvaluate(Expression expr, out Rational result)
        {
            _ok = true;
            Visit(expr);
            result = _ok ? _last : Rational.Zero;
            return _ok;
        }

        public override Expression Visit(Expression node)
        {
            if (!_ok) return node;
            if (node == null)
            {
                _ok = false;
                return node;
            }

            switch (node.NodeType)
            {
                case ExpressionType.Constant:
                    VisitConstant((ConstantExpression)node);
                    return node;
                case ExpressionType.Parameter:
                    VisitParameter((ParameterExpression)node);
                    return node;
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                    VisitBinary((BinaryExpression)node);
                    return node;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    VisitUnary((UnaryExpression)node);
                    return node;
                case ExpressionType.Call:
                    VisitCall((MethodCallExpression)node);
                    return node;
                default:
                    _ok = false;
                    return node;
            }
        }

        private void VisitConstant(ConstantExpression c)
        {
            var v = c.Value;
            if (v is int i) _last = Rational.Create(i);
            else if (v is long l) _last = Rational.Create(l);
            else if (v is decimal dec) _last = Rational.FromDecimal(dec);
            else if (v is BigInteger bi) _last = new Rational(bi);
            else if (v is double d)
            {
                if (Math.Abs(d % 1) < double.Epsilon) _last = Rational.Create((long)d);
                else _ok = false;
            }
            else _ok = false;
        }

        private void VisitParameter(ParameterExpression p)
        {
            if (p.Name == _paramName) _last = _paramValue;
            else _ok = false;
        }

        private void VisitUnary(UnaryExpression u)
        {
            Visit(u.Operand);
            if (!_ok) return;
            if (u.NodeType == ExpressionType.Negate || u.NodeType == ExpressionType.NegateChecked)
                _last = -_last; // Используем перегрузку унарного минуса
        }

        private void VisitBinary(BinaryExpression b)
        {
            Visit(b.Left);
            if (!_ok) return;
            var left = _last;
            Visit(b.Right);
            if (!_ok) return;
            var right = _last;

            try
            {
                switch (b.NodeType)
                {
                    case ExpressionType.Add: _last = left + right; break;
                    case ExpressionType.Subtract: _last = left - right; break;
                    case ExpressionType.Multiply: _last = left * right; break;
                    case ExpressionType.Divide: _last = left / right; break;
                    default: _ok = false; break;
                }
            }
            catch
            {
                _ok = false;
            }
        }

        private void VisitCall(MethodCallExpression call)
        {
            // 1. Обработка Math.Pow(x, n)
            if (call.Method.Name == "Pow" && call.Arguments.Count == 2)
            {
                // Вычисляем основание
                Visit(call.Arguments[0]);
                if (!_ok) return;
                var baseVal = _last;

                // Вычисляем степень
                Visit(call.Arguments[1]);
                if (!_ok) return;
                var expVal = _last;

                // Rational -> Double для проверки целостности степени
                double dExp = expVal.ToDouble();

                // Поддерживаем только целые положительные степени для Rational
                if (Math.Abs(dExp % 1) < double.Epsilon && dExp >= 0)
                {
                    int n = (int)dExp;
                    Rational res = Rational.One;
                    for (int i = 0; i < n; i++) res = res * baseVal;
                    _last = res;
                    return;
                }

                _ok = false;
                return;
            }

            // 2. Обработка тригонометрии в нуле
            if (call.Method.DeclaringType == typeof(Math) && call.Arguments.Count == 1)
            {
                Visit(call.Arguments[0]);
                if (!_ok) return;
                if (_last.IsZero)
                {
                    switch (call.Method.Name)
                    {
                        case "Sin":
                        case "Tan": _last = Rational.Zero; return;
                        case "Cos": _last = Rational.One; return;
                    }
                }
            }

            _ok = false;
        }
    }
}