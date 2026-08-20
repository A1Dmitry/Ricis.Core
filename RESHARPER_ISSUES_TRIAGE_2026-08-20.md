# Triage `IssuesReport.xml` — ReSharper 2024.1.3

**Источник:** приложенный `IssuesReport.xml`, scope `Ricis.Core.sln`.

## Исполнительное резюме

В отчёте содержится **1276** IDE findings. Они не являются 1276 production defects. Значительную долю составляют spelling/markup/naming/style suggestions: 212 `MarkupTextTypo`, 208 `MarkupAttributeTypo`, 174 `StringLiteralTypo`, 98 `IdentifierTypo`, 76 `CommentTypo` и 72 `MergeIntoPattern`.

Report указывает два `CSharpErrors` в `Logging/RicisProofLogReportRenderer.cs`, связанные с generated JSON serializer context. Текущая проверка реального исходного дерева командой `dotnet build Ricis.Core.sln --configuration Release` завершилась **успешно: 0 warnings, 0 errors**. Следовательно, эти два finding нельзя считать актуальным compiler blocker. Они выглядят как отсутствие source-generator context в ReSharper analysis snapshot: `RicisProofLogJsonContext.Default` и parameterless constructor генерируются `System.Text.Json` source generator для partial context.

> Порядок принятия решений: фактический Release build и regression gates имеют приоритет над статическим IDE snapshot. Ни один source-generator contract не удаляется только ради устранения ложного IDE finding.

## Классификация

| Класс | Количество / примеры | Решение |
|---|---|---|
| Stale/false-positive compiler report | 2 `CSharpErrors` в `RicisProofLogReportRenderer.cs` | Закрыть как stale/IDE source-generator limitation после приложенного Release build evidence. Не менять production source. |
| Проверить до изменения proof endpoint | 2 `AssignNullToNotNullAttribute`, 4 `PossibleNullReferenceException`, 2 `PossibleMultipleEnumeration`, 2 always-true/false conditions, 3 equal-ternary conditions | Добавить focussed regression/mutation cases в Step 3 QA specification; чинить только после воспроизводимого failure/контракта. |
| Nullable/style redundancy | 15 constant null-coalescing, constant conditional access, nullable-contract branches в Finance | Низкий приоритет. Проверки HTTPS, provider fields и public guard contracts не переписывать механически. |
| Numeric audit candidates | 77 `CompareOfFloatsByEqualityOperator` | Разделить tests/demo/exact discrete assertions и production numeric comparison. Не заменять все equality на epsilon глобально: это может изменить RICIS structural/exact semantics. |
| Documentation/editor dictionary noise | 768 spelling/markup/comment/naming related findings | Отдельный documentation/style sprint; исключить domain terms `RICIS`, `Jacobian`, `Sinh`, `Tanh`, `SP4`, `A6` из ложных typo candidates через dictionary/inspection configuration. |

## Фактические проверки контекста

### `RicisProofLogReportRenderer`

Текущий исходник содержит source-generated `RicisProofLogJsonContext` with `[JsonSerializable]` and uses generated `Default`. Реальный .NET 8 Release build sees the generated partial members and compiles. The XML report likely analysed the partial class without source-generator output.

### `RicisAcademicProofExtensions`

Finding `ConditionIsAlwaysTrueOrFalse` at line 1723 marks `quotient is null`. The code first creates `quotient` only when `factor` is non-null and each selected product branch is an `Expression`. This is an IDE redundancy candidate, not evidence that the proof rule is unsound. Any simplification must retain the existing SP2 structural contract and get a direct regression test.

### `AlgebraicReductionVisitor`

The two `PossibleMultipleEnumeration` findings at method-call traversal are valid performance/clarity candidates: `node.Arguments.Select(Visit)` is compared through `SequenceEqual` and then passed to `Expression.Call`. Materialising once to an array can avoid a repeated visitor enumeration. This does not currently demonstrate an incorrect mathematical result, but it belongs in a focused DRY/performance task with expression-tree preservation tests.

### `ExpressionSimplifierVisitor`

The four possible-null findings arise from casts of `ConstantExpression.Value` inside Boolean conditional helpers. They require a dedicated type/nullable test before modification. The same visitor has short-circuit reductions, so a future QA test must check that rewrites never suppress an impure left operand. This is a **manual test obligation**, not a confirmed defect from this report.

### Finance nullable-contract branches

The Finance findings in `PaymentLaunch.cs` and `BepaidPaymentLaunchPort.cs` target `link is null`, `action is null` and `deepLink is not null` after APIs whose annotations already establish non-null values. Those guards are redundant under current annotations but occur in public/security-sensitive HTTPS validation. They are P3 cleanup candidates only; no route validation is to be weakened for code style.

## Additions to Step 3 QA test specification

| ID | Test obligation | Target | Expected result |
|---|---|---|---|
| IR-QA-01 | Source-generated JSON serializer contract | `RicisProofLogReportRenderer` | Release build and JSON typed-log regression prove generated `Default` is available; no hand-written serializer fallback. |
| IR-QA-02 | Single traversal of method-call arguments | `AlgebraicReductionVisitor.VisitMethodCall` | Each argument visits once; identical/non-identical tree reconstruction remains structural-equivalent. |
| IR-QA-03 | Null-safe Boolean constant handling | `ExpressionSimplifierVisitor` | `bool` constants reduce correctly; unsupported/null-valued constants are not cast unsafely. |
| IR-QA-04 | Short-circuit side-effect preservation | Logical simplification pipeline | A reduction may not turn `impure() && false` or `impure() || true` into a constant when that suppresses required left evaluation. |
| IR-QA-05 | Float comparison classification | Production numeric methods only | Tolerance is used only where numeric approximation is intended; structural/exact comparison remains exact. |
| IR-QA-06 | Finance guard retention | Payment launch / deep-link validation | Invalid/non-HTTPS provider URLs remain rejected after nullable cleanup. |
| IR-QA-07 | API proof snapshot source-generator/build gate | New `/api/proofs/v1/*` transport | DTO JSON serialization is source-generated/build-verified; malformed output remains a controlled failure. |

## Impact on C# Core-backed proof endpoint sprint

No finding in the XML invalidates the Step 1 business specification or Step 2 architecture contract. The following must be included in Step 3 test design before endpoint implementation:

1. source-generated JSON serialization validation for new proof DTOs;
2. malformed proof API response rejection in the frontend bridge;
3. anti-fallback proof-path tests;
4. state transition tests proving that `STRUCTURALLY_VERIFIED + REQUIRES_CORE_LEAN` remains `partial`;
5. short-circuit semantic tests for existing logical simplifiers, because proof endpoints must not expose a semantically altered derivation.

The remaining IDE style findings are a distinct cleanup backlog. Combining them with the proof endpoint transport would violate the project's atomic change and direct-test policy.

## Recommendation

Treat the XML as a useful **backlog discovery input**, not a release-blocking error report. Proceed with Step 3 QA specification for proof endpoints, adding `IR-QA-01` through `IR-QA-07`. Open a later small, isolated DRY/static-analysis cleanup sprint for materialised method-call arguments, proven redundant nullable guards, inspection dictionary configuration and selected numeric-comparison audit.
