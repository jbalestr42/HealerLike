using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// The telegraphed strike of the Marker Titan: marks a unit, then strikes it after a delay
public class MarkedStrikeSkillTests
{
    // The target is given by the test, the real one is picked among the entities of the EntityManager
    class TestMarkedStrikeSkill : MarkedStrikeSkill
    {
        public GameObject nextTarget;

        protected override GameObject FindTarget()
        {
            return nextTarget;
        }
    }

    const float Interval = 8f;
    const float Delay = 3f;

    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _assets = new List<Object>();
    TestMarkedStrikeSkill _skill;
    Entity _target;
    readonly List<GameObject> _marked = new List<GameObject>();
    readonly List<GameObject> _struck = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        // 80% of the max health of the unit struck
        ConsumerFactory strike = ScriptableObject.CreateInstance<ConsumerFactory>();
        strike.data = new ConsumerData
        {
            value = new MaxHealthValue { data = new MaxHealthValueData { multiplier = 0.8f } },
            valueOwner = ConsumerValueOwner.Target,
        };
        _assets.Add(strike);

        Entity boss = _units.Create(2000f, 2000f, "Boss");
        _skill = boss.gameObject.AddComponent<TestMarkedStrikeSkill>();
        _skill.data = new MarkedStrikeSkillData { interval = Interval, delay = Delay, strikeConsumer = strike };
        _skill.OnMarked.AddListener(_marked.Add);
        _skill.OnStrike.AddListener(_struck.Add);

        _target = _units.Create(100f, 100f, "Target");
        _skill.nextTarget = _target.gameObject;
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object asset in _assets)
        {
            Object.DestroyImmediate(asset);
        }
        _assets.Clear();
        _marked.Clear();
        _struck.Clear();
    }

    void ProcessHits()
    {
        TestUnits.Process(_target.health);
    }

    [Test]
    public void BeforeTheInterval_NothingIsMarked()
    {
        _skill.Tick(Interval - 0.1f);

        Assert.IsFalse(_skill.isMarking);
        Assert.IsEmpty(_marked);
    }

    [Test]
    public void AfterTheInterval_MarksTheTarget_AndWaitsTheDelay()
    {
        _skill.Tick(Interval);

        Assert.AreSame(_target.gameObject, _skill.markedTarget);
        CollectionAssert.AreEqual(new[] { _target.gameObject }, _marked);
        Assert.AreEqual(Delay, _skill.remainingDelay, 0.0001f);
        Assert.IsEmpty(_struck);
    }

    [Test]
    public void AfterTheDelay_StrikesTheMarkedTarget()
    {
        _skill.Tick(Interval);

        _skill.Tick(Delay);
        ProcessHits();

        CollectionAssert.AreEqual(new[] { _target.gameObject }, _struck);
        Assert.AreEqual(20f, _target.health.Value, 0.0001f);
        Assert.IsFalse(_skill.isMarking);
    }

    [Test]
    public void BeforeTheDelay_DoesNotStrikeYet()
    {
        _skill.Tick(Interval);

        _skill.Tick(Delay - 0.1f);
        ProcessHits();

        Assert.IsEmpty(_struck);
        Assert.AreEqual(100f, _target.health.Value);
        Assert.AreEqual(0.1f, _skill.remainingDelay, 0.0001f);
    }

    [Test]
    public void MarkedTargetDeadBeforeTheStrike_TheStrikeIsLost()
    {
        _skill.Tick(Interval);
        _target.health.SetValue(0f);

        _skill.Tick(Delay);

        Assert.IsEmpty(_struck);
        Assert.IsFalse(_skill.isMarking);
    }

    [Test]
    public void MarkedTargetDestroyedBeforeTheStrike_TheStrikeIsLost()
    {
        _skill.Tick(Interval);
        Object.DestroyImmediate(_target.gameObject);

        _skill.Tick(Delay);

        Assert.IsEmpty(_struck);
        Assert.IsFalse(_skill.isMarking);
    }

    [Test]
    public void NobodyToMark_MarksAsSoonAsATargetIsThere()
    {
        _skill.nextTarget = null;
        _skill.Tick(Interval);
        Assert.IsFalse(_skill.isMarking);

        _skill.nextTarget = _target.gameObject;
        _skill.Tick(0.1f);

        Assert.AreSame(_target.gameObject, _skill.markedTarget);
    }

    [Test]
    public void AfterAStrike_TheNextMarkComesAfterAnotherInterval()
    {
        _skill.Tick(Interval);
        _skill.Tick(Delay);

        _skill.Tick(Interval - 0.1f);
        Assert.IsFalse(_skill.isMarking);

        _skill.Tick(0.1f);
        Assert.IsTrue(_skill.isMarking);
        Assert.AreEqual(2, _marked.Count);
    }

    [Test]
    public void Cooldown_IsTheIntervalBeforeTheNextMark()
    {
        Assert.AreEqual(Interval, _skill.cooldownDuration);

        _skill.Tick(2f);

        Assert.AreEqual(6f, _skill.remainingInterval, 0.0001f);
        Assert.AreEqual(6f / Interval, _skill.cooldownProgress, 0.0001f);
    }

    [Test]
    public void SkillLine_ShowsTheIntervalTheDelayAndTheNextMark()
    {
        _skill.Tick(2f);

        Assert.AreEqual("every 8.0s, strikes 3.0s after the mark · next mark in 6.0s", EntityInfoFormatter.FormatMarkedStrike(_skill));
    }

    [Test]
    public void SkillLine_WhileMarking_ShowsTheMarkedUnitAndTheTimeLeft()
    {
        _skill.Tick(Interval);
        _skill.Tick(1.2f);

        // Test units have no data: named after their game object
        Assert.AreEqual("every 8.0s, strikes 3.0s after the mark · striking Target in 1.8s", EntityInfoFormatter.FormatMarkedStrike(_skill));
        StringAssert.Contains("striking Target in 1.8s", EntityInfoFormatter.FormatSkill(_skill));
    }

    [Test]
    public void Reset_ClearsTheMarkAndRestartsTheInterval()
    {
        _skill.Tick(Interval);

        _skill.Reset();

        Assert.IsFalse(_skill.isMarking);
        _skill.Tick(Interval - 0.1f);
        Assert.IsFalse(_skill.isMarking);
    }
}

}
