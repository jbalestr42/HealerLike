using System.Collections.Generic;
using NUnit.Framework;

namespace Entities
{

// The last entity to damage another one is credited with its kill
public class EntityKillTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _attacker;
    Entity _victim;
    List<Entity> _kills;

    [SetUp]
    public void SetUp()
    {
        _attacker = _units.Create(100f, 100f, "Attacker");
        _victim = _units.Create(0f, 100f, "Victim");
        _kills = new List<Entity>();
        _attacker.OnKill.AddListener(_kills.Add);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void NotifyAttacker_Damage_MakesTheAttackerTheLastOne()
    {
        Entity.NotifyAttacker(_attacker.gameObject, _victim.gameObject, -10f);

        Assert.AreSame(_attacker, _victim.lastAttacker);
    }

    [Test]
    public void NotifyAttacker_ALaterAttacker_ReplacesTheLastOne()
    {
        Entity other = _units.Create(100f, 100f, "Other");

        Entity.NotifyAttacker(_attacker.gameObject, _victim.gameObject, -10f);
        Entity.NotifyAttacker(other.gameObject, _victim.gameObject, -10f);

        Assert.AreSame(other, _victim.lastAttacker);
    }

    [Test]
    public void NotifyAttacker_AHeal_DoesNotChangeTheLastOne()
    {
        Entity.NotifyAttacker(_attacker.gameObject, _victim.gameObject, 10f);

        Assert.IsNull(_victim.lastAttacker);
    }

    [Test]
    public void NotifyKiller_ReportsTheKillToTheLastAttacker()
    {
        Entity.NotifyAttacker(_attacker.gameObject, _victim.gameObject, -10f);

        _victim.NotifyKiller();

        CollectionAssert.AreEqual(new[] { _victim }, _kills);
    }

    [Test]
    public void NotifyKiller_WithoutAttacker_ReportsNothing()
    {
        Assert.DoesNotThrow(() => _victim.NotifyKiller());
        CollectionAssert.IsEmpty(_kills);
    }
}

}
