using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// The rest room: heal every unit or resurrect a dead one
public class RestRoomTests
{
    List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    EntityData CreateUnit(string title)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.title = title;
        unit.description = $"{title} description";
        _created.Add(unit);
        return unit;
    }

    [Test]
    public void RestHealConsumer_NeverCritical()
    {
        ConsumerFactory restHeal = AssetDatabase.LoadAssetAtPath<ConsumerFactory>("Assets/Data/Run/RestHealConsumer.asset");

        Assert.IsNotNull(restHeal);
        Assert.IsFalse(restHeal.data.canBeCritical);
    }

    [Test]
    public void CreateRestChoices_NoDeadUnit_OnlyHealCanBePicked()
    {
        List<EventChoice> choices = AscensionGameType.CreateRestChoices("Heals", false, null, null);

        Assert.AreEqual(2, choices.Count);
        Assert.AreEqual("Heal", choices[0].label);
        Assert.AreEqual("Heals", choices[0].description);
        Assert.IsTrue(choices[0].isAvailable);
        Assert.AreEqual("Resurrect", choices[1].label);
        Assert.IsFalse(choices[1].isAvailable);
    }

    [Test]
    public void CreateRestChoices_DeadUnit_ResurrectCanBePicked()
    {
        List<EventChoice> choices = AscensionGameType.CreateRestChoices("Heals", true, null, null);

        Assert.IsTrue(choices[1].isAvailable);
    }

    [Test]
    public void CreateRestChoices_EachChoiceAppliesItsOwnAction()
    {
        List<string> calls = new List<string>();
        List<EventChoice> choices = AscensionGameType.CreateRestChoices("Heals", true, () => calls.Add("heal"), () => calls.Add("resurrect"));

        choices[1].onSelected();
        choices[0].onSelected();

        CollectionAssert.AreEqual(new[] { "resurrect", "heal" }, calls);
    }

    [Test]
    public void CreateResurrectChoices_OneChoicePerDeadUnitThenBack()
    {
        EntityData knight = CreateUnit("Knight");
        EntityData archer = CreateUnit("Archer");

        // Two knights died: listed once
        List<EventChoice> choices = AscensionGameType.CreateResurrectChoices(new List<EntityData> { knight, archer, knight, null }, null, null);

        Assert.AreEqual(3, choices.Count);
        Assert.AreEqual("Knight", choices[0].label);
        Assert.AreEqual("Knight description", choices[0].description);
        Assert.AreEqual("Archer", choices[1].label);
        Assert.AreEqual("Back", choices[2].label);
        Assert.IsTrue(choices.TrueForAll(choice => choice.isAvailable));
    }

    [Test]
    public void CreateResurrectChoices_PickingAUnitResurrectsIt()
    {
        EntityData knight = CreateUnit("Knight");
        EntityData archer = CreateUnit("Archer");
        List<EntityData> resurrected = new List<EntityData>();
        int back = 0;
        List<EventChoice> choices = AscensionGameType.CreateResurrectChoices(new List<EntityData> { knight, archer }, resurrected.Add, () => back++);

        choices[1].onSelected();

        CollectionAssert.AreEqual(new[] { archer }, resurrected);
        Assert.AreEqual(0, back);
    }

    [Test]
    public void CreateResurrectChoices_BackGoesBackWithoutResurrecting()
    {
        List<EntityData> resurrected = new List<EntityData>();
        int back = 0;
        List<EventChoice> choices = AscensionGameType.CreateResurrectChoices(new List<EntityData> { CreateUnit("Knight") }, resurrected.Add, () => back++);

        choices[choices.Count - 1].onSelected();

        CollectionAssert.IsEmpty(resurrected);
        Assert.AreEqual(1, back);
    }
}

}
