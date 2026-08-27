# RICIS JSON report business contract

## Назначение

JSON-отчёт является самостоятельным машинно-читаемым semantic artifact. Он не является сериализацией `RicisLogEntry`, внутреннего `ILog<TSender>` или общего технического journal. JSON pipeline получает классифицированные события и строит versioned document model, пригодную для downstream audit, UI и повторной обработки.

## Schema boundary

Поле `schema` имеет стабильное значение `ricis-semantic-report/v1`. Поле `reportType` фиксирует `json-semantic`. Поле `kernelVerification` явно отделяет semantic audit artifact от kernel-verified theorem. Порядок `events` совпадает с semantic sequence, но JSON не обязан раскрывать внутренние before/after snapshots: они относятся к Text Trace и включаются только в техническом JSON appendix по явной опции.

## Event projection

Каждое JSON event содержит `sequence`, `kind`, `visibility`, `sender`, `phase`, `eventCode`, `message` и `status`. `sender` является плоским descriptor, а не CLR type object. Exception event содержит только классифицированные поля `exceptionType`, `handlingStatus` и `publicCause`; полный stack trace остаётся Text Trace.

## Invariants

JSON serializer обязан быть детерминированным, сохранять порядок, экранировать пользовательские строки средствами `System.Text.Json`, отклонять null/неупорядоченный input и не выполнять expression tree. Unknown events получают `unclassified` и не становятся proof steps автоматически. `null` logger не создаёт JSON artifact и не меняет computation.

## QA acceptance criteria

QA проверяет versioned schema, отсутствие raw journal fields (`before`, `after`, `exceptionTrace`, CLR object payload), sender/phase/event classification, exception public cause, stable order, deterministic output, unknown-event isolation и legacy compatibility.
