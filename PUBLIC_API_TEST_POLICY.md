# Public API Test Policy

**Статус:** обязательная проектная норма Ricis.Core и Ricis.Finance.

## Основное правило

> После добавления или изменения любого `public` метода его regression tests обязательны в том же изменении.

Public method не считается завершённым, пока в проекте одновременно не существуют:

1. тест успешного штатного сценария;
2. тесты значимых boundary, invalid или rejection paths, если они определены контрактом метода;
3. стабильный test ID в соответствующей regression suite;
4. запуск этого suite в общем quality gate;
5. обновлённая документация или CLI-пример, если метод является пользовательской операцией.

Это правило распространяется на public методы классов, records, structs, interfaces, extension methods, static utility API и публичные overloads. Если добавляется overload, тест должен проверять именно новый overload, а не только уже существующий.

## Запрет незавершённого public API

Изменение, в котором public method добавлен без его regression tests, запрещено принимать в основной branch. Наличие компиляции, XML-документации, CLI-вызова или косвенного покрытия внутренним тестом не заменяет direct contract test для нового public метода.

Если public method намеренно не должен быть доступен пользователю, он не должен объявляться `public`: необходимо выбрать `internal` или `private` и покрыть его через соответствующий внутренний контракт.

## Запрет тихого удаления и переклассификации

> Отсутствие внутреннего caller, отметка ReSharper `UnusedMember`, отсутствие reflection или неполное test coverage означают, что назначение API требуется выяснить. Они **не являются** разрешением молча удалить, сузить, сделать менее доступным или переклассифицировать метод, extension, visitor route, lambda-oriented façade, proof/document route, domain capability или иной потенциальный API.

Перед удалением, сужением доступности или переклассификацией требуется один versioned **Removal Decision Record**, содержащий одновременно:

1. назначение и владельца контракта;
2. direct regression tests существующего observable behavior;
3. полный caller/contract graph, включая CLI, HTTP, JSON, Lean, log, provider and domain boundaries;
4. явный migration target либо обоснование отсутствия потребителей;
5. явное подтверждение пользователя/владельца продукта;
6. SemVer/deprecation decision для фактического public surface.

Это правило распространяется также на `internal` методы, когда они являются или могут являться самостоятельным façade, visitor route, proof/log route или lambda/expression reducer. Их нулевой caller graph требует product/architecture review, а не автоматического удаления.

## Обязательный штраф за нарушение

Нарушение запрета тихого удаления является **release-blocking incident**. До завершения remediation запрещены дальнейшие deletion/refactoring commits в затронутом workstream. Обязательный штраф состоит из всех действий:

1. немедленно восстановить удаленный contract или совместимый façade;
2. добавить direct positive, boundary and safety regression coverage;
3. создать versioned incident record с причиной, затронутым contract, commit и corrective actions;
4. повторить полный Core, Finance и Lean quality gate;
5. провести отдельный remediation commit и уведомить пользователя;
6. не продолжать новые удаления до явного подтверждения исправленного policy/incident результата пользователем.

Ни build, ни пониженное число IDE findings, ни успешный internal test не могут компенсировать этот штраф.

### QA-штраф и контрольная обязанность

QA обязан остановить change до commit, если в нём есть удаление, сужение доступности или переклассификация потенциального API без Removal Decision Record. QA не вправе заменить этот контроль предположением, что ReSharper видит всю модель, или тем, что у кандидата нет внутренних callers.

Если QA пропускает такое нарушение, это самостоятельный **QA release-blocking incident**, независимо от того, кто выполнил удаление. Для QA применяются обязательные corrective actions:

1. QA verdict для затронутого cleanup batch аннулируется; deletion считается непроверенным;
2. все removals/reclassifications с последнего утверждённого baseline проходят повторный dependency and contract audit;
3. QA обязан добавить конкретный regression/gate case, который сделал бы пропуск невозможным в будущем;
4. QA не может утвердить следующий deletion/refactoring batch до завершения incident remediation, полного quality gate и явного пользовательского подтверждения;
5. incident record обязан отдельно указать: какое QA правило не сработало, почему evidence было недостаточно и какой blocking check добавлен.

### Обязательный QA deletion gate

Перед итоговым QA verdict для любого cleanup commit проверяются и фиксируются все строки:

| QA ID | Обязательная проверка | Failure effect |
|---|---|---|
| `QA-DEL-01` | Есть ли в diff удаление, reduced visibility или reclassification метода/type/member? | При `да` запускается Removal Decision Record review. |
| `QA-DEL-02` | Это public, extension, interface, proof/log, lambda/visitor façade, domain capability, DTO/wire или потенциальный API? | Без явного owner decision removal блокируется. |
| `QA-DEL-03` | Есть direct behavior/safety regression и полный caller/contract graph? | Отсутствие любого evidence блокирует commit. |
| `QA-DEL-04` | Есть explicit user approval и SemVer/migration decision, если surface может быть external? | Отсутствие approval блокирует commit. |
| `QA-DEL-05` | Полный Core, Finance и Lean gate выполнен после restoration/removal? | Любой failure блокирует QA verdict. |

## Требование лаконичной C# лямбда-записи для тестовых математических выражений

При объявлении входных математических условий и функций в модульных и регрессионных тестах **запрещается** использовать громоздкую, трудночитаемую ручную сборку деревьев выражений (через `Expression.Parameter`, `Expression.Divide`, `Expression.Multiply`, `Expression.Subtract`, `Expression.Add` и т.д.).

Все входные математические выражения для тестов должны объявляться в виде чистой и прозрачной C# лямбда-записи:

```csharp
// Рекомендуемый стандартизированный синтаксис:
Expression<Func<double, double>> expression = x => (x * x * x - 8.0) / (x - 2.0);
```

Это гарантирует легкую читаемость, наглядное сравнение с математической постановкой задачи и исключает ошибки ручного построения AST в коде тестов.

## Минимальный test contract

Для каждого нового public method в regression suite должна быть запись с устойчивым ID и явным названием метода. Например:

```csharp
("API17: RicisType.NewMethod handles the documented boundary", NewMethodHandlesBoundary)
```

Тест обязан проверять результат, тип/структуру expression tree или документированный exception contract. Один только факт вызова метода без проверки результата недостаточен.

## Связь с CLI и artifacts

Если public method представлен в консольном приложении, должен существовать CLI smoke test или включённая в versioned smoke log команда. Если результат относится к Lean proof/documentation, он дополнительно подчиняется `LEAN_ARTIFACT_POLICY.md`: проверенный artifact прикрепляется к проекту, регистрируется в manifest и помечается обязательным источником знаний для модели.

## Acceptance gate

Перед commit автор обязан подтвердить:

| Проверка | Обязательное условие |
|---|---|
| Public method test | Есть direct regression test с test ID |
| Negative/boundary behavior | Проверено, если предусмотрено контрактом |
| Suite registration | Suite подключена к общему harness |
| CLI surface | Добавлен пример/smoke test, если API user-facing |
| Full quality gate | Core/Finance suites и связанные artifacts проходят |
| Documentation | Audit/backlog обновлены при изменении public surface |

Нарушение любой строки означает, что public API change не готов к фиксации в основной branch.

## История правила

Правило введено после аудита public API и консольного приложения, в рамках которого были добавлены тесты `API01–API16` для utility и `RicisType` contracts. Все дальнейшие public API additions обязаны продолжать эту схему идентифицируемого regression coverage.
