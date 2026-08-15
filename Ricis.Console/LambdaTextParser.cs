using System.Globalization;
using System.Linq.Expressions;

namespace Ricis.ConsoleApp;

/// <summary>
/// Parses a constrained mathematical lambda into a typed LINQ expression tree.
/// No C# source is compiled or evaluated: the parser accepts only the grammar
/// documented by the console and only whitelisted System.Math functions.
/// </summary>
public sealed class LambdaTextParser
{
    public Expression<Func<double, double>> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new LambdaParseException("Пустая строка не является выражением.", 0);
        }

        var source = text.Trim();
        var arrow = source.IndexOf("=>", StringComparison.Ordinal);
        var parameterName = "x";
        var body = source;

        if (arrow >= 0)
        {
            parameterName = source[..arrow].Trim();
            body = source[(arrow + 2)..].Trim();
            if (!IsIdentifier(parameterName))
            {
                throw new LambdaParseException("Слева от => ожидается один идентификатор параметра.", 0);
            }
        }

        if (body.Length == 0)
        {
            throw new LambdaParseException("После => ожидается тело выражения.", source.Length);
        }

        var parameter = Expression.Parameter(typeof(double), parameterName);
        var parser = new BodyParser(body, parameter);
        var expression = parser.Parse();
        return Expression.Lambda<Func<double, double>>(expression, parameter);
    }

    private static bool IsIdentifier(string value) =>
        value.Length > 0 &&
        (char.IsLetter(value[0]) || value[0] == '_') &&
        value.Skip(1).All(ch => char.IsLetterOrDigit(ch) || ch == '_');

    private sealed class BodyParser
    {
        private readonly string _text;
        private readonly ParameterExpression _parameter;
        private Token _current;
        private int _position;

        public BodyParser(string text, ParameterExpression parameter)
        {
            _text = text;
            _parameter = parameter;
            _current = NextToken();
        }

        public Expression Parse()
        {
            var result = ParseAddition();
            if (_current.Kind != TokenKind.End)
            {
                throw Error($"Неожиданный символ '{_current.Text}'.");
            }

            return result;
        }

        private Expression ParseAddition()
        {
            var left = ParseMultiplication();
            while (_current.Kind is TokenKind.Plus or TokenKind.Minus)
            {
                var operation = _current.Kind;
                Consume(operation);
                var right = ParseMultiplication();
                left = operation == TokenKind.Plus ? Expression.Add(left, right) : Expression.Subtract(left, right);
            }

            return left;
        }

        private Expression ParseMultiplication()
        {
            var left = ParseUnary();
            while (_current.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.Percent)
            {
                var operation = _current.Kind;
                Consume(operation);
                var right = ParseUnary();
                left = operation switch
                {
                    TokenKind.Star => Expression.Multiply(left, right),
                    TokenKind.Slash => Expression.Divide(left, right),
                    _ => Expression.Modulo(left, right)
                };
            }

            return left;
        }

        private Expression ParseUnary()
        {
            if (_current.Kind == TokenKind.Plus)
            {
                Consume(TokenKind.Plus);
                return ParseUnary();
            }

            if (_current.Kind == TokenKind.Minus)
            {
                Consume(TokenKind.Minus);
                return Expression.Negate(ParseUnary());
            }

            return ParsePower();
        }

        private Expression ParsePower()
        {
            var left = ParsePrimary();
            if (_current.Kind == TokenKind.Caret)
            {
                Consume(TokenKind.Caret);
                var right = ParseUnary();
                return Expression.Power(left, right);
            }

            return left;
        }

        private Expression ParsePrimary()
        {
            if (_current.Kind == TokenKind.Number)
            {
                var token = _current;
                Consume(TokenKind.Number);
                if (!double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw new LambdaParseException($"Невозможно разобрать число '{token.Text}'.", token.Position);
                }

                return Expression.Constant(value);
            }

            if (_current.Kind == TokenKind.Identifier)
            {
                var token = _current;
                Consume(TokenKind.Identifier);

                if (_current.Kind == TokenKind.LeftParenthesis)
                {
                    return ParseFunctionCall(token);
                }

                return ParseIdentifier(token);
            }

            if (_current.Kind == TokenKind.LeftParenthesis)
            {
                Consume(TokenKind.LeftParenthesis);
                var nested = ParseAddition();
                Consume(TokenKind.RightParenthesis);
                return nested;
            }

            throw Error("Ожидаются число, параметр, функция или открывающая скобка.");
        }

        private Expression ParseIdentifier(Token token)
        {
            if (string.Equals(token.Text, _parameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                return _parameter;
            }

            var name = NormaliseName(token.Text);
            return name switch
            {
                "PI" => Expression.Constant(Math.PI),
                "E" => Expression.Constant(Math.E),
                _ => throw new LambdaParseException(
                    $"Неизвестный идентификатор '{token.Text}'. Допустимы {_parameter.Name}, pi и e.", token.Position),
            };
        }

        private Expression ParseFunctionCall(Token function)
        {
            Consume(TokenKind.LeftParenthesis);
            var arguments = new List<Expression>();
            if (_current.Kind != TokenKind.RightParenthesis)
            {
                arguments.Add(ParseAddition());
                while (_current.Kind == TokenKind.Comma)
                {
                    Consume(TokenKind.Comma);
                    arguments.Add(ParseAddition());
                }
            }

            Consume(TokenKind.RightParenthesis);
            var name = NormaliseName(function.Text);
            return name switch
            {
                "SIN" => OneArgumentMath(nameof(Math.Sin), arguments, function),
                "COS" => OneArgumentMath(nameof(Math.Cos), arguments, function),
                "TAN" => OneArgumentMath(nameof(Math.Tan), arguments, function),
                "SINH" => OneArgumentMath(nameof(Math.Sinh), arguments, function),
                "COSH" => OneArgumentMath(nameof(Math.Cosh), arguments, function),
                "TANH" => OneArgumentMath(nameof(Math.Tanh), arguments, function),
                "EXP" => OneArgumentMath(nameof(Math.Exp), arguments, function),
                "LOG" => OneArgumentMath(nameof(Math.Log), arguments, function),
                "LOG10" => OneArgumentMath(nameof(Math.Log10), arguments, function),
                "SQRT" => OneArgumentMath(nameof(Math.Sqrt), arguments, function),
                "ABS" => OneArgumentMath(nameof(Math.Abs), arguments, function),
                "SIGN" => Sign(arguments, function),
                "CLAMP" => ThreeArgumentMath(nameof(Math.Clamp), arguments, function),
                "MOD" => Modulo(arguments, function),
                "POW" => TwoArgumentMath(nameof(Math.Pow), arguments, function),
                _ => throw new LambdaParseException(
                    $"Функция '{function.Text}' не поддерживается. Используйте Sin, Cos, Tan, Sinh, Cosh, Tanh, Exp, Log, Log10, Sqrt, Abs, Sign, Clamp, Mod или Pow.",
                    function.Position),
            };
        }

        private static Expression OneArgumentMath(string methodName, IReadOnlyList<Expression> arguments, Token token)
        {
            if (arguments.Count != 1)
            {
                throw new LambdaParseException($"{token.Text} принимает ровно один аргумент.", token.Position);
            }

            var method = typeof(Math).GetMethod(methodName, [typeof(double)])!;
            return Expression.Call(method, arguments[0]);
        }

        private static Expression Sign(IReadOnlyList<Expression> arguments, Token token)
        {
            if (arguments.Count != 1)
            {
                throw new LambdaParseException($"{token.Text} принимает ровно один аргумент.", token.Position);
            }

            var method = typeof(Math).GetMethod(nameof(Math.Sign), [typeof(double)])!;
            return Expression.Convert(Expression.Call(method, arguments[0]), typeof(double));
        }

        private static Expression Modulo(IReadOnlyList<Expression> arguments, Token token)
        {
            if (arguments.Count != 2)
            {
                throw new LambdaParseException($"{token.Text} принимает ровно два аргумента.", token.Position);
            }

            return Expression.Modulo(arguments[0], arguments[1]);
        }

        private static Expression ThreeArgumentMath(string methodName, IReadOnlyList<Expression> arguments, Token token)
        {
            if (arguments.Count != 3)
            {
                throw new LambdaParseException($"{token.Text} принимает ровно три аргумента.", token.Position);
            }

            var method = typeof(Math).GetMethod(methodName, [typeof(double), typeof(double), typeof(double)])!;
            return Expression.Call(method, arguments[0], arguments[1], arguments[2]);
        }

        private static Expression TwoArgumentMath(string methodName, IReadOnlyList<Expression> arguments, Token token)
        {
            if (arguments.Count != 2)
            {
                throw new LambdaParseException($"{token.Text} принимает ровно два аргумента.", token.Position);
            }

            var method = typeof(Math).GetMethod(methodName, [typeof(double), typeof(double)])!;
            return Expression.Call(method, arguments[0], arguments[1]);
        }

        private static string NormaliseName(string name)
        {
            var separator = name.LastIndexOf('.');
            return (separator >= 0 ? name[(separator + 1)..] : name).ToUpperInvariant();
        }

        private void Consume(TokenKind expected)
        {
            if (_current.Kind != expected)
            {
                throw Error($"Ожидается '{Display(expected)}', получено '{_current.Text}'.");
            }

            _current = NextToken();
        }

        private Token NextToken()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }

            if (_position >= _text.Length)
            {
                return new Token(TokenKind.End, string.Empty, _position);
            }

            var start = _position;
            var current = _text[_position];
            switch (current)
            {
                case '+': _position++; return new Token(TokenKind.Plus, "+", start);
                case '-': _position++; return new Token(TokenKind.Minus, "-", start);
                case '*': _position++; return new Token(TokenKind.Star, "*", start);
                case '/': _position++; return new Token(TokenKind.Slash, "/", start);
                case '%': _position++; return new Token(TokenKind.Percent, "%", start);
                case '^': _position++; return new Token(TokenKind.Caret, "^", start);
                case '(': _position++; return new Token(TokenKind.LeftParenthesis, "(", start);
                case ')': _position++; return new Token(TokenKind.RightParenthesis, ")", start);
                case ',': _position++; return new Token(TokenKind.Comma, ",", start);
            }

            if (char.IsDigit(current) || current == '.')
            {
                var hasExponent = false;
                _position++;
                while (_position < _text.Length)
                {
                    var ch = _text[_position];
                    if (char.IsDigit(ch) || ch == '.')
                    {
                        _position++;
                        continue;
                    }

                    if ((ch == 'e' || ch == 'E') && !hasExponent)
                    {
                        hasExponent = true;
                        _position++;
                        if (_position < _text.Length && (_text[_position] == '+' || _text[_position] == '-'))
                        {
                            _position++;
                        }

                        continue;
                    }

                    break;
                }

                return new Token(TokenKind.Number, _text[start.._position], start);
            }

            if (char.IsLetter(current) || current == '_')
            {
                _position++;
                while (_position < _text.Length)
                {
                    var ch = _text[_position];
                    if (char.IsLetterOrDigit(ch) || ch is '_' or '.')
                    {
                        _position++;
                        continue;
                    }

                    break;
                }

                return new Token(TokenKind.Identifier, _text[start.._position], start);
            }

            throw new LambdaParseException($"Недопустимый символ '{current}'.", start);
        }

        private LambdaParseException Error(string message) => new(message, _current.Position);

        private static string Display(TokenKind kind) => kind switch
        {
            TokenKind.Plus => "+",
            TokenKind.Minus => "-",
            TokenKind.Star => "*",
            TokenKind.Slash => "/",
            TokenKind.Percent => "%",
            TokenKind.Caret => "^",
            TokenKind.LeftParenthesis => "(",
            TokenKind.RightParenthesis => ")",
            _ => kind.ToString(),
        };
    }

    private enum TokenKind
    {
        End,
        Number,
        Identifier,
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Caret,
        LeftParenthesis,
        RightParenthesis,
        Comma,
    }

    private readonly record struct Token(TokenKind Kind, string Text, int Position);
}

public sealed class LambdaParseException(string message, int position) : Exception($"{message} Позиция: {position}.")
{
    public int Position { get; } = position;
}
