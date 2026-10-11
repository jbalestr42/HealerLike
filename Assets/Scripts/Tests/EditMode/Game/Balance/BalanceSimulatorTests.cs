using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class BalanceSimulatorTests
{
    readonly Entities.TestUnits _units = new Entities.TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void OutputPath_BalanceSimulationInTheBalanceLogs_MeasureInItsOwnFolder()
    {
        string balance = BalanceSimulator.GetOutputPath("QuickTest", "20261005-100000", null);
        Assert.AreEqual(System.IO.Path.GetDirectoryName(CombatLogFile.defaultPath), System.IO.Path.GetDirectoryName(balance));
        Assert.AreEqual("sim-QuickTest-20261005-100000.jsonl", System.IO.Path.GetFileName(balance));

        Assert.AreEqual(System.IO.Path.Combine("Measures", "20261005", "WaveDps.jsonl"), BalanceSimulator.GetOutputPath("WaveDps", "20261005-100000", System.IO.Path.Combine("Measures", "20261005")));
    }

    [Test]
    public void KeepAlive_PreventsTheDeath_AndRefillsTheHealth()
    {
        Entity dying = _units.Create(0f, 100f, "Dummy");
        dying.AddDeathPrevention(BalanceSimulator.KeepAlive);

        Assert.IsTrue(dying.TryPreventDeath());
        Assert.AreEqual(100f, dying.health.Value);
    }

    [Test]
    public void KeepAliveIfSimulation_OnlyTheUnitsTaggedSimulation()
    {
        GameplayTag simulation = ScriptableObject.CreateInstance<GameplayTag>();
        simulation.name = TagNames.Simulation;
        Entity dummy = _units.Create(0f, 100f, "Dummy");
        dummy.AddTag(simulation);
        Entity unit = _units.Create(0f, 100f, "Unit");

        BalanceSimulator.KeepAliveIfSimulation(dummy);
        BalanceSimulator.KeepAliveIfSimulation(unit);

        Assert.IsTrue(dummy.TryPreventDeath());
        Assert.IsFalse(unit.TryPreventDeath());
        Object.DestroyImmediate(simulation);
    }

    [Test]
    public void KeepAliveIfSimulation_FixedTeamDies_ThePlayerUnitsDie_TheEnemiesStillDont()
    {
        GameplayTag simulation = ScriptableObject.CreateInstance<GameplayTag>();
        simulation.name = TagNames.Simulation;
        Entity dummy = _units.Create(0f, 100f, "Dummy");
        dummy.AddTag(simulation);
        dummy.entityType = Entity.EntityType.Player;
        Entity attacker = _units.Create(0f, 100f, "Attacker");
        attacker.AddTag(simulation);
        attacker.entityType = Entity.EntityType.Computer;

        BalanceSimulator.KeepAliveIfSimulation(dummy, true);
        BalanceSimulator.KeepAliveIfSimulation(attacker, true);

        Assert.IsFalse(dummy.TryPreventDeath());
        Assert.IsTrue(attacker.TryPreventDeath());
        Object.DestroyImmediate(simulation);
    }

    // A buff handler of a fake buff logging into log, tagged with tag when given
    static ABuffHandlerFactory CreateHandler(Buff.FakeBuffData log, GameplayTag tag, List<Object> created)
    {
        Buff.FakeBuffFactory buffFactory = ScriptableObject.CreateInstance<Buff.FakeBuffFactory>();
        buffFactory.data = log;
        BuffHandlerFactory handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory> { buffFactory },
            tags = tag != null ? new List<GameplayTag> { tag } : new List<GameplayTag>(),
        };
        created.Add(buffFactory);
        created.Add(handlerFactory);
        return handlerFactory;
    }

    [Test]
    public void KeepAlive_AsANewUnit_TheBuffsGainedInTheFightAreGone_ThePermanentOnesStay()
    {
        List<Object> created = new List<Object>();
        GameplayTag permanent = ScriptableObject.CreateInstance<GameplayTag>();
        permanent.name = TagNames.Permanent;
        created.Add(permanent);
        Entity dummy = _units.Create(0f, 100f, "Dummy");
        BuffManager buffManager = dummy.GetComponent<BuffManager>();
        TestHelpers.SetPrivateField(dummy, "_buffManager", buffManager);
        Buff.FakeBuffData poison = new Buff.FakeBuffData();
        Buff.FakeBuffData item = new Buff.FakeBuffData();
        buffManager.AddHandler(CreateHandler(poison, null, created), dummy.gameObject, dummy.gameObject);
        buffManager.AddHandler(CreateHandler(item, permanent, created), dummy.gameObject, dummy.gameObject);
        buffManager.ForceUpdate();

        Assert.IsTrue(BalanceSimulator.KeepAlive(dummy));

        CollectionAssert.AreEqual(new[] { "Add", "Remove" }, poison.log);
        CollectionAssert.AreEqual(new[] { "Add" }, item.log);
        Assert.AreEqual(100f, dummy.health.Value);
        foreach (Object asset in created)
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void KeepAlive_EveryTime()
    {
        Entity dying = _units.Create(0f, 100f, "Dummy");
        dying.AddDeathPrevention(BalanceSimulator.KeepAlive);
        dying.TryPreventDeath();
        dying.health.SetValue(0f);

        Assert.IsTrue(dying.TryPreventDeath());
        Assert.AreEqual(100f, dying.health.Value);
    }
}

}
