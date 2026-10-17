using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// A named part of the cycle of a ConfigurableSkill (e.g. a wind-up then a slam): its steps played once each, in order
public class PhaseSkillStepTests
{
    readonly List<Object> _objects = new List<Object>();
    ConfigurableSkill _skill;

    [SetUp]
    public void SetUp()
    {
        GameObject go = Track(new GameObject("Unit"));
        _skill = go.AddComponent<ConfigurableSkill>();
        _skill.data = new ConfigurableSkillData();
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

    PhaseSkillStep CreatePhase(params ASkillStepFactory[] steps)
    {
        PhaseSkillStepFactory factory = Track(ScriptableObject.CreateInstance<PhaseSkillStepFactory>());
        factory.data = new PhaseSkillStepData { title = "Slam", skillStepFactories = new List<ASkillStepFactory>(steps) };
        PhaseSkillStep phase = (PhaseSkillStep)factory.AddSkillStep(_skill.gameObject);
        phase.Init();
        phase.Reset();
        return phase;
    }

    [Test]
    public void Duration_IsTheSumOfItsSteps()
    {
        PhaseSkillStep phase = CreatePhase(CreateWait(2f), CreateWait(0.5f));

        Assert.AreEqual(2.5f, phase.GetDuration(), 0.0001f);
    }

    [Test]
    public void BeforeTheEndOfItsSteps_IsNotDone()
    {
        PhaseSkillStep phase = CreatePhase(CreateWait(2f), CreateWait(0.5f));
        float deltaTime = 2.25f;

        Assert.IsFalse(phase.Update(_skill, ref deltaTime));
        Assert.AreEqual(2.25f, phase.GetElapsed(), 0.0001f);
    }

    [Test]
    public void AtTheEndOfItsSteps_IsDone_TheTimeLeftGoingOn()
    {
        PhaseSkillStep phase = CreatePhase(CreateWait(2f), CreateWait(0.5f));
        float deltaTime = 3f;

        Assert.IsTrue(phase.Update(_skill, ref deltaTime));
        Assert.AreEqual(0.5f, deltaTime, 0.0001f);
    }

    [Test]
    public void Progress_IsThePartOfThePhasePlayed()
    {
        PhaseSkillStep phase = CreatePhase(CreateWait(2f));
        float deltaTime = 0.5f;

        phase.Update(_skill, ref deltaTime);

        Assert.AreEqual(0.25f, phase.GetProgress(), 0.0001f);
    }

    [Test]
    public void Reset_StartsThePhaseAgain()
    {
        PhaseSkillStep phase = CreatePhase(CreateWait(2f));
        float deltaTime = 1f;
        phase.Update(_skill, ref deltaTime);

        phase.Reset();

        Assert.AreEqual(0f, phase.GetProgress(), 0.0001f);
    }

    [Test]
    public void WithoutSteps_IsInstant()
    {
        PhaseSkillStep phase = CreatePhase();
        float deltaTime = 1f;

        Assert.IsTrue(phase.Update(_skill, ref deltaTime));
        Assert.AreEqual(1f, deltaTime, 0.0001f);
        Assert.AreEqual(0f, phase.GetDuration());
        Assert.AreEqual(0f, phase.GetProgress());
    }

    [Test]
    public void ShootsAtTargets_OnlyWhenOneOfItsStepsDoes()
    {
        PhaseSkillStepFactory waiting = Track(ScriptableObject.CreateInstance<PhaseSkillStepFactory>());
        waiting.data = new PhaseSkillStepData { skillStepFactories = new List<ASkillStepFactory> { CreateWait(1f) } };
        PhaseSkillStepFactory shooting = Track(ScriptableObject.CreateInstance<PhaseSkillStepFactory>());
        shooting.data = new PhaseSkillStepData
        {
            skillStepFactories = new List<ASkillStepFactory> { CreateWait(1f), Track(ScriptableObject.CreateInstance<ShootProjectileSkillStepFactory>()) },
        };

        Assert.IsFalse(waiting.shootsAtTargets);
        Assert.IsTrue(shooting.shootsAtTargets);
    }
}

}
