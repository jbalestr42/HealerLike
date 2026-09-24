using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

public class FakeCooldownSkill : ACooldownSkill<SkillDataBase>
{
    public override float cooldownDuration => 4f;
    public override bool Execute(GameObject source) => true;
}

public class ACooldownSkillTests
{
    GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Skill");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    FakeCooldownSkill CreateSkill()
    {
        FakeCooldownSkill skill = _go.AddComponent<FakeCooldownSkill>();
        skill.data = new SkillDataBase { onSkillTriggerFactory = new List<AOnSkillTriggerFactory>() };
        return skill;
    }

    [Test]
    public void GetComponent_FindsTheSkillAsICooldownSkill()
    {
        FakeCooldownSkill skill = CreateSkill();

        ICooldownSkill cooldownSkill = _go.GetComponent<ICooldownSkill>();

        Assert.AreSame(skill, cooldownSkill);
    }

    [Test]
    public void CooldownProgress_BeforeFirstUse_IsZero()
    {
        ICooldownSkill cooldownSkill = CreateSkill();

        Assert.AreEqual(0f, cooldownSkill.cooldownProgress);
    }

    [Test]
    public void CooldownProgress_AfterUse_ReportsTheSkillProgressThroughTheInterface()
    {
        FakeCooldownSkill skill = CreateSkill();
        ICooldownSkill cooldownSkill = skill;

        skill.UpdateBehaviour(_go);

        Assert.AreEqual(skill.cooldownProgress, cooldownSkill.cooldownProgress);
        Assert.AreEqual(1f, cooldownSkill.cooldownProgress); // cooldown 4 / duration 4
    }
}

}
