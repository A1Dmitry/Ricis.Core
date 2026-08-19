# Whole-project DRY audit

## Scope

Проверены 156 C# файлов в `Ricis.Core`, `Ricis.Console`, `Ricis.Finance` и `RegressionTests`, исключая `bin` и `obj`. Аудит разделяет текстовое сходство и семантическое дублирование: одинаковые строки сами по себе не являются основанием для рефакторинга.

## Confirmed duplication

| ID | Область | Доказательство | Решение |
|---|---|---|---|
| DRY-P01 | Production expression visitors | Однотипный single-parameter visitor с `VisitParameter` по reference identity и extension rebinding через `RicisSpecialExpressionRebinder` повторён в `RicisComplexFunction` и analytic/proof extensions | Выполнено: создан `ParameterRebindingVisitorBase`, от него унаследованы 7 single-parameter visitors |
| DRY-P02 | Production list parameter visitors | Jacobian, Matrix и Vector повторяли list-to-list mapping с одинаковым identity lookup и extension traversal | Выполнено: создан `ParameterMappingVisitorBase`; Jacobian/Matrix/Vector parameter and coordinate visitors унаследованы от него |
| DRY-T01 | Regression assertions | `Require` и `Expect<TException>` повторяются в suites | Частично выполнено через `RegressionAssertions`; оставшиеся suites мигрировать однородными группами |
| DRY-T02 | Regression exception assertion | `RequireArgumentException` был продублирован в `RicisAcademicProofSuite` и `RiemannHypothesisProofSuite` | Выполнено: обе suites используют `RegressionAssertions.Expect<ArgumentException>`; static catalog/harness contract не изменён |
| DRY-T03 | Numeric comparison assertion | `AssertClose` повторял finite-check и absolute-tolerance predicate в Classical/Modern comparison suites | Выполнено: общий predicate в `RegressionAssertions.AssertClose`; каждая suite сохраняет собственный tolerance и diagnostic message |
| DRY-ARCH-01 | Regression test lifecycle | Большинство suites имеют одинаковый `Tests` catalog shape, но harness использует static members | Исследовано: базовый класс не вводится механически, потому что static catalog не имеет общего instance lifecycle |
| DRY-C01 | Console sample output | Повторяются sample-point loops и formatting, но columns differ by expression/result types | Кандидат для typed renderer/helper, не объединять строковой конкатенацией |
| DRY-F01 | Finance validation | Повторяются non-empty identifier checks, но поля принадлежат разным aggregates и trust boundaries | Сначала проверить value object/contract abstraction; механическое объединение запрещено |

## Similar but intentionally distinct

`ArgumentNullException.ThrowIfNull`, `InvalidOperationException`, parser error messages, `Expression.Parameter` и `Console.WriteLine` повторяются часто, но в разных boundary contracts. Их нельзя централизовать только по совпадению текста.

`ParameterSubstitutionVisitor` в vector calculus использует dictionary substitution и специальную обработку `ZeroInfinityExpression`, `InfinityExpression` и deferred derivatives. Он не является тем же контрактом, что single-parameter rebinding, и остаётся отдельным.

Finance `ProviderPayment`, `Invoice`, `Settlement` и `Payout` имеют похожие lifecycle/identifier checks, но различаются aggregate invariants. Их можно объединять только через явно названные domain value objects или policies с тестами, а не через общий базовый aggregate.

## DRY-P01 implementation boundary

`ParameterRebindingVisitorBase` содержит только общий single-parameter contract: source parameter, arbitrary expression replacement, identity-based substitution и recursive RICIS extension rebinding. Производные private visitors сохраняют локальные имена и call sites, поэтому public API и harness contracts не меняются.

`R​​icisVectorCalculusExtensions` использует dictionary-to-expression substitution и custom extension cases. Он не наследует `ParameterMappingVisitorBase`, потому что его extension handling и root metadata reconstruction отличаются от обычного parameter mapping.

## Recommended order

1. **DRY-T01:** миграция оставшихся regression `Require`/`Assert` однородными группами.
2. **DRY-T02:** completed; продолжить проверку оставшихся специализированных assertion helpers.
3. **DRY-T03:** completed; проверить `AssertIdentity` и CLI typed rendering.
4. **DRY-C01:** typed console sample renderer после snapshot/smoke tests.
5. **DRY-F01:** domain validation value objects только после отдельного contract design.

Каждый шаг выполняется отдельным commit. Production refactoring допускается только при полном Core/Finance regression gate и сохранении Lean artifact gate.
