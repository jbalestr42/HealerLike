using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class SimulationQueueTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationQueue.Clear();
    }

    [Test]
    public void Start_TheFirstFightIsCurrent()
    {
        SimulationJob first = new SimulationJob { floor = 1 };

        SimulationQueue.Start(null, new List<SimulationJob> { first, new SimulationJob { floor = 2 } }, "path", "run");

        Assert.IsTrue(SimulationQueue.isRunning);
        Assert.AreSame(first, SimulationQueue.current);
        Assert.AreEqual(0, SimulationQueue.index);
        Assert.AreEqual(2, SimulationQueue.count);
        Assert.AreEqual("path", SimulationQueue.outputPath);
        Assert.AreEqual("run", SimulationQueue.runId);
    }

    [Test]
    public void Advance_PastTheLastFight_NotRunningAnymoreButKeepsTheCount()
    {
        SimulationJob second = new SimulationJob { floor = 2 };
        SimulationQueue.Start(null, new List<SimulationJob> { new SimulationJob(), second }, "path", "run");

        SimulationQueue.Advance();
        Assert.AreSame(second, SimulationQueue.current);
        SimulationQueue.Advance();
        SimulationQueue.Advance();

        Assert.IsFalse(SimulationQueue.isRunning);
        Assert.IsNull(SimulationQueue.current);
        Assert.AreEqual(2, SimulationQueue.index);
        Assert.AreEqual(2, SimulationQueue.count);
    }

    [Test]
    public void Start_RecordsWhenTheSimulationStarted()
    {
        SimulationQueue.Start(null, new List<SimulationJob> { new SimulationJob() }, "path", "run");

        Assert.AreEqual(Time.realtimeSinceStartup, SimulationQueue.startRealtime, 1f);
        Assert.GreaterOrEqual(SimulationQueue.elapsedRealtime, 0f);
    }

    [Test]
    public void Start_CopiesTheList()
    {
        List<SimulationJob> jobs = new List<SimulationJob> { new SimulationJob() };

        SimulationQueue.Start(null, jobs, "path", "run");
        jobs.Add(new SimulationJob());

        Assert.AreEqual(1, SimulationQueue.count);
    }

    [Test]
    public void Start_KeepsThePlanUntilCleared()
    {
        SimulationPlan plan = ScriptableObject.CreateInstance<SimulationPlan>();

        SimulationQueue.Start(plan, new List<SimulationJob> { new SimulationJob() }, "path", "run");
        Assert.AreSame(plan, SimulationQueue.plan);
        SimulationQueue.Clear();

        Assert.IsNull(SimulationQueue.plan);
        Object.DestroyImmediate(plan);
    }

    [Test]
    public void Clear_NothingLeft()
    {
        SimulationQueue.Start(null, new List<SimulationJob> { new SimulationJob() }, "path", "run");

        SimulationQueue.Clear();

        Assert.IsFalse(SimulationQueue.isRunning);
        Assert.AreEqual(0, SimulationQueue.count);
    }
}

}
