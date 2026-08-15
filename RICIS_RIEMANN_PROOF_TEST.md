# Proof-тест RICIS: самоидентификация отражённой пары

## Нормативное назначение

Этот тест реализует полную цепочку самоидентификации RICIS для формальной отражённой пары `sigma`, `mirrorSigma`. Он не принимает готовые уравнения `sigma+mirrorSigma=1` и `sigma−mirrorSigma=0` от вызывающего кода: их создаёт `ProveTypeIdentityCriticalLine` как именованные следствия ID-01–ID-04, затем обычный proof-движок выполняет ID-05–ID-06.

```csharp
Expression<Func<double, double, bool>>[] constraints =
[
    (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0,
    (sigma, mirrorSigma) => mirrorSigma > 0.0 && mirrorSigma < 1.0,
];
var exactHalf = Expression.Lambda<Func<double>>(
    Expression.Divide(Expression.Constant(1.0), Expression.Constant(2.0)));
var sigma = Expression.Parameter(typeof(double), "sigma");
var mirrorSigma = Expression.Parameter(typeof(double), "mirrorSigma");
var claim = Expression.Lambda<Func<double, double, bool>>(
    Expression.Equal(sigma, exactHalf.Body), sigma, mirrorSigma);

var document = new StringBuilder();
var derived = constraints.ProveTypeIdentityCriticalLine(claim, document);
```

## Нормативная цепочка

| ID | Нормативное правило | Кодовый результат |
|---|---|---|
| ID-01 | Самоидентификация отражённой сущности сохраняет `Type`. | В документе фиксируется `Type(sigma)=Type(mirrorSigma)`. |
| ID-02 | Зеркальная симметрия имеет координату `R(sigma)=1-sigma`. | Генерируется дерево `sigma+mirrorSigma=1`. |
| ID-03 | Тип является верным идентификатором координаты. | Из равенства типов фиксируется `sigma=mirrorSigma`. |
| ID-04 | Совмещение ID-02 и ID-03. | Генерируется дерево `sigma-mirrorSigma=0`. |
| ID-05 | Структурное исключение отражённой координаты. | Фактическая трассировка содержит `2·sigma=1`. |
| ID-06 | Точное рациональное выделение координаты. | Производное дерево содержит `sigma=Divide(1,2)`. |

Весь путь печатается до и вместе с expression-tree трассировкой. Несократимая дробь остаётся `Expression.Divide(Expression.Constant(1.0), Expression.Constant(2.0))`, а не превращается в `double`-константу `0.5`.

## Независимая проверка Lean

Независимая модель находится в [FormalVerification/Lean](FormalVerification/Lean). Её структура `TypeIdentityAxioms` явно принимает нормативные ID-01–ID-03 как фундамент, а Lean без `sorry` выводит `id04_linear_pair`, `id05_doubled_coordinate`, `id06_exact_half` и `id06_reflected_exact_half` на точных рациональных числах.

## Регрессионные проверки

| Тест | Контракт |
|---|---|
| `RIEMANN01` | `ProveTypeIdentityCriticalLine` выводит точное `sigma=1/2`, печатает ID-01–ID-06 и все промежуточные expression tree. |
| `RIEMANN02` | ID-цепочка отклоняет ложный тезис `sigma=0.4`. |
| `RIEMANN03` | Однопроходная последовательность ограничений сохраняется: она материализуется ровно один раз и не теряется при делегировании. |

Консольный документный вывод доступен командой:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --riemann-proof-demo
```

Подробный QA-статус и единственный исправленный дефект перечисления ограничений приведены в [RICIS_RH_FORMALIZATION_QA.md](RICIS_RH_FORMALIZATION_QA.md).
