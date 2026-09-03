using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class AutomationScenarioTests
{
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
        var service = new AutomationScenarioService();
        var (angles0, status0) = service.StepScenarioFrame(0.0);
        Assert.AreEqual(0.0, angles0.Q1Degrees, 1e-2);
        Assert.IsFalse(string.IsNullOrWhiteSpace(status0));

        var (angles50, status50) = service.StepScenarioFrame(50.0);
        Assert.IsTrue(angles50.Q1Degrees < 40.0, "Q1 must sweep towards Box B");
        Assert.IsFalse(string.IsNullOrWhiteSpace(status50));

        var (angles100, status100) = service.StepScenarioFrame(100.0);
        Assert.AreEqual(-35.0, angles100.Q1Degrees, 1e-2);
        Assert.IsFalse(string.IsNullOrWhiteSpace(status100));
        Assert.IsFalse(service.Workpieces[2].IsGrabbed);
        Assert.AreEqual(-0.35, service.Workpieces[2].Position.Y, 1e-9);
    }
}
