using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// Without any target (shooting spawns no projectile, they need the EntityManager singleton), counting the times the
// targets of the entity are asked for
public class CountingTargetProvider : MonoBehaviour, ITargetProvider
{
    public int getTargetsCalls;

    public List<GameObject> GetTargets()
    {
        getTargetsCalls++;
        return new List<GameObject>();
    }
    public int targetCount { get; set; }
    public TargetBehaviourType targetBehaviourType { get; set; }
}

// Finding every enemy in range needs the EntityManager singleton: replaced by a known list, counting the calls
public class EveryTargetCountingStep : ShootProjectileSkillStep
{
    public int everyTargetCalls;
    public readonly List<GameObject> everyTarget = new List<GameObject>();

    protected override List<GameObject> FindEveryTarget(GameObject unit)
    {
        everyTargetCalls++;
        return everyTarget;
    }
}

// A step of a ConfigurableSkill shooting projectiles: at the targets of the entity, or at every enemy in range (e.g. the
// Ravager slamming the whole team)
public class ShootProjectileSkillStepTests
{
    readonly List<Object> _objects = new List<Object>();
    GameObject _unit;
    CountingTargetProvider _targetProvider;
    ConfigurableSkill _skill;
    readonly List<ProjectileAttack> _attacks = new List<ProjectileAttack>();

    [SetUp]
    public void SetUp()
    {
        _unit = Track(new GameObject("Unit"));
        Entity entity = null;
        // Adding Entity triggers Entity.Reset() (NREs without a full Init()), only its attacks are listened to
        TestHelpers.WithLoggingDisabled(() => entity = _unit.AddComponent<Entity>());
        entity.OnAttack.AddListener(_attacks.Add);
        _targetProvider = _unit.AddComponent<CountingTargetProvider>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
        _attacks.Clear();
    }

    T Track<T>(T obj) where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    // A skill shooting once, the step itself shot directly (the skill creates its own steps)
    EveryTargetCountingStep Shoot(bool atEveryTarget)
    {
        ShootProjectileSkillStepData data = new ShootProjectileSkillStepData
        {
            projectiles = new List<ProjectileData> { new ProjectileData() },
            atEveryTarget = atEveryTarget,
        };
        ShootProjectileSkillStepFactory shoot = Track(ScriptableObject.CreateInstance<ShootProjectileSkillStepFactory>());
        shoot.data = data;
        _skill = _unit.AddComponent<ConfigurableSkill>();
        _skill.data = new ConfigurableSkillData
        {
            onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
            skillStepFactories = new List<ASkillStepFactory> { shoot },
        };
        TestHelpers.InvokePrivate(_skill, "Start");

        EveryTargetCountingStep step = new EveryTargetCountingStep { data = data, source = _unit };
        step.Init();
        float deltaTime = 0.1f;
        step.Update(_skill, ref deltaTime);
        return step;
    }

    [Test]
    public void AtEveryTarget_ShootsAtEveryTargetInRange()
    {
        EveryTargetCountingStep step = Shoot(true);

        Assert.AreEqual(1, _attacks.Count);
        Assert.AreEqual(1, step.everyTargetCalls);
        Assert.AreSame(step.everyTarget, _attacks[0].targets);
    }

    [Test]
    public void AtEveryTarget_LeavesTheTargetsOfTheEntityAlone()
    {
        _targetProvider.targetCount = 1;

        Shoot(true);

        Assert.AreEqual(0, _targetProvider.getTargetsCalls);
        Assert.AreEqual(1, _targetProvider.targetCount);
    }

    [Test]
    public void ByDefault_ShootsAtTheTargetsOfTheEntity()
    {
        EveryTargetCountingStep step = Shoot(false);

        Assert.AreEqual(1, _attacks.Count);
        Assert.AreEqual(0, step.everyTargetCalls);
        Assert.IsNull(_attacks[0].targets, "the current targets of the entity");
        Assert.AreEqual(1, _targetProvider.getTargetsCalls);
    }
}

}
