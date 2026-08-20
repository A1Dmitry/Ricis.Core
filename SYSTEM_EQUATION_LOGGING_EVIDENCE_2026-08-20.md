# System-equation solver logging evidence — 2026-08-20

Пользовательское требование: отдельный тест решения системы уравнений и полный пошаговый лог этого решения.

## Test input

Система задаётся expression trees:

```text
x + y = 5
x - y = 1
claim: x = 3
```

Solver не исполняет исходные уравнения; коэффициенты извлекаются структурно, determinant проверяется, затем строятся независимые expression trees для `x` и `y`.

## Direct solver test: SYS01

`SYS01: Binary system solver emits complete four-step journal` вызывает `equations.Prove(constraints, claim, proof, log)` и проверяет:

| Проверка | Требование |
|---|---|
| Derived expression | `x == 3` |
| Runtime regression | derived expression истинно при `(x,y)=(3,2)` |
| Event order | `RICIS_SYSTEM_START` → `RICIS_SYSTEM_COEFFICIENTS` → `RICIS_SYSTEM_ELIMINATION` → `RICIS_SYSTEM_COMPLETE` |
| Trace payload | elimination event содержит before/after expressions |
| Proof step 1 | линейная комбинация уравнений |
| Proof step 2 | выделение первой координаты |
| Proof step 3 | подстановка в первое уравнение |
| Proof step 4 | выделение второй координаты |
| Conclusion | итоговое `Следовательно, система выводит` присутствует |

## Tex integration test: PDF10

`PDF10: Binary system ILog reaches every solver step and LaTeX` вызывает `ProveDocumentWithLog` одним solver pass. Тест проверяет derived result, exact journal event order, LaTeX document header, typed event codes `RICIS_SYSTEM_COEFFICIENTS` и `RICIS_SYSTEM_ELIMINATION`, а также последний solver protocol step.

## Quality gate

| Gate | Result |
|---|---:|
| Solution build | PASS |
| Core regressions | 393/393 PASS |
| SYS01 | PASS |
| PDF10 | PASS |
| Numerics UnitTests | 124/124 PASS |
| Finance regressions | 19/19 PASS |
| `git diff --check` | PASS |
| Deleted files | 0 |
