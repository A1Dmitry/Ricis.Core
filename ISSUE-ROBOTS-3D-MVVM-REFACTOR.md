# Refactor `Ricis.Robotics3D.App`: MVVM, consistent kinematics and scenario state

## Цель

Перевести `Ricis.Robotics3D.App` на практическую MVVM-архитектуру, устранить рассогласование между доменной моделью и 3D-сценой, сделать сценарий переноса деталей детерминированным и тестируемым, а также убрать неподтверждённое поведение GPU-диагностики.

## Объём работ

| ID | Направление | Результат | Приоритет |
|---|---|---|---|
| R3D-01 | MVVM | `MainWindow.xaml.cs` не содержит логики построения сцены; сцена предоставляется ViewModel через bindable `Model3DGroup`. | P0 |
| R3D-02 | Единая сцена | Геометрия и материалы создаются в отдельном `RobotSceneBuilder`; обновляются только после изменения состояния ViewModel. | P0 |
| R3D-03 | State machine | Доменный `ScenarioState` является единственным источником истины; состояния `Stopped`, `Running`, `Paused`, `Completed` синхронизированы с UI. | P0 |
| R3D-04 | Финальный кадр | Кадр 100% обрабатывается до остановки таймера; все детали находятся в целевых позициях и имеют `IsGrabbed == false`. | P0 |
| R3D-05 | Команды | Start/Pause/Reset имеют корректные `CanExecute` и уведомляют UI об изменениях доступности. | P1 |
| R3D-06 | Кинематика | Устранено дублирование формул рендера и solver; сцена строится по последовательным DH-преобразованиям PUMA 560 для Q1–Q6. | P0 |
| R3D-07 | Asset metadata | Убраны неподтверждённые заявления о внешнем UR5/PUMA asset и лицензии; процедурная модель обозначена явно. | P1 |
| R3D-08 | GPU | Убрано ложное автоопределение NVIDIA по умолчанию; режимы отражают диагностическую политику и не обещают управление WPF pipeline. | P1 |
| R3D-09 | Жизненный цикл | Таймер останавливается и события отписываются при закрытии/освобождении ViewModel. | P1 |
| R3D-10 | Тесты | Добавлены тесты ViewModel/state machine и доменного сценария на границах 0/20/80/100%. | P1 |
| R3D-11 | UI | Строки и состояния получают понятные binding-и; фиксированные индексы ComboBox заменяются перечислением режимов. | P2 |
| R3D-12 | Сборка | Проект собирается в Windows/.NET 8 CI без необоснованного подавления предупреждений. | P1 |

## Критерии приёмки

- `MainWindow.xaml.cs` содержит только конструктор окна и не создаёт `MeshBuilder`, `Material` или доменные объекты.
- Изменение угла обновляет телеметрию и bindable сцену из одного снимка состояния.
- Невозможно запустить завершённый сценарий без Reset.
- Pause не меняет прогресс, а повторный Start продолжает сценарий.
- На 100% сервис находится в `Completed`, таймер остановлен, все детали отпущены и перемещены в Box B.
- Переключение GPU-режима не выдаёт ложный факт обнаружения конкретной видеокарты.
- Для application слоя есть автоматические тесты без запуска WPF окна.
- В репозитории явно указано, что текущая процедурная сцена использует шестиосевую модель PUMA 560 и стандартные DH-параметры.

## Реализация в текущем изменении

В рамках текущего изменения выполняются R3D-01—R3D-10 в пределах доступного проекта. Полная проверка WPF-сборки должна быть выполнена в Windows/.NET SDK CI, поскольку локальный sandbox не содержит `dotnet`.

## Риски и ограничения

Публичный конструктор `JointAngles(double, double, double)` сохранён для совместимости, а value object расширен до шести осей. Кинематика и UI используют стандартные DH-параметры PUMA 560 и полный шестикоординатный вектор; трёхосевой конструктор оставлен как shorthand с Q4–Q6 равными нулю.

## References

[1]: https://www.mathworks.com/help/robotics/ug/build-manipulator-robot-using-kinematic-dh-parameters.html "Build Manipulator Robot Using Kinematic DH Parameters"
[2]: https://www.mathworks.com/help/robotics/ug/design-a-trajectory-with-velocity-limits-using-a-trapezoidal-velocity-profile.html "Design Trajectory with Velocity Limits Using a Trapezoidal Velocity Profile"
[3]: https://control.ros.org/rolling/doc/ros2_controllers/joint_trajectory_controller/doc/trajectory.html "ROS 2 Control Trajectory Representation"
