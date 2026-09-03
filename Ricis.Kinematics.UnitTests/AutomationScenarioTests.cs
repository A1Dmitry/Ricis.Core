using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class AutomationScenarioTests
{
    private readonly AutomationScenarioService _scenarioService = new();

    [TestMethod]
    public void ScenarioInitialization_PlacesThreeWorkpiecesInSourceBoxA()
    {
        Assert.AreEqual(3, _scenarioService.Workpieces.Count);
        Assert.AreEqual("P1_Cube", _scenarioService.Workpieces[0].Id);
        Assert.AreEqual(WorkpieceShape.Cube, _scenarioService.Workpieces[0].Shape);
        Assert.AreEqual("P2_Sphere", _scenarioService.Workpieces[1].Id);
        Assert.AreEqual("P3_Pyramid", _scenarioService.Workpieces[2].Id);
    }

    [TestMethod]
    public void StepScenarioFrame_AtProgressPoints_UpdatesWorkpiecePositionsAndJointAngles()
    {
        // At start (0% progress)
        var (angles0, status0) = _scenarioService.StepScenarioFrame(0.0);
        Assert.IsNotNull(angles0);
        Assert.IsNotNull(status0);

        // At midpoint (50% progress)
        var (angles50, status50) = _scenarioService.StepScenarioFrame(50.0);
        Assert.IsNotNull(angles50);
        Assert.IsNotNull(status50);

        // At end (100% progress)
        var (angles100, status100) = _scenarioService.StepScenarioFrame(100.0);
        Assert.IsNotNull(angles100);
        Assert.IsNotNull(status100);
    }
}
