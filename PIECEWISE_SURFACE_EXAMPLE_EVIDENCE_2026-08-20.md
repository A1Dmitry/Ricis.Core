# Piecewise nullable surface example — evidence

## Example

Создан production example `RicisPiecewiseSurfaceExample`:

```text
(x, y) =>
    (0 <= x <= 5) &&
    (1 <= y <= 5) &&
    (y > x^2 / 5)
        ? (double?)(x * y)
        : null
```

Expression tree строится deferred-образом; factory не компилирует и не исполняет lambda. Результат имеет тип `Expression<Func<double,double,double?>>`.

## Tests

| Test | Проверка | Result |
|---|---|---|
| PWS01 | `(2,2)` возвращает `4`; `(0,1)` включена; `(6,2)`, `(2,0.5)`, `(4,3)` возвращают `null` | PASS |
| PWS02 | ordered typed events, before/after domain, product/null branch и LaTeX rendering | PASS |
| PWS03 | `Build(null)` строит то же expression tree, что и legacy `Build()` | PASS |

Typed journal codes: `RICIS_PIECEWISE_START`, `RICIS_PIECEWISE_DOMAIN`, `RICIS_PIECEWISE_VALUE_BRANCH`, `RICIS_PIECEWISE_NULL_BRANCH`, `RICIS_PIECEWISE_COMPLETE`.

LaTeX audit renderer теперь показывает `EventCode` вместе с message и корректно экранирует `_` и `^`, поэтому report пригоден для пошагового объяснения области и обеих ветвей.

## Quality gate

| Gate | Result |
|---|---:|
| Solution build | PASS, 0 warnings / 0 errors |
| Core regressions | 398/398 PASS |
| Numerics UnitTests | pending final gate |
| Finance regressions | pending final gate |
| Deleted files | 0 |
