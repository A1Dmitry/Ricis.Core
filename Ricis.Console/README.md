# Ricis.Console

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-14`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.


`Ricis.Console` — интерактивный и command-line вход в RICIS III. Он принимает математическую строку, строит ограниченное `Expression<Func<double, double>>`, передаёт его в `RicisPhasePipeline` и выводит исходную и производную RICIS-формы.

> Ввод **не компилируется как C#** и не может вызвать произвольный метод. Используется рекурсивный parser с фиксированной grammar и белым списком функций.

## Запуск

Из корня решения:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release
```

Одиночную lambda можно передать напрямую. Консоль обработает её один раз и завершится без interactive loop:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- \
  "x => ((x ^ 2) - 25) / (x - 5)"
```

Существующий явный режим `--expr` также поддерживается:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- \
  --expr "x => sin(x) / x"
```

Встроенные проверки parser-а и пакетный каталог запускаются следующими командами:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --self-test
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --all
```

Каталог `--all` содержит **58** входных выражений: исходный stress-набор и примеры новых parser-функций.

## Формат ввода

Вводится полная lambda либо тело выражения; во втором случае параметр по умолчанию — `x`. Параметром может быть любой корректный идентификатор, например `t`, `about` или `coordinate`.

```text
x => (x*x - 9) / (x - 3)
sin(x) / x
about => derivative(about ^ 3)
x => integral(x + 1, 5)
x => compoundInterest(100, 10, 2)
```

| Категория | Поддержка |
|---|---|
| Параметр | Один идентификатор: `x => ...`, `t => ...`, `about => ...`. |
| Операторы | `+`, `-`, `*`, `/`, `%`, `^`, круглые скобки. |
| Степень | `x ^ 2` или `pow(x, 2)`; оператор `**` не используется. |
| Константы | `pi`, `e`. |
| Аналитические функции | `sin`, `cos`, `tan`, `sinh`, `cosh`, `tanh`, `exp`, `log`, `log10`, `sqrt`, `abs`, `pow`. |
| Другие scalar-функции | `sign`, `clamp`, `mod`, `min`, `max`, `positivePart`, `negativePart`, `distance`. |
| RICIS-функции | `derivative`/`dxdt`, `sum`, `integral`. |
| Финансовая expression-форма | `compoundInterest(S, r, n)` или `interest(S, r, n)`. |
| Варианты имени | Регистр не важен; допустимы `sin(x)` и `Math.Sin(x)`. |

`about` является явным opt-in для авторских SEO metadata. После упрощения `about => ...` в `ToString()` производного дерева добавляется блок `[SEO AUTHOR]`; вычислимая семантика expression tree от этого не меняется. Подробности приведены в [`../AUTHOR_SEO_METADATA.md`](../AUTHOR_SEO_METADATA.md).

## Системы выражений

Точка с запятой разбирает строку как несколько lambda-выражений одной системы:

```text
x => x + 1; x => derivative(x ^ 3); x => integral(x, 5)
```

Каждый фрагмент обязан быть непустой lambda. Parser сохраняет их как отдельные expression tree; общий структурный контейнер системы — `ExpressionSystem<T>`, построенный поверх существующего `RicisVectorExpression<T>`. Разделитель `;` не означает скалярное сложение.

## Интерактивные команды

| Команда | Назначение |
|---|---|
| `help` | Показать актуальную grammar и примеры. |
| `examples` | Показать готовые выражения из каталога. |
| `selftest` | Запустить встроенные parser checks. |
| `all` | Запустить 58 выражений каталога. |
| `exit` / `quit` | Завершить программу. |

Дополнительные демонстрационные режимы доступны как CLI-флаги: `--proof-demo`, `--academic-proof-demo`, `--system-proof-demo`, `--riemann-proof-demo`, `--lean-doc-demo`, `--continuous-demo`, `--complex-demo`, `--interest-demo`, `--analytic-demo` и `--author-seo-demo`.

`--lean-doc-demo` печатает настоящий structured LeanDoc для canonical ID-01–ID-06 bridge:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --lean-doc-demo \
  > /tmp/ricis_generated.lean
cd ../FormalVerification/Lean
lake env lean /tmp/ricis_generated.lean
```

Generic `RicisProofDocumentFormat.Lean` для произвольного C# expression tree не создаёт комментарий, похожий на доказательство: он выполняет controlled rejection. Корректный Lean source строится через `RicisLeanTemplate` из `RicisLeanStructuredData` и `RicisLeanRequestedRows`.

## Семантика RICIS

Console передаёт полную lambda в `RicisPhasePipeline.Simplify`. Поэтому сохраняется нормативный приоритет RICIS III: тождество сущности, структурная алгебра и внутренние мосты применяются до классического fallback для не переопределённых операций. Сингулярные и proof-выражения остаются отложенными деревьями; они не исполняются parser-ом для получения символического результата.
