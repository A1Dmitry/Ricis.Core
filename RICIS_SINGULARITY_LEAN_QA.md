# QA: сложная RICIS singularity → A6 LeanDoc

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.


## Цель

Тест проверяет сложную двухпеременную singularity, для которой классическая double-оценка на certified boundary не даёт корректного конечного результата, тогда как RICIS сохраняет deferred payload и применяет геометрический A6 bridge.

## Исходная структурная система

Сертифицированное состояние Jacobian задаётся как:

```text
0_det(J) at {x=1, y=2} × ∞_Inv(J) at {x=1, y=2}
```

Inverse payload содержит две сложные deferred-компоненты:

```text
G₁(x,y) = ((x + 1) · (y + 2)) / (y − 2)
G₂(x,y) = (x² + 3) / (y − 2)
```

В точке `(x,y)=(1,2)` знаменатель обеих компонент равен нулю. Классическая оценка `G₁(1,2)` и `G₂(1,2)` становится `NaN` или `Infinity`; она не является источником RICIS-результата.

## Нормативный RICIS result

`RicisJacobianSingularityExpression<double>` принимает уже сертифицированный structural zero determinant и сохраняет оба ключа. `ApplyA6GeometricBridge()` выполняет bridge покомпонентно:

```text
0_det(J) · ∞_G₁ → det(J) · G₁
0_det(J) · ∞_G₂ → det(J) · G₂
```

Результат остаётся структурным произведением expression trees. Вызова делегата для получения singular value внутри RICIS proof нет.

## LeanTemplate input

QA test запрашивает:

```csharp
new RicisLeanRequestedRows(
    [RicisLeanProofRow.A6IndexedZeroInfinityBridge])
```

и передаёт его в:

```csharp
RicisLeanTemplate.Render(
    new RicisLeanStructuredData(),
    requestedRows)
```

Generated `LeanDoc` содержит только typed structured A6 model:

```lean
structure A6Payloads where
  zeroPayload : ℚ → ℚ
  infinityPayload : ℚ → ℚ

def a6BridgeAt (A : A6Payloads) (key : ℚ) : ℚ :=
  A.zeroPayload key * A.infinityPayload key

theorem a6_indexed_zero_infinity_bridge ... := by
  rfl

theorem a6_payload_product_commutative ... := by
  unfold a6BridgeAt
  ring
```

Здесь нет сериализации C# `ToString()`, arbitrary theorem text, `sorry` или `sorryAx`. Lean theorem формализует именно RICIS A6 payload bridge, а не классическую попытку вычислить `0 · ∞`.

## Сверка

| Слой | Ожидаемый результат | Фактический результат |
|---|---|---|
| Classical double at `(1,2)` | `NaN`/`Infinity`, finite comparison невозможен | подтверждено тестом `SQA02` |
| RICIS determinant state | structural zero with keys `{x=1,y=2}` | подтверждено `SQA01` |
| RICIS A6 payload count | две componentwise products | подтверждено `SQA01` |
| Lean rows | `A6IndexedZeroInfinityBridge` | generated `a6_indexed_zero_infinity_bridge` и `a6_payload_product_commutative` |
| Lean compilation | без ошибок | `lake env lean` завершился успешно |
| Forbidden placeholders | нет `sorry`/`sorryAx` | scan завершился успешно |

## Граница утверждения

Этот тест доказывает корректность **структурного A6 bridge**, заданного `RicisJacobianSingularityExpression` и typed Lean model. Он не заявляет, что arbitrary multivariate C# expression tree автоматически переведён в Lean. Перевод конкретного payload в `A6Payloads` является явно определённым предметным bridge; классическая `NaN`/`Infinity`-оценка не используется как proof premise.

## Воспроизведение

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj \
  --configuration Release -- --lean-a6-demo \
  > FormalVerification/Lean/Generated/ComplexSingularityA6.lean

cd FormalVerification/Lean
lake env lean Generated/ComplexSingularityA6.lean
```

Regression test: `SQA01`–`SQA03` в `RegressionTests/RicisLeanSingularitySuite.cs`.
