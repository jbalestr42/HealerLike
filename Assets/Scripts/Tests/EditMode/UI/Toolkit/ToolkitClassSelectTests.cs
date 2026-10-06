using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UI.Toolkit
{
    // The menu's class choice: which classes it offers, what a pick records, and that Main plays the pick
    public class ToolkitClassSelectTests
    {
        static readonly string ManagersPrefab = "Assets/Prefabs/Managers.prefab";
        static readonly string MenuScene = "Assets/Scenes/Toolkit/MenuToolkit.unity";
        static readonly string DruidPath = "Assets/Data/Characters/DruidCharacter/DruidCharacter.asset";

        readonly List<Object> _created = new List<Object>();
        readonly List<CharacterData> _chosen = new List<CharacterData>();
        ToolkitClassSelect _select;
        GameData _data;

        [SetUp]
        public void SetUp()
        {
            CharacterSelection.selected = null;
            _chosen.Clear();
            _data = Create<GameData>();
            _data.characters = new List<CharacterData> { Character("Cleric"), null, Character("Druid"), Character("Warlock") };
            _select = new ToolkitClassSelect();
            _select.Init(null, _chosen.Add);
        }

        [TearDown]
        public void TearDown()
        {
            CharacterSelection.selected = null;
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        T Create<T>() where T : ScriptableObject
        {
            T created = ScriptableObject.CreateInstance<T>();
            _created.Add(created);
            return created;
        }

        CharacterData Character(string title)
        {
            CharacterData character = Create<CharacterData>();
            character.name = title + "Character";
            character.title = title;
            character.text = title + " description";
            return character;
        }

        [Test]
        public void Offered_GameData_KeepsItsClassesInOrderWithoutEmptyEntries()
        {
            List<CharacterData> offered = ToolkitClassSelect.Offered(_data);

            CollectionAssert.AreEqual(new[] { _data.characters[0], _data.characters[2], _data.characters[3] }, offered);
        }

        [Test]
        public void Offered_NoData_OffersNothing()
        {
            CollectionAssert.IsEmpty(ToolkitClassSelect.Offered(null));
        }

        [Test]
        public void Open_GameData_OffersOneCardPerClassThenRandom()
        {
            Assert.IsTrue(_select.Open(_data));

            Assert.IsTrue(_select.isOpen);
            List<ToolkitCardModel> models = _select.BuildModels();
            CollectionAssert.AreEqual(new[] { "Cleric", "Druid", "Warlock", "Random" },
                models.ConvertAll(model => model.title));
            Assert.AreSame(_data.characters[2], models[1].source);
            Assert.AreSame(_data.characters[2], models[1].iconSource, "A class card shows the class's data icon.");
            CollectionAssert.IsEmpty(_chosen, "Opening the screen chooses nothing.");
            Assert.IsNull(CharacterSelection.selected);
        }

        [Test]
        public void Open_NoClasses_StaysClosed()
        {
            _data.characters = new List<CharacterData>();

            Assert.IsFalse(_select.Open(_data));

            Assert.IsFalse(_select.isOpen);
        }

        [Test]
        public void Choose_OfferedClass_RecordsTheSelectionAndContinuesOnce()
        {
            _select.Open(_data);
            CharacterData druid = _data.characters[2];

            Assert.IsTrue(_select.Choose(druid));

            Assert.AreSame(druid, CharacterSelection.selected);
            CollectionAssert.AreEqual(new[] { druid }, _chosen);
            Assert.IsFalse(_select.isOpen);
        }

        [Test]
        public void Choose_ClassOutsideTheOffer_IsRefused()
        {
            _select.Open(_data);

            Assert.IsFalse(_select.Choose(Character("Stranger")));

            Assert.IsNull(CharacterSelection.selected);
            CollectionAssert.IsEmpty(_chosen);
            Assert.IsTrue(_select.isOpen);
        }

        [Test]
        public void Choose_ScreenClosed_IsRefused()
        {
            Assert.IsFalse(_select.Choose(_data.characters[0]));

            Assert.IsNull(CharacterSelection.selected);
            CollectionAssert.IsEmpty(_chosen);
        }

        [Test]
        public void ChooseRandom_PicksTheOfferedClassAtTheRandomIndex()
        {
            _select.Open(_data);
            int offeredCount = -1;
            _select.randomIndex = count =>
            {
                offeredCount = count;
                return 2;
            };

            Assert.IsTrue(_select.ChooseRandom());

            Assert.AreEqual(3, offeredCount, "The Random card draws among the offered classes only.");
            Assert.AreSame(_data.characters[3], CharacterSelection.selected);
            CollectionAssert.AreEqual(new[] { _data.characters[3] }, _chosen);
        }

        [Test]
        public void ChooseRandom_DefaultDraw_PicksAnOfferedClass()
        {
            List<CharacterData> offered = ToolkitClassSelect.Offered(_data);
            for (int draw = 0; draw < 20; draw++)
            {
                _select.Open(_data);

                Assert.IsTrue(_select.ChooseRandom());

                CollectionAssert.Contains(offered, CharacterSelection.selected);
            }
        }

        [Test]
        public void RandomCard_Activated_PicksAnOfferedClass()
        {
            _select.Open(_data);
            _select.randomIndex = count => 0;
            ToolkitCardModel random = _select.BuildModels()[3];

            random.activate.Invoke(random);

            Assert.AreSame(_data.characters[0], CharacterSelection.selected);
        }

        [Test]
        public void Close_OpenScreen_ChoosesNothing()
        {
            _select.Open(_data);

            _select.Close();

            Assert.IsFalse(_select.isOpen);
            Assert.IsNull(CharacterSelection.selected);
            CollectionAssert.IsEmpty(_chosen);
        }

        // Julien's select card shows the description, skills, starting items and units; the Toolkit card keeps them
        [Test]
        public void Describe_Druid_ShowsDescriptionSkillsItemsAndUnits()
        {
            CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>(DruidPath);
            Assert.IsNotNull(druid, DruidPath);

            string text = ToolkitClassSelect.Describe(druid);

            StringAssert.StartsWith(druid.text, text);
            foreach (ACharacterSkillFactory skill in druid.skills)
            {
                StringAssert.Contains(skill.Create().GetData().name, text);
            }

            foreach (AItemFactory item in druid.items)
            {
                StringAssert.Contains(item.title, text);
            }

            StringAssert.Contains("Units: ", text);
            foreach (EntityData unit in druid.entities)
            {
                StringAssert.Contains(unit.title, text);
            }

            Assert.Greater(druid.entities.Count, 0);
        }

        // The card reads name, one role line, then the kit apart: the role is Julien's description and nothing else
        [Test]
        public void Role_Druid_IsItsDescriptionAlone()
        {
            CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>(DruidPath);

            Assert.AreEqual(druid.text, ToolkitClassSelect.Role(druid));
        }

        [Test]
        public void Details_Druid_GivesSkillsStartingItemsAndUnitsOneLineEach()
        {
            CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>(DruidPath);

            string[] lines = ToolkitClassSelect.Details(druid).Split('\n');

            Assert.AreEqual(3, lines.Length, string.Join(" | ", lines));
            StringAssert.StartsWith("Skills: ", lines[0]);
            foreach (ACharacterSkillFactory skill in druid.skills)
            {
                StringAssert.Contains(skill.Create().GetData().name, lines[0]);
            }

            StringAssert.DoesNotContain("- ", lines[0], "Julien's list markers are not carried onto the one line.");
            StringAssert.StartsWith("Starts with: ", lines[1]);
            foreach (AItemFactory item in druid.items)
            {
                StringAssert.Contains(item.title + " (" + item.GetItem().description + ")", lines[1]);
            }

            StringAssert.StartsWith("Units: ", lines[2]);
            foreach (EntityData unit in druid.entities)
            {
                StringAssert.Contains(unit.title, lines[2]);
            }
            StringAssert.DoesNotContain(druid.text, ToolkitClassSelect.Details(druid), "The role is not repeated.");
        }

        [Test]
        public void Details_NoSkillsItemsOrUnits_IsEmpty()
        {
            Assert.AreEqual("", ToolkitClassSelect.Details(Character("Plain")));
        }

        [Test]
        public void MarkDetails_KitLines_TintsEachLineLabelOnly()
        {
            string marked = ToolkitClassSelect.MarkDetails("Skills: Heal, Shield\nStarts with: Tome (Units: 2)\nUnits: Normal");

            string open = "<color=" + ToolkitClassSelect.DetailKeyColor + ">";
            Assert.AreEqual(open + "Skills:</color> Heal, Shield\n" + open + "Starts with:</color> Tome (Units: 2)\n"
                + open + "Units:</color> Normal", marked);
        }

        [Test]
        public void MarkDetails_Empty_StaysEmpty()
        {
            Assert.AreEqual("", ToolkitClassSelect.MarkDetails(""));
            Assert.IsNull(ToolkitClassSelect.MarkDetails(null));
        }

        [Test]
        public void BuildModels_ClassCard_LeadsWithTheRoleAndKeepsTheKitApart()
        {
            CharacterData druid = AssetDatabase.LoadAssetAtPath<CharacterData>(DruidPath);
            _data.characters = new List<CharacterData> { druid };
            _select.Open(_data);

            List<ToolkitCardModel> models = _select.BuildModels();

            Assert.AreEqual(druid.title, models[0].title);
            Assert.AreEqual(druid.text, models[0].description);
            Assert.AreEqual(ToolkitClassSelect.MarkDetails(ToolkitClassSelect.Details(druid)), models[0].details);
            Assert.IsTrue(string.IsNullOrEmpty(models[1].details), "The Random card has no kit.");
        }

        [Test]
        public void Describe_NoSkillsItemsOrUnits_IsTheDescriptionAlone()
        {
            CharacterData plain = Character("Plain");

            Assert.AreEqual("Plain description", ToolkitClassSelect.Describe(plain));
        }

        // DataManager.GetCharacter replaces a class missing from its data with a random one, so the menu must offer
        // exactly the characters of the data Main's DataManager plays
        [Test]
        public void MenuScene_OffersTheCharactersOfMainsDataManager()
        {
            DataManager manager = AssetDatabase.LoadAssetAtPath<GameObject>(ManagersPrefab).GetComponent<DataManager>();
            Assert.IsNotNull(manager, ManagersPrefab);
            Assert.IsNotNull(manager.data, "Managers.prefab has no game data.");
            GameData menuData = MenuGameData();
            Assert.IsNotNull(menuData, "MenuToolkit's ToolkitGameUI has no game data for the class choice.");

            List<CharacterData> offered = ToolkitClassSelect.Offered(menuData);

            CollectionAssert.AreEqual(manager.data.characters, offered);
            Assert.GreaterOrEqual(offered.Count, 3, "Cleric, Druid and Warlock at least.");
            foreach (CharacterData character in offered)
            {
                Assert.AreSame(character, manager.GetCharacter(character), character.title + " would be replaced.");
            }
        }

        // The ToolkitGameUI of the menu scene, read from the scene file so the test never opens a scene
        static GameData MenuGameData()
        {
            string script = AssetDatabase.AssetPathToGUID("Assets/Scripts/UI/Toolkit/ToolkitGameUI.cs");
            string scene = File.ReadAllText(MenuScene);
            Match block = Regex.Match(scene, "m_Script: \\{fileID: 11500000, guid: " + script
                + ", type: 3\\}(?:(?!--- !u!).)*", RegexOptions.Singleline);
            Assert.IsTrue(block.Success, "MenuToolkit has no ToolkitGameUI.");
            Match data = Regex.Match(block.Value, "_gameData: \\{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\\}");
            if (!data.Success)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameData>(AssetDatabase.GUIDToAssetPath(data.Groups[1].Value));
        }
    }
}
