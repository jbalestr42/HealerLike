using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace UI
{

// A reward choice can be a unit: picked by chance, shown with its details, then added to the units to place
public class UnitRewardTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    EntityData CreateZealot()
    {
        EntityData zealot = ScriptableObject.CreateInstance<EntityData>();
        _objects.Add(zealot);
        zealot.title = "Zealot";
        zealot.description = "Hits harder when healthy";
        zealot.attributes = new Dictionary<AttributeType, float> { { AttributeType.HealthMax, 100f } };
        zealot.items = new List<AItemFactory>();
        return zealot;
    }

    [Test]
    public void UnitRewardChance_IsAChanceInTheGameData()
    {
        // 0 for now: the units only join through the events
        GameData data = UnityEditor.AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset");

        Assert.GreaterOrEqual(data.unitRewardChance, 0f);
        Assert.LessOrEqual(data.unitRewardChance, 1f);
    }

    [Test]
    public void GetUnitDetails_IsTheTooltipWithoutTheName()
    {
        EntityData zealot = CreateZealot();

        Assert.AreEqual("Hits harder when healthy\n\n- Max HP: 100", CharacterCardText.GetUnitDetails(zealot));
        Assert.AreEqual("<b>Zealot</b>\nHits harder when healthy\n\n- Max HP: 100", CharacterCardText.GetUnitTooltip(zealot));
    }

    [Test]
    public void GetUnitDetails_ShowsTheAttackCooldownAsAttacksPerSecond()
    {
        EntityData zealot = CreateZealot();
        zealot.attributes[AttributeType.AttackRate] = 2f;

        StringAssert.Contains("- Attack Speed: 0.5/s", CharacterCardText.GetUnitDetails(zealot));
    }

    [Test]
    public void EntityButton_ShowsTheUnitAsANewUnitWithItsDetails()
    {
        GameObject go = new GameObject("Unit Reward");
        _objects.Add(go);
        SelectEntityUpgradeButton button = go.AddComponent<SelectEntityUpgradeButton>();
        TMP_Text title = new GameObject("Title").AddComponent<TextMeshPro>();
        TMP_Text description = new GameObject("Description").AddComponent<TextMeshPro>();
        _objects.Add(title.gameObject);
        _objects.Add(description.gameObject);
        TestHelpers.SetPrivateField(button, "_title", title);
        TestHelpers.SetPrivateField(button, "_description", description);
        EntityData zealot = CreateZealot();

        button.Init(zealot);

        Assert.AreSame(zealot, button.entity);
        Assert.AreEqual("New unit: Zealot", title.text);
        Assert.AreEqual(CharacterCardText.GetUnitDetails(zealot), description.text);
    }
}

}
