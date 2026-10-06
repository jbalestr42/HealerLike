using Entities;
using NUnit.Framework;

namespace Buff
{

// Phylactery: the first time the holder dies in a battle, it comes back with a part of its health.
// The battle start comes from AscensionGameType in the game, the buff is rearmed directly here
public class ReviveOnDeathBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    ReviveOnDeathBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(0f, 100f, "Owner");
        _buff = new ReviveOnDeathBuff { data = new ReviveOnDeathBuffData { healthRatio = 0.3f } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);
        _units.DestroyAll();
    }

    [Test]
    public void Death_TheFirstOne_IsPreventedAndHealsTheRatioOfTheMaxHealth()
    {
        Assert.IsTrue(_owner.TryPreventDeath());
        TestUnits.Process(_owner.health);

        Assert.IsTrue(_buff.isUsed);
        Assert.AreEqual(30f, _owner.health.Value, 0.0001f);
    }

    [Test]
    public void Death_TheReviveIsAHealIgnoringArmorAndInvincibility()
    {
        _owner.TryPreventDeath();

        ResourceModifier revive = TestUnits.GetPendingModifiers(_owner)[0];
        Assert.AreSame(_owner.gameObject, revive.source);
        Assert.IsTrue(revive.consumers[0].ignoreDamageReduction);
        Assert.IsTrue(revive.consumers[0].ignoreConsumerPrevention);
    }

    [Test]
    public void Death_ASecondOneInTheSameBattle_IsNotPrevented()
    {
        _owner.TryPreventDeath();
        TestUnits.Process(_owner.health);

        Assert.IsFalse(_owner.TryPreventDeath());
    }

    [Test]
    public void Death_InTheNextBattle_IsPreventedAgain()
    {
        _owner.TryPreventDeath();
        TestUnits.Process(_owner.health);

        _buff.Rearm();

        Assert.IsTrue(_owner.TryPreventDeath());
    }

    [Test]
    public void Death_AfterRemove_IsNotPrevented()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Assert.IsFalse(_owner.TryPreventDeath());
    }
}

}
