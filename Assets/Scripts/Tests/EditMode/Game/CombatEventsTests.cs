using NUnit.Framework;

namespace Game
{

public class CombatEventsTests
{
    [TearDown]
    public void TearDown()
    {
        CombatEvents.Clear();
    }

    [Test]
    public void Clear_NoListenerCalledByTheNextFight()
    {
        int calls = 0;
        CombatEvents.OnEntitySpawned.AddListener(_ => calls++);
        CombatEvents.OnProjectileSpawned.AddListener(_ => calls++);
        CombatEvents.OnRoundStart.AddListener(() => calls++);
        CombatEvents.OnBattleStart.AddListener(() => calls++);
        CombatEvents.OnBattleEnd.AddListener(_ => calls++);
        CombatEvents.OnRoundEnd.AddListener(() => calls++);
        CombatEvents.OnEntityKilled.AddListener(_ => calls++);
        CombatEvents.OnEntitySummoned.AddListener(_ => calls++);

        CombatEvents.Clear();
        CombatEvents.OnEntitySpawned.Invoke(null);
        CombatEvents.OnProjectileSpawned.Invoke(null);
        CombatEvents.OnRoundStart.Invoke();
        CombatEvents.OnBattleStart.Invoke();
        CombatEvents.OnBattleEnd.Invoke(true);
        CombatEvents.OnRoundEnd.Invoke();
        CombatEvents.OnEntityKilled.Invoke(null);
        CombatEvents.OnEntitySummoned.Invoke(null);

        Assert.AreEqual(0, calls);
    }
}

}
