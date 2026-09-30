using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace UI
{

// The reward choices shown after a battle or in a treasure room: each shows the name and the description of its item
public class UpgradeButtonTests
{
    class StubItem : AItem
    {
        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => "Iron Shield";
        public override string description => "+10 armor";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    readonly List<GameObject> _objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }

    TMP_Text CreateText(string name)
    {
        GameObject go = new GameObject(name);
        _objects.Add(go);
        return go.AddComponent<TextMeshProUGUI>();
    }

    [Test]
    public void ItemButton_ShowsTitleAndDescription()
    {
        GameObject go = new GameObject("Item Button");
        _objects.Add(go);
        SelectItemUpgradeButton button = go.AddComponent<SelectItemUpgradeButton>();
        TMP_Text title = CreateText("Title");
        TMP_Text description = CreateText("Description");
        TestHelpers.SetPrivateField(button, "_title", title);
        TestHelpers.SetPrivateField(button, "_description", description);

        button.Init(new StubItem());

        Assert.AreEqual("Iron Shield", title.text);
        Assert.AreEqual("+10 armor", description.text);
    }

    [Test]
    public void PlayerItemButton_ShowsTitleAndDescription()
    {
        GameObject go = new GameObject("Player Item Button");
        _objects.Add(go);
        SelectPlayerItemUpgradeButton button = go.AddComponent<SelectPlayerItemUpgradeButton>();
        TMP_Text title = CreateText("Title");
        TMP_Text description = CreateText("Description");
        TestHelpers.SetPrivateField(button, "_title", title);
        TestHelpers.SetPrivateField(button, "_description", description);

        button.Init(new StubItem());

        Assert.AreEqual("Iron Shield", title.text);
        Assert.AreEqual("+10 armor", description.text);
    }
}

}
