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
}

}
