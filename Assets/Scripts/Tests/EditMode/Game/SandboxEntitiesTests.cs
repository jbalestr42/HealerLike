using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the entity data of the sandbox, which lists every entity of the game
public class SandboxEntitiesTests
{
    const string SandboxDataPath = "Assets/Data/SandboxData.asset";

    SandboxData _sandboxData;

    [SetUp]
    public void SetUp()
    {
        _sandboxData = AssetDatabase.LoadAssetAtPath<SandboxData>(SandboxDataPath);
    }

    [Test]
    public void SandboxData_IsFound()
    {
        Assert.IsNotNull(_sandboxData, SandboxDataPath);
        CollectionAssert.IsNotEmpty(_sandboxData.entities);
    }

    [Test]
    public void EveryEntity_HasATitleAttributesAndValidReferences()
    {
        List<string> invalid = new List<string>();
        foreach (EntityData entity in _sandboxData.entities)
        {
            if (entity == null)
            {
                invalid.Add("null entity");
                continue;
            }

            bool isValid = !string.IsNullOrEmpty(entity.title)
                && entity.attributes.Count > 0
                && entity.skillFactories != null && !entity.skillFactories.Contains(null)
                && !entity.items.Contains(null);
            if (!isValid)
            {
                invalid.Add(entity.name);
            }
        }

        CollectionAssert.IsEmpty(invalid, "Entities with a missing title, attribute, skill or item");
    }

    // Without these pieces the entity crashes when it shoots or has no health bar
    [Test]
    public void EveryEntityModel_HasTheComponentsNeededByAnEntity()
    {
        List<string> invalid = new List<string>();
        foreach (EntityData entity in _sandboxData.entities)
        {
            if (entity == null)
            {
                continue;
            }

            GameObject model = entity.model;
            bool isValid = model != null
                && model.GetComponent<EntityModel>() != null
                && model.GetComponentInChildren<SkillSource>(true) != null
                && model.GetComponentInChildren<SkillTargetPointTag>(true) != null
                && model.GetComponentInChildren<EntityHUD>(true) != null;
            if (!isValid)
            {
                invalid.Add($"{entity.name} ({(model != null ? model.name : "no model")})");
            }
        }

        CollectionAssert.IsEmpty(invalid, "Entities whose model misses EntityModel, SkillSource, SkillTargetPointTag or EntityHUD");
    }

    // A summoned entity must be a real, playable entity, and the summons must be limited
    [Test]
    public void EverySummonSkill_SummonsASandboxEntityAndIsLimited()
    {
        List<string> invalid = new List<string>();
        foreach (EntityData entity in _sandboxData.entities)
        {
            if (entity == null)
            {
                continue;
            }

            foreach (ASkillFactory skillFactory in entity.skillFactories)
            {
                if (skillFactory is SummonSkillFactory summon)
                {
                    bool isValid = summon.data.entity != null
                        && _sandboxData.entities.Contains(summon.data.entity)
                        && summon.data.maxAlive >= 1
                        && summon.data.cooldown > 0f;
                    if (!isValid)
                    {
                        invalid.Add($"{entity.name} ({skillFactory.name})");
                    }
                }
            }
        }

        CollectionAssert.IsEmpty(invalid, "Summon skills without a sandbox entity, a max alive or a cooldown");
    }

    // A training target: only in the sandbox, it never attacks and never dies
    [Test]
    public void PunchingBag_IsASandboxOnlyTargetThatNeverAttacks()
    {
        EntityData bag = _sandboxData.entities.Find(entity => entity != null && entity.name == "PunchingBagEntity");
        Assert.IsNotNull(bag);
        CollectionAssert.IsEmpty(bag.skillFactories);
        CollectionAssert.IsNotEmpty(bag.items, "The regen item");
        Assert.GreaterOrEqual(bag.attributes[AttributeType.HealthMax], 1000f);

        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset");
        foreach (GameData.WavePool pool in gameData.wavePools)
        {
            foreach (WavePatternData wave in pool.wavePatterns)
            {
                foreach (EntitySlot slot in wave.slots)
                {
                    Assert.AreNotEqual(bag, slot.entity, $"{wave.name} is a wave of the run");
                }
            }
        }
    }

    [Test]
    public void Necromancer_RaisesUpToTwoFrailSkeletons()
    {
        EntityData necromancer = _sandboxData.entities.Find(entity => entity != null && entity.name == "NecromancerEntity");
        Assert.IsNotNull(necromancer);

        SummonSkillFactory summon = necromancer.skillFactories.Find(skill => skill is SummonSkillFactory) as SummonSkillFactory;
        Assert.IsNotNull(summon);
        Assert.AreEqual("SkeletonEntity", summon.data.entity.name);
        Assert.AreEqual(2, summon.data.maxAlive);
        // Frailer than the Necromancer itself
        Assert.Less(summon.data.entity.attributes[AttributeType.HealthMax], necromancer.attributes[AttributeType.HealthMax]);
    }
}

}
