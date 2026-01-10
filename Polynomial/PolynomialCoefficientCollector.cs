using Ricis.Core.Rationals;
using Ricis.Core.Simplifiers;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

public class PolynomialCoefficientCollector(ParameterExpression parameterExpression)
    : ExpressionVisitor, IExpressionVisitor
{
    private readonly ParameterExpression _parameter =
        parameterExpression ?? throw new ArgumentNullException(nameof(parameterExpression));

    private Rational _currentMultiplier = Rational.One;
    private int _currentPower = -1;

    public bool IsPolynomial { get; private set; } = true;
    public Dictionary<int, Rational> Coefficients { get; } = new();

    public new void Visit(Expression expr)
    {
        IsPolynomial = true;
        Coefficients.Clear();
        base.Visit(expr);
        if (!IsPolynomial) Coefficients.Clear();
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _parameter || node.Name == _parameter.Name)
        {
            if (_currentPower < 0) _currentPower = 1;
            AddToCoefficients(_currentPower, _currentMultiplier);
        }
        else
        {
            IsPolynomial = false;
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var value = ConvertConstantToRational(node.Value);
        if (_currentPower < 0)
            AddToCoefficients(0, value * _currentMultiplier);
        else
            AddToCoefficients(_currentPower, _currentMultiplier * value);
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (!IsPolynomial) return node;

        switch (node.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.Subtract:
                var multBefore = _currentMultiplier;
                var powBefore = _currentPower;

                base.Visit(node.Left);

                _currentMultiplier = multBefore;
                _currentPower = powBefore;

                if (node.NodeType == ExpressionType.Subtract)
                    _currentMultiplier = -_currentMultiplier;

                base.Visit(node.Right);

                _currentMultiplier = multBefore;
                _currentPower = powBefore;
                break;

            case ExpressionType.Multiply:
                VisitMultiply(node);
                break;

            default:
                IsPolynomial = false;
                break;
        }
        return node;
    }

    private void VisitMultiply(BinaryExpression node)
    {
        var outerState = SaveState();

        _currentMultiplier = Rational.One;
        _currentPower = -1;
        Coefficients.Clear();
        base.Visit(node.Left);
        if (!IsPolynomial) { RestoreState(outerState); return; }
        var leftCoeffs = new Dictionary<int, Rational>(Coefficients);

        _currentMultiplier = Rational.One;
        _currentPower = -1;
        Coefficients.Clear();
        base.Visit(node.Right);
        if (!IsPolynomial) { RestoreState(outerState); return; }
        var rightCoeffs = new Dictionary<int, Rational>(Coefficients);

        RestoreState(outerState);

        foreach (var left in leftCoeffs)
            foreach (var right in rightCoeffs)
            {
                var newPower = left.Key + right.Key;
                var newCoeff = left.Value * right.Value * _currentMultiplier;
                AddToCoefficients(newPower, newCoeff);
            }
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert) return base.Visit(node.Operand);
        if (node.NodeType == ExpressionType.Negate)
        {
            _currentMultiplier = -_currentMultiplier;
            base.Visit(node.Operand);
            _currentMultiplier = -_currentMultiplier;
            return node;
        }
        IsPolynomial = false;
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Math) && node.Method.Name == "Pow")
        {
            var arg0 = Unwrap(node.Arguments[0]);
            var arg1 = Unwrap(node.Arguments[1]);

            bool isParam = arg0 is ParameterExpression p && (p == _parameter || p.Name == _parameter.Name);

            if (isParam && arg1 is ConstantExpression c)
            {
                var ratVal = ConvertConstantToRational(c.Value);
                if (ratVal.Denominator == 1)
                {
                    var power = (int)ratVal.Numerator;
                    if (power >= 0)
                    {
                        AddToCoefficients(power, _currentMultiplier);
                        return node;
                    }
                }
            }
        }
        IsPolynomial = false;
        return node;
    }

    private Expression Unwrap(Expression ex)
    {
        if (ex.NodeType == ExpressionType.Convert && ex is UnaryExpression u) return Unwrap(u.Operand);
        return ex;
    }

    private void AddToCoefficients(int power, Rational coeff)
    {
        if (coeff.IsZero) return;
        if (Coefficients.TryGetValue(power, out var existing)) coeff += existing;
        if (coeff.IsZero) Coefficients.Remove(power);
        else Coefficients[power] = coeff;
    }

    private Rational ConvertConstantToRational(object value)
    {
        if (value is double db)
        {
            var intValue = (long)Math.Round(db);
            if (Math.Abs(db - intValue) < double.Epsilon) return Rational.Create(intValue);
            IsPolynomial = false; return Rational.Zero;
        }
        return value switch
        {
            int i => Rational.Create(i),
            long l => Rational.Create(l),
            BigInteger bi => new Rational(bi),
            decimal d => Rational.FromDecimal(d),
            _ => throw new ArgumentException($"Unsupported constant type: {value?.GetType()}")
        };
    }

    private (int power, Rational mult, Dictionary<int, Rational> coeffs) SaveState()
    {
        return (_currentPower, _currentMultiplier, new Dictionary<int, Rational>(Coefficients));
    }

    private void RestoreState((int power, Rational mult, Dictionary<int, Rational> coeffs) state)
    {
        _currentPower = state.power;
        _currentMultiplier = state.mult;
        Coefficients.Clear();
        foreach (var kv in state.coeffs) Coefficients[kv.Key] = kv.Value;
    }
}