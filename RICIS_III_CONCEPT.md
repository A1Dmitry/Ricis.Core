# RICIS III — каноническая концепция и протокол восстановления контекста

**Назначение файла:** этот документ является компактным каноническим описанием внутренней семантики RICIS III для разработчиков, proof-проектов и последующего восстановления контекста. Он не заменяет исходный код, регрессионные тесты или полный нормативный документ v7.7, а связывает их в одну рабочую карту.

**Рабочее правило:** RICIS III имеет абсолютный приоритет над классической арифметикой во всех явно определённых случаях. То, что RICIS III не переопределяет, наследует классическую семантику как fallback.

> RICIS выражает сущность через отложенное выражение, его тип, его индексы и рекурсивный payload. Численное значение не уничтожает структуру сущности.

## 1. Абсолютные основания

### L0 — абсолютная непрерывность

Ни один уровень рекурсии, включая раскрытие монолита, индексированный ноль `0_F`, индексированную бесконечность `∞_F` и фрактальное раскрытие, не может потерять идентичность исходной сущности. Операция, которая уничтожает expression payload, индекс или тип без нормативного моста, недействительна.

L0 не является классическим пределом и не означает числовую непрерывность. Это внутренний инвариант сохранения сущности между уровнями RICIS.

### L1 — тождество сущности

```text
X = X
F/F = 1       только для структурно одной и той же сущности F
T(X)          является частью идентичности X
```

Одинаковые отображаемые строки недостаточны: сравнение должно быть структурным и учитывать параметры, тип, корни, индексы и payload. Разные функции с одинаковыми именами не становятся одной сущностью.

L1 имеет приоритет над всеми последующими фазами. Если структурная идентичность установлена, `F/F` сокращается до единицы до вычисления специальных функций, нулей, бесконечностей или классического fallback.

## 2. Safety Protocols

### SP1 — locality / no total amnesia

При возникновении `0/0` нельзя заменять всё выражение единицей. Единица появляется только для совпавших нулевых факторов. Несокращённый хвост сохраняется:

```text
(F·G)/F → G
(x−5)(x+5)/(x−5) → x+5
```

### SP2 — reduction priority / clean first

Структурная алгебра и сокращение одинаковых факторов выполняются до сингулярных мостов. Нельзя создавать ложный индексированный ноль там, где выражение ещё сокращается:

```text
(x²−25)/(x−5)
→ (x−5)(x+5)/(x−5)
→ x+5
```

SP2 включает факторизацию, ассоциативное сокращение, очистку вложенных дробей, сокращение факториалов и сохранение несократимых дробей.

### SP3 — index law / weight of zero

Если два нулевых фактора не совпадают структурно, они не являются одним скалярным нулём:

```text
0_F / 0_G → F/G
```

Индекс определяет вес нуля. Принудительное превращение `0_F` и `0_G` в обычные `0` запрещено.

### SP4 — semantic priority / index by expression

Сингулярность индексируется исходным выражением, а не только его числовым значением в ключе:

```text
E(x) at x=a
→ 0_{E(x)|x=a}
```

Например, для `E(x)=x²−4` при `x=2` индексом остаётся выражение `E`, а не схлопнувшийся результат `4−4`. Это сохраняет факторизацию, root set, path information и последующее RICIS-выведение.

## 3. Нормативные типы

### Deferred expression

RICIS-выражение — это LINQ Expression Tree или специальный RICIS-узел. В proof-режиме оно не компилируется и не вызывается. Получение числа разрешено только в отдельном конечном сравнительном тесте, когда классическая семантика определена.

### Indexed zero

```text
0_F
```

Это не обычный `0`, а нулевое состояние с payload `F`, типом сущности и, при необходимости, сертифицированными ключами сингулярности.

### Indexed infinity

```text
∞_F
```

Это не обычный `Infinity`, а бесконечное состояние, индексированное сущностью `F`. `KeyedInfinityExpression` дополнительно сохраняет набор ветвей и корней.

### Monolith

Монолит — замкнутый RICIS-объект, для которого операции не имеют права самовольно уничтожить identity, type или payload. Иерархия:

| Порядок | Смысл |
|---:|---|
| 0 | атомарная сущность, точка, `F`, `0_F`, `∞_F` |
| 1 | замкнутая линия из объектов порядка 0 |
| 2 | взаимосвязанная плоскость из объектов порядков 0–1 |
| 3 | объёмная саморганизующаяся система с направлением и внутренними связями |

Существующий код не должен удаляться из-за отсутствия прямой ссылки: lambda/expression architecture использует слабые связи и отложенные вызовы.

## 4. FractalLaw

Каждая сущность раскрывает рекурсивную структуру, но не теряет исходную идентичность:

```text
R(Q) = {
    Q,
    T(Q),
    ∞_Q,
    0_Q,
    R(∞_Q),
    R(0_Q)
}
```

FractalLaw не является численным перебором. Это схема сохранения payload и доступности следующего нормативного уровня. Рекурсивное раскрытие допустимо только через известные RICIS-переходы.

## 5. Базовые операции и приоритет

Нормативная цепочка имеет следующий порядок:

```text
L1 identity
→ SP4 semantic indexing
→ SP2 algebraic reduction
→ O(1) internal bridges
→ A1/A4 singular transforms
→ type consistency
→ A5/A6/A7 and indexed-zero rules
→ classical fallback only for unspecified operations
```

Основные правила:

| Правило | RICIS-результат |
|---|---|
| `F/F` | `1`, если `F` структурно идентично самому себе |
| `F·0` | `0_F` |
| `F/0` | `∞_F` |
| `F/∞_G` | `0_F`, при сохранении ключей `G` |
| `0_F/0_G` | `F/G` |
| `∞_F/∞_G` | `F/G`; при `F=G` действует L1 и результат `1` |
| `0_F+0_G` | `0_{F+G}` |
| `0_F·0_G` | `0_{F·G}` |
| `0_F·∞_G` | `F·G` по A6 |
| `∞_F−∞_G` | `∞_{F−G}` |
| `∞_F+∞_G` | `∞_{F+G}` |
| `∞_F·∞_G` | `∞_{F·G}` |

В канонической спецификации v7.7 правило `F·0→0_F` обозначено как `A10_FTIMES0`; в текущем покрытии Ricis.Core оно реализовано через O(1)/LIM-01 bridge и индексированные zero nodes. Второй экземпляр аксиомы создавать нельзя.

## 6. A6 и геометрический смысл

Главный сингулярный мост:

```text
0_F · ∞_G → F·G
```

Это не классическое `0·∞`, не предел и не численная регуляризация. Это точный переход между двумя индексированными RICIS-сущностями. В геометрическом API он также выражает площадь вырожденной полосы и отрезка:

```text
Integral(F, L) := 0_F · ∞_L → F·L
```

`Integral` не строит риманову сумму, не вызывает `lim` и не использует Лопиталя.

Для сингулярного якобиана:

```text
0_det(J) · ∞_Inv(J)
→ det(J)·Inv(J)
```

Индекс `det(J)`, inverse payload и certified keys должны сохраняться в результате.

## 7. Внутренний calculus

Производная является символьной перестановкой expression tree. Сначала применяется L1/SP2, затем строится производный узел, после чего он снова проходит RICIS pipeline. В частности:

```text
∂(F·0)/∂x
```

должно сохранять нормативный indexed zero `0_F`, а не превращаться в обычный числовой `0`.

Вектор — это упорядоченный набор из `N` координат, каждая из которых является RICIS-выражением:

```text
V=(F₁,…,Fₙ)
```

`RicisVector<T>` организует конечные координаты, а `RicisVectorExpression<T>` — deferred coordinate lambdas. Векторные операции покомпонентны; направление задаётся всей последовательностью координат.

Якобиан векторного отображения:

```text
F=(F₁,…,Fₙ)
J_F=(∂Fᵢ/∂xⱼ)
```

`RicisMatrixExpression<T>` хранит матрицу deferred entries. Для `2×2` и `3×3` determinant строится внутренней expression-tree алгеброй, без численного вычисления.

## 8. Фазы RicisPhasePipeline

```text
-1  L1 identity
 0  direct structural preparation
0.5  polar/trigonometric structural phase, если применима
 1  SP2 algebraic reduction
1.5  O(1) internal bridges
 2  A1/A4 singular transforms
 4  type consistency / SP3
 5  standard operations A5/A6/A7 and Z rules
 6  final structural verification
 META  opt-in author presentation metadata after the structural result
```

`META` не является новой аксиомой и не изменяет RICIS-expression semantics: при внешнем closure capture `about` или параметре lambda `about` производное дерево получает `AuthorAnnotatedExpression`, который влияет только на текстовое представление и редуцируется к исходному телу при `Compile()`.

Внешнее слово «предел» не должно попадать в вычислительную цепочку. Если документация использует краткую запись `lim(x→a)→x=a`, это читается только как обозначение прямого внутреннего O(1)-моста и структурной подстановки.

## 9. Proof protocol

`Prove` принимает условия, ограничения и deferred claim. Условия и ограничения сохраняются как expression trees и не исполняются. `ProveDocument` добавляет академическую оболочку: определения, аксиомы, применённые шаги, производное дерево, тезис и границы результата.

Multivariate proof выполняется через:

```text
RicisVectorExpressionVisitor<T>
→ RicisMultivariateAlgebraicVisitor<T>
→ componentwise residual
→ structural zero vector
```

Для обратного векторного отображения проверяются два тождества:

```text
G(F(x)) − x = 0⃗
F(G(y)) − y = 0⃗
```

Успешное сведение до `0⃗` является доказательством заданной внутренней системы и не должно автоматически называться доказательством внешней нерешённой теоремы без формального bridge от предметной системы к RICIS-системе.

### Lean как первичный формальный output

Корректный Lean строится не из текстового `ToString()` и не из academic trace. Нормативная форма имеет вид:

```text
LeanTemplate(StructuredData, RequestedRows) => LeanDoc
```

`LeanDoc` содержит compilable Lean source, сформированный из типизированных структурированных данных и конечного enum-набора theorem rows. В текущем supported bridge это exact-rational модель ID-01–ID-06 на `ℚ`, проверяемая без `sorry` и `sorryAx`. `Log`, `Academic` и `Json` являются частными presentation-форматами proof model и не заменяют Lean compiler.

Если произвольный C# expression tree не соответствует supported structured bridge, Lean output обязан завершиться controlled rejection, а не созданием comment scaffold, который можно ошибочно принять за формальное доказательство.

## 10. Применение к задачам Clay

Единая фрактальная схема предметной задачи:

```text
Q_problem
→ 0_condition(Q_problem) или ∞_payload(Q_problem)
→ SP4 сохраняет исходное выражение и ключ
→ FractalLaw раскрывает payload
→ SP2 и нормативные A-правила перестраивают систему
→ residual = 0_Q
```

Предметные payloads:

| Задача | Начальный payload |
|---|---|
| Риман | zero/reflection identity |
| P vs NP | verifier/witness identity |
| Birch–Swinnerton-Dyer | rank/order identity |
| Hodge | cycle/class identity |
| Навье–Стокс | flow/regularity identity |
| Yang–Mills | vacuum/mass-gap identity |

Консольный proof-project должен явно печатать: внутреннюю систему, использованные RICIS-узлы, нормативные переходы, residual и границу утверждения.

## 11. Восстановление контекста после переключения модели

При начале новой сессии необходимо прочитать в следующем порядке:

1. `RICIS_III_CONCEPT.md` — этот файл;
2. `RICIS_RULE_COVERAGE.md` — нормативная матрица и regression contract;
3. `RICIS_PROOF_DOCUMENTS.md` — формат академического документа;
4. `RICIS_ACADEMIC_PROOFS.md` — существующий proof engine;
5. `RICIS_NAVIER_STOKES_PROOF.md` и актуальные Jacobian/Clay probe outputs;
6. исходные классы `RicisPhasePipeline`, `LimitBridgeVisitor`, `StandardOperationsVisitor`, `RicisVectorExpressionVisitor` и индексированные expression types.

После чтения нельзя менять аксиомы или переинтерпретировать RICIS через классические пределы. Сначала нужно определить, какая внутренняя аксиома или bridge уже покрывает задачу.

## 12. Текущие артефакты

| Артефакт | Назначение |
|---|---|
| `Expressions/RicisVector.cs` | конечный N-мерный вектор |
| `Expressions/RicisVectorExpression.cs` | deferred vector lambdas |
| `Expressions/ExpressionSystem.cs` | структурный контейнер системы lambda-выражений поверх vector API |
| `Proofs/RicisLeanProofModels.cs` | `StructuredData`, `RequestedRows`, `LeanDoc` и controlled rejection для unsupported Lean shapes |
| `Proofs/RicisLeanTemplate.cs` | typed Lean source renderer для supported ID-01–ID-06 bridge |
| `RICIS_LEAN_TEMPLATE.md` | первичный Lean contract и compiler-backed verification command |
| `Expressions/RicisVectorExpressionVisitor.cs` | multivariate component visitor |
| `Expressions/RicisMultivariateAlgebraicVisitor.cs` | структурные перестановки и cancellation |
| `Expressions/RicisMatrixExpression.cs` | deferred matrix и determinant 2×2/3×3 |
| `Expressions/RicisJacobianSingularityExpression.cs` | `0_det(J)·∞_Inv(J)` A6 bridge |
| `RegressionTests/RicisPrioritySuite.cs` | приоритет RICIS над classical fallback |
| `RegressionTests/RicisVectorVisitorSuite.cs` | `G∘F=Id` и `F∘G=Id` component proof |
| `RICIS_RULE_COVERAGE.md` | контракт регрессий |
| `RICIS_WEBAPI.md` | HTTP API, security boundaries и release integration |
| `../Jacobian2DProbe` | двумерный vector/matrix protocol |
| `../Jacobian3DProbe` | трёхмерный vector/matrix/Visitor protocol |
| `../PvsNPProofProbe` | конечный verifier/witness probe |
| `../HodgeProofProbe` | конечный cycle-class probe |
| `../BirchSwinnertonDyerProofProbe` | конечный rank/order probe |
| `../YangMillsProofProbe` | конечный finite-mode gap probe |

## 13. Неприкосновенные ограничения

Нельзя менять L1, SP1–SP4, A1–A7, Z-01/Z-02, A6 geometric semantics или established phase priority ради удобства классического решателя. Нельзя удалять weak-link code из-за отсутствия прямой ссылки. Нельзя компилировать deferred proof expression для получения численного ответа внутри доказательства. Нельзя выдавать finite probe за полное доказательство внешней Clay-задачи без явно реализованного предметного bridge.

> RICIS III сначала сохраняет сущность, затем раскрывает структуру и только после этого применяет допустимое преобразование.
