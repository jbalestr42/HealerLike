using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

public class FakeCooldownSkill : ACooldownSkill<SkillDataBase>
{
    public bool canExecute = true;
    public int uses;

    public override float cooldownDuration => 4f;

    public override bool Execute(GameObject source)
    {
        if (canExecute)
        {
            uses++;
        }
        return canExecute;
    }
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

    // Game time until the first use, ticking by 0.01s
    static float TimeToFirstUse(FakeCooldownSkill skill, GameObject source)
    {
        float time = 0f;
        while (skill.uses == 0 && time < 10f)
        {
            skill.Tick(source, 0.01f);
            time += 0.01f;
        }
        return time;
    }

    [Test]
    public void FirstUse_WithoutStagger_RightAway()
    {
        FakeCooldownSkill skill = CreateSkill();

        skill.Tick(_go, 0.01f);

        Assert.AreEqual(1, skill.uses);
    }

    [Test]
    public void InitCooldown_DelaysTheFirstUseByThatTime()
    {
        FakeCooldownSkill skill = CreateSkill();

        skill.InitCooldown(1.5f);

        Assert.AreEqual(1.5f, TimeToFirstUse(skill, _go), 0.02f);
    }

    [Test]
    public void InitCooldown_Negative_UsedRightAway()
    {
        FakeCooldownSkill skill = CreateSkill();

        skill.InitCooldown(-2f);
        skill.Tick(_go, 0.01f);

        Assert.AreEqual(1, skill.uses);
    }

    [Test]
    public void Reset_AfterInitCooldown_UsedRightAway()
    {
        FakeCooldownSkill skill = CreateSkill();
        skill.InitCooldown(3f);

        skill.Reset();
        skill.Tick(_go, 0.01f);

        Assert.AreEqual(1, skill.uses);
    }

    [Test]
    public void ShootProjectileSkill_StaggersItsFirstShotAsTheGameDataSays()
    {
        GameData data = ScriptableObject.CreateInstance<GameData>();
        try
        {
            Assert.IsTrue(data.staggerFirstAttacks, "on by default");
            Assert.IsTrue(ShootProjectileSkill.StaggersFirstShot(data));
            data.staggerFirstAttacks = false;
            Assert.IsFalse(ShootProjectileSkill.StaggersFirstShot(data));
            Assert.IsTrue(ShootProjectileSkill.StaggersFirstShot(null), "on without data");
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void GameDataOfTheGame_StaggersTheFirstAttacks()
    {
        GameData data = UnityEditor.AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset");

        Assert.IsTrue(data.staggerFirstAttacks);
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

    // Uses of a 4s cooldown skill over 40s, in updates of the given length
    int CountUses(float deltaTime)
    {
        FakeCooldownSkill skill = CreateSkill();
        for (float played = 0f; played < 40f - 0.0001f; played += deltaTime)
        {
            skill.Tick(_go, deltaTime);
        }
        Object.DestroyImmediate(skill);
        return skill.uses;
    }

    [Test]
    public void Uses_AsManyWithLongUpdatesAsWithShortOnes()
    {
        // Used at 0, 4, ..., 36s
        Assert.AreEqual(10, CountUses(0.25f));
        Assert.AreEqual(10, CountUses(1f));
        Assert.AreEqual(10, CountUses(3f));
    }

    [Test]
    public void Tick_TheUpdateThatEndsTheCooldownCounts()
    {
        FakeCooldownSkill skill = CreateSkill();
        skill.Tick(_go, 1f);

        for (int i = 0; i < 4; i++)
        {
            skill.Tick(_go, 1f);
        }

        Assert.AreEqual(2, skill.uses);
    }

    [Test]
    public void Tick_NothingToUseItOn_UsedOnceItCanBe_WithoutCatchingUpTheLostUses()
    {
        FakeCooldownSkill skill = CreateSkill();
        skill.canExecute = false;
        for (int i = 0; i < 10; i++)
        {
            skill.Tick(_go, 1f);
        }

        skill.canExecute = true;
        skill.Tick(_go, 1f);
        skill.Tick(_go, 1f);

        Assert.AreEqual(1, skill.uses);
    }

    [Test]
    public void TimeBeforeUse_AfterAUse_CountsDownTheCooldown()
    {
        FakeCooldownSkill skill = CreateSkill();
        skill.Tick(_go, 0.01f);

        skill.Tick(_go, 1f);

        Assert.AreEqual(1, skill.uses);
        Assert.AreEqual(3f, skill.timeBeforeUse, 0.0001f);
    }

    [Test]
    public void TimeBeforeUse_ReadyWithoutTarget_StaysAtZero()
    {
        FakeCooldownSkill skill = CreateSkill();
        skill.canExecute = false;

        skill.Tick(_go, 1f);

        Assert.AreEqual(0f, skill.timeBeforeUse);
    }
}

}
