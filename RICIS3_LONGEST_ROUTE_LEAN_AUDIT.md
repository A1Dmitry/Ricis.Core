# RICIS III — Longest Dependency Route Audit for Lean Expansion

**Status:** Extracted from `ricis3-map-2026-08-20-13-21-36.json`; formal Lean expansion in progress.

## Route-selection result

The map contains **146 nodes**, **177 explicit edges** and no dependency-reference gaps. The dependency graph is acyclic under the declared `dependencyIds`. Its maximal depth is **10 nodes / 9 dependency edges**. There are four equal-length routes. This audit chooses the first canonical route because its terminal node is the direct reduction of **spectral asymptotics** to the RICIS core, rather than a stability-metric sibling.

| Depth | Node ID | Map node | Target function | State |
|---:|---|---|---|---|
| 0 | `math-singularity` | Разрешение сингулярностей (деление на ноль) | `FormalizeFunction(РазрешениесингулярностейДелениенаноль)` | `resolved` |
| 1 | `real-catalog-1` | Гипотеза Ходжа | `ResolveSingularity(ГипотезаХоджа)` | `resolved` |
| 2 | `real-catalog-5` | Проблема делителей нуля в групповых кольцах | `ResolveSingularity(Проблемаделителейнулявгрупповыхкольцах)` | `resolved` |
| 3 | `real-catalog-6` | Сингулярность функции Вейерштрасса | `ResolveSingularity(СингулярностьфункцииВейерштрасса)` | `resolved` |
| 4 | `real-catalog-10` | Теорема об индексе Атьи—Зингера | `ResolveSingularity(ТеоремаобиндексеАтьиЗингера)` | `resolved` |
| 5 | `real-catalog-11` | Гипотеза Пуанкаре | `ResolveSingularity(ГипотезаПуанкаре)` | `resolved` |
| 6 | `real-catalog-23` | Теория Морса | `ResolveSingularity(ТеорияМорса)` | `resolved` |
| 7 | `real-catalog-25` | Теория узлов | `Formalize(Теорияузлов)` | `resolved` |
| 8 | `real-catalog-26` | Спектральная асимптотика | `Formalize(Спектральнаяасимптотика)` | `resolved` |
| 9 | `agent-offline-real-catalog-26-1785943252938-0` | Спектральная асимптотика: сведение к RICIS-ядру | `ReduceToRicisCore(Formalize(Спектральнаяасимптотика))` | `resolved` |

## Tied longest routes

The same 8-node prefix branches only at depth 8–9. The alternate terminal targets are `StabilityMetric(Formalize(Спектральнаяасимптотика))`, `ReduceToRicisCore(Formalize(Квазикристаллы))` and `StabilityMetric(Formalize(Квазикристаллы))`.

## Evidence boundary

The JSON state `resolved` records that the RICIS route was generated and its proof record contains the expected L1 → SP4 → SP2 → A6 → L1 transformation trace. It is not a claim that the document independently proves the external Hodge, Poincaré, Atiyah–Singer or spectral-asymptotics statements in their standard mathematical meanings.

The Lean expansion will prove the **route-composition theorem**: under explicit per-node reduction certificates, the composition preserves the declared RICIS route invariant from the singularity root to the spectral-asymptotics reduction endpoint. Each domain theorem remains an external premise unless separately formalized in Lean with its standard definitions and hypotheses.

## Extraction evidence

The extraction script found `cycles_detected=0`, `longest_node_count=10`, `longest_edge_count=9` and `maximum_route_variants=4`.
