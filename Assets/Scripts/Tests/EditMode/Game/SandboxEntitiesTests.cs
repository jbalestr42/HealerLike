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
}

}
