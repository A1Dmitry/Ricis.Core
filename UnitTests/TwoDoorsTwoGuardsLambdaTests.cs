using System.Reflection;
using ComponentDescription = System.ComponentModel.DescriptionAttribute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Core.Puzzles;

namespace Ricis.Core.UnitTests;

/// <summary>
/// Direct behavioural and metadata regression tests for the typed lambda-only two-doors puzzle model.
/// </summary>
[ComponentDescription("Набор прямых тестов лямбда-модели задачи о двух дверях и двух охранниках.")]
[TestClass]
public sealed class TwoDoorsTwoGuardsLambdaTests
{
    /// <summary>
    /// Verifies that a truthful first guard and a lying second guard preserve the left safe door.
    /// </summary>
    [ComponentDescription("Проверка варианта: сначала правдивый, затем лжец, безопасна левая дверь.")]
    [TestMethod]
    public void Escape_TruthThenLiar_LeftDoorSafe_SelectsLeftDoor()
    {
        var selectedDoor = TwoDoorsTwoGuardsLambda<string>.Escape(
            TwoDoorsTwoGuardsLambda<string>.Truth,
            TwoDoorsTwoGuardsLambda<string>.Liar,
            TwoDoorsTwoGuardsLambda<string>.True)("left", "right");

        Assert.AreEqual("left", selectedDoor);
    }

    /// <summary>
    /// Verifies that a lying first guard and a truthful second guard preserve the left safe door.
    /// </summary>
    [ComponentDescription("Проверка варианта: сначала лжец, затем правдивый, безопасна левая дверь.")]
    [TestMethod]
    public void Escape_LiarThenTruth_LeftDoorSafe_SelectsLeftDoor()
    {
        var selectedDoor = TwoDoorsTwoGuardsLambda<string>.Escape(
            TwoDoorsTwoGuardsLambda<string>.Liar,
            TwoDoorsTwoGuardsLambda<string>.Truth,
            TwoDoorsTwoGuardsLambda<string>.True)("left", "right");

        Assert.AreEqual("left", selectedDoor);
    }

    /// <summary>
    /// Verifies that a truthful first guard and a lying second guard preserve the right safe door.
    /// </summary>
    [ComponentDescription("Проверка варианта: сначала правдивый, затем лжец, безопасна правая дверь.")]
    [TestMethod]
    public void Escape_TruthThenLiar_RightDoorSafe_SelectsRightDoor()
    {
        var selectedDoor = TwoDoorsTwoGuardsLambda<string>.Escape(
            TwoDoorsTwoGuardsLambda<string>.Truth,
            TwoDoorsTwoGuardsLambda<string>.Liar,
            TwoDoorsTwoGuardsLambda<string>.False)("left", "right");

        Assert.AreEqual("right", selectedDoor);
    }

    /// <summary>
    /// Verifies that a lying first guard and a truthful second guard preserve the right safe door.
    /// </summary>
    [ComponentDescription("Проверка варианта: сначала лжец, затем правдивый, безопасна правая дверь.")]
    [TestMethod]
    public void Escape_LiarThenTruth_RightDoorSafe_SelectsRightDoor()
    {
        var selectedDoor = TwoDoorsTwoGuardsLambda<string>.Escape(
            TwoDoorsTwoGuardsLambda<string>.Liar,
            TwoDoorsTwoGuardsLambda<string>.Truth,
            TwoDoorsTwoGuardsLambda<string>.False)("left", "right");

        Assert.AreEqual("right", selectedDoor);
    }

    /// <summary>
    /// Verifies that every public lambda-model entity has a non-empty description for discovery tooling.
    /// </summary>
    [ComponentDescription("Проверка DescriptionAttribute для всех публичных сущностей лямбда-модели.")]
    [TestMethod]
    public void LambdaModel_PublicEntities_HaveNonEmptyDescriptionAttributes()
    {
        var modelType = typeof(TwoDoorsTwoGuardsLambda<string>);
        var publicFields = modelType.GetFields(BindingFlags.Public | BindingFlags.Static);

        Assert.IsNotNull(typeof(ChurchBoolean<>).GetCustomAttribute<ComponentDescription>());
        Assert.IsNotNull(typeof(DoorGuard<>).GetCustomAttribute<ComponentDescription>());
        Assert.IsNotNull(modelType.GetCustomAttribute<ComponentDescription>());
        Assert.AreEqual(6, publicFields.Length, "Модель должна документировать шесть опубликованных лямбда-термов.");

        foreach (var field in publicFields)
        {
            var description = field.GetCustomAttribute<ComponentDescription>();
            Assert.IsNotNull(description, $"Сущность '{field.Name}' должна иметь DescriptionAttribute.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(description.Description), $"Описание сущности '{field.Name}' не должно быть пустым.");
        }
    }
}
