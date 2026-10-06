using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

public class CharacterSelectPanelTests
{
    readonly List<Object> _objects = new List<Object>();
    GameObject _canvas;

    [SetUp]
    public void SetUp()
    {
        _canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_canvas);
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    ACharacterSkillFactory CreateSkill(string name, string description = "")
    {
        ApplyConsumerCharacterSkillFactory skill = CreateTracked<ApplyConsumerCharacterSkillFactory>();
        skill.data = new ApplyConsumerCharacterSkillData { name = name, description = description };
        return skill;
    }

    EntityData CreateEntity(string title)
    {
        EntityData entity = CreateTracked<EntityData>();
        entity.title = title;
        return entity;
    }

    AItemFactory CreateItem(string name, string description)
    {
        ItemFactory item = CreateTracked<ItemFactory>();
        item.data = new ItemData { name = name, description = description };
        return item;
    }

    List<string> GetCardTexts(CharacterSelectPanel panel, int index)
    {
        List<string> texts = new List<string>();
        foreach (TMP_Text text in panel.transform.Find("Cards").GetChild(index).GetComponentsInChildren<TMP_Text>())
        {
            texts.Add(text.text);
        }
        return texts;
    }

    CharacterData CreateCharacter(string title)
    {
        CharacterData character = CreateTracked<CharacterData>();
        character.title = title;
        character.text = title + " description";
        character.skills = new List<ACharacterSkillFactory> { CreateSkill("Heal"), CreateSkill("Shield") };
        character.entities = new List<EntityData> { CreateEntity("Normal"), CreateEntity("Zealot") };
        character.items = new List<AItemFactory>();
        character.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f }, { AttributeType.HealPower, 20f } };
        return character;
    }

    // A starting item adding Heal Power, like the Sacred Tome
    AItemFactory CreateHealPowerItem(float healPower)
    {
        FlatModifierFactory modifier = CreateTracked<FlatModifierFactory>();
        modifier.data = new FlatModifierData { type = AttributeType.HealPower, modifierType = AttributeModifierType.Add, value = healPower };
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { modifier } };
        ItemFactory item = CreateTracked<ItemFactory>();
        item.data = new ItemData { name = "Sacred Tome", description = "+" + healPower + " Heal Power", buffs = new List<ABuffHandlerFactory> { handler } };
        return item;
    }

    Character CreatePreview(CharacterData character)
    {
        return CharacterSelectPanel.CreatePreview(character, _canvas.transform);
    }

    [Test]
    public void GetItems_ListsOneItemPerLineWithItsEffect()
    {
        CharacterData character = CreateCharacter("Cleric");
        character.items = new List<AItemFactory> { CreateItem("Sacred Tome", "+10 Heal Power"), CreateItem("Relic", "") };

        Assert.AreEqual("- Sacred Tome: +10 Heal Power\n- Relic", CharacterCardText.GetItems(character));
    }

    [Test]
    public void GetItems_WithoutItem_IsEmpty()
    {
        Assert.AreEqual("", CharacterCardText.GetItems(CreateCharacter("Druid")));
    }

    [Test]
    public void Card_ShowsTheItemsOfTheCharacter()
    {
        CharacterData cleric = CreateCharacter("Cleric");
        cleric.items = new List<AItemFactory> { CreateItem("Sacred Tome", "+10 Heal Power") };

        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { cleric }, _ => { }, () => { });

        List<string> texts = GetCardTexts(panel, 0);
        CollectionAssert.Contains(texts, "Items");
        CollectionAssert.Contains(texts, "- Sacred Tome: +10 Heal Power");
    }

    [Test]
    public void Card_WithoutItem_HasNoItemsSection()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Druid") }, _ => { }, () => { });

        CollectionAssert.DoesNotContain(GetCardTexts(panel, 0), "Items");
    }

    [Test]
    public void Texts_MissingEntries_AreSkipped()
    {
        CharacterData character = CreateTracked<CharacterData>();
        character.skills = new List<ACharacterSkillFactory> { null, CreateSkill("Heal") };
        character.entities = null;
        character.items = new List<AItemFactory> { null };

        Assert.AreEqual("", CharacterCardText.GetItems(character));
        Assert.AreEqual("", CharacterCardText.GetDescription(character));
    }

    [Test]
    public void GetSkillTooltip_ShowsTheNameAndTheDescriptionWithItsValues()
    {
        ACharacterSkillFactory skill = CreateSkill("Shield", "Gives armor during {data:name}");

        Assert.AreEqual("<b>Shield</b>\nGives armor during Shield", CharacterCardText.GetSkillTooltip(skill, CreatePreview(CreateCharacter("Cleric"))));
    }

    [Test]
    public void GetSkillTooltip_WithoutDescription_ShowsTheName()
    {
        Assert.AreEqual("<b>Heal</b>", CharacterCardText.GetSkillTooltip(CreateSkill("Heal"), CreatePreview(CreateCharacter("Cleric"))));
    }

    // The real values of the character in game: a heal from its Heal Power, its starting items included
    [Test]
    public void GetSkillTooltip_ComputesTheValuesFromTheStatsOfTheCharacter_StartingItemsIncluded()
    {
        CharacterData cleric = CreateCharacter("Cleric");
        cleric.items = new List<AItemFactory> { CreateHealPowerItem(10f) };
        ACharacterSkillFactory heal = CreateSkill("Heal", "Heals for [{attribute:current:HealPower}x1.5]");

        Assert.AreEqual("<b>Heal</b>\nHeals for 45", CharacterCardText.GetSkillTooltip(heal, CreatePreview(cleric)));
    }

    [Test]
    public void GetStats_ListsTheStatsOfTheCharacter_StartingItemsIncluded()
    {
        CharacterData cleric = CreateCharacter("Cleric");
        cleric.items = new List<AItemFactory> { CreateHealPowerItem(10f) };

        Assert.AreEqual("- Max Mana: 100\n- Heal Power: 30", CharacterCardText.GetStats(CreatePreview(cleric)));
    }

    [Test]
    public void Card_ShowsTheStatsOfTheCharacter()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, () => { });

        List<string> texts = GetCardTexts(panel, 0);
        CollectionAssert.Contains(texts, "Stats");
        CollectionAssert.Contains(texts, "- Max Mana: 100\n- Heal Power: 20");
    }

    [Test]
    public void GetUnitTooltip_ShowsTheDescriptionTheStatsAndTheInnateItems()
    {
        EntityData zealot = CreateEntity("Zealot");
        zealot.description = "Hits harder when healthy";
        zealot.attributes = new Dictionary<AttributeType, float> { { AttributeType.Damage, 4f }, { AttributeType.HealthMax, 120f }, { AttributeType.FlatArmor, 0f } };
        zealot.items = new List<AItemFactory> { CreateItem("Zeal", "+50% damage while above 70% health") };

        Assert.AreEqual("<b>Zealot</b>\nHits harder when healthy\n\n- Max HP: 120\n- Damage: 4\n\nPassives\n<b>Zeal</b>: +50% damage while above 70% health",
            CharacterCardText.GetUnitTooltip(zealot));
    }

    [Test]
    public void GetUnitTooltip_WithoutStats_PutsThePassivesRightAfterTheDescription()
    {
        EntityData keeper = CreateEntity("Grove Keeper");
        keeper.description = "Doesn't attack";
        keeper.attributes = new Dictionary<AttributeType, float> { { AttributeType.Damage, 0f } };
        keeper.items = new List<AItemFactory> { CreateItem("Grove", "Heals over time") };

        Assert.AreEqual("<b>Grove Keeper</b>\nDoesn't attack\n\nPassives\n<b>Grove</b>: Heals over time", CharacterCardText.GetUnitTooltip(keeper));
    }

    [Test]
    public void GetUnitTooltip_WithOnlyATitle_ShowsTheTitle()
    {
        EntityData unit = CreateEntity("Normal");
        unit.attributes = null;
        unit.items = null;

        Assert.AreEqual("<b>Normal</b>", CharacterCardText.GetUnitTooltip(unit));
    }

    [Test]
    public void Card_ShowsOneIconPerSkill_AndOneLabelPerUnit()
    {
        CharacterData cleric = CreateCharacter("Cleric");
        Sprite icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.zero);
        _objects.Add(icon);
        ((ApplyConsumerCharacterSkillFactory)cleric.skills[0]).data.icon = icon;

        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { cleric }, _ => { }, () => { });

        Transform card = panel.transform.Find("Cards").GetChild(0);
        Transform skills = card.Find("Skills");
        Assert.AreEqual(2, skills.childCount);
        Assert.AreEqual("Heal", skills.GetChild(0).name);
        Assert.AreSame(icon, skills.GetChild(0).GetComponent<Image>().sprite);
        Transform units = card.Find("Units");
        Assert.AreEqual(2, units.childCount);
        Assert.AreEqual("Zealot", units.GetChild(1).GetComponentInChildren<TMP_Text>().text);
    }

    [Test]
    public void Tooltip_IsHiddenUntilAnElementIsHovered()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, () => { });

        Assert.IsFalse(panel.tooltip.activeSelf);
    }

    [Test]
    public void HoveringASkill_ShowsItsDetails_UntilThePointerLeaves()
    {
        CharacterData cleric = CreateCharacter("Cleric");
        cleric.skills = new List<ACharacterSkillFactory> { CreateSkill("Shield", "Gives armor") };
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { cleric }, _ => { }, () => { });
        HoverTooltipTrigger shield = panel.transform.Find("Cards").GetChild(0).Find("Skills").GetChild(0).GetComponent<HoverTooltipTrigger>();

        shield.OnPointerEnter(null);
        Assert.IsTrue(panel.tooltip.activeSelf);
        Assert.AreEqual("<b>Shield</b>\nGives armor", panel.tooltipText);

        shield.OnPointerExit(null);
        Assert.IsFalse(panel.tooltip.activeSelf);
    }

    [Test]
    public void HoveringAUnit_ShowsItsDetails()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, () => { });
        HoverTooltipTrigger zealot = panel.transform.Find("Cards").GetChild(0).Find("Units").GetChild(1).GetComponent<HoverTooltipTrigger>();

        zealot.OnPointerEnter(null);

        Assert.IsTrue(panel.tooltip.activeSelf);
        StringAssert.StartsWith("<b>Zealot</b>", panel.tooltipText);
    }

    // The tooltip is drawn over the cards, but must not catch the pointer: the hovered element would lose it
    [Test]
    public void Tooltip_DoesNotBlockThePointer()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, () => { });

        foreach (Graphic graphic in panel.tooltip.GetComponentsInChildren<Graphic>(true))
        {
            Assert.IsFalse(graphic.raycastTarget, graphic.name);
        }
    }

    [Test]
    public void Create_ShowsOneCardPerCharacter()
    {
        List<CharacterData> characters = new List<CharacterData> { CreateCharacter("Cleric"), CreateCharacter("Druid"), CreateCharacter("Warlock") };

        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, characters, _ => { }, () => { });

        Transform cards = panel.transform.Find("Cards");
        Assert.AreEqual(3, cards.childCount);
        Assert.AreEqual("Druid", cards.GetChild(1).name);
        List<string> texts = new List<string>();
        foreach (TMP_Text text in cards.GetChild(1).GetComponentsInChildren<TMP_Text>())
        {
            texts.Add(text.text);
        }
        CollectionAssert.Contains(texts, "Druid description");
    }

    [Test]
    public void ClickingTheSelectButtonOfACard_ChoosesItsCharacter()
    {
        List<CharacterData> characters = new List<CharacterData> { CreateCharacter("Cleric"), CreateCharacter("Druid") };
        CharacterData chosen = null;
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, characters, character => chosen = character, () => { });

        panel.transform.Find("Cards").GetChild(1).Find("Select").GetComponent<Button>().onClick.Invoke();

        Assert.AreSame(characters[1], chosen);
    }

    // Only the button picks: hovering or clicking the rest of the card to read it chooses nothing
    [Test]
    public void Card_IsNotAButton()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, () => { });

        Assert.IsNull(panel.transform.Find("Cards").GetChild(0).GetComponent<Button>());
    }

    [Test]
    public void SelectButton_IsTheLastElementOfTheCard()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, () => { });

        Transform card = panel.transform.Find("Cards").GetChild(0);
        Assert.AreEqual("Select", card.GetChild(card.childCount - 1).name);
    }

    [Test]
    public void ClickingRandom_ChoosesOneOfTheCharacters()
    {
        List<CharacterData> characters = new List<CharacterData> { CreateCharacter("Cleric"), CreateCharacter("Druid"), CreateCharacter("Warlock") };
        CharacterData chosen = null;
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, characters, character => chosen = character, () => { });

        panel.transform.Find("Random").GetComponent<Button>().onClick.Invoke();

        CollectionAssert.Contains(characters, chosen);
    }

    [Test]
    public void Random_WithoutCharacter_IsNotShown()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData>(), _ => { }, () => { });

        Assert.IsNull(panel.transform.Find("Random"));
    }

    [Test]
    public void WithoutBackAction_HasNoBackButton()
    {
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData> { CreateCharacter("Cleric") }, _ => { }, null);

        Assert.IsNull(panel.transform.Find("Back"));
    }

    [Test]
    public void ClickingBack_CallsBack()
    {
        bool back = false;
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, new List<CharacterData>(), _ => { }, () => back = true);

        panel.transform.Find("Back").GetComponent<Button>().onClick.Invoke();

        Assert.IsTrue(back);
    }
}

}
