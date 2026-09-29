using NUnit.Framework;

namespace Entities
{

// An entity about to die asks its death preventions (e.g. a revive) whether one keeps it alive
public class EntityDeathPreventionTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _entity;

    [SetUp]
    public void SetUp()
    {
        _entity = _units.Create(0f, 100f, "Dying");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void TryPreventDeath_WithoutPrevention_LetsItDie()
    {
        Assert.IsFalse(_entity.TryPreventDeath());
    }

    [Test]
    public void TryPreventDeath_APreventionKeepingItAlive_PreventsTheDeath()
    {
        _entity.AddDeathPrevention(entity => true);

        Assert.IsTrue(_entity.TryPreventDeath());
    }

    [Test]
    public void TryPreventDeath_EveryPreventionDeclining_LetsItDie()
    {
        _entity.AddDeathPrevention(entity => false);
        _entity.AddDeathPrevention(entity => false);

        Assert.IsFalse(_entity.TryPreventDeath());
    }

    [Test]
    public void TryPreventDeath_StopsAtTheFirstPreventionKeepingItAlive()
    {
        int asked = 0;
        _entity.AddDeathPrevention(entity => { asked++; return true; });
        _entity.AddDeathPrevention(entity => { asked++; return true; });

        _entity.TryPreventDeath();

        Assert.AreEqual(1, asked);
    }

    [Test]
    public void TryPreventDeath_AsksTheLastAddedPreventionFirst()
    {
        string asked = "";
        _entity.AddDeathPrevention(entity => { asked += "first"; return true; });
        _entity.AddDeathPrevention(entity => { asked += "last"; return true; });

        _entity.TryPreventDeath();

        Assert.AreEqual("last", asked);
    }

    [Test]
    public void TryPreventDeath_APreventionRemovingItself_LetsTheOthersBeAsked()
    {
        bool isOtherAsked = false;
        _entity.AddDeathPrevention(entity => { isOtherAsked = true; return false; });
        Entity.DeathPrevention oneShot = null;
        oneShot = entity => { entity.RemoveDeathPrevention(oneShot); return false; };
        _entity.AddDeathPrevention(oneShot);

        Assert.DoesNotThrow(() => _entity.TryPreventDeath());
        Assert.IsTrue(isOtherAsked);
    }

    [Test]
    public void TryPreventDeath_IsAskedWithTheDyingEntity()
    {
        Entity asked = null;
        _entity.AddDeathPrevention(entity => { asked = entity; return false; });

        _entity.TryPreventDeath();

        Assert.AreSame(_entity, asked);
    }

    [Test]
    public void TryPreventDeath_AfterRemovingThePrevention_LetsItDie()
    {
        Entity.DeathPrevention prevention = entity => true;
        _entity.AddDeathPrevention(prevention);
        _entity.RemoveDeathPrevention(prevention);

        Assert.IsFalse(_entity.TryPreventDeath());
    }
}

}
