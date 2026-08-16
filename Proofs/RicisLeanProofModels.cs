namespace Ricis.Core.Proofs;

/// <summary>
/// Selects a theorem row that the canonical RICIS Lean identity model can emit.
/// Rows are structural proof requests, not arbitrary text fragments.
/// </summary>
public enum RicisLeanProofRow
{
    /// <summary>Emits the ID-01 type-preservation theorem.</summary>
    Id01TypePreserved,

    /// <summary>Emits the ID-02 reflection-sum theorem.</summary>
    Id02ReflectionSum,

    /// <summary>Emits the ID-03 faithful-coordinate theorem.</summary>
    Id03SameCoordinate,

    /// <summary>Emits the ID-04 linear-pair theorem.</summary>
    Id04LinearPair,

    /// <summary>Emits the ID-05 doubled-coordinate theorem.</summary>
    Id05DoubledCoordinate,

    /// <summary>Emits the ID-06 exact-half theorem.</summary>
    Id06ExactHalf,

    /// <summary>Emits the reflected-coordinate exact-half theorem.</summary>
    Id06ReflectedExactHalf,

    /// <summary>Emits the negative ID-03 collapsed-type guard theorem.</summary>
    CollapsedTypeGuard,
}

/// <summary>
/// Supplies the structured identifiers and type names used by the canonical
/// Lean ID-01–ID-06 template. Values are validated as Lean identifiers before
/// source generation, so they cannot inject arbitrary Lean statements.
/// </summary>
public sealed class RicisLeanStructuredData
{
    /// <summary>
    /// Initializes structured Lean names for the reflected-coordinate model.
    /// </summary>
    /// <param name="namespaceName">The Lean namespace to generate.</param>
    /// <param name="typeTagName">The type parameter name of TypeIdentityAxioms.</param>
    /// <param name="typeOfName">The identity type-map field name.</param>
    /// <param name="reflectName">The reflection field name.</param>
    /// <param name="sigmaName">The primary coordinate name.</param>
    /// <param name="mirrorSigmaName">The reflected coordinate name used in comments and bridge rows.</param>
    public RicisLeanStructuredData(
        string namespaceName = "RicisIdentity",
        string typeTagName = "TypeTag",
        string typeOfName = "typeOf",
        string reflectName = "reflect",
        string sigmaName = "sigma",
        string mirrorSigmaName = "mirrorSigma")
    {
        NamespaceName = RequireIdentifier(namespaceName, nameof(namespaceName));
        TypeTagName = RequireIdentifier(typeTagName, nameof(typeTagName));
        TypeOfName = RequireIdentifier(typeOfName, nameof(typeOfName));
        ReflectName = RequireIdentifier(reflectName, nameof(reflectName));
        SigmaName = RequireIdentifier(sigmaName, nameof(sigmaName));
        MirrorSigmaName = RequireIdentifier(mirrorSigmaName, nameof(mirrorSigmaName));
    }

    /// <summary>Gets the generated Lean namespace.</summary>
    public string NamespaceName { get; }

    /// <summary>Gets the generated Lean type parameter name.</summary>
    public string TypeTagName { get; }

    /// <summary>Gets the generated identity type-map field name.</summary>
    public string TypeOfName { get; }

    /// <summary>Gets the generated reflection field name.</summary>
    public string ReflectName { get; }

    /// <summary>Gets the generated primary coordinate name.</summary>
    public string SigmaName { get; }

    /// <summary>Gets the generated reflected-coordinate name.</summary>
    public string MirrorSigmaName { get; }

    private static string RequireIdentifier(string value, string parameterName)
    {
        var reserved = value is "axiom" or "theorem" or "def" or "namespace" or "end" or "by" or
            "where" or "match" or "with" or "if" or "then" or "else" or "let" or "fun" or
            "structure" or "class" or "instance" or "import" or "open" or "section" or
            "variable" or "example" or "Type" or "Prop";
        if (string.IsNullOrWhiteSpace(value) ||
            reserved ||
            !(char.IsLetter(value[0]) || value[0] == '_') ||
            value.Any(character => !(char.IsLetterOrDigit(character) || character == '_' || character == '\'')))
        {
            throw new ArgumentException("Значение должно быть безопасным Lean identifier.", parameterName);
        }

        return value;
    }
}

/// <summary>
/// Indicates that a generic C# proof shape has no supported structured Lean bridge.
/// </summary>
public sealed class RicisUnsupportedLeanProofShapeException : NotSupportedException
{
    /// <summary>Creates an unsupported Lean proof shape exception.</summary>
    /// <param name="message">The diagnostic message.</param>
    public RicisUnsupportedLeanProofShapeException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Represents an immutable requested-row set for the Lean template. Required
/// dependencies are expanded in canonical theorem order before rendering.
/// </summary>
public sealed class RicisLeanRequestedRows
{
    /// <summary>
    /// Initializes and dependency-expands requested Lean proof rows.
    /// </summary>
    /// <param name="rows">The requested theorem rows.</param>
    public RicisLeanRequestedRows(IEnumerable<RicisLeanProofRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var requested = rows.ToHashSet();
        if (requested.Any(row => !Enum.IsDefined(row)))
        {
            throw new ArgumentException("Набор содержит неизвестную Lean proof row.", nameof(rows));
        }

        if (requested.Contains(RicisLeanProofRow.Id06ReflectedExactHalf))
        {
            requested.Add(RicisLeanProofRow.Id06ExactHalf);
        }

        if (requested.Contains(RicisLeanProofRow.Id06ExactHalf))
        {
            requested.Add(RicisLeanProofRow.Id05DoubledCoordinate);
        }

        if (requested.Contains(RicisLeanProofRow.Id05DoubledCoordinate))
        {
            requested.Add(RicisLeanProofRow.Id04LinearPair);
        }

        if (requested.Contains(RicisLeanProofRow.Id04LinearPair))
        {
            requested.Add(RicisLeanProofRow.Id02ReflectionSum);
            requested.Add(RicisLeanProofRow.Id03SameCoordinate);
        }

        if (requested.Contains(RicisLeanProofRow.Id02ReflectionSum))
        {
            requested.Add(RicisLeanProofRow.Id01TypePreserved);
        }

        if (requested.Contains(RicisLeanProofRow.Id03SameCoordinate))
        {
            requested.Add(RicisLeanProofRow.Id01TypePreserved);
        }

        Rows = Array.AsReadOnly(
            Enum.GetValues<RicisLeanProofRow>()
                .Where(requested.Contains)
                .ToArray());
    }

    /// <summary>Gets the dependency-expanded rows in canonical enum order.</summary>
    public IReadOnlyList<RicisLeanProofRow> Rows { get; }
}

/// <summary>
/// Contains generated Lean source and the exact structured rows used to create it.
/// </summary>
public sealed class RicisLeanDoc
{
    /// <summary>Creates a generated Lean document.</summary>
    /// <param name="source">The complete Lean source text.</param>
    /// <param name="rows">The expanded requested rows used by the template.</param>
    public RicisLeanDoc(string source, RicisLeanRequestedRows rows)
    {
        Source = string.IsNullOrWhiteSpace(source)
            ? throw new ArgumentException("Lean source не может быть пустым.", nameof(source))
            : source;
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>Gets the generated Lean source.</summary>
    public string Source { get; }

    /// <summary>Gets the expanded requested rows.</summary>
    public RicisLeanRequestedRows Rows { get; }

    /// <summary>Returns the generated Lean source.</summary>
    public override string ToString() => Source;
}
