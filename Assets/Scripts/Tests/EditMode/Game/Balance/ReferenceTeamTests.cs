using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class ReferenceTeamTests
{
    readonly List<Object> _created = new List<Object>();

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
        _created.Add(unit);
        return unit;
    }

    AItemFactory CreateItem(string title)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData { name = title };
        _created.Add(item);
        return item;
    }

    [Test]
    public void Copy_SameContentOnAnotherFloor_ListsNotShared()
    {
        ReferenceTeam team = new ReferenceTeam { floor = 1, rewardCount = 2, units = new List<EntityData> { CreateUnit("Knight") } };

        ReferenceTeam copy = team.Copy(4);
        copy.units.Add(CreateUnit("Archer"));

        Assert.AreEqual(4, copy.floor);
        Assert.AreEqual(2, copy.rewardCount);
        Assert.AreEqual(1, team.units.Count);
    }

    [Test]
    public void GetItemCount_OnlyTheItemsOfTheUnit()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Knight"), CreateUnit("Archer") } };
        team.unitItems.Add(new ReferenceTeam.UnitItem { unit = 1, item = CreateItem("Bow") });
        team.unitItems.Add(new ReferenceTeam.UnitItem { unit = 1, item = CreateItem("Quiver") });

        Assert.AreEqual(0, team.GetItemCount(0));
        Assert.AreEqual(2, team.GetItemCount(1));
    }

    [Test]
    public void Describe_UnitsWithTheirItemsThenThePlayerItems()
    {
        ReferenceTeam team = new ReferenceTeam { floor = 3, rewardCount = 2, units = new List<EntityData> { CreateUnit("Knight"), CreateUnit("Archer") } };
        team.unitItems.Add(new ReferenceTeam.UnitItem { unit = 0, item = CreateItem("Iron Shield") });
        team.playerItems.Add(CreateItem("Tome"));

        Assert.AreEqual("Floor 3, 2 rewards: Knight [Iron Shield], Archer | Player items: Tome", team.Describe());
    }
}

}
