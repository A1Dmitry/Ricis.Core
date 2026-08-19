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
