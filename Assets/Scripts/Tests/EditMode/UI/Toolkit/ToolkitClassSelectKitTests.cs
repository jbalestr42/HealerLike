using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    // The class cards carry the kit of their class; the Random card carries none
    public class ToolkitClassSelectKitTests
    {
        readonly List<Object> _created = new List<Object>();
        ToolkitClassSelect _select;
        GameData _data;
        CharacterData _cleric;

        [SetUp]
        public void SetUp()
        {
            _cleric = Create<CharacterData>();
            _cleric.title = "Cleric";
            _cleric.text = "Cleric description";
            _cleric.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f } };
            ApplyConsumerCharacterSkillFactory heal = Create<ApplyConsumerCharacterSkillFactory>();
            heal.data = new ApplyConsumerCharacterSkillData { name = "Heal", description = "Heals" };
            _cleric.skills = new List<ACharacterSkillFactory> { heal };
            EntityData zealot = Create<EntityData>();
            zealot.title = "Zealot";
            _cleric.entities = new List<EntityData> { zealot };
            _data = Create<GameData>();
            _data.characters = new List<CharacterData> { _cleric };
            _select = new ToolkitClassSelect();
            _select.Init(null, null);
            _select.Open(_data);
        }

        [TearDown]
        public void TearDown()
        {
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

        [Test]
        public void BuildModels_ClassCard_CarriesItsStatsAndItsSkillsThenUnits()
        {
            List<ToolkitCardModel> models = _select.BuildModels();

            Assert.AreEqual("- Max Mana: 100", models[0].stats);
            Assert.AreEqual(2, models[0].kit.Count);
            Assert.AreEqual("Heal", models[0].kit[0].title);
            Assert.AreEqual("Zealot", models[0].kit[1].title);
        }

        [Test]
        public void BuildModels_RandomCard_HasNoStatsOrKit()
        {
            List<ToolkitCardModel> models = _select.BuildModels();

            Assert.IsTrue(string.IsNullOrEmpty(models[1].stats));
            Assert.IsNull(models[1].kit);
        }

        [Test]
        public void BuildModels_Twice_LeavesNoPreviewCharacterBehind()
        {
            int before = Object.FindObjectsByType<global::Character>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            _select.BuildModels();
            _select.BuildModels();

            Assert.AreEqual(before, Object.FindObjectsByType<global::Character>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        }
    }
}
