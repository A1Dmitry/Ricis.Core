using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Proofs;
using Ricis.Core.SpecialFunctions;

namespace Ricis.Core.Extensions;

/// <summary>
/// Builds auditable academic proof protocols for deferred RICIS expressions.
/// The proof is a symbolic derivation: supplied conditions and constraints are
/// recorded as hypotheses but are never compiled, evaluated, or treated as an
/// oracle of semantic truth.
/// </summary>
public static class RicisAcademicProofExtensions
{
    /// <summary>
    /// Derives a provable unary scalar expression through the normative RICIS
    /// pipeline and appends an academic protocol containing only effective
    /// RICIS transformations to <paramref name="proof"/>. The returned lambda is an independent derived
    /// expression; all conditions and constraints remain unevaluated expression
    /// trees in the written hypotheses.
    /// </summary>
    /// <typeparam name="T">
    /// The intrinsic or generic-math scalar type of the delayed RICIS function.
    /// </typeparam>
    /// <param name="conditions">
    /// Formal assumptions represented by unary boolean lambdas over the same
    /// scalar parameter as <paramref name="claim"/>.
    /// </param>
    /// <param name="constraints">
    /// Formal domain restrictions represented by unary boolean lambdas. They
    /// are recorded separately from assumptions to preserve academic meaning.
    /// </param>
    /// <param name="claim">
    /// The deferred scalar expression whose RICIS normal form is to be derived.
    /// </param>
    /// <param name="proof">
    /// An output buffer that receives the proof protocol. Its existing contents
    /// are preserved and a complete new proof section is appended.
    /// </param>
    /// <returns>The independently normalized <see cref="Expression{TDelegate}"/> representing the derived RICIS expression.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required collection, claim, or proof buffer is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a hypothesis or the claim is not a unary lambda over T.
    /// </exception>
    public static Expression<Func<T, T>> Prove<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        StringBuilder proof)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(proof);

        var conditionList = conditions.ToList();
        var constraintList = constraints.ToList();
        ValidateHypotheses(conditionList, nameof(conditions));
        ValidateHypotheses(constraintList, nameof(constraints));
        ValidateClaim(claim);

        NumericConstants.Register<T>();
        var trace = new List<RicisPhaseTraceStep>();
        var derived = RicisPhasePipeline.SimplifyWithTrace(claim, trace) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                $"RICIS-конвейер должен сохранить Expression<Func<{typeof(T).Name}, {typeof(T).Name}>> при доказательстве.");

        AppendAcademicProtocol(proof, conditionList, constraintList, claim, trace, derived);
        return derived;
    }

    /// <summary>
    /// Derives a unary scalar expression and writes an academic proof document
    /// with an explicit finite or conditional proof scope. The document records
    /// the supplied hypotheses without evaluating them and embeds the effective
    /// RICIS derivation produced by <see cref="Prove{T}(IEnumerable{Expression{Func{T, Boolean}}}, IEnumerable{Expression{Func{T, Boolean}}}, Expression{Func{T, T}}, StringBuilder)"/>.
    /// </summary>
    /// <typeparam name="T">The intrinsic or generic-math scalar type of the delayed expression.</typeparam>
    /// <param name="conditions">The formal unary assumptions.</param>
    /// <param name="constraints">The formal unary domain restrictions.</param>
    /// <param name="claim">The delayed scalar expression to derive.</param>
    /// <param name="profile">Academic metadata, stated premises, and proof boundaries.</param>
    /// <param name="document">The buffer receiving the complete Markdown proof document.</param>
    /// <returns>The independent expression tree returned by the underlying symbolic derivation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> or <paramref name="document"/> is null.</exception>
    public static Expression<Func<T, T>> ProveDocument<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        RicisProofDocumentProfile profile,
        StringBuilder document)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(document);

        var derivation = new StringBuilder();
        var derived = conditions.Prove(constraints, claim, derivation);
        AppendProofDocument(document, profile, derivation, derived);
        return derived;
    }

    /// <summary>
    /// Derives a stated coordinate of a supported two-variable linear system and
    /// writes an academic proof document with an explicit finite or conditional
    /// proof scope. The system and restrictions remain unevaluated expression trees.
    /// </summary>
    /// <param name="equations">The two formal linear equations.</param>
    /// <param name="constraints">The formal binary domain restrictions.</param>
    /// <param name="claim">The coordinate equality to derive.</param>
    /// <param name="profile">Academic metadata, stated premises, and proof boundaries.</param>
    /// <param name="document">The buffer receiving the complete Markdown proof document.</param>
    /// <returns>The independent coordinate equality returned by symbolic elimination.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> or <paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the linear system or its claim is unsupported or contradictory.</exception>
    public static Expression<Func<double, double, bool>> ProveDocument(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        RicisProofDocumentProfile profile,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(document);

        var derivation = new StringBuilder();
        var derived = equations.Prove(constraints, claim, derivation);
        AppendProofDocument(document, profile, derivation, derived);
        return derived;
    }

    /// <summary>
    /// Applies the normative RICIS type-identity chain ID-01 through ID-06 to a
    /// formal reflected pair and writes a fully named academic proof document.
    /// The method constructs the two derived linear consequences
    /// <c>sigma+mirrorSigma=1</c> and <c>sigma-mirrorSigma=0</c> from the
    /// registered identity rules, then delegates exact elimination to the
    /// ordinary two-variable proof engine. No supplied constraint is compiled.
    /// </summary>
    /// <param name="constraints">Optional binary domain restrictions over the formal reflected pair.</param>
    /// <param name="claim">The coordinate equality to derive, normally <c>sigma=1/2</c>.</param>
    /// <param name="document">The buffer receiving the complete named proof document.</param>
    /// <returns>An independent derived equality expression for the claimed coordinate.</returns>
    /// <exception cref="ArgumentException">Thrown when the claim is not a supported exact coordinate consequence of the ID chain.</exception>
    public static Expression<Func<double, double, bool>> ProveTypeIdentityCriticalLine(
        this IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(document);
        var constraintList = constraints.ToList();
        ValidateBinaryHypotheses(constraintList, nameof(constraints));
        if (claim.Parameters.Count != 2)
        {
            throw new ArgumentException("Тезис цепочки ID-01–ID-06 обязан иметь две double-координаты.", nameof(claim));
        }

        var sigma = claim.Parameters[0];
        var mirrorSigma = claim.Parameters[1];
        var equations = new Expression<Func<double, double, bool>>[]
        {
            Expression.Lambda<Func<double, double, bool>>(
                Expression.Equal(Expression.Add(sigma, mirrorSigma), Expression.Constant(1.0)),
                sigma,
                mirrorSigma),
            Expression.Lambda<Func<double, double, bool>>(
                Expression.Equal(Expression.Subtract(sigma, mirrorSigma), Expression.Constant(0.0)),
                sigma,
                mirrorSigma),
        };
        var profile = CreateTypeIdentityProfile(sigma.Name ?? "sigma", mirrorSigma.Name ?? "mirrorSigma", claim);
        return equations.ProveDocument(constraintList, claim, profile, document);
    }

    /// <summary>
    /// Derives a stated coordinate of a two-variable linear system through
    /// symbolic elimination and writes an academic proof protocol to
    /// <paramref name="proof"/>. The supported system contains exactly two
    /// independent equations in the forms <c>x+y=c</c>, <c>x-y=c</c>,
    /// <c>y+x=c</c>, or <c>y-x=c</c>, together with optional two-variable
    /// boolean domain constraints. No equation or constraint is compiled.
    /// </summary>
    /// <param name="equations">The two formal equations of the linear system.</param>
    /// <param name="constraints">Optional domain constraints over the same pair of variables.</param>
    /// <param name="claim">A coordinate claim of the form <c>x=c</c> or <c>y=c</c>.</param>
    /// <param name="proof">The output buffer for the academic derivation.</param>
    /// <returns>An independent derived equality expression for the proved coordinate.</returns>
    /// <exception cref="ArgumentException">Thrown when the system is unsupported, degenerate, non-finite, overflows its finite double derivation, or the claim contradicts its symbolic solution.</exception>
    public static Expression<Func<double, double, bool>> Prove(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        StringBuilder proof)
    {
        ArgumentNullException.ThrowIfNull(equations);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(proof);

        var equationList = equations.ToList();
        var constraintList = constraints.ToList();
        if (equationList.Count != 2)
        {
            throw new ArgumentException("Система должна содержать ровно два линейных уравнения.", nameof(equations));
        }

        ValidateBinaryHypotheses(equationList, nameof(equations));
        ValidateBinaryHypotheses(constraintList, nameof(constraints));
        var first = ReadSupportedLinearEquation(equationList[0], nameof(equations));
        var second = ReadSupportedLinearEquation(equationList[1], nameof(equations));
        var determinant = (first.X * second.Y) - (second.X * first.Y);
        if (determinant == 0.0)
        {
            throw new ArgumentException("Линейная система вырождена: её determinant равен нулю.", nameof(equations));
        }

        var solutionX = ((first.Constant * second.Y) - (second.Constant * first.Y)) / determinant;
        var solutionY = ((first.X * second.Constant) - (second.X * first.Constant)) / determinant;
        if (!double.IsFinite(solutionX) || !double.IsFinite(solutionY))
        {
            throw new ArgumentException(
                "Линейная система не допускает конечного double-вывода без переполнения или неопределённости.",
                nameof(equations));
        }

        var (coordinate, claimedValue) = ReadCoordinateClaim(claim);
        if (!double.IsFinite(claimedValue))
        {
            throw new ArgumentException("Тезис системы должен содержать конечную double-константу или константную дробь.", nameof(claim));
        }

        var provenValue = coordinate == 0 ? solutionX : solutionY;
        if (claimedValue != provenValue)
        {
            throw new ArgumentException(
                $"Тезис требует {claim}, но система символически выводит {(coordinate == 0 ? "x" : "y")} = {provenValue:G17}.",
                nameof(claim));
        }

        var hasExactIntegralSystem = TrySolveIntegralSystem(first, second, out var exactX, out var exactY);
        var solutionXExpression = hasExactIntegralSystem
            ? BuildExactRationalExpression(exactX)
            : Expression.Constant(solutionX);
        var solutionYExpression = hasExactIntegralSystem
            ? BuildExactRationalExpression(exactY)
            : Expression.Constant(solutionY);
        var parameter = claim.Parameters[coordinate];
        var derived = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(parameter, coordinate == 0 ? solutionXExpression : solutionYExpression),
            claim.Parameters);
        AppendLinearSystemProtocol(
            proof,
            equationList,
            constraintList,
            claim,
            first,
            second,
            solutionX,
            solutionY,
            solutionXExpression,
            solutionYExpression,
            coordinate,
            derived);
        return derived;
    }

    private static RicisProofDocumentProfile CreateTypeIdentityProfile(
        string sigmaName,
        string mirrorSigmaName,
        Expression<Func<double, double, bool>> claim)
    {
        return new RicisProofDocumentProfile(
            title: "Нормативный вывод RICIS: тождество типа отражённой пары",
            scope: RicisProofScope.FiniteDerivation,
            @abstract: "Документирует полную нормативную цепочку ID-01–ID-06 для формальной отражённой пары и её точного рационального следствия.",
            theorem: $"По ID-01–ID-06 для пары {sigmaName}, {mirrorSigmaName} доказывается `{claim}`.",
            definitions:
            [
                $"{sigmaName} и {mirrorSigmaName} — координаты формальной отражённой пары.",
                "Type — сохранённый компонент идентичности Id(X)={Value(X), Type(X)}.",
            ],
            normativeSteps:
            [
                new RicisProofAxiomStep("ID-01", "самоидентификация", $"Сохранение идентичности отражённой пары даёт Type({sigmaName})=Type({mirrorSigmaName})."),
                new RicisProofAxiomStep("ID-02", "зеркальная симметрия", $"R({sigmaName})=1−{sigmaName}; следовательно {sigmaName}+{mirrorSigmaName}=1."),
                new RicisProofAxiomStep("ID-03", "верность типа координате", $"Равенство Type({sigmaName})=Type({mirrorSigmaName}) даёт {sigmaName}={mirrorSigmaName}."),
                new RicisProofAxiomStep("ID-04", "линейная пара идентичности", $"Из ID-02 и ID-03 следует {sigmaName}−{mirrorSigmaName}=0 при {sigmaName}+{mirrorSigmaName}=1."),
                new RicisProofAxiomStep("ID-05", "структурное исключение", $"Линейная комбинация ID-04 даёт 2·{sigmaName}=1."),
                new RicisProofAxiomStep("ID-06", "точное рациональное выделение", $"Из 2·{sigmaName}=1 следует {sigmaName}=Divide(1,2)."),
            ],
            limitations:
            [
                "Документ фиксирует нормативную цепочку ID-01–ID-06 для одной формальной отражённой пары; область применения пары задаётся моделью пользователя.",
            ]);
    }

    private static void AppendProofDocument(
        StringBuilder document,
        RicisProofDocumentProfile profile,
        StringBuilder derivation,
        LambdaExpression derived)
    {
        if (document.Length > 0 && document[^1] != '\n')
        {
            document.AppendLine();
        }

        document.Append("# ").AppendLine(profile.Title);
        document.AppendLine();
        document.AppendLine("## Аннотация");
        document.AppendLine(profile.Abstract);
        document.AppendLine();
        document.AppendLine("## Доказательный статус");
        document.AppendLine(profile.Scope switch
        {
            RicisProofScope.FiniteDerivation =>
                "**Конечное символическое выведение.** Документ сертифицирует только преобразование явно переданных expression tree.",
            RicisProofScope.ConditionalTheorem =>
                "**Условная теорема.** Заключение выводится только при истинности перечисленных формальных предпосылок; RICIS не объявляет их истинными.",
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        });
        document.AppendLine();
        AppendDocumentSection(document, "Определения", profile.Definitions, "Дополнительные определения не заданы.");
        AppendDocumentSection(document, "Аксиомы и внешние предпосылки", profile.Axioms, "Дополнительные аксиомы не заданы.");
        AppendNormativeAxiomSteps(document, profile.NormativeSteps);
        document.AppendLine("## Теорема или конечный тезис");
        document.AppendLine(profile.Theorem);
        document.AppendLine();
        document.AppendLine("## Машинно воспроизводимое символическое выведение");
        document.AppendLine("Следующие шаги получены из expression tree; входные условия и ограничения не компилировались и не исполнялись.");
        document.AppendLine();
        AppendNestedMarkdown(document, derivation);
        document.AppendLine();
        document.AppendLine("## Воспроизводимый результат");
        document.Append("Производное expression tree: `").Append(derived).AppendLine("`.");
        document.AppendLine();
        AppendDocumentSection(
            document,
            "Границы и непроверенные утверждения",
            profile.Limitations,
            "Внешняя истинность предпосылок, универсальные кванторы и утверждения вне входных expression tree данным документом не доказываются.");
    }

    private static void AppendNormativeAxiomSteps(
        StringBuilder document,
        IReadOnlyList<RicisProofAxiomStep> steps)
    {
        document.AppendLine("## Нормативная цепочка RICIS");
        if (steps.Count == 0)
        {
            document.AppendLine("Дополнительные именованные нормативные шаги не заданы.");
        }
        else
        {
            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                document.Append("### Шаг ").Append(index + 1).Append(": ")
                    .Append(step.RuleId).Append(" — ").AppendLine(step.Title);
                document.Append("**Нормативное следствие:** ").AppendLine(step.Statement);
            }
        }

        document.AppendLine();
    }

    private static void AppendNestedMarkdown(StringBuilder document, StringBuilder derivation)
    {
        var lines = derivation.ToString().Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith('#'))
            {
                document.Append("##");
            }

            document.AppendLine(line);
        }
    }

    private static void AppendDocumentSection(
        StringBuilder document,
        string title,
        IReadOnlyList<string> lines,
        string emptyMessage)
    {
        document.Append("## ").AppendLine(title);
        if (lines.Count == 0)
        {
            document.AppendLine(emptyMessage);
        }
        else
        {
            for (var index = 0; index < lines.Count; index++)
            {
                document.Append(index + 1).Append(". ").AppendLine(lines[index]);
            }
        }

        document.AppendLine();
    }

    private static void ValidateBinaryHypotheses(
        IEnumerable<Expression<Func<double, double, bool>>> hypotheses,
        string parameterName)
    {
        foreach (var hypothesis in hypotheses)
        {
            if (hypothesis is null || hypothesis.Parameters.Count != 2 ||
                hypothesis.Parameters[0].Type != typeof(double) ||
                hypothesis.Parameters[1].Type != typeof(double) ||
                hypothesis.ReturnType != typeof(bool))
            {
                throw new ArgumentException(
                    "Каждое уравнение или ограничение должно быть лямбдой Func<Double, Double, Boolean> с двумя параметрами.",
                    parameterName);
            }
        }
    }

    private static LinearEquation ReadSupportedLinearEquation(
        Expression<Func<double, double, bool>> equation,
        string parameterName)
    {
        if (equation.Body is not BinaryExpression { NodeType: ExpressionType.Equal, Left: var left, Right: ConstantExpression { Value: double constant } } ||
            left is not BinaryExpression { NodeType: var operation, Left: var firstTerm, Right: var secondTerm } ||
            operation is not ExpressionType.Add and not ExpressionType.Subtract)
        {
            throw new ArgumentException(
                "Поддерживаются линейные уравнения формы x+y=c, x-y=c, y+x=c или y-x=c, где c — double-константа.",
                parameterName);
        }

        var firstIndex = ParameterIndex(firstTerm, equation.Parameters);
        var secondIndex = ParameterIndex(secondTerm, equation.Parameters);
        if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex)
        {
            throw new ArgumentException(
                "Каждое линейное уравнение должно содержать обе переменные ровно по одному разу.",
                parameterName);
        }

        if (!double.IsFinite(constant))
        {
            throw new ArgumentException("Правая часть каждого уравнения должна быть конечной double-константой.", parameterName);
        }

        var firstCoefficient = 1.0;
        var secondCoefficient = operation == ExpressionType.Add ? 1.0 : -1.0;
        var xCoefficient = firstIndex == 0 ? firstCoefficient : secondCoefficient;
        var yCoefficient = firstIndex == 1 ? firstCoefficient : secondCoefficient;
        return new LinearEquation(xCoefficient, yCoefficient, constant);
    }

    private static int ParameterIndex(Expression expression, IReadOnlyList<ParameterExpression> parameters) =>
        expression is ParameterExpression parameter && ReferenceEquals(parameter, parameters[0]) ? 0 :
        expression is ParameterExpression second && ReferenceEquals(second, parameters[1]) ? 1 : -1;

    private static (int Coordinate, double Value) ReadCoordinateClaim(Expression<Func<double, double, bool>> claim)
    {
        if (claim.Body is not BinaryExpression { NodeType: ExpressionType.Equal, Left: var left, Right: var right })
        {
            throw new ArgumentException("Тезис системы должен иметь форму x=c или y=c.", nameof(claim));
        }

        var leftIndex = ParameterIndex(left, claim.Parameters);
        if (leftIndex >= 0 && TryReadFiniteDoubleScalar(right, out var rightValue))
        {
            return (leftIndex, rightValue);
        }

        var rightIndex = ParameterIndex(right, claim.Parameters);
        if (rightIndex >= 0 && TryReadFiniteDoubleScalar(left, out var leftValue))
        {
            return (rightIndex, leftValue);
        }

        throw new ArgumentException(
            "Тезис системы должен иметь форму x=c или y=c, где c — конечная double-константа либо константная дробь.",
            nameof(claim));
    }

    private static bool TryReadFiniteDoubleScalar(Expression expression, out double value)
    {
        if (expression is ConstantExpression { Value: double constant } && double.IsFinite(constant))
        {
            value = constant;
            return true;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Divide, Left: var numerator, Right: var denominator } &&
            TryReadFiniteDoubleScalar(numerator, out var numeratorValue) &&
            TryReadFiniteDoubleScalar(denominator, out var denominatorValue) &&
            denominatorValue != 0.0)
        {
            value = numeratorValue / denominatorValue;
            return double.IsFinite(value);
        }

        value = 0.0;
        return false;
    }

    private static bool TrySolveIntegralSystem(
        LinearEquation first,
        LinearEquation second,
        out ExactRational x,
        out ExactRational y)
    {
        x = default;
        y = default;
        if (!TryReadIntegralDouble(first.Constant, out var firstConstant) ||
            !TryReadIntegralDouble(second.Constant, out var secondConstant))
        {
            return false;
        }

        var firstX = new BigInteger(first.X);
        var firstY = new BigInteger(first.Y);
        var secondX = new BigInteger(second.X);
        var secondY = new BigInteger(second.Y);
        var determinant = (firstX * secondY) - (secondX * firstY);
        if (determinant.IsZero)
        {
            return false;
        }

        x = ExactRational.Create(
            (firstConstant * secondY) - (secondConstant * firstY),
            determinant);
        y = ExactRational.Create(
            (firstX * secondConstant) - (secondX * firstConstant),
            determinant);
        return true;
    }

    private static bool TryReadIntegralDouble(double value, out BigInteger integer)
    {
        integer = BigInteger.Zero;
        if (!double.IsFinite(value) || value != Math.Truncate(value))
        {
            return false;
        }

        try
        {
            integer = new BigInteger(value);
            return (double)integer == value;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static Expression BuildExactRationalExpression(ExactRational value)
    {
        var numerator = Expression.Constant((double)value.Numerator);
        if (value.Denominator.IsOne)
        {
            return numerator;
        }

        return Expression.Divide(numerator, Expression.Constant((double)value.Denominator));
    }

    private static void AppendLinearSystemProtocol(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<double, double, bool>>> equations,
        IReadOnlyList<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        LinearEquation first,
        LinearEquation second,
        double solutionX,
        double solutionY,
        Expression solutionXExpression,
        Expression solutionYExpression,
        int coordinate,
        Expression<Func<double, double, bool>> derived)
    {
        if (proof.Length > 0 && proof[^1] != '\n')
        {
            proof.AppendLine();
        }

        var x = claim.Parameters[0];
        var y = claim.Parameters[1];
        var xName = x.Name ?? "x";
        var yName = y.Name ?? "y";
        var eliminationCoefficient = (second.X * first.Y) - (first.X * second.Y);
        var eliminationConstant = (second.Constant * first.Y) - (first.Constant * second.Y);
        var combined = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(
                Expression.Multiply(Expression.Constant(eliminationCoefficient), x),
                Expression.Constant(eliminationConstant)),
            x,
            y);
        var xResult = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(x, solutionXExpression), x, y);
        var firstWithProofParameters = new ParameterSubstitutionVisitor(equations[0].Parameters[0], x)
            .Visit(equations[0].Body);
        firstWithProofParameters = new ParameterSubstitutionVisitor(equations[0].Parameters[1], y)
            .Visit(firstWithProofParameters);
        var substituted = Expression.Lambda<Func<double, double, bool>>(
            new ParameterSubstitutionVisitor(x, solutionXExpression).Visit(firstWithProofParameters),
            x,
            y);
        var yResult = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(y, solutionYExpression), x, y);

        proof.AppendLine("# Формальный вывод RICIS III: система линейных уравнений");
        proof.AppendLine();
        proof.AppendLine("## Система уравнений");
        for (var index = 0; index < equations.Count; index++)
        {
            proof.Append(index + 1).Append(". Уравнение: `").Append(equations[index]).AppendLine("`.");
        }

        proof.AppendLine();
        proof.AppendLine("## Ограничения области");
        AppendBinaryHypotheses(proof, constraints);
        proof.AppendLine();
        proof.AppendLine("## Тезис");
        proof.Append("Доказуемое следствие: `").Append(claim).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("## Символическое исключение");
        proof.AppendLine("Ни уравнения, ни ограничения не исполнялись численно; коэффициенты извлечены из их expression tree.");
        proof.AppendLine();
        proof.AppendLine("### Шаг 1: Линейная комбинация уравнений системы");
        proof.Append("**Основание:** умножение первого равенства на противоположный коэффициент ")
            .Append(yName)
            .Append(" второго и второго на коэффициент ")
            .Append(yName)
            .AppendLine(" первого; переменная исключается по детерминанту.");
        proof.Append("До: `").Append(equations[0]).Append("`; `").Append(equations[1]).AppendLine("`.");
        proof.Append("После: `").Append(combined).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("### Шаг 2: Выделение первой координаты");
        proof.AppendLine("**Основание:** точное деление обеих частей линейного равенства на ненулевой коэффициент.");
        proof.Append("До: `").Append(combined).AppendLine("`.");
        proof.Append("После: `").Append(xResult).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("### Шаг 3: Подстановка найденной координаты в первое уравнение");
        proof.AppendLine("**Основание:** подстановка равных выражений в формальное равенство.");
        proof.Append("До: `").Append(equations[0]).AppendLine("`.");
        proof.Append("После: `").Append(substituted).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("### Шаг 4: Выделение второй координаты");
        proof.AppendLine("**Основание:** точное решение линейного равенства по оставшейся координате.");
        proof.Append("До: `").Append(substituted).AppendLine("`.");
        proof.Append("После: `").Append(yResult).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("## Заключение");
        proof.Append("Следовательно, система выводит ").Append(xName).Append('=').Append(solutionXExpression)
            .Append(" и ").Append(yName).Append('=').Append(solutionYExpression)
            .Append("; требуемая координата ").Append(coordinate == 0 ? xName : yName)
            .Append(" доказана выражением `").Append(derived).AppendLine("`.");
    }

    private static void AppendBinaryHypotheses(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<double, double, bool>>> constraints)
    {
        if (constraints.Count == 0)
        {
            proof.AppendLine("Формальные ограничения не заданы.");
            return;
        }

        for (var index = 0; index < constraints.Count; index++)
        {
            proof.Append(index + 1).Append(". Ограничение: `").Append(constraints[index]).AppendLine("`.");
        }
    }

    private sealed record LinearEquation(double X, double Y, double Constant);

    private readonly record struct ExactRational(BigInteger Numerator, BigInteger Denominator)
    {
        public static ExactRational Create(BigInteger numerator, BigInteger denominator)
        {
            if (denominator.IsZero)
            {
                throw new DivideByZeroException("Рациональный вывод не может иметь нулевой знаменатель.");
            }

            if (denominator.Sign < 0)
            {
                numerator = BigInteger.Negate(numerator);
                denominator = BigInteger.Negate(denominator);
            }

            var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
            return new ExactRational(numerator / divisor, denominator / divisor);
        }
    }

    private sealed class ParameterSubstitutionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        private readonly Expression _replacement;

        public ParameterSubstitutionVisitor(ParameterExpression parameter, Expression replacement)
        {
            _parameter = parameter;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            ReferenceEquals(node, _parameter) ? _replacement : base.VisitParameter(node);
    }

    private static void ValidateHypotheses<T>(
        IEnumerable<Expression<Func<T, bool>>> hypotheses,
        string parameterName)
        where T : INumber<T>
    {
        foreach (var hypothesis in hypotheses)
        {
            if (hypothesis is null)
            {
                throw new ArgumentException("Список гипотез не может содержать null.", parameterName);
            }

            if (hypothesis.Parameters.Count != 1 || hypothesis.Parameters[0].Type != typeof(T) ||
                hypothesis.ReturnType != typeof(bool))
            {
                throw new ArgumentException(
                    $"Каждая гипотеза {parameterName} должна быть лямбдой Func<{typeof(T).Name}, Boolean> с одним параметром.",
                    parameterName);
            }
        }
    }

    private static void ValidateClaim<T>(Expression<Func<T, T>> claim)
        where T : INumber<T>
    {
        if (claim.Parameters.Count != 1 || claim.Parameters[0].Type != typeof(T) || claim.ReturnType != typeof(T))
        {
            throw new ArgumentException(
                $"Тезис должен быть лямбдой Func<{typeof(T).Name}, {typeof(T).Name}> с одним параметром.",
                nameof(claim));
        }
    }

    private static void AppendAcademicProtocol<T>(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<T, bool>>> conditions,
        IReadOnlyList<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        IReadOnlyList<RicisPhaseTraceStep> trace,
        Expression<Func<T, T>> derived)
        where T : INumber<T>
    {
        if (proof.Length > 0 && proof[^1] != '\n')
        {
            proof.AppendLine();
        }

        proof.AppendLine("# Формальный вывод RICIS III");
        proof.AppendLine();
        proof.AppendLine("## Предпосылки");
        AppendHypotheses(proof, "Условие", conditions);
        proof.AppendLine();
        proof.AppendLine("## Ограничения области");
        AppendHypotheses(proof, "Ограничение", constraints);
        proof.AppendLine();
        proof.AppendLine("## Тезис");
        proof.Append("Доказуемое отложенное выражение: `").Append(claim).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("## Нормативный вывод");
        proof.AppendLine("Ни одна предпосылка не исполнялась численно. Ниже записаны только нормативные фазы, которые действительно изменили expression tree; неизменяющие и неприменённые фазы в текст доказательства не включаются.");
        proof.AppendLine();

        var effectiveSteps = trace.Where(step => step.Changed).ToList();
        if (effectiveSteps.Count == 0)
        {
            proof.AppendLine("Ни одна нормативная фаза не изменила тезис: производное выражение структурно совпадает с исходным.");
            proof.AppendLine();
        }

        for (var index = 0; index < effectiveSteps.Count; index++)
        {
            var step = effectiveSteps[index];
            proof.Append("### Шаг ").Append(index + 1).Append(": ").AppendLine(step.PhaseName);
            proof.Append("**Нормативное основание:** ").AppendLine(step.RuleFamily + ".");

            var intermediateSteps = BuildIntermediateSteps(step);
            if (intermediateSteps.Count == 0)
            {
                proof.Append("До: `").Append(step.Before).AppendLine("`.");
                proof.Append("После: `").Append(step.After).AppendLine("`.");
            }
            else
            {
                proof.AppendLine("**Промежуточное выведение:**");
                for (var detailIndex = 0; detailIndex < intermediateSteps.Count; detailIndex++)
                {
                    var detail = intermediateSteps[detailIndex];
                    proof.Append("#### Шаг ")
                        .Append(index + 1)
                        .Append('.')
                        .Append(detailIndex + 1)
                        .Append(": ")
                        .AppendLine(detail.Title);
                    proof.Append("**Основание:** ").AppendLine(detail.Rule + ".");
                    proof.Append("До: `").Append(FormatAsLambda(step.Before, detail.Before)).AppendLine("`.");
                    proof.Append("После: `").Append(FormatAsLambda(step.Before, detail.After)).AppendLine("`.");
                }
            }

            proof.AppendLine();
        }

        proof.AppendLine("## Заключение");
        proof.Append("Следовательно, в рамках перечисленных формальных условий и ограничений производное RICIS-выражение имеет вид: `")
            .Append(derived)
            .AppendLine("`.");
        proof.AppendLine("Протокол фиксирует символическое выведение и не утверждает истинность внешних предпосылок вне переданных expression tree.");
    }

    private static IReadOnlyList<IntermediateProofStep> BuildIntermediateSteps(RicisPhaseTraceStep step)
    {
        if (!step.RuleFamily.StartsWith("SP2", StringComparison.Ordinal) ||
            step.Before is not LambdaExpression before ||
            step.After is not LambdaExpression after ||
            before.Parameters.Count != 1)
        {
            return [];
        }

        if (TryBuildDifferenceOfSquaresSteps(before.Body, after.Body, out var squares))
        {
            return squares;
        }

        if (TryBuildDifferenceOfCubesSteps(before.Body, after.Body, out var cubes))
        {
            return cubes;
        }

        if (TryBuildSumOfCubesSteps(before.Body, after.Body, out var sumOfCubes))
        {
            return sumOfCubes;
        }

        if (TryBuildNestedRatioSteps(before.Body, after.Body, out var nestedRatio))
        {
            return nestedRatio;
        }

        if (before.Body is not BinaryExpression { NodeType: ExpressionType.Divide, Right: BinaryExpression { NodeType: ExpressionType.Multiply } } &&
            TryBuildCommonFactorSteps(before.Body, after.Body, out var commonFactor))
        {
            return commonFactor;
        }

        if (TryBuildAssociativeFactorSteps(before.Body, after.Body, out var associativeFactors))
        {
            return associativeFactors;
        }

        if (TryBuildAdjacentFactorialSteps(before.Body, after.Body, out var factorials))
        {
            return factorials;
        }

        return [];
    }

    private static bool TryBuildDifferenceOfSquaresSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Subtract } numerator, Right: BinaryExpression denominator } ||
            denominator.NodeType is not ExpressionType.Subtract and not ExpressionType.Add ||
            !TryReadSquare(numerator.Left, out var leftBase) ||
            !TryReadSquare(numerator.Right, out var rightBase) ||
            !denominator.Left.AreEqual(leftBase) ||
            !denominator.Right.AreEqual(rightBase))
        {
            return false;
        }

        var factor = denominator;
        var quotient = denominator.NodeType == ExpressionType.Subtract
            ? Expression.Add(leftBase, rightBase)
            : Expression.Subtract(leftBase, rightBase);
        var factorized = Expression.Divide(Expression.Multiply(factor, quotient), factor);
        if (!quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                "Разложение разности квадратов",
                "SP2: A²−B² = (A−B)(A+B)",
                before,
                factorized),
            new IntermediateProofStep(
                "Сокращение общего множителя",
                "SP2: (F·G)/F = G",
                factorized,
                quotient),
        ];
        return true;
    }

    private static bool TryBuildDifferenceOfCubesSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Subtract } numerator, Right: var denominator } ||
            !TryReadCube(numerator.Left, out var leftBase) ||
            !TryReadCube(numerator.Right, out var rightBase) ||
            denominator is not BinaryExpression { NodeType: ExpressionType.Subtract } difference ||
            !difference.Left.AreEqual(leftBase) ||
            !difference.Right.AreEqual(rightBase))
        {
            return false;
        }

        var leftSquare = Expression.Multiply(leftBase, leftBase);
        // AlgebraicReductionVisitor canonicalizes the mixed term as B·A.
        // Reproduce that exact expression tree so the proof chain terminates
        // at the independently derived SP2 result without a fictitious swap.
        var product = Expression.Multiply(rightBase, leftBase);
        var rightSquare = BuildSquare(rightBase);
        var quotient = Expression.Add(Expression.Add(leftSquare, product), rightSquare);
        var factor = Expression.Subtract(leftBase, rightBase);
        var factorized = Expression.Divide(Expression.Multiply(factor, quotient), factor);
        if (!quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                "Разложение разности кубов",
                "SP2: A³−B³ = (A−B)(A²+A·B+B²)",
                before,
                factorized),
            new IntermediateProofStep(
                "Сокращение общего множителя",
                "SP2: (F·G)/F = G",
                factorized,
                quotient),
        ];
        return true;
    }

    private static bool TryBuildSumOfCubesSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Add } numerator, Right: BinaryExpression { NodeType: ExpressionType.Add } denominator } ||
            !TryReadCube(numerator.Left, out var leftBase) ||
            !TryReadCube(numerator.Right, out var rightBase) ||
            !denominator.Left.AreEqual(leftBase) ||
            !denominator.Right.AreEqual(rightBase))
        {
            return false;
        }

        var leftSquare = Expression.Multiply(leftBase, leftBase);
        var negativeProduct = Expression.Multiply(BuildNegated(rightBase), leftBase);
        var rightSquare = BuildSquare(rightBase);
        var quotient = Expression.Add(Expression.Add(leftSquare, negativeProduct), rightSquare);
        var factor = Expression.Add(leftBase, rightBase);
        var factorized = Expression.Divide(Expression.Multiply(factor, quotient), factor);
        if (!quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                "Разложение суммы кубов",
                "SP2: A³+B³ = (A+B)(A²−A·B+B²)",
                before,
                factorized),
            new IntermediateProofStep(
                "Сокращение общего множителя",
                "SP2: (F·G)/F = G",
                factorized,
                quotient),
        ];
        return true;
    }

    private static bool TryBuildNestedRatioSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: var numerator, Right: BinaryExpression { NodeType: ExpressionType.Divide } ratio })
        {
            return false;
        }

        var normalized = Expression.Divide(Expression.Multiply(numerator, ratio.Right), ratio.Left);
        if (normalized.AreEqual(after))
        {
            steps =
            [
                new IntermediateProofStep(
                    "Очищение вложенного знаменателя",
                    "SP2: F/(G/H) = (F·H)/G",
                    before,
                    normalized),
            ];
            return true;
        }

        if (TryBuildCommonFactorSteps(normalized, after, out var commonFactor))
        {
            steps =
            [
                new IntermediateProofStep(
                    "Очищение вложенного знаменателя",
                    "SP2: F/(G/H) = (F·H)/G",
                    before,
                    normalized),
                .. commonFactor,
            ];
            return true;
        }

        if (TryBuildAssociativeFactorSteps(normalized, after, out var associativeFactors))
        {
            steps =
            [
                new IntermediateProofStep(
                    "Очищение вложенного знаменателя",
                    "SP2: F/(G/H) = (F·H)/G",
                    before,
                    normalized),
                .. associativeFactors,
            ];
            return true;
        }

        return false;
    }

    private static bool TryBuildAssociativeFactorSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: var numerator, Right: var denominator })
        {
            return false;
        }

        var numeratorFactors = FlattenBuiltInProduct(numerator);
        var denominatorFactors = FlattenBuiltInProduct(denominator);
        if (numeratorFactors.Count < 2 && denominatorFactors.Count < 2)
        {
            return false;
        }

        var remainingNumerator = numeratorFactors.ToList();
        var remainingDenominator = denominatorFactors.ToList();
        var cancelled = 0;
        for (var denominatorIndex = remainingDenominator.Count - 1; denominatorIndex >= 0; denominatorIndex--)
        {
            var matchIndex = remainingNumerator.FindIndex(factor => factor.AreEqual(remainingDenominator[denominatorIndex]));
            if (matchIndex < 0)
            {
                continue;
            }

            remainingNumerator.RemoveAt(matchIndex);
            remainingDenominator.RemoveAt(denominatorIndex);
            cancelled++;
        }

        if (cancelled == 0)
        {
            return false;
        }

        var reducedNumerator = BuildProduct(remainingNumerator, numerator.Type);
        var reducedDenominator = BuildProduct(remainingDenominator, denominator.Type);
        var reduced = reducedDenominator.IsOne()
            ? reducedNumerator
            : Expression.Divide(reducedNumerator, reducedDenominator);
        if (!reduced.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                "Ассоциативное сокращение множителей",
                "SP2: сокращение общего мультимножества факторов",
                before,
                reduced),
        ];
        return true;
    }

    private static List<Expression> FlattenBuiltInProduct(Expression expression)
    {
        var factors = new List<Expression>();
        CollectBuiltInProductFactors(expression, factors);
        return factors;
    }

    private static void CollectBuiltInProductFactors(Expression expression, ICollection<Expression> factors)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply } product &&
            (product.Method is null || NumericConstants.IsIntrinsicNumeric(product.Type)))
        {
            CollectBuiltInProductFactors(product.Left, factors);
            CollectBuiltInProductFactors(product.Right, factors);
            return;
        }

        factors.Add(expression);
    }

    private static Expression BuildProduct(IReadOnlyList<Expression> factors, Type scalarType) => factors.Count switch
    {
        0 => NumericConstants.OneOf(scalarType),
        1 => factors[0],
        _ => factors.Aggregate(Expression.Multiply),
    };

    private static bool TryBuildAdjacentFactorialSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: MethodCallExpression numerator, Right: MethodCallExpression denominator } ||
            numerator.Method != typeof(Factorial).GetMethod(nameof(Factorial.Of)) ||
            denominator.Method != typeof(Factorial).GetMethod(nameof(Factorial.Of)) ||
            numerator.Arguments.Count != 1 || denominator.Arguments.Count != 1)
        {
            return false;
        }

        var value = numerator.Arguments[0];
        if (denominator.Arguments[0] is not BinaryExpression { NodeType: ExpressionType.Subtract, Left: var predecessorValue, Right: var decrement } ||
            !predecessorValue.AreEqual(value) ||
            !IsOneOrStaticOne(decrement, value.Type) ||
            !value.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                "Сокращение соседних факториалов",
                "SP2: n!/(n−1)! = n",
                before,
                value),
        ];
        return true;
    }

    private static bool IsOneOrStaticOne(Expression expression, Type scalarType)
    {
        if (expression.IsOne())
        {
            return true;
        }

        return expression is MemberExpression { Expression: null, Member: var member } &&
               member.Name == "One" &&
               member.DeclaringType == scalarType;
    }

    private static bool TryBuildCommonFactorSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Multiply } product, Right: var denominator })
        {
            return false;
        }

        var factor = product.Left.AreEqual(denominator)
            ? product.Left
            : product.Right.AreEqual(denominator)
                ? product.Right
                : null;
        var quotient = factor is null
            ? null
            : product.Left.AreEqual(factor)
                ? product.Right
                : product.Left;
        if (factor is null || quotient is null || !quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                "Сокращение общего множителя",
                "SP2: (F·G)/F = G",
                before,
                quotient),
        ];
        return true;
    }

    private static Expression BuildNegated(Expression expression)
    {
        if (expression is ConstantExpression { Type: var type, Value: double value } && type == typeof(double))
        {
            return Expression.Constant(-value, typeof(double));
        }

        return Expression.Negate(expression);
    }

    private static Expression BuildSquare(Expression expression)
    {
        if (expression is ConstantExpression { Type: var type, Value: double value } && type == typeof(double))
        {
            return Expression.Constant(value * value, typeof(double));
        }

        return Expression.Multiply(expression, expression);
    }

    private static bool TryReadSquare(Expression expression, out Expression @base)
    {
        @base = null;
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply, Left: var left, Right: var right } &&
            left.AreEqual(right))
        {
            @base = left;
            return true;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Power, Left: var powerBase, Right: ConstantExpression { Value: double exponent } } &&
            exponent == 2.0)
        {
            @base = powerBase;
            return true;
        }

        return TryReadExactDoublePower(expression, 2, out @base);
    }

    private static bool TryReadCube(Expression expression, out Expression @base)
    {
        @base = null;
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply, Left: BinaryExpression { NodeType: ExpressionType.Multiply } square, Right: var third } &&
            square.Left.AreEqual(square.Right) &&
            square.Left.AreEqual(third))
        {
            @base = square.Left;
            return true;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Power, Left: var powerBase, Right: ConstantExpression { Value: double exponent } } &&
            exponent == 3.0)
        {
            @base = powerBase;
            return true;
        }

        return TryReadExactDoublePower(expression, 3, out @base);
    }

    private static bool TryReadExactDoublePower(Expression expression, int power, out Expression @base)
    {
        @base = null;
        if (expression is not ConstantExpression { Type: var type, Value: double value } || type != typeof(double))
        {
            return false;
        }

        var root = power == 2 ? Math.Sqrt(value) : Math.Cbrt(value);
        var rounded = Math.Round(root);
        if (double.IsNaN(root) || double.IsInfinity(root) || Math.Pow(rounded, power) != value)
        {
            return false;
        }

        @base = Expression.Constant(rounded, typeof(double));
        return true;
    }

    private static Expression FormatAsLambda(Expression sourceLambda, Expression body)
    {
        if (sourceLambda is LambdaExpression lambda)
        {
            return Expression.Lambda(lambda.Type, body, lambda.Parameters);
        }

        return body;
    }

    private sealed record IntermediateProofStep(string Title, string Rule, Expression Before, Expression After);

    private static void AppendHypotheses<T>(
        StringBuilder proof,
        string label,
        IReadOnlyList<Expression<Func<T, bool>>> hypotheses)
        where T : INumber<T>
    {
        if (hypotheses.Count == 0)
        {
            proof.AppendLine("Формальные высказывания не заданы.");
            return;
        }

        for (var index = 0; index < hypotheses.Count; index++)
        {
            proof.Append(index + 1)
                .Append(". ")
                .Append(label)
                .Append(": `")
                .Append(hypotheses[index])
                .AppendLine("`.");
        }
    }
}
