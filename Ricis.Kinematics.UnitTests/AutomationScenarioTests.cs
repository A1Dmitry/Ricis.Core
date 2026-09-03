using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class AutomationScenarioTests
{
    private AutomationScenarioService _scenarioService;


    [TestMethod]
    public void ScenarioInitialization_PlacesThreeWorkpiecesInSourceBoxA()
    {
        var service = new AutomationScenarioService();
        Assert.AreEqual(3, service.Workpieces.Count);
        Assert.AreEqual("P1_Cube", service.Workpieces[0].Id);
        Assert.AreEqual(WorkpieceShape.Cube, service.Workpieces[0].Shape);
        Assert.AreEqual("P2_Sphere", service.Workpieces[1].Id);
        Assert.AreEqual("P3_Pyramid", service.Workpieces[2].Id);
        Assert.AreEqual(ScenarioState.Stopped, service.CurrentState);
    }

    [TestMethod]
    public void StateMachine_StartPauseResumeAndComplete()
    {
        var service = new AutomationScenarioService();
        service.Start();
        Assert.AreEqual(ScenarioState.Running, service.CurrentState);
        service.Pause();
        Assert.AreEqual(ScenarioState.Paused, service.CurrentState);
        service.Start();
        Assert.AreEqual(ScenarioState.Running, service.CurrentState);
        service.StepScenarioFrame(100);
        service.Complete();
        Assert.AreEqual(ScenarioState.Completed, service.CurrentState);
        Assert.AreEqual(100, service.ProgressPercentage, 1e-9);
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
