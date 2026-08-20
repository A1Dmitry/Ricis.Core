# Подробный trace кусочной поверхности RICIS

## Исправление результата

Предыдущий отчёт был неполным: он показывал входную лямбду и marker-события, но не показывал настоящий промежуточный путь. В текущем increment добавлен отдельный `ICollection<RicisPhaseTraceStep>` с полными expression-tree snapshots `before`/`after` и node-to-root routes.

## Фактический порядок преобразований

| № | Event | Before | After | Rule family |
|---:|---|---|---|---|
| 1 | `RICIS_PIECEWISE_X_STRIP` | `x >= 0` | `(x >= 0) AndAlso (x <= 5)` | `eq2` |
| 2 | `RICIS_PIECEWISE_Y_STRIP` | `y >= 1` | `(y >= 1) AndAlso (y <= 5)` | `g` |
| 3 | `RICIS_PIECEWISE_PARABOLA` | `y` | `y > ((x * x) / 5)` | `b` |
| 4 | `RICIS_PIECEWISE_DOMAIN` | x-strip | `x-strip AndAlso (y-strip AndAlso parabola-boundary)` | `SP2 / domain intersection` |
| 5 | `RICIS_PIECEWISE_VALUE` | `x * y` | `Convert((x * y), Nullable<double>)` | `A1 / value branch` |
| 6 | `RICIS_PIECEWISE_CONDITIONAL` | nullable value branch | `IIF(domain, Convert(x*y), null)` | `A4 / conditional branch` |

Каждый из шести шагов создаётся через `RicisPhaseTraceStep`, поэтому для него сохраняются `Before`, `After`, `BeforeNodeToRoot`, `AfterNodeToRoot`, `RuleFamily`, `Changed` и `WasSkipped=false`. Одновременно каждый шаг публикуется в `ILog` как typed `Trace` event с теми же before/after snapshots. Это уже не декларативная запись о наличии фазы, а журнал фактических промежуточных expression trees.

Полный canonical event order: `RICIS_PIECEWISE_START`, шесть перечисленных transformation events, `RICIS_PIECEWISE_COMPLETE`.

## QA

`PWS02` теперь требует шесть trace steps, `Changed=true` для каждого шага, непустые node-to-root маршруты до и после, корректный порядок typed events и присутствие преобразований в LaTeX. Дополнительно проверены значения `(2,2) -> 4` и `(4,3) -> null`.

`PWS01` проверяет область, границы и null outside. `PWS03` подтверждает, что `Build(null)` сохраняет legacy expression tree.

## Quality gate

| Проверка | Результат |
|---|---:|
| Build | PASS, 0 warnings / 0 errors |
| Core regressions | 398/398 PASS |
| Numerics UnitTests | 124/124 PASS |
| Finance regressions | 19/19 PASS |
| `git diff --check` | PASS |
| Deleted files | 0 |
