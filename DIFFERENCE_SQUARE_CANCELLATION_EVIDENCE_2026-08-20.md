# Difference-of-squares cancellation evidence — 2026-08-20

## Требуемое выражение

В proof/document test введено точное expression tree:

```text
(25 - x^2) / (x - 5)
```

Сохранено формальное ограничение области `x != 5`. При `x=2` результат равен `-7`, при `x=6` результат равен `-11`.

## Логирование

Добавлен тест `CANCEL03: (25-x^2)/(x-5) is logged through cancellation`. Он вызывает generic `ProveDocumentWithLog` с LaTeX output и ищет в canonical journal `RICIS_PHASE_TRACE` для rule family `SP2`.

Тест проверяет, что запись содержит:

| Поле | Проверка |
|---|---|
| Before | исходная дробь с `/` и знаменателем `x - 5` |
| After | отличное от before структурное состояние после cancellation attempt |
| Rule family | `SP2: сокращение до сингулярностей` |
| Document | node-to-root маршрут и ограничение `x - 5` присутствуют в Tex |

## Дополнительный system cancellation event

Для binary system constraints добавлена запись `RICIS_SYSTEM_CONSTRAINT_NORMALIZATION`, фиксирующая before/after нормализацию ограничения с одинаковым множителем в числителе и знаменателе. Значение `log = null` не запускает дополнительное журналирование и сохраняет legacy путь.

Во время QA был обнаружен и устранён OOM-риск: вложенный typed logging полного constraint pipeline раздувал документ при повторном включении phase events. Production path оставляет один bounded canonical cancellation event с before/after, а обычная normalization выполняется без вложенного journal fan-out.

## Gate

| Проверка | Результат |
|---|---:|
| Build | PASS, 0 warnings / 0 errors |
| Core regressions | 395/395 PASS |
| `SYS02` binary numerator/denominator cancellation | PASS |
| `CANCEL03` exact `(25-x^2)/(x-5)` | PASS |
| Numerics | 124/124 PASS |
| Finance | 19/19 PASS |
| `git diff --check` | PASS |
| Deleted files | 0 |
