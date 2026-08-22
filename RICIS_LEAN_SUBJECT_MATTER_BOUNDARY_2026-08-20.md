# Граница внешней предметной области RICIS/Lean: root-to-leaf

**Статус:** `Deferred` для внешних предметных утверждений. Конкретные engine- и route-артефакты могут быть `KernelChecked`, но это не переносит их статус на Hodge, Poincaré, внешнюю геометрию или аналитическую теорию сингулярностей.

**Дата обновления:** 22 августа 2026 года.
**Авторитетный реестр:** [`FormalVerification/Lean/Artifacts/manifest.json`](FormalVerification/Lean/Artifacts/manifest.json). На дату обновления штатный verifier подтверждает **9 зарегистрированных артефактов**. Предыдущая формулировка «8/8» была устаревшей.

> **Главное правило:** Lean-код доказывает только то typed statement, которое действительно записано в конкретном сохранённом source и принято Lean kernel. Название узла карты, комментарий, provenance link, C# regression test или успешная редукция RICIS.Core не создают доказательство внешней математической теоремы сами по себе.

## 1. Что уже установлено и чего это не означает

| Слой | Текущий статус | Что действительно установлено | Чего это не устанавливает |
|---|---|---|---|
| Локальный engine invariant | `KernelChecked` для конкретных artifacts | Конкретный rank-one payload, локальные алгебраические обязанности и заявленные инварианты engine route | Теорему в геометрии, топологии, анализе или спектральной теории |
| Route composition | `KernelChecked` для конкретной композиции | Последовательность выбранных десяти узлов и доказанные переходы, записанные в route source | Семантическую корректность внешних названий узлов |
| A6 payload bridge | `KernelChecked` в точной рациональной payload-модели | Структурное произведение `zeroPayload(key) * infinityPayload(key)` и его коммутативность | Универсальную теорему обо всех пределах, полюсах или аналитических сингулярностях |
| C# regression | `RegressionChecked` / `Tested` | Контракт кода, трасса RICIS.Core, наличие artifacts и ожидаемый вывод | Lean kernel theorem или предметную теорему |
| Author/provenance card | Документный metadata graph | Целостность заявленных ссылок и направления карточки | Научную истинность внешней публикации или связь RICIS с её предметом |
| Внешний узел карты | `Deferred` | Имя, позиция и зависимость в каталоге | Typed proposition, гипотезы и semantic bridge |

`LeanAgentScenarioEmulator` должен использовать эту таблицу как trust boundary: прочтение mandatory artifacts создаёт **Lean-informed context**, но не повышает статус внешнего тезиса автоматически.

## 2. Лестница статусов и правило повышения

Внешнее утверждение проходит стадии только последовательно.

| Стадия | Требование | Разрешённая формулировка |
|---|---|---|
| `Catalogued` | Есть название, описание или вершина карты | «Узел каталогизирован» |
| `Hypothesis` | Агент/человек сформулировал возможный тезис и путь | «Гипотеза» |
| `FormalizationRequired` | Зафиксированы typed proposition, домен и гипотезы | «Формализация требуется» |
| `BridgeSpecified` | Описано, как объект RICIS соответствует объекту предметной теории | «Semantic bridge задан, но не доказан» |
| `RegressionChecked` | Код и artifacts имеют C# regression evidence | «Контракт реализации проверен» |
| `KernelCheckedEngine` | Lean проверил только engine/route statement | «KernelChecked для engine artifact» |
| `KernelCheckedDomain` | Lean проверил domain theorem и доказанный semantic bridge | «KernelChecked для конкретной предметной теоремы» |
| `ProvedSubjectMatter` | Domain theorem, hypotheses и bridge объединены в явный финальный theorem | «Предметный тезис доказан в указанной формализации» |

Запрещённые переходы:

```text
Catalogued → ProvedSubjectMatter
RegressionChecked → KernelCheckedDomain
KernelCheckedEngine → ProvedSubjectMatter
```

Любой такой переход требует нового Lean source, явного theorem statement, полного набора предпосылок и kernel-проверки закреплённым toolchain.

## 3. Универсальный контракт semantic bridge

Прежде чем внешний узел может использовать результат RICIS, должны существовать и быть доказаны следующие компоненты.

| Компонент | Обязательное содержание | Почему нужен |
|---|---|---|
| `DomainModel` | Lean-типы объектов предметной области: пространства, функции, операторы, цепи, комплексы, спектры или многообразия | RICIS expression tree сам по себе не является объектом внешней теории |
| `Assumptions` | Все условия теоремы: конечность, гладкость, компактность, связность, регулярность, ориентируемость, непрерывность, невырожденность и т. п. | Нельзя скрывать условия в комментарии или profile |
| `RicisEncoding` | Точная функция/структура, кодирующая предметный объект в RICIS-представление | Должно быть определено, что именно означает payload, key, indexed zero или infinity |
| `Decode` | Обратная интерпретация либо доказанный способ извлечения предметного свойства из результата RICIS | Нужна, чтобы RICIS output имел математическое значение вне engine |
| `Soundness` | Теорема: преобразования RICIS сохраняют нужный предметный предикат | Редукция не должна менять смысл кодируемого объекта |
| `Completeness` | Где требуется, теорема: всякое релевантное предметное состояние представимо данным encoding | Иначе RICIS может охватывать только частный класс случаев |
| `Local theorem` | Отдельная Lean-теорема для каждого узла | Граф не может принимать placeholder-поля вместо доказательства |
| `Edge theorem` | Доказательство переноса гипотез и вывода между двумя узлами | Иначе route composition не имеет семантического значения |
| `Final composition` | Теорема, объединяющая local и edge theorems | Только здесь возможен предметный итог |

### 3.1. Минимальная схема в Lean-псевдокоде

```lean
structure DomainAssumptions where
  -- Explicit mathematical hypotheses.

structure RicisEncoding (X : DomainObject) where
  expression : RicisExpression
  -- The relation between X and expression is formal, not prose.

def DomainProperty (X : DomainObject) : Prop := ...

def RicisProperty (e : RicisExpression) : Prop := ...

theorem encoding_sound
    (h : DomainAssumptions X)
    (enc : RicisEncoding X) :
    RicisProperty enc.expression ↔ DomainProperty X := ...

theorem ricis_step_preserves_property
    (h : RicisProperty e) :
    RicisProperty (reduceRicis e) := ...

theorem domain_theorem_from_ricis
    (h : DomainAssumptions X)
    (enc : RicisEncoding X)
    (r : RicisProperty (reduceRicis enc.expression)) :
    DomainProperty X := ...
```

Это только **схема контракта**, а не готовое доказательство. Каждый placeholder должен быть заменён реальными Lean definitions и доказательствами.

## 4. Hodge: что именно необходимо формализовать

Название «Hodge» в карте не является формулировкой Hodge conjecture. До появления точного statement узел остаётся `Deferred`.

### 4.1. Возможный точный target

Для стандартной рациональной Hodge conjecture target должен быть сформулирован в терминах выбранной формализации, например:

```text
Для гладкого проективного комплексного многообразия X и p ≥ 0
каждый рациональный Hodge class в H^(2p)(X, ℚ) ∩ H^(p,p)(X)
принадлежит ℚ-линейной оболочке классов алгебраических циклов codimension p.
```

Эта запись описывает математический target, но не означает, что он уже формализован или доказан в проекте.

### 4.2. Обязательные типы и предпосылки

| Категория | Необходимая формализация |
|---|---|
| Геометрический объект | `X` как гладкое проективное комплексное многообразие; выбранный Lean-тип и его структура |
| Когомология | Группы/пространства `H^(k)(X, ℚ)` и комплексное расширение при необходимости |
| Hodge decomposition | Определение компоненты `H^(p,p)` и доказательства её свойств |
| Алгебраические циклы | Тип циклов нужной кодименсии, рациональная линейная оболочка и cycle class map |
| Target predicate | Предикат «является рациональным Hodge class» и предикат «порождается классами циклов» |
| Гипотезы | Гладкость, проективность, комплексная структура, конечномерность и все library-side условия |

### 4.3. Требуемый bridge с RICIS

Нужна точная интерпретация: какие RICIS expression/payload/key кодируют цикл, класс когомологии, оператор или отношение между ними. Затем требуется theorem вида:

```text
RicisInvariant(encode(X, alpha))
↔ IsRationalHodgeClass(X, alpha)
```

и отдельная theorem, связывающая успешную RICIS-редукцию с принадлежностью `alpha` образу/оболочке cycle class map. Без обеих частей `Hodge` остаётся только label map node.

### 4.4. Текущий статус

`Deferred`. В проекте нет указанных typed definitions, target theorem и semantic bridge. Наличие kernel-checked engine route не является доказательством Hodge conjecture.

## 5. Poincaré: что именно необходимо формализовать

Название «Poincaré» также не является готовой theorem statement. Для Poincaré conjecture в трёхмерном виде потенциальный target должен быть сформулирован примерно так:

```text
Каждое замкнутое, связное, просто связное трёхмерное многообразие
гомеоморфно 3-сфере.
```

### 5.1. Обязательная formalization matrix

| Элемент | Что требуется |
|---|---|
| Многообразие | Lean-тип топологического 3-manifold, Hausdorff/second-countable/local Euclidean условия и dimension=3 |
| Замкнутость | Точное определение compact without boundary либо выбранный эквивалент |
| Связность | Формальный предикат connected |
| Простая связность | Формальный фундаментальный group / null-homotopy contract |
| `S³` | Выбранная формальная модель 3-сферы |
| Заключение | Typed statement о homeomorphism между `M` и `S³` |
| Импортированный результат | Если используется существующая library theorem, необходимы точная версия и все её гипотезы |

### 5.2. Требуемый bridge с RICIS

Сам по себе маршрут RICIS не кодирует многообразие. Нужен encoding многообразия/триангуляции/потока/инварианта в expression tree, а затем soundness theorem, которая доказывает, что каждая RICIS-фаза сохраняет именно топологические свойства, необходимые для target.

Например, недостаточно показать, что payload равен нулю. Нужно доказать, что этот payload корректно представляет определённый топологический инвариант и что нулевое значение вместе с явными гипотезами действительно влечёт homeomorphism к `S³`.

### 5.3. Текущий статус

`Deferred`. Карта содержит Poincaré-labelled node, но текущая запись — проза без typed manifold statement, без representation theorem и без переноса условий через RICIS.

## 6. Внешние аналитические утверждения и сингулярности

RICIS.Core умеет структурно представлять ключи, indexed zero и infinity. Это не делает каждый аналитический вывод автоматически формальным.

### 6.1. Отдельные классы аналитических задач

| Класс | Минимальная typed proposition | Обязательные условия |
|---|---|---|
| Устранимая сингулярность | Существует расширение `g`, непрерывное/голоморфное в точке, и `g=f` вне точки | Домен, punctured neighborhood, тип функции, регулярность |
| Полюс | Существует порядок `m` и ненулевой regular factor | Точка, локальное кольцо/голоморфность, определение порядка полюса |
| Существенная сингулярность | Сингулярность не removable и не pole | Комплексный/реальный выбранный домен и точное определение класса |
| Предел | `Tendsto f (𝓝[≠] a) (𝓝 L)` либо эквивалент | Topological/filter definitions, domain restriction, one-/two-sided direction |
| Осцилляция `sin(1/x)` | Двусторонний предел не существует | Две явные последовательности или filter-level proof различающихся кластерных значений |
| Дифференцируемость | `DifferentiableAt`/derivative theorem | Область, норма, непрерывность/гладкость, необходимые library hypotheses |

### 6.2. Что должен доказывать bridge

Для каждой expression tree нужна интерпретация вида:

```text
EvalRicis(expression, x) = f(x)  на допустимом домене
```

и теоремы о том, что:

1. RICIS factorization/cancellation сохраняет функцию на punctured domain;
2. исключённые ключи не теряются;
3. `InfinityExpression` не отождествляется с обычным extended-real limit без отдельного определения;
4. индекс payload связан с математически определённой локальной величиной;
5. если требуется аналитический limit, он доказывается через filters/последовательности, а не выводится из строки отображения RICIS.

### 6.3. Примеры текущего корректного статуса

| RICIS output | Корректное утверждение сейчас | Нельзя утверждать без bridge |
|---|---|---|
| `∞₁ at {x=-2,x=2}` для `1/(x²−4)` | Engine сертифицировал два сингулярных ключа в своей модели | Полную theorem о двустороннем extended-real limit в каждой точке |
| `sin(∞₁ when x=0)` для `sin(1/x)` | Аргумент `1/x` структурно индексирован, outer `sin` сохранён | Что `sin(∞)` определён, равен числу или является классическим пределом |
| `(x²−25)/(x−5) → x+5` | Algebraic cancellation выполнена структурно | Что область определения автоматически расширена на `x=5` |

### 6.4. Текущий статус

Локальные engine contracts и C# regressions могут быть `RegressionChecked`, а отдельные Lean payload artifacts — `KernelChecked` в указанной модели. Полноценные аналитические theorems о функциях, пределах, голоморфности и классификации сингулярностей остаются `Deferred`, пока не появятся typed analytic definitions и soundness bridge.

## 7. Геометрические, индексные и спектральные утверждения

Для геометрии, Atiyah–Singer-labelled relation и Weyl/spectral-asymptotics node требуется отдельная формализация каждого объекта.

| Область | Необходимый target | Ключевые предпосылки | Что должен сделать RICIS bridge |
|---|---|---|---|
| Дифференциальная геометрия | Theorem о кривизне, форме, потоке или инварианте | Smooth manifold, atlas, metric/connection, compactness/orientation при необходимости | Доказать, какой объект кодируется expression и как фазы сохраняют геометрический предикат |
| Atiyah–Singer index | Точное equality аналитического и топологического индекса конкретного elliptic operator | Manifold, vector bundles, ellipticity, Fredholm structure, orientations | Связать RICIS payload с конкретным operator/index, а не с именем «index» |
| Morse theory | Theorem о critical points, indices и topology sublevel sets | Smooth function, nondegenerate critical points, compactness/boundary assumptions | Показать, что RICIS key/payload представляет критическую точку и сохраняет Morse index |
| Knot theory | Equality/invariance конкретного knot invariant | Knot embedding, isotopy, invariant definition | Доказать, что reduction соответствует допустимому преобразованию диаграммы/инварианта |
| Spectral asymptotics | Точное asymptotic statement о собственных значениях/trace | Self-adjointness, compact resolvent, ellipticity, dimension/measure data | Связать RICIS expression с оператором, спектром и асимптотическим предикатом |

Во всех строках словосочетание «RICIS core» описывает engine endpoint. Оно не является доказательством соответствующей геометрической или спектральной теоремы.

## 8. Route nodes и текущий пробел

Выбранный route:

```text
math-singularity
→ Hodge
→ group-ring zero divisors
→ Weierstrass singularity
→ Atiyah–Singer
→ Poincaré
→ Morse
→ knot theory
→ spectral asymptotics
→ ReduceToRicisCore(spectral asymptotics)
```

Для concrete engine interpretation source `LongestRouteConcreteEngineProof.lean` доказывает конкретный rank-one payload route. Он не содержит стандартные definitions всех перечисленных областей. Следовательно, этот route остаётся доказательством **структурной engine-композиции**, а не доказательством последовательности внешних теорем.

Для каждого узла требуются четыре отдельные deliverables:

1. `Domain.lean` с definitions и assumptions;
2. `NodeTheorem.lean` с local typed theorem;
3. `RicisBridge.lean` с `encode/decode`, soundness и нужной completeness;
4. `EdgeTheorem.lean` с переносом условий между соседними узлами.

Только после kernel-компиляции всех этих частей допустима сборка final root-to-leaf domain theorem.

## 9. Правила для агента и CachedSolutions

Агент обязан:

1. прочитать `manifest.json` и `LEAN_ARTIFACT_POLICY.md`;
2. определить статус каждого используемого artifact;
3. не выводить предметный смысл из node label или source URL;
4. помечать ответ `Hypothesis`, если typed proposition/bridge отсутствует;
5. использовать RICIS.Core для глубокого engine reduction и сохранять фазовую трассу;
6. выдавать `KernelChecked` только при наличии конкретного проверенного Lean source и точного theorem name;
7. выдавать `ProvedSubjectMatter` только после kernel-checked domain theorem и доказанного semantic bridge.

Для `CachedSolutions` это означает: подтверждённость записи относится к её тесту/контракту (`confirmed` для продукта), но не к внешнему предметному theorem claim, если такой claim не прошёл отдельную формальную лестницу.

## 10. Проверяемые acceptance criteria для будущего предметного узла

Новый внешний node может быть повышен с `Deferred` не раньше, чем CI проверяет:

```text
1. Domain.lean существует и экспортирует конечные typed definitions.
2. Target theorem использует именно эти types, а не string/metadata placeholders.
3. Все assumptions перечислены в theorem signature.
4. RicisBridge.lean содержит encode/decode и theorem soundness.
5. Local theorem не содержит sorry, sorryAx, admit или неразрешённые аксиомы.
6. Каждая edge theorem переносит конкретные hypotheses.
7. source внесён в manifest с provenance, theoremNames и knowledgeSource.
8. lake env lean <source> проходит закреплённым toolchain.
9. C# regression test проверяет source existence, theorem name и trace provenance.
10. Документация называет ровно тот theorem, который скомпилирован.
```

## References

[1]: [`FormalVerification/Lean/Artifacts/manifest.json`](FormalVerification/Lean/Artifacts/manifest.json) — авторитетный registry Lean-artifacts.
[2]: [`LEAN_ARTIFACT_POLICY.md`](LEAN_ARTIFACT_POLICY.md) — policy evidence, statuses и trust boundary.
[3]: [`LEAN_ARTIFACT_INVENTORY.md`](LEAN_ARTIFACT_INVENTORY.md) — различие собственных theorem sources и evidence artifacts.
[4]: [`RICIS3_LONGEST_ROUTE_FULL_PROOF_GAP_MATRIX.md`](RICIS3_LONGEST_ROUTE_FULL_PROOF_GAP_MATRIX.md) — concrete engine route и gap matrix внешних узлов.
[5]: [`FormalVerification/Lean/RicisIdentity/TypeIdentity.lean`](FormalVerification/Lean/RicisIdentity/TypeIdentity.lean) — conditional ID-01–ID-06 exact rational model.
[6]: [`FormalVerification/Lean/Generated/ComplexSingularityA6.lean`](FormalVerification/Lean/Generated/ComplexSingularityA6.lean) — structural A6 payload bridge.
[7]: [`RICIS_III_ACADEMIC_LEAN_REPORT_2026-08-21.md`](RICIS_III_ACADEMIC_LEAN_REPORT_2026-08-21.md) — distinction between academic provenance and kernel theorem stack.
