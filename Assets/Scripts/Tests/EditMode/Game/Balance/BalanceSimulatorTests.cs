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
