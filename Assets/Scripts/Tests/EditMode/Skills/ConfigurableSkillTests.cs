using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// A skill made of steps played in a loop (e.g. a burst of shots then a pause): its cooldown is a whole cycle
public class ConfigurableSkillTests
{
    readonly List<Object> _objects = new List<Object>();
    ConfigurableSkill _skill;

    [SetUp]
    public void SetUp()
    {
        // 3 waits of 0.5s (a burst), then a pause of 2s: a cycle of 3.5s
        RepeatSkillStepFactory burst = ScriptableObject.CreateInstance<RepeatSkillStepFactory>();
        burst.data = new RepeatSkillStepData { skillStepFactories = new List<ASkillStepFactory> { CreateWait(0.5f) }, count = 3 };
        _objects.Add(burst);

        GameObject go = new GameObject("Unit");
        _objects.Add(go);
        _skill = go.AddComponent<ConfigurableSkill>();
        _skill.data = new ConfigurableSkillData
        {
            onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
            skillStepFactories = new List<ASkillStepFactory> { burst, CreateWait(2f) },
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

    DurationSkillStepFactory CreateWait(float seconds)
    {
        DurationSkillStepFactory wait = ScriptableObject.CreateInstance<DurationSkillStepFactory>();
        wait.data = new DurationSkillStepData { duration = new FlatValue { data = new FlatValueData { value = seconds } } };
        _objects.Add(wait);
        return wait;
    }

    // Steps end one per update, as in the game where an update lasts a frame
    void Play(float seconds, float step = 0.5f)
    {
        for (float played = 0f; played < seconds - 0.0001f; played += step)
        {
            _skill.Tick(_skill.gameObject, step);
        }
    }

    [Test]
    public void Cooldown_IsAWholeCycleOfTheSteps()
    {
        Assert.AreEqual(3.5f, _skill.cooldownDuration, 0.0001f);
    }

    [Test]
    public void CycleStart_TheWholeCooldownIsLeft()
    {
        Assert.AreEqual(1f, _skill.cooldownProgress, 0.0001f);
    }

    [Test]
    public void DuringTheBurst_CountsTheRepeatsDone()
    {
        Play(1f);

        Assert.AreEqual(2.5f / 3.5f, _skill.cooldownProgress, 0.0001f);
    }

    [Test]
    public void DuringThePause_CountsTheWholeBurstAndThePauseSpent()
    {
        Play(1.5f);
        Play(1f, 1f);

        Assert.AreEqual(1f / 3.5f, _skill.cooldownProgress, 0.0001f);
    }

    [Test]
    public void LongUpdates_TheTimeBeyondAStepGoesOnInTheNextOnes()
    {
        // 4.5s: a whole cycle of 3.5s, then 1s of the next one (2 repeats of the burst)
        Play(4.5f, 0.75f);

        Assert.AreEqual(2.5f / 3.5f, _skill.cooldownProgress, 0.0001f);
    }

    [Test]
    public void AfterACycle_TheWholeCooldownIsLeftAgain()
    {
        Play(1.5f);
        Play(2f, 2f);

        Assert.AreEqual(1f, _skill.cooldownProgress, 0.0001f);
    }

    // In the pause after the burst: 2s left, 1s once reduced
    [Test]
    public void ReduceCooldown_TheCycleGoesOnByThatTime()
    {
        Play(1.5f);
        Assert.AreEqual(2f / 3.5f, _skill.cooldownProgress, 0.0001f);

        _skill.ReduceCooldown(1f);

        Assert.AreEqual(1f / 3.5f, _skill.cooldownProgress, 0.0001f);
    }

    [Test]
    public void ReduceCooldown_Negative_NothingChanges()
    {
        Play(1.5f);

        _skill.ReduceCooldown(-1f);

        Assert.AreEqual(2f / 3.5f, _skill.cooldownProgress, 0.0001f);
    }
}

// The phases of a ConfigurableSkill (e.g. strikes, then a slam charged for 2s), the one under way shown above the unit
public class ConfigurableSkillPhaseTests
{
    readonly List<Object> _objects = new List<Object>();
    ConfigurableSkill _skill;
    PhaseSkillStepFactory _strikes;
    PhaseSkillStepFactory _slam;

    [SetUp]
    public void SetUp()
    {
        // Strikes for 1s, a pause of 1s outside any phase, then a slam charged for 2s
        _strikes = CreatePhase("Strikes", 1f);
        _slam = CreatePhase("Slam", 2f);
        _skill = CreateSkill(_strikes, CreateWait(1f), _slam);
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

    ConfigurableSkill CreateSkill(params ASkillStepFactory[] steps)
    {
        GameObject go = Track(new GameObject("Unit"));
        ConfigurableSkill skill = go.AddComponent<ConfigurableSkill>();
        skill.data = new ConfigurableSkillData
        {
            onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
            skillStepFactories = new List<ASkillStepFactory>(steps),
        };
        TestHelpers.InvokePrivate(skill, "Start");
        return skill;
    }

    DurationSkillStepFactory CreateWait(float seconds)
    {
        DurationSkillStepFactory wait = Track(ScriptableObject.CreateInstance<DurationSkillStepFactory>());
        wait.data = new DurationSkillStepData { duration = new FlatValue { data = new FlatValueData { value = seconds } } };
        return wait;
    }

    PhaseSkillStepFactory CreatePhase(string title, float seconds)
    {
        PhaseSkillStepFactory phase = Track(ScriptableObject.CreateInstance<PhaseSkillStepFactory>());
        phase.data = new PhaseSkillStepData { title = title, skillStepFactories = new List<ASkillStepFactory> { CreateWait(seconds) } };
        return phase;
    }

    [Test]
    public void CycleStart_TheFirstPhaseJustStarts()
    {
        Assert.IsTrue(_skill.TryGetPhase(out PhaseSkillStepData phase, out float progress));
        Assert.AreSame(_strikes.data, phase);
        Assert.AreEqual(0f, progress, 0.0001f);
    }

    [Test]
    public void DuringAPhase_ThePartOfItPlayed()
    {
        _skill.Tick(_skill.gameObject, 1f);
        _skill.Tick(_skill.gameObject, 1f);
        _skill.Tick(_skill.gameObject, 0.5f);

        Assert.IsTrue(_skill.TryGetPhase(out PhaseSkillStepData phase, out float progress));
        Assert.AreSame(_slam.data, phase);
        Assert.AreEqual(0.25f, progress, 0.0001f);
    }

    // Between two phases: the next one is shown, still to come
    [Test]
    public void OutsideAnyPhase_TheNextOneNotStarted()
    {
        _skill.Tick(_skill.gameObject, 1f);
        _skill.Tick(_skill.gameObject, 0.5f);

        Assert.IsTrue(_skill.TryGetPhase(out PhaseSkillStepData phase, out float progress));
        Assert.AreSame(_slam.data, phase);
        Assert.AreEqual(0f, progress, 0.0001f);
    }

    [Test]
    public void AfterTheLastPhase_TheFirstOneAgain()
    {
        _skill.Tick(_skill.gameObject, 1f);
        _skill.Tick(_skill.gameObject, 1f);
        _skill.Tick(_skill.gameObject, 2f);

        Assert.IsTrue(_skill.TryGetPhase(out PhaseSkillStepData phase, out _));
        Assert.AreSame(_strikes.data, phase);
    }

    [Test]
    public void WithoutPhases_HasNone()
    {
        ConfigurableSkill skill = CreateSkill(CreateWait(1f));

        Assert.IsFalse(skill.TryGetPhase(out PhaseSkillStepData phase, out _));
        Assert.IsNull(phase);
    }

    // The steps are only created once the skill starts
    [Test]
    public void NotStarted_HasNone()
    {
        GameObject go = Track(new GameObject("Unit"));
        ConfigurableSkill skill = go.AddComponent<ConfigurableSkill>();
        skill.data = new ConfigurableSkillData { skillStepFactories = new List<ASkillStepFactory> { _slam } };

        Assert.IsFalse(skill.TryGetPhase(out _, out _));
    }
}

}
