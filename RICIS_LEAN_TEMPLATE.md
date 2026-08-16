# RICIS LeanTemplate и LeanDoc

## Главный контракт

В RICIS корректный Lean является первичным проверяемым документом. Его нельзя строить конкатенацией `ToString()` expression tree или оборачивать academic trace в комментарии.

Нормативная форма генерации:

```text
LeanTemplate(StructuredData, RequestedRows) => LeanDoc
```

В C# это выражается так:

```csharp
var data = new RicisLeanStructuredData();
var requestedRows = new RicisLeanRequestedRows(
    [RicisLeanProofRow.Id06ReflectedExactHalf]);

RicisLeanDoc leanDoc = RicisLeanTemplate.Render(data, requestedRows);
string leanSource = leanDoc.Source;
```

`RicisLeanDoc` содержит полный Lean source и dependency-expanded `RequestedRows`. `StructuredData` содержит только валидированные Lean identifiers. Текстовые proof-фрагменты, произвольные theorem statements и C# expression `ToString()` в Lean source не интерполируются.

## StructuredData

`RicisLeanStructuredData` описывает имена, необходимые canonical ID-01–ID-06 bridge:

| Поле | Назначение |
|---|---|
| `NamespaceName` | Lean namespace generated document. |
| `TypeTagName` | Параметр типа `TypeIdentityAxioms`. |
| `TypeOfName` | Отображение `ℚ → TypeTag`. |
| `ReflectName` | Отражение `ℚ → ℚ`. |
| `SigmaName` | Основная координата. |
| `MirrorSigmaName` | Имя reflected coordinate в структурном описании. |

Все значения проверяются как Lean identifiers. Строка с переводом строки, `axiom`, `theorem`, `by` или другим injected statement не может попасть в generated source через этот API.

## RequestedRows

`RicisLeanRequestedRows` принимает только enum `RicisLeanProofRow` и автоматически раскрывает необходимые зависимости:

```text
ID-06 reflected half
  → ID-06 exact half
  → ID-05 doubled coordinate
  → ID-04 linear pair
  → ID-02 reflection sum + ID-03 same coordinate
  → ID-01 type preserved
```

Rows являются структурными запросами, а не строковыми инструкциями. Их итоговый порядок канонический и детерминированный.

## Поддержанный bridge

В текущей версии реально поддерживается canonical reflected-pair model из `FormalVerification/Lean/RicisIdentity/TypeIdentity.lean`:

- exact domain `ℚ`;
- explicit `TypeIdentityAxioms` fields for ID-01–ID-03;
- ID-04 linear pair;
- ID-05 `2 * sigma = 1`;
- ID-06 exact `sigma = 1 / 2`;
- reflected exact-half theorem;
- negative collapsed-type guard;
- no `sorry` and no `sorryAx` in generated source.

`RicisLeanTemplate` генерирует theorem source по этому known structured shape. Он не утверждает, что любой произвольный C# expression tree, parser input, vector system или Навье—Стокс-сценарий автоматически переводим в Lean.

## Controlled rejection

Вызов generic API:

```csharp
conditions.ProveDocument(
    constraints,
    claim,
    profile,
    RicisProofDocumentFormat.Lean,
    document);
```

для неподдержанного C# expression shape завершается `RicisUnsupportedLeanProofShapeException`. Это намеренная защита от ложного Lean-документа. Для supported structured bridge следует использовать `RicisLeanTemplate.Render`.

## Compiler acceptance

Корректность `LeanDoc` подтверждается не строковым сравнением, а Lean compiler:

```bash
export PATH="$HOME/.elan/bin:$PATH"
dotnet run --project Ricis.Console/Ricis.Console.csproj \
  --configuration Release -- --lean-doc-demo \
  > /tmp/ricis_generated.lean

cd FormalVerification/Lean
lake env lean /tmp/ricis_generated.lean
```

Успешный процесс должен завершиться без Lean errors. Дополнительно generated source проверяется на отсутствие `sorry` и `sorryAx`.

## Частные форматы

`Academic`, `Log` и `Json` остаются частными представлениями RICIS proof model. Они полезны для человека, диагностики и машинного обмена, но не заменяют Lean compiler. `Func<string,string>` применяется только к тексту частного документа; он не может изменять Lean structured data, requested rows, theorem validity или derived expression.
