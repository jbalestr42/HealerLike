using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// Finding every enemy in range needs the EntityManager singleton: replaced by a known list
public class KnownTargetsEqualizeStep : EqualizeHealthSkillStep
{
    public readonly List<GameObject> everyTarget = new List<GameObject>();

    protected override List<GameObject> FindEveryTarget(GameObject unit)
    {
        return everyTarget;
    }
}

// Every enemy in range set to the average share of its max health (e.g. the Arbiter: a full tank and a dying unit
// both end at half health)
public class EqualizeHealthSkillStepTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    [Test]
    public void Played_EqualizesEveryEnemyInRange_AndIsDone()
    {
        Entity full = _units.Create(100f, 100f, "Full");
        Entity wounded = _units.Create(20f, 100f, "Wounded");
        GameObject go = new GameObject("Arbiter");
        _objects.Add(go);
        ConfigurableSkill skill = go.AddComponent<ConfigurableSkill>();
        KnownTargetsEqualizeStep step = new KnownTargetsEqualizeStep { data = new EqualizeHealthSkillStepData(), source = go };
        step.everyTarget.Add(full.gameObject);
        step.everyTarget.Add(wounded.gameObject);
        float deltaTime = 0.5f;

        Assert.IsTrue(step.Update(skill, ref deltaTime));
        Assert.AreEqual(60f, full.health.Value, 0.001f);
        Assert.AreEqual(60f, wounded.health.Value, 0.001f);
        Assert.AreEqual(0.5f, deltaTime, 0.0001f, "taking no time");
    }

    [Test]
    public void Mode_IsRelativeByDefault()
    {
        Assert.AreEqual(BalanceLifeMode.Relative, new EqualizeHealthSkillStepData().mode);
    }

    // Absolute: the total health is kept, 100 out of 300 gives a third to each
    [Test]
    public void Played_Absolute_KeepsTheTotalHealth()
    {
        Entity tank = _units.Create(20f, 200f, "Tank");
        Entity small = _units.Create(80f, 100f, "Small");
        GameObject go = new GameObject("Arbiter");
        _objects.Add(go);
        ConfigurableSkill skill = go.AddComponent<ConfigurableSkill>();
        KnownTargetsEqualizeStep step = new KnownTargetsEqualizeStep
        {
            data = new EqualizeHealthSkillStepData { mode = BalanceLifeMode.Absolute },
            source = go,
        };
        step.everyTarget.Add(tank.gameObject);
        step.everyTarget.Add(small.gameObject);
        float deltaTime = 0.5f;

        Assert.IsTrue(step.Update(skill, ref deltaTime));
        Assert.AreEqual(200f / 3f, tank.health.Value, 0.001f);
        Assert.AreEqual(100f / 3f, small.health.Value, 0.001f);
    }
}

}
