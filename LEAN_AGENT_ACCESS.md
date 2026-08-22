# Доступ агента к Lean-файлам

## Проверенный контракт

Агент получает доступ к Lean-материалам проекта через постоянный каталог `FormalVerification/Lean/`. Все подтверждённые артефакты перечислены в `FormalVerification/Lean/Artifacts/manifest.json`.

Каждая запись manifest обязана иметь:

```json
"knowledgeSource": {
  "mandatoryForModelStudy": true,
  "role": "mandatory-project-knowledge-source"
}
```

Это означает, что Lean-артефакт является обязательным источником знаний для последующего анализа модели, а не временным файлом из `/tmp` или только выводом теста.

## Как агент должен использовать файлы

Агент должен сначала прочитать `FormalVerification/Lean/README.md`, затем найти нужный artifact в `manifest.json`, изучить его theorem names и provenance, и только после этого использовать результат как возможное доказательное основание. Для kernel-проверки используется закреплённый toolchain из `FormalVerification/Lean/lean-toolchain` и команда:

```bash
cd FormalVerification/Lean
lake env lean <source.lean>
```

C# regression test и Lean kernel compilation являются разными уровнями evidence. Нельзя считать C#-проверку kernel theorem; для этого нужен реально скомпилированный Lean source без `sorry` и `admit`.

## Результат текущей проверки

Автоматические тесты подтверждают, что все Lean-файлы из manifest существуют и доступны из корня проекта, имеют обязательную отметку `mandatory-project-knowledge-source`, а Lean artifacts не содержат `sorry` или `admit`.

В текущем sandbox отсутствуют исполняемые команды `lean`, `lake` и `elan`, поэтому здесь подтверждён **доступ к файлам и knowledge contract**, но не выполнена новая kernel-компиляция. Для kernel verification требуется среда с установленным Lean toolchain `leanprover/lean4:v4.33.0`.

Проверка зафиксирована в `UnitTests/LeanAgentAccessTests.cs`.
