# Riemann-связанный proof-тест RICIS

## Назначение и граница утверждения

Гипотеза Римана утверждает, что все нетривиальные нули дзета-функции имеют действительную часть `1/2`; Clay Mathematics Institute продолжает относить её к нерешённым задачам.[1] Поэтому данный тест **не является и не объявляется доказательством гипотезы Римана**.

Тест проверяет более узкую способность академического протокола RICIS: вывести конечное линейное следствие из двух явно переданных равенств для формальной пары действительных частей `sigma` и `mirrorSigma`.

```csharp
Expression<Func<double, double, bool>>[] equations =
[
    (sigma, mirrorSigma) => sigma + mirrorSigma == 1.0,
    (sigma, mirrorSigma) => sigma - mirrorSigma == 0.0,
];
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
```

| Роль | Формальное выражение | Значение в тесте |
|---|---|---|
| Симметрия | `sigma + mirrorSigma = 1` | Модель симметрии относительно линии `Re(s)=1/2`. |
| Равенство пары | `sigma − mirrorSigma = 0` | Явная дополнительная гипотеза о совпадении действительных частей данной пары. |
| Критическая полоса | `0 < sigma < 1`, `0 < mirrorSigma < 1` | Формальные ограничения области; они сохраняются в протоколе и не исполняются. |
| Проверяемое следствие | `sigma = 1/2` | Конечный результат линейного исключения. |

Из двух уравнений протокол выводит `2·sigma=1`, затем `sigma=1/2`, подставляет найденную координату в первое уравнение и получает `mirrorSigma=1/2`. Несократимая дробь остаётся в дереве как `Divide(Constant(1.0), Constant(2.0))`, а не материализуется в double-константу `0.5`. Явное построение через `Expression.Divide` необходимо, поскольку компилятор C# может свернуть литеральное выражение `() => 1.0 / 2.0` до константы ещё до создания expression tree. Это доказывает только условное следствие для **данной явно заданной пары**.

## Регрессионные проверки

| Тест | Контракт |
|---|---|
| `RIEMANN01` | Строит доказательство `sigma=1/2`; структурно сравнивает правую часть производного дерева с эталоном `Expression.Divide(1.0, 2.0)`, проверяет реальные имена параметров в `StringBuilder`, все четыре линейных шага и отсутствие ложной декларации о доказательстве гипотезы Римана. |
| `RIEMANN02` | Проверяет, что ложный тезис `sigma=0.4` отклоняется. |

Сценарий использует `ProveDocument` с `RicisProofScope.ConditionalTheorem`: в отчёте явно публикуются определения формальной пары, `P1`, `P2` и границы результата. Полный документный контракт приведён в [RICIS_PROOF_DOCUMENTS.md](RICIS_PROOF_DOCUMENTS.md), а незакрытые мосты к аналитической гипотезе перечислены в [RICIS_RH_FORMALIZATION_QA.md](RICIS_RH_FORMALIZATION_QA.md).

Консольный вывод доступен командой:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --riemann-proof-demo
```

## Обнаруженная граница возможностей

Успешный `RIEMANN01` показывает, что текущий two-variable `Prove` и `ProveDocument` покрывают детерминированное исключение в конечной линейной модели. Он одновременно локализует методы, необходимые для формализации более содержательного Riemann-связанного исследования, но **не реализованные и не требуемые этим тестом**: представление комплексной переменной `s`, символический узел `ζ(s)`, функциональное уравнение и кванторное утверждение «для каждого нетривиального нуля». Их отсутствие не является основанием делать вывод о гипотезе Римана из данного протокола.

## Reference

[1] [Clay Mathematics Institute — Riemann Hypothesis](https://www.claymath.org/millennium/riemann-hypothesis/)
