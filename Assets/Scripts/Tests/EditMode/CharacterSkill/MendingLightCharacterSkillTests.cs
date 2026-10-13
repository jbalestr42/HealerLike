using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// The allies come from the EntityManager singleton in the game
public class TestMendingLightCharacterSkill : MendingLightCharacterSkill
{
    public List<GameObject> allies = new List<GameObject>();

    protected override List<GameObject> GetAllies() => allies;
}

// Mending Light: heals the target, then jumps right away to the most wounded ally not healed yet, each unit healed
// giving 1 Devotion
public class MendingLightCharacterSkillTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _characterGo;
    Character _character;
    ResourceAttribute _devotion;
    ConsumerFactory _heal;
    ConsumerFactory _gain;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        _devotion = TestHelpers.CreateResourceAttribute(_characterGo, ResourceType.Devotion, AttributeType.DevotionMax, 5f);
        _devotion.SetValue(0f);
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its event is used
        TestHelpers.WithLoggingDisabled(() => _character = _characterGo.AddComponent<Character>());

        // Heals 20, never critical
        _heal = ScriptableObject.CreateInstance<ConsumerFactory>();
        _heal.data = new ConsumerData { canBeCritical = false, value = new FlatValue { data = new FlatValueData { value = -20f } } };
        // Gives 1
        _gain = ScriptableObject.CreateInstance<ConsumerFactory>();
        _gain.data = new ConsumerData { canBeCritical = false, ignoreDamageReduction = true, value = new FlatValue { data = new FlatValueData { value = -1f } } };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_heal);
        Object.DestroyImmediate(_gain);
    }

    TestMendingLightCharacterSkill CreateSkill(int jumps, params Entity[] allies)
    {
        TestMendingLightCharacterSkill skill = new TestMendingLightCharacterSkill
        {
            data = new MendingLightCharacterSkillData { consumer = _heal, multiplier = 1f, jumps = jumps, gainedResource = ResourceType.Devotion, gainPerHeal = _gain },
        };
        foreach (Entity ally in allies)
        {
            skill.allies.Add(ally.gameObject);
        }
        return skill;
    }

    static List<GameObject> GameObjects(params Entity[] entities)
    {
        return new List<Entity>(entities).ConvertAll(entity => entity.gameObject);
    }

    [Test]
    public void GetJumps_TheMostWoundedFirst_NeverTheTarget()
    {
        Entity target = _units.Create(10f, 100f, "Target");
        Entity half = _units.Create(50f, 100f, "Half");
        Entity low = _units.Create(20f, 100f, "Low");
        Entity almostFull = _units.Create(90f, 100f, "Almost Full");

        List<Entity> jumps = MendingLightCharacterSkill.GetJumps(target.gameObject, GameObjects(target, half, low, almostFull), 3);

        CollectionAssert.AreEqual(new[] { low, half, almostFull }, jumps);
    }

    [Test]
    public void GetJumps_NoMoreThanTheJumps()
    {
        Entity target = _units.Create(10f, 100f, "Target");
        Entity a = _units.Create(50f, 100f, "A");
        Entity b = _units.Create(20f, 100f, "B");

        List<Entity> jumps = MendingLightCharacterSkill.GetJumps(target.gameObject, GameObjects(target, a, b), 1);

        CollectionAssert.AreEqual(new[] { b }, jumps);
    }

    [Test]
    public void GetJumps_SkipsTheFullAndTheDead()
    {
        Entity target = _units.Create(10f, 100f, "Target");
        Entity full = _units.Create(100f, 100f, "Full");
        Entity dead = _units.Create(0f, 100f, "Dead");

        Assert.IsEmpty(MendingLightCharacterSkill.GetJumps(target.gameObject, GameObjects(target, full, dead), 3));
    }

    [Test]
    public void Mend_HealsTheTargetAndTheJumps_EachGiving1Devotion_AndCountingAsATarget()
    {
        Entity target = _units.Create(10f, 100f, "Target");
        Entity wounded = _units.Create(30f, 100f, "Wounded");
        Entity lessWounded = _units.Create(60f, 100f, "Less Wounded");
        Entity full = _units.Create(100f, 100f, "Full");
        List<GameObject> targets = new List<GameObject>();
        _character.OnSkillAppliedOnTarget.AddListener(targets.Add);
        TestMendingLightCharacterSkill skill = CreateSkill(3, target, wounded, lessWounded, full);

        skill.Mend(_characterGo, target.gameObject);
        foreach (Entity unit in new[] { target, wounded, lessWounded, full })
        {
            TestUnits.Process(unit.health);
        }
        TestHelpers.InvokePrivate(_devotion, "Update");

        Assert.AreEqual(30f, target.health.Value, 0.01f);
        Assert.AreEqual(50f, wounded.health.Value, 0.01f);
        Assert.AreEqual(80f, lessWounded.health.Value, 0.01f);
        Assert.AreEqual(100f, full.health.Value, 0.01f);
        Assert.AreEqual(3f, _devotion.Value, 0.001f);
        CollectionAssert.AreEqual(new[] { target.gameObject, wounded.gameObject, lessWounded.gameObject }, targets);
    }
}

}
