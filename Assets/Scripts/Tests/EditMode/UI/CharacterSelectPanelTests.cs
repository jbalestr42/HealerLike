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

    ACharacterSkillFactory CreateSkill(string name)
    {
        ApplyConsumerCharacterSkillFactory skill = CreateTracked<ApplyConsumerCharacterSkillFactory>();
        skill.data = new ApplyConsumerCharacterSkillData { name = name };
        return skill;
    }

    EntityData CreateEntity(string title)
    {
        EntityData entity = CreateTracked<EntityData>();
        entity.title = title;
        return entity;
    }

    CharacterData CreateCharacter(string title)
    {
        CharacterData character = CreateTracked<CharacterData>();
        character.title = title;
        character.text = title + " description";
        character.skills = new List<ACharacterSkillFactory> { CreateSkill("Heal"), CreateSkill("Shield") };
        character.entities = new List<EntityData> { CreateEntity("Normal"), CreateEntity("Zealot") };
        return character;
    }

    [Test]
    public void GetSkills_ListsOneSkillNamePerLine()
    {
        Assert.AreEqual("- Heal\n- Shield", CharacterCardText.GetSkills(CreateCharacter("Cleric")));
    }

    [Test]
    public void GetUnits_ListsTheUnitTitles()
    {
        Assert.AreEqual("Normal, Zealot", CharacterCardText.GetUnits(CreateCharacter("Cleric")));
    }

    [Test]
    public void Texts_MissingOrNullEntries_AreSkipped()
    {
        CharacterData character = CreateTracked<CharacterData>();
        character.skills = new List<ACharacterSkillFactory> { null, CreateSkill("Heal") };
        character.entities = null;

        Assert.AreEqual("- Heal", CharacterCardText.GetSkills(character));
        Assert.AreEqual("", CharacterCardText.GetUnits(character));
        Assert.AreEqual("", CharacterCardText.GetDescription(character));
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
        CollectionAssert.Contains(texts, "- Heal\n- Shield");
    }

    [Test]
    public void ClickingACard_ChoosesItsCharacter()
    {
        List<CharacterData> characters = new List<CharacterData> { CreateCharacter("Cleric"), CreateCharacter("Druid") };
        CharacterData chosen = null;
        CharacterSelectPanel panel = CharacterSelectPanel.Create(_canvas.transform, characters, character => chosen = character, () => { });

        panel.transform.Find("Cards").GetChild(1).GetComponent<Button>().onClick.Invoke();

        Assert.AreSame(characters[1], chosen);
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
