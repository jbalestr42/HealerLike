using System.Collections.Generic;
using NUnit.Framework;
using Skills;
using UnityEngine;

namespace Buff
{

// Instant: brings the next use of every skill of the holder closer (e.g. the Purifier when it gets blessed)
public class ReduceCooldownBuffTests
{
    GameObject _holder;

    [SetUp]
    public void SetUp()
    {
        _holder = new GameObject("Holder");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_holder);
    }

    // Just used: 4s before the next use
    FakeCooldownSkill AddUsedSkill()
    {
        FakeCooldownSkill skill = _holder.AddComponent<FakeCooldownSkill>();
        skill.data = new SkillDataBase { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>() };
        skill.Tick(_holder, 0f);
        return skill;
    }

    static ReduceCooldownBuff CreateBuff(float seconds)
    {
        return new ReduceCooldownBuff { data = new ReduceCooldownBuffData { seconds = seconds } };
    }

    [Test]
    public void Instant_TakesTheTimeOffTheCooldownOfTheSkill()
    {
        FakeCooldownSkill skill = AddUsedSkill();

        CreateBuff(1f).Instant(_holder, _holder);

        Assert.AreEqual(3f, skill.timeBeforeUse, 0.0001f);
    }

    [Test]
    public void Instant_EverySkillOfTheHolder()
    {
        FakeCooldownSkill first = AddUsedSkill();
        FakeCooldownSkill second = AddUsedSkill();

        CreateBuff(1.5f).Instant(_holder, _holder);

        Assert.AreEqual(2.5f, first.timeBeforeUse, 0.0001f);
        Assert.AreEqual(2.5f, second.timeBeforeUse, 0.0001f);
    }

    // Given twice (e.g. blessed twice), twice as much
    [Test]
    public void Instant_Twice_AddsUp()
    {
        FakeCooldownSkill skill = AddUsedSkill();
        ReduceCooldownBuff buff = CreateBuff(1f);

        buff.Instant(_holder, _holder);
        buff.Instant(_holder, _holder);

        Assert.AreEqual(2f, skill.timeBeforeUse, 0.0001f);
    }

    [Test]
    public void Instant_OnlyTheHolder()
    {
        FakeCooldownSkill skill = AddUsedSkill();
        GameObject other = new GameObject("Other");
        try
        {
            CreateBuff(1f).Instant(_holder, other);

            Assert.AreEqual(4f, skill.timeBeforeUse, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(other);
        }
    }

    [Test]
    public void Instant_WithoutTarget_NothingHappens()
    {
        Assert.DoesNotThrow(() => CreateBuff(1f).Instant(_holder, null));
    }
}

}
