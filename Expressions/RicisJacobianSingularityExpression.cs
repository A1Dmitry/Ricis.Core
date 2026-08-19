using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Phases;
using Ricis.Core.Extensions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents the RICIS structural state of a singular Jacobian.
/// The determinant is retained as the indexed zero payload and the formal inverse
/// entries are retained as indexed infinity payloads; this is not a classical matrix inverse.
/// </summary>
/// <typeparam name="T">The scalar coordinate type of the Jacobian entries.</typeparam>
public sealed class RicisJacobianSingularityExpression<T>
    where T : INumber<T>
{
    private readonly ReadOnlyCollection<LambdaExpression> _inversePayload;
    private readonly ReadOnlyCollection<(ParameterExpression Param, double Value)> _roots;

    /// <summary>
    /// Initializes a singular Jacobian state from its determinant and formal inverse payload entries.
    /// </summary>
    /// <param name="determinant">The delayed determinant expression, normally structurally zero at the singular key.</param>
    /// <param name="inversePayload">The delayed formal inverse entries with a common parameter signature.</param>
    /// <param name="roots">Certified singularity keys retained by both indexed nodes.</param>
    /// <exception cref="ArgumentNullException">Thrown when an input is null.</exception>
    /// <exception cref="ArgumentException">Thrown when payload entries have incompatible signatures or result types.</exception>
    public RicisJacobianSingularityExpression(
        LambdaExpression determinant,
        IEnumerable<LambdaExpression> inversePayload,
        IEnumerable<(ParameterExpression Param, double Value)> roots = null)
    {
        ArgumentNullException.ThrowIfNull(determinant);
        ArgumentNullException.ThrowIfNull(inversePayload);
        if (determinant.ReturnType != typeof(T))
            throw new ArgumentException("Determinant должен возвращать тип T.", nameof(determinant));

        var copied = inversePayload.ToArray();
        if (copied.Length == 0)
            throw new ArgumentException("Формальный inverse payload обязан иметь хотя бы одну компоненту.", nameof(inversePayload));
        if (copied.Any(entry => entry.ReturnType != typeof(T) || entry.Parameters.Count != determinant.Parameters.Count))
            throw new ArgumentException("Все inverse payload элементы должны иметь тип T и сигнатуру determinant.", nameof(inversePayload));
        for (var i = 0; i < determinant.Parameters.Count; i++)
        {
            if (copied.Any(entry => entry.Parameters[i].Type != determinant.Parameters[i].Type))
                throw new ArgumentException("Все payload элементы должны иметь одинаковые типы параметров.", nameof(inversePayload));
        }

        Determinant = determinant;
        _inversePayload = Array.AsReadOnly(copied);
        _roots = Array.AsReadOnly((roots ?? []).ToArray());
    }

    /// <summary>
    /// Gets the delayed determinant expression used as the `0_det(J)` index.
    /// </summary>
    public LambdaExpression Determinant { get; }

    /// <summary>
    /// Gets the delayed formal inverse payload entries used as `∞_Inv(J)` numerators.
    /// </summary>
    public IReadOnlyList<LambdaExpression> InversePayload => _inversePayload;

    /// <summary>
    /// Gets the certified singularity keys carried by the indexed determinant and inverse nodes.
    /// </summary>
    public IReadOnlyList<(ParameterExpression Param, double Value)> Roots => _roots;

    /// <summary>
    /// Gets whether the determinant body is structurally an indexed zero candidate.
    /// </summary>
    public bool IsStructuralSingular => Determinant.Body.IsZero();

    /// <summary>
    /// Applies the RICIS A6 geometric bridge entrywise.
    /// Each result is the exact structural product `det(J)·Inv(J)` and retains
    /// the determinant and inverse payload in the resulting expression tree.
    /// </summary>
    /// <returns>The bridged scalar payload entries.</returns>
    public IReadOnlyList<LambdaExpression> ApplyA6GeometricBridge()
    {
        var parameters = CreateParameters(Determinant.Parameters);
        var determinantBody = Rebind(Determinant, parameters);
        var result = new List<LambdaExpression>(_inversePayload.Count);
        foreach (var payload in _inversePayload)
        {
            var payloadBody = Rebind(payload, parameters);
            var indexedZero = new ZeroInfinityExpression(determinantBody, Roots.ToList());
            var indexedInfinity = InfinityExpression.CreateLazy(payloadBody, Roots.ToList());
            var bridged = RicisPhasePipeline.Simplify(Expression.Multiply(indexedZero, indexedInfinity))
                ?? throw new InvalidOperationException("A6 geometric bridge не построил payload.");
            result.Add(Expression.Lambda(bridged, parameters));
        }

        return new ReadOnlyCollection<LambdaExpression>(result);
    }

    /// <summary>
    /// Returns the structural RICIS record of determinant and inverse payload.
    /// </summary>
    public override string ToString()
    {
        var keys = Roots.Count == 0
            ? string.Empty
            : $" at {{{string.Join(", ", Roots.Select(root => $"{root.Param.Name}={root.Value:G17}"))}}}";
        return $"0_{{{Determinant.Body}}}{keys} × ∞_{{Inv(J)}}{keys} → ({string.Join(", ", _inversePayload)})";
    }

    private static Expression Rebind(LambdaExpression source, IReadOnlyList<ParameterExpression> target) =>
        new ParameterRebindVisitor(source.Parameters, target).Visit(source.Body)
        ?? throw new InvalidOperationException("Не удалось переназначить Jacobian singularity expression.");

    private static ParameterExpression[] CreateParameters(IReadOnlyList<ParameterExpression> source) =>
        source.Select(parameter => Expression.Parameter(parameter.Type, parameter.Name ?? "x")).ToArray();

    private sealed class ParameterRebindVisitor : ParameterMappingVisitorBase
    {
        public ParameterRebindVisitor(
            IReadOnlyList<ParameterExpression> source,
            IReadOnlyList<ParameterExpression> target)
            : base(source, target.Cast<Expression>().ToArray())
        {
        }
    }
}
