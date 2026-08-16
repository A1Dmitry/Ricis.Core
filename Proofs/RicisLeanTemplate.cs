using System.Text;

namespace Ricis.Core.Proofs;

/// <summary>
/// Generates compilable Lean source from structured RICIS data and requested
/// theorem rows. The renderer never interpolates arbitrary proof text or C#
/// expression ToString output into Lean code.
/// </summary>
public static class RicisLeanTemplate
{
    /// <summary>
    /// Builds a Lean document from structured names and requested theorem rows.
    /// Dependencies are expanded by <see cref="RicisLeanRequestedRows"/> before
    /// this method emits the canonical ID-01–ID-06 proof shape.
    /// </summary>
    /// <param name="data">Validated structured Lean identifiers.</param>
    /// <param name="requestedRows">Requested theorem rows and their dependencies.</param>
    /// <returns>A typed Lean document containing compilable source.</returns>
    public static RicisLeanDoc Render(
        RicisLeanStructuredData data,
        RicisLeanRequestedRows requestedRows)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(requestedRows);
        if (requestedRows.Rows.Count == 0)
        {
            throw new ArgumentException("LeanTemplate требует хотя бы одну requested proof row.", nameof(requestedRows));
        }

        var builder = new StringBuilder();
        AppendHeader(builder, data);
        AppendAxioms(builder, data);
        foreach (var row in requestedRows.Rows)
        {
            AppendRow(builder, data, row);
        }

        builder.Append("end ").AppendLine(data.NamespaceName);
        return new RicisLeanDoc(builder.ToString(), requestedRows);
    }

    private static void AppendHeader(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("import Mathlib");
        builder.AppendLine();
        builder.Append("namespace ").AppendLine(data.NamespaceName);
        builder.AppendLine();
        builder.AppendLine("/-- Generated from structured RICIS proof rows; exact rational domain. -/");
        builder.Append("structure TypeIdentityAxioms (").Append(data.TypeTagName).AppendLine(" : Type) where");
        builder.Append("  ").Append(data.TypeOfName).Append(" : ℚ → ").AppendLine(data.TypeTagName);
        builder.Append("  ").Append(data.ReflectName).Append(" : ℚ → ℚ");
        builder.AppendLine();
        builder.Append("  reflectionCoordinate : ∀ sigma, ").Append(data.ReflectName).Append(" sigma = 1 - sigma");
        builder.AppendLine();
        builder.Append("  identityPreservesType : ∀ sigma, ").Append(data.TypeOfName).Append(" sigma = ")
            .Append(data.TypeOfName).Append(" (").Append(data.ReflectName).Append(" sigma)");
        builder.AppendLine();
        builder.Append("  typeCoordinateFaithful : Function.Injective ").Append(data.TypeOfName);
        builder.AppendLine();
        builder.AppendLine();
    }

    private static void AppendAxioms(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- ID-01: reflection preserves the identity type. -/");
        builder.Append("theorem id01_type_preserved {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    A.").Append(data.TypeOfName).Append(" ").Append(data.SigmaName).Append(" = A.")
            .Append(data.TypeOfName).Append(" (A.").Append(data.ReflectName).Append(" ").Append(data.SigmaName).AppendLine(") :=");
        builder.Append("  A.identityPreservesType ").Append(data.SigmaName).AppendLine();
        builder.AppendLine();
    }

    private static void AppendRow(
        StringBuilder builder,
        RicisLeanStructuredData data,
        RicisLeanProofRow row)
    {
        switch (row)
        {
            case RicisLeanProofRow.Id01TypePreserved:
                return;
            case RicisLeanProofRow.Id02ReflectionSum:
                AppendId02(builder, data);
                return;
            case RicisLeanProofRow.Id03SameCoordinate:
                AppendId03(builder, data);
                return;
            case RicisLeanProofRow.Id04LinearPair:
                AppendId04(builder, data);
                return;
            case RicisLeanProofRow.Id05DoubledCoordinate:
                AppendId05(builder, data);
                return;
            case RicisLeanProofRow.Id06ExactHalf:
                AppendId06(builder, data);
                return;
            case RicisLeanProofRow.Id06ReflectedExactHalf:
                AppendReflectedHalf(builder, data);
                return;
            case RicisLeanProofRow.CollapsedTypeGuard:
                AppendCollapsedGuard(builder);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(row), row, "Неизвестная Lean proof row.");
        }
    }

    private static void AppendId02(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- ID-02: reflected coordinates sum to one. -/");
        builder.Append("theorem id02_reflection_sum {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    ").Append(data.SigmaName).Append(" + A.").Append(data.ReflectName).Append(" ").Append(data.SigmaName).AppendLine(" = 1 := by");
        builder.Append("  rw [A.reflectionCoordinate ").Append(data.SigmaName).AppendLine("]");
        builder.AppendLine("  ring");
        builder.AppendLine();
    }

    private static void AppendId03(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- ID-03: faithful type identifies the coordinate. -/");
        builder.Append("theorem id03_same_coordinate {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    ").Append(data.SigmaName).Append(" = A.").Append(data.ReflectName).Append(" ").Append(data.SigmaName).AppendLine(" :=");
        builder.Append("  A.typeCoordinateFaithful (id01_type_preserved A ").Append(data.SigmaName).AppendLine(")");
        builder.AppendLine();
    }

    private static void AppendId04(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- ID-04: the named identity rules produce the linear pair. -/");
        builder.Append("theorem id04_linear_pair {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    ").Append(data.SigmaName).Append(" + A.").Append(data.ReflectName).Append(" ").Append(data.SigmaName)
            .Append(" = 1 ∧ ").Append(data.SigmaName).Append(" - A.").Append(data.ReflectName).Append(" ").Append(data.SigmaName).AppendLine(" = 0 := by");
        builder.AppendLine("  constructor");
        builder.Append("  · exact id02_reflection_sum A ").Append(data.SigmaName).AppendLine();
        builder.Append("  · have h := id03_same_coordinate A ").Append(data.SigmaName).AppendLine();
        builder.AppendLine("    linarith");
        builder.AppendLine();
    }

    private static void AppendId05(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- ID-05: eliminate the reflected coordinate. -/");
        builder.Append("theorem id05_doubled_coordinate {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    2 * ").Append(data.SigmaName).AppendLine(" = 1 := by");
        builder.Append("  have h := id04_linear_pair A ").Append(data.SigmaName).AppendLine();
        builder.AppendLine("  linarith");
        builder.AppendLine();
    }

    private static void AppendId06(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- ID-06: exact critical coordinate. -/");
        builder.Append("theorem id06_exact_half {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    ").Append(data.SigmaName).AppendLine(" = 1 / 2 := by");
        builder.Append("  have h := id05_doubled_coordinate A ").Append(data.SigmaName).AppendLine();
        builder.AppendLine("  linarith");
        builder.AppendLine();
    }

    private static void AppendReflectedHalf(StringBuilder builder, RicisLeanStructuredData data)
    {
        builder.AppendLine("/-- The reflected coordinate obtains the same exact rational result. -/");
        builder.Append("theorem id06_reflected_exact_half {T : Type} (A : TypeIdentityAxioms T) (")
            .Append(data.SigmaName).AppendLine(" : ℚ) :");
        builder.Append("    A.").Append(data.ReflectName).Append(" ").Append(data.SigmaName).AppendLine(" = 1 / 2 := by");
        builder.Append("  rw [A.reflectionCoordinate ").Append(data.SigmaName).AppendLine("]");
        builder.Append("  have h := id06_exact_half A ").Append(data.SigmaName).AppendLine();
        builder.AppendLine("  linarith");
        builder.AppendLine();
    }

    private static void AppendCollapsedGuard(StringBuilder builder)
    {
        builder.AppendLine("/-- A collapsed type map cannot satisfy coordinate faithfulness. -/");
        builder.AppendLine("def collapsedType : ℚ → Unit := fun _ => ()");
        builder.AppendLine();
        builder.AppendLine("theorem collapsed_type_violates_id03 : ¬ Function.Injective collapsedType := by");
        builder.AppendLine("  intro faithful");
        builder.AppendLine("  have zeroEqualsOne : (0 : ℚ) = 1 := faithful rfl");
        builder.AppendLine("  norm_num at zeroEqualsOne");
        builder.AppendLine();
    }
}
