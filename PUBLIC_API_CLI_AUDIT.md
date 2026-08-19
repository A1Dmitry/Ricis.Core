# Public API и CLI audit

**Статус:** аудит начат.

## Обнаруженная CLI-поверхность

`Ricis.Console/Program.cs` зарегистрировал команды `--self-test`, `--all`, `--author-seo-demo`, `--derivative-demo`, `--structural-demo`, `--integral-demo`, `--sum-demo`, `--proof-demo`, `--academic-proof-demo`, `--system-proof-demo`, `--riemann-proof-demo`, `--lean-doc-demo`, `--lean-a6-demo`, `--jacobian-proof-demo`, `--jacobian-proof-latex`, `--jacobian-proof-lean`, `--continuous-demo`, `--complex-demo`, `--interest-demo`, `--analytic-demo` и `--expr`.

`--all` не запускает эти handlers. Он обрабатывает только входы `ExampleCatalog.All`, поэтому отдельный handler может компилироваться и работать, но не быть частью общего примера. Это проверяемый источник расхождения, на который указал пользователь.

## Уже представленные в ExampleCatalog возможности

Каталог содержит singularity/limit expressions L0–L50 и следующие новые операции: `derivative`, `integral`, `sum`, `compoundInterest`, `min`, `positivePart`, `negativePart`, `distance`.

## Обнаруженные пробелы CLI-примеров

| Public capability / demo | Отдельный handler | В `ExampleCatalog.All` |
|---|---:|---:|
| `max` | Да, через `--continuous-demo` | Нет |
| `clamp` | Да, через `--continuous-demo` | Нет |
| `sin/cos/tan/sinh/cosh/tanh/exp/log/log10/sqrt/pow` extension surface | Частично через `--analytic-demo` | Нет как отдельные catalog cases |
| complex API: `AsComplex`, `Conjugate`, `Add`, `Subtract`, `Multiply`, `SquaredNorm`, `Norm` | Да, через `--complex-demo` | Нет |
| proof operations: `Compose`, `At`, `Difference`, `Ratio`, `Product` | Да, через `--proof-demo` | Нет |
| vector calculus API | Не обнаружен отдельный CLI handler | Нет |
| exact evaluation / linear extraction / polar conversion / numeric constants | Не обнаружены отдельные CLI handlers | Нет |

## Обнаруженные public utility gaps

| Public type/method group | Текущее покрытие direct tests | Действие |
|---|---:|---|
| `ExactEvaluator.TryEvaluate` | Не найден отдельный suite/reference | Добавить rational positive, unsupported node, unknown parameter и division-by-zero cases |
| `CircleSectors.FromRadians`, `InSectors`, `ToString` | Не найден отдельный suite/reference | Добавить normalization, exact sector, invalid angle/sector и formatting cases |
| `PolarConverter.ExactSinCos`, `TryCollapseTrig`, `CollapseConstantTrig`, `ToPolarSector` | Не найден отдельный suite/reference | Добавить exact/non-exact sectors, trig poles, non-call passthrough и singular monolith cases |
| `NumericConstants.Register`, `ZeroOf`, `OneOf`, `TryOneOf`, `IsIntrinsicNumeric`, `IsZero`, `IsOne` | Косвенно используется simplifier; direct suite не найден | Добавить typed constants, intrinsic classification, registered/unregistered and identity predicates |
| `RicisType.Equals`, `IsCompatibleWith`, `Operate`, `CreateTuple` | Не найден отдельный suite/reference | Добавить equality/hash, scalar compatibility, division identity and canonical tuple cases |
| `LinearExtractor` | `internal`, не public API | Не включать в public API обязательный список; покрыть косвенно через solver tests при необходимости |

## Первичная классификация test coverage

Существующие suites уже покрывают analytic sugar, continuous sugar и complex API. Это не заменяет CLI smoke tests: CLI должен проверять, что public method подключён к воспроизводимому примеру и команда возвращает ожидаемый exit code.

Предварительно требуют отдельной проверки:

1. Все CLI handlers должны запускаться из Release build с exit code 0.
2. `--all` должен включать representative examples для `max`, `clamp`, analytic sugar, complex API и proof operations либо явно документировать, почему они являются отдельными demos.
3. Vector calculus, exact evaluator, linear extractor, polar converter и numeric constants должны быть сопоставлены с существующими regression IDs; при отсутствии теста требуется добавить его.
4. Для каждой CLI-команды, которая сейчас только печатает expression, нужен проверяемый output contract, а не тест только факта запуска.

## CLI smoke-аудит

На текущем Release build проверены все зарегистрированные команды: `--self-test`, `--all`, все математические/proof/Lean demos и новый `--public-api-demo`. Все команды завершились с `EXIT=0`. Полный вывод зафиксирован в `CLI_SMOKE_AUDIT.log`.

В `ExampleCatalog` добавлены representative cases L59–L66 для `max`, `clamp`, `cosh`, `tanh`, `log10`, `sign`, `mod` и `pow`. Отдельная команда `--public-api-demo` проверяет uncovered utility surface и печатает PASS/FAIL для `ExactEvaluator`, `CircleSectors`, `PolarConverter`, `NumericConstants` и `RicisType`.

## Проверка RicisType.GetHashCode и expression tree

После исправления `GetHashCode` выполнены отдельные проверки `API11–API16`. Они подтверждают structural comparison эквивалентных expression trees, `HashSet<RicisType>`, constructor/properties/static constants, null и unrelated-object equality, полную compatibility matrix, все ветви `Operate`, canonical tuple и `ToString`. Полный Core regression suite завершился результатом **344/344 PASS**.

Причина безопасности изменения: `RicisType.Equals` и `Equals(object)` сравнивают только `Signature`, тогда как прежний hash включал ещё `IsComposite`, нарушая обязательный invariant равенства и hash code. Новый hash использует только `StringComparer.Ordinal` для `Signature`. Поиск usages показал, что `RicisType.GetHashCode` не участвует в expression-tree node hashing или canonical tree traversal; `RicisType` используется как public type metadata и static expression constants.

Финальный quality gate после расширения suite: Console Release build — 0 warnings/0 errors; Finance regression — **12/12 PASS**; Lean manifest — **6/6 PASS**; `git diff --check` — PASS. NuGet publication не выполнялась.

## Обязательное правило для нового public API

После добавления или изменения любого `public` метода его direct regression tests обязательны в том же изменении. Тест должен иметь устойчивый ID, быть подключён к общему harness и проверять результат, структуру дерева или exception contract. Для user-facing метода также обязателен CLI/example smoke coverage. Полный нормативный контракт сохранён в [`PUBLIC_API_TEST_POLICY.md`](./PUBLIC_API_TEST_POLICY.md).

Изменение public API без собственного regression test считается незавершённым и не принимается в основную ветку.

## Правило аудита

Нельзя объявлять public method покрытым только потому, что компилируется файл. Для закрытия пробела нужны одновременно: regression test семантики, CLI/example coverage для пользовательского сценария (если метод предназначен для CLI) и quality-gate запуск соответствующей команды.
