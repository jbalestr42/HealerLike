using NUnit.Framework;

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
    public void KeepAlive_PreventsTheDeath_AndRefillsTheHealth()
    {
        Entity dying = _units.Create(0f, 100f, "Dummy");
        dying.AddDeathPrevention(BalanceSimulator.KeepAlive);

        Assert.IsTrue(dying.TryPreventDeath());
        Assert.AreEqual(100f, dying.health.Value);
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
