using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

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

    // Halo: no part of the max health, 1 health
    [Test]
    public void Death_FlatHealth_AddedToTheRatio()
    {
        _buff.data.healthRatio = 0f;
        _buff.data.flatHealth = 1f;

        _owner.TryPreventDeath();
        TestUnits.Process(_owner.health);

        Assert.AreEqual(1f, _owner.health.Value, 0.0001f);
    }

    [Test]
    public void Death_FlatHealthAndRatio_BothHealed()
    {
        _buff.data.flatHealth = 5f;

        _owner.TryPreventDeath();
        TestUnits.Process(_owner.health);

        Assert.AreEqual(35f, _owner.health.Value, 0.0001f);
    }

    // Halo: invincible for a while, from itself
    [Test]
    public void Death_WithABuff_TheHolderGetsIt()
    {
        BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Duration, duration = 2f, maxStacks = 1, buffFactoryList = new List<ABuffFactory>(), tags = new List<GameplayTag>() };
        try
        {
            _buff.data.buffHandlerFactory = handler;
            BuffManager buffManager = _owner.GetComponent<BuffManager>();
            buffManager.isEnabled = true;

            _owner.TryPreventDeath();
            buffManager.ForceUpdate();

            BuffManager.BuffHandlerData given = buffManager.GetActiveHandlers().Find(h => h.buffHandlerFactory == handler);
            Assert.IsNotNull(given);
            Assert.AreSame(_owner.gameObject, given.source);
        }
        finally
        {
            Object.DestroyImmediate(handler);
        }
    }

    // Halo: kept alive and blessed, from itself
    [Test]
    public void Death_WithAStatus_TheHolderGetsIt()
    {
        BuffHandlerFactory statusHandler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        statusHandler.data = new BuffHandlerData { durationType = DurationType.Duration, duration = 4f, maxStacks = 1, buffFactoryList = new List<ABuffFactory>(), tags = new List<GameplayTag>() };
        StatusData status = ScriptableObject.CreateInstance<StatusData>();
        status.buffHandlerFactory = statusHandler;
        try
        {
            _buff.data.status = status;
            BuffManager buffManager = _owner.GetComponent<BuffManager>();
            buffManager.isEnabled = true;

            _owner.TryPreventDeath();
            buffManager.ForceUpdate();

            BuffManager.BuffHandlerData given = buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == statusHandler);
            Assert.IsNotNull(given);
            Assert.AreSame(_owner.gameObject, given.source);
        }
        finally
        {
            Object.DestroyImmediate(status);
            Object.DestroyImmediate(statusHandler);
        }
    }

    // Phylactery: no buff nor status, nothing more
    [Test]
    public void Death_WithoutBuffNorStatus_GivesNoBuff()
    {
        BuffManager buffManager = _owner.GetComponent<BuffManager>();
        buffManager.isEnabled = true;

        _owner.TryPreventDeath();
        buffManager.ForceUpdate();

        CollectionAssert.IsEmpty(buffManager.GetActiveHandlers());
    }

    [Test]
    public void Death_AfterRemove_IsNotPrevented()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Assert.IsFalse(_owner.TryPreventDeath());
    }
}

}
