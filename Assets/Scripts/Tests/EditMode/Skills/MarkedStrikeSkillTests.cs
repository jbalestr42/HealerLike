using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// The telegraphed strike of the Marker Titan: marks a unit, then strikes it after a delay
public class MarkedStrikeSkillTests
{
    // The targets are given by the test, the real ones are picked among the entities of the EntityManager
    class TestMarkedStrikeSkill : MarkedStrikeSkill
    {
        public GameObject nextTarget;
        // Picked after nextTarget, in this order
        public List<GameObject> nextOthers = new List<GameObject>();

        protected override List<GameObject> FindTargets(int count)
        {
            List<GameObject> targets = new List<GameObject>();
            if (nextTarget != null)
            {
                targets.Add(nextTarget);
                targets.AddRange(nextOthers);
            }
            return targets.GetRange(0, Mathf.Min(count, targets.Count));
        }
    }

    const float Interval = 8f;
    const float Delay = 3f;

    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _assets = new List<Object>();
    TestMarkedStrikeSkill _skill;
    Entity _boss;
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

        _boss = _units.Create(2000f, 2000f, "Boss");
        _skill = _boss.gameObject.AddComponent<TestMarkedStrikeSkill>();
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

    // The phases of the boss shorten the interval through this multiplier
    [Test]
    public void SkillCooldownMultiplier_ShortensTheInterval()
    {
        _boss.GetComponent<AttributeManager>().Add(AttributeType.SkillCooldownMultiplier, new Attribute(0.75f));

        _skill.Tick(5.9f);
        Assert.IsFalse(_skill.isMarking);
        _skill.Tick(0.1f);

        Assert.IsTrue(_skill.isMarking);
        Assert.AreEqual(6f, _skill.cooldownDuration, 0.0001f);
        Assert.AreEqual("every 6.0s", EntityInfoFormatter.FormatMarkedStrike(_skill).Substring(0, 10));
    }

    [Test]
    public void MissingHealthBonus_AtFullHealth_LeavesTheStrikeUnchanged()
    {
        _skill.data.missingHealthDamageBonus = 0.5f;

        _skill.Tick(Interval);
        _skill.Tick(Delay);
        ProcessHits();

        Assert.AreEqual(20f, _target.health.Value, 0.0001f);
    }

    [Test]
    public void MissingHealthBonus_GrowsTheStrikeWithTheMissingHealthOfTheEntity()
    {
        _skill.data.missingHealthDamageBonus = 0.25f;
        _boss.health.SetValue(1000f);

        _skill.Tick(Interval);
        _skill.Tick(Delay);
        ProcessHits();

        // 80 x (1 + 0.25 x 50% missing) = 90
        Assert.AreEqual(10f, _target.health.Value, 0.0001f);
    }

    [Test]
    public void LongUpdate_TheTimeBeyondTheIntervalCountsForTheDelay()
    {
        _skill.Tick(Interval + 1f);

        Assert.IsTrue(_skill.isMarking);
        Assert.AreEqual(Delay - 1f, _skill.remainingDelay, 0.0001f);
    }

    [Test]
    public void LongUpdate_TheTimeBeyondTheDelayCountsForTheNextInterval()
    {
        _skill.Tick(Interval);
        _skill.Tick(Delay + 2f);

        Assert.AreEqual(1, _struck.Count);
        Assert.AreEqual(Interval - 2f, _skill.remainingInterval, 0.0001f);
    }

    [Test]
    public void NobodyToMarkForAWhile_TheDelayStartsAtTheMark()
    {
        _skill.nextTarget = null;
        _skill.Tick(Interval);
        _skill.Tick(5f);

        _skill.nextTarget = _target.gameObject;
        _skill.Tick(0.1f);

        // At most the time of the update that marked is counted, not the time spent waiting
        Assert.AreEqual(Delay - 0.1f, _skill.remainingDelay, 0.0001f);
    }

    // The first marked unit takes the whole strike, the second half of it, the third a quarter
    void MarkThreeUnits(out Entity second, out Entity third)
    {
        second = _units.Create(100f, 100f, "Second");
        third = _units.Create(100f, 100f, "Third");
        _skill.nextOthers.Add(second.gameObject);
        _skill.nextOthers.Add(third.gameObject);
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f, 0.25f };
        _skill.Tick(Interval);
    }

    [Test]
    public void SeveralMultipliers_MarksAsManyUnits_InTheOrderOfTheTargeting()
    {
        MarkThreeUnits(out Entity second, out Entity third);

        CollectionAssert.AreEqual(new[] { _target.gameObject, second.gameObject, third.gameObject }, _marked);
        Assert.AreEqual(3, _skill.marks.Count);
        Assert.AreSame(_target.gameObject, _skill.markedTarget);
        Assert.AreEqual(1f, _skill.GetDamageMultiplier(_target.gameObject));
        Assert.AreEqual(0.5f, _skill.GetDamageMultiplier(second.gameObject));
        Assert.AreEqual(0.25f, _skill.GetDamageMultiplier(third.gameObject));
    }

    [Test]
    public void SeveralMarkedUnits_EachTakesItsPartOfTheStrike()
    {
        MarkThreeUnits(out Entity second, out Entity third);

        _skill.Tick(Delay);
        ProcessHits();
        TestUnits.Process(second.health);
        TestUnits.Process(third.health);

        // 80% of their max health, then half of it, then a quarter
        Assert.AreEqual(20f, _target.health.Value, 0.0001f);
        Assert.AreEqual(60f, second.health.Value, 0.0001f);
        Assert.AreEqual(80f, third.health.Value, 0.0001f);
        CollectionAssert.AreEqual(new[] { _target.gameObject, second.gameObject, third.gameObject }, _struck);
        Assert.IsFalse(_skill.isMarking);
        Assert.AreEqual(0f, _skill.GetDamageMultiplier(second.gameObject), "no mark left");
    }

    [Test]
    public void FewerUnitsThanMultipliers_MarksTheOnesThere_TheFirstTakingTheWholeStrike()
    {
        _skill.data.targetDamageMultipliers = new List<float> { 1f, 0.5f, 0.25f };

        _skill.Tick(Interval);
        _skill.Tick(Delay);
        ProcessHits();

        Assert.AreEqual(1, _marked.Count);
        Assert.AreEqual(20f, _target.health.Value, 0.0001f);
    }

    [Test]
    public void OneOfTheMarkedUnitsDestroyed_TheOthersAreStillStruck()
    {
        MarkThreeUnits(out Entity second, out Entity third);

        Object.DestroyImmediate(_target.gameObject);
        _skill.Tick(0.1f);
        Assert.IsTrue(_skill.isMarking);
        Assert.AreSame(second.gameObject, _skill.markedTarget);

        _skill.Tick(Delay);
        TestUnits.Process(second.health);

        CollectionAssert.AreEqual(new[] { second.gameObject, third.gameObject }, _struck);
        Assert.AreEqual(60f, second.health.Value, 0.0001f);
    }

    [Test]
    public void SkillLine_WhileMarkingSeveralUnits_ShowsTheirPartOfTheStrike()
    {
        MarkThreeUnits(out _, out _);
        _skill.Tick(1.2f);

        Assert.AreEqual("every 8.0s, strikes 3.0s after the mark · striking Target, Second (x0.5), Third (x0.25) in 1.8s", EntityInfoFormatter.FormatMarkedStrike(_skill));
    }

    [Test]
    public void Data_OneUnitPerMultiplier_TheWholeStrikeWithoutMultiplier()
    {
        MarkedStrikeSkillData data = new MarkedStrikeSkillData();
        Assert.AreEqual(1, data.targetCount, "one unit by default");
        Assert.AreEqual(1f, data.GetTargetDamageMultiplier(0));

        data.targetDamageMultipliers = new List<float> { 1f, 0.5f };
        Assert.AreEqual(2, data.targetCount);
        Assert.AreEqual(0.5f, data.GetTargetDamageMultiplier(1));
        Assert.AreEqual(1f, data.GetTargetDamageMultiplier(5));

        data.targetDamageMultipliers = new List<float>();
        Assert.AreEqual(1, data.targetCount, "at least one unit");
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
