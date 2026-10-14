using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// A step of a ConfigurableSkill giving a buff to the unit using it, taking no time (e.g. the Armored Footman strikes
// twice, then raises its guard)
public class ApplyBuffSkillStepTests
{
    readonly List<Object> _objects = new List<Object>();
    GameObject _unit;
    BuffManager _buffManager;
    BuffHandlerFactory _guard;
    ConfigurableSkill _skill;

    [SetUp]
    public void SetUp()
    {
        _unit = Track(new GameObject("Unit"));
        _buffManager = _unit.AddComponent<BuffManager>();
        _buffManager.isEnabled = true;
        TestHelpers.CreateAttributeManager(_unit, AttributeType.HitArmor, 0f);

        // The guard: the next hit blocked
        FlatModifierFactory hitArmor = Track(ScriptableObject.CreateInstance<FlatModifierFactory>());
        hitArmor.data = new FlatModifierData { type = AttributeType.HitArmor, modifierType = AttributeModifierType.Override, value = 1f };
        _guard = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _guard.data = new BuffHandlerData { durationType = DurationType.Instant, buffFactoryList = new List<ABuffFactory> { hitArmor }, tags = new List<GameplayTag>() };

        ApplyBuffSkillStepFactory raiseGuard = Track(ScriptableObject.CreateInstance<ApplyBuffSkillStepFactory>());
        raiseGuard.data = new ApplyBuffSkillStepData { buffHandlerFactory = _guard };

        // Waits 1s, raises its guard, waits 2s
        _skill = _unit.AddComponent<ConfigurableSkill>();
        _skill.data = new ConfigurableSkillData
        {
            onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
            skillStepFactories = new List<ASkillStepFactory> { CreateWait(1f), raiseGuard, CreateWait(2f) },
        };
        TestHelpers.InvokePrivate(_skill, "Start");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T Track<T>(T obj) where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    DurationSkillStepFactory CreateWait(float seconds)
    {
        DurationSkillStepFactory wait = Track(ScriptableObject.CreateInstance<DurationSkillStepFactory>());
        wait.data = new DurationSkillStepData { duration = new FlatValue { data = new FlatValueData { value = seconds } } };
        return wait;
    }

    float HitArmor()
    {
        _buffManager.ForceUpdate();
        // Computed on the update of the attributes, every frame in the game
        Attribute hitArmor = _unit.GetComponent<AttributeManager>().Get(AttributeType.HitArmor);
        hitArmor.Update();
        return hitArmor.Value;
    }

    [Test]
    public void BeforeTheStep_NoBuff()
    {
        _skill.Tick(_unit, 0.5f);

        Assert.AreEqual(0f, HitArmor());
    }

    [Test]
    public void TheStepReached_TheUnitGetsTheBuff()
    {
        _skill.Tick(_unit, 1f);

        Assert.AreEqual(1f, HitArmor());
    }

    // The step takes no time: the cycle is the waits alone
    [Test]
    public void TakesNoTime()
    {
        Assert.AreEqual(3f, _skill.cooldownDuration, 0.0001f);
    }

    // Raised again each cycle, never more than one hit blocked (Override)
    [Test]
    public void EachCycle_TheBuffAgain()
    {
        _skill.Tick(_unit, 1f);
        HitArmor();
        _unit.GetComponent<AttributeManager>().Get(AttributeType.HitArmor).BaseValue = 0f;

        _skill.Tick(_unit, 2f);
        _skill.Tick(_unit, 1f);

        Assert.AreEqual(1f, HitArmor());
    }

    [Test]
    public void WithoutBuff_NothingAndTheCycleGoesOn()
    {
        ApplyBuffSkillStep step = new ApplyBuffSkillStep { data = new ApplyBuffSkillStepData(), source = _unit };
        float deltaTime = 0.5f;

        Assert.IsTrue(step.Update(_skill, ref deltaTime));
        Assert.AreEqual(0.5f, deltaTime);
    }
}

}
