# Улучшенная постановка задачи Римана для RICIS III

## Резюме

Текущий RICIS proof engine успешно решает **условную критическую линейную лемму** для формальной отражённой пары `sigma` и `mirrorSigma`. Он не доказывает полную гипотезу Римана, потому что пока не содержит формальной аналитической модели дзета-функции, универсального квантора по всем нетривиальным нулям и теорем, связывающих функциональное уравнение дзета-функции с каждой такой парой.

Официальная формулировка Clay Mathematics Institute утверждает, что все нетривиальные нули дзета-функции имеют действительную часть `1/2` [1]. Поэтому корректная RICIS-постановка должна разделять аналитический вход и алгебраическое ядро:

> Если для произвольного нетривиального нуля `s = sigma + i*t` доказаны принадлежность критической полосе, отражённость `1 - s`, сопряжённая симметрия и тождество типа, то RICIS ID-01–ID-06 выводят `sigma = 1/2`.

Это является необходимой **условной леммой**, но не полной RH без доказательства её универсальных аналитических предпосылок.

## Улучшенная формальная задача

Пусть `Zeta : Complex → Complex` — формально определённая дзета-функция или эквивалентная completed xi-функция. Вводятся следующие предикаты:

| Объект | Формальное содержание |
|---|---|
| `NontrivialZero(s)` | `Zeta(s) = 0`, при этом `s` не является полюсом и не является тривиальным отрицательным чётным нулём |
| `CriticalStrip(s)` | `0 < Re(s) ∧ Re(s) < 1` |
| `FunctionalReflection(s, mirror)` | `mirror = 1 − s` и нулевая принадлежность переносится по функциональному уравнению |
| `ConjugateSymmetry(s)` | `Zeta(conj(s)) = conj(Zeta(s))`, поэтому нулевая принадлежность сохраняется при сопряжении |
| `TypeIdentity(s, mirror)` | RICIS identity component даёт `Type(s) = Type(mirror)` |
| `CriticalLine(s)` | `Re(s) = 1/2` |

Улучшенный тезис задаётся так:

```text
∀ s : Complex,
  NontrivialZero(s) →
  CriticalStrip(s) ∧
  FunctionalReflection(s, 1 - s) ∧
  TypeIdentity(Re(s), Re(1 - s)) →
  Re(s) = 1/2
```

Алгебраическая часть, доступная текущему движку, материализуется в системе:

```text
sigma + mirrorSigma = 1
sigma - mirrorSigma = 0
claim: sigma = 1/2
```

Важно: две первые строки не должны считаться аналитически доказанными только потому, что метод `ProveTypeIdentityCriticalLine` умеет их сгенерировать. Для полной RH они должны быть результатом отдельно формализованных теорем о `Zeta` и её нулях.

## Разделение зон ответственности

Доказательный API теперь разделён через наследование. Абстрактный `RicisProofCase` владеет только жизненным циклом одного запуска, последовательностью `RicisProofMonitorEntry`, результатом и общим документным output. Он не знает, что такое дзета-функция, ноль или критическая линия.

`RiemannHypothesisProofCase : RicisProofCase` владеет только RH-specific частью: списком незамкнутых аналитических obligations, входной отражённой парой и делегированием ID-01–ID-06 существующему `ProveTypeIdentityCriticalLine`. Наследник возвращает `ConditionalTheorem`, а не `FiniteDerivation`, пока аналитические obligations имеют статус `OPEN`.

Таким образом, расширение комплексной дзета-модели может быть реализовано отдельным слоем и не загрязняет generic RICIS algebra engine. Это также исключает преждевременное превращение внешних математических предпосылок в скрытые аксиомы.

## Мониторинг proof attempt

Запускался документный сценарий:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj --configuration Release -- --riemann-proof-demo
```

Мониторинг показал следующую последовательность:

| Этап | Состояние | Наблюдаемый результат |
|---|---|---|
| ID-01 | PASS | Сохранена идентичность отражённой пары |
| ID-02 | PASS | Построено `sigma + mirrorSigma = 1` |
| ID-03 | PASS | Выведено `sigma = mirrorSigma` |
| ID-04 | PASS | Построено `sigma - mirrorSigma = 0` |
| ID-05 | PASS | Линейное исключение дало `2·sigma = 1` |
| ID-06 | PASS | Точное структурное следствие `sigma = 1/2` |
| Проверка производного выражения | PASS | `(0.5, 0.5) → True`, `(0.4, 0.6) → False` |

Regression runner подтвердил `RIEMANN01`, `RIEMANN02`, `RIEMANN03`; полный набор составил **300/300 passed**.

Точный текущий результат RICIS:

```text
(sigma, mirrorSigma) => (sigma == (1 / 2))
```

Дробь сохранена как структурное `Expression.Divide(1, 2)`, а не заменена на `double 0.5`.

## Что фактически доказано

Текущая система доказала корректное следствие линейной системы: при наличии нормативных RICIS premises `sigma + mirrorSigma = 1` и `sigma - mirrorSigma = 0` первая координата равна `1/2`. Это полезная и воспроизводимая алгебраическая лемма, которую можно использовать как внутренний шаг будущей формализации.

Она ещё не доказала утверждение `∀ s, NontrivialZero(s) → Re(s)=1/2`. Причина не в незавершённости ID-01–ID-06, а в отсутствии формального перехода от аналитического объекта `Zeta` к входной отражённой паре. Проверка нескольких численных нулей также не замыкает универсальный квантор; Clay отдельно отличает вычислительную проверку первых нулей от доказательства для всех нулей [1].

## Мониторинг недостающих функций

| Приоритет | Недостающая функция или теорема | Почему нужна | Минимальный RICIS/Lean контракт |
|---|---|---|---|
| P0 | `Complex` scalar/vector expression support | Текущий RH demo работает с двумя `double`, а не с комплексным `s` | `Re : Complex → Real`, `Im : Complex → Real`, conjugation и точная арифметика |
| P0 | `Zeta(s)` или completed `Xi(s)` | Без объекта функции нельзя формально определить ноль | `Zeta : Complex → Complex` с безопасным symbolic node |
| P0 | `NontrivialZero(s)` | Нужно отделить нетривиальные нули от тривиальных нулей и полюса | Predicate с domain/pole/trivial-zero exclusions |
| P0 | Analytic continuation/domain | Ряд дзета-функции сам по себе не задаёт функцию на всей критической полосе | Formal domain theorem and continuation bridge |
| P0 | Functional equation | Нужен доказанный переход `s ↦ 1−s` для zero membership | `ZetaZero(s) ↔ ZetaZero(1−s)` или equivalent `Xi` symmetry |
| P0 | Universal zero-to-pair bridge | Нужна связь каждого `s` с `sigma=Re(s)` и его reflected coordinate | `NontrivialZero(s) → ReflectedPair(Re(s), Re(1−s))` |
| P1 | Conjugation symmetry | Нужна полная симметрия нулей и согласование complex payload | `Zeta(conj s)=conj(Zeta s)` |
| P1 | Critical-strip theorem | Для RH нужно явно ограничить рассматриваемые нули полосой | `NontrivialZero(s) → 0 < Re(s) ∧ Re(s) < 1` |
| P1 | Universal quantifier in proof documents | Текущий `ProveDocument` выводит один supplied expression tree | Document theorem scope for `∀ s` and instantiated obligations |
| P1 | Exact complex Lean backend | Нужна компиляция не только линейной рациональной леммы, но и analytic premises | Lean definitions/theorems for complex zeta model, without placeholder `sorry` |
| P2 | Zero multiplicity and pole bookkeeping | Нужна корректная обработка повторных нулей и особой точки `s=1` | Certified zero/pole metadata |
| P2 | Equivalent criterion adapters | Можно выбрать более удобную форму: xi symmetry, Li criterion, Nyman–Beurling или explicit formula | Each adapter must state and prove equivalence, not merely rename the claim |

## Следующий инженерный шаг

Наиболее безопасный следующий шаг — не добавлять ещё одну линейную комбинацию, а реализовать отдельный typed symbolic layer для `Complex`, `Re`, `Im`, `Conjugate`, `Zeta`, `NontrivialZero` и `FunctionalEquation`. После этого нужно формализовать bridge theorem, который имеет вид `NontrivialZero(s) →` конкретные RICIS premises. Только этот bridge позволит передавать в `ProveTypeIdentityCriticalLine` не произвольную отражённую пару, а пару, полученную из реального аналитического нуля.

До появления этого bridge корректный статус результата должен оставаться **ConditionalTheorem: Critical-line identity under formal RICIS premises**, а не `FiniteDerivation` полной гипотезы Римана.

## References

[1]: https://www.claymath.org/millennium/riemann-hypothesis/ "Clay Mathematics Institute — Riemann Hypothesis"

[2]: https://mathworld.wolfram.com/RiemannHypothesis.html "Wolfram MathWorld — Riemann Hypothesis"
