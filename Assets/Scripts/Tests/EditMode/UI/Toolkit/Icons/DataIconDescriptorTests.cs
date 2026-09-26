using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Icons
{

    public class DataIconDescriptorTests
    {
        class FakeData : IDataIconMetadata
        {
            public string title;
            public string label { get { return title; } }
            public Sprite icon { get { return null; } }
        }

        ScriptableObject _asset;

        [TearDown]
        public void TearDown()
        {
            if (_asset != null)
            {
                Object.DestroyImmediate(_asset);
            }
        }

        [Test]
        public void From_Null_ReturnsUnknownData()
        {
            DataIconDescriptor descriptor = DataIconDescriptor.From(null);

            Assert.AreEqual("Unknown", descriptor.label);
            Assert.AreEqual(DataIconKind.Data, descriptor.kind);
        }

        [Test]
        public void From_EntityData_UsesTitleAndCreatureKind()
        {
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            _asset = data;
            data.name = "Asset";
            data.title = "Forest guardian";

            DataIconDescriptor descriptor = DataIconDescriptor.From(data);

            Assert.AreEqual("Forest guardian", descriptor.label);
            Assert.AreEqual(DataIconKind.Creature, descriptor.kind);
        }

        [Test]
        public void From_CharacterSkillData_UsesNameAndSpellKind()
        {
            DataIconDescriptor descriptor = DataIconDescriptor.From(new CharacterSkillData { name = "Heal" });

            Assert.AreEqual("Heal", descriptor.label);
            Assert.AreEqual(DataIconKind.Spell, descriptor.kind);
        }

        [Test]
        public void From_ExplicitMetadata_UsesLabelAndDataKind()
        {
            DataIconDescriptor descriptor = DataIconDescriptor.From(new FakeData { title = "New content" });

            Assert.AreEqual("New content", descriptor.label);
            Assert.AreEqual(DataIconKind.Data, descriptor.kind);
        }

        [TestCase("Heal")]
        [TestCase("")]
        public void From_FactoryAndRuntimeSkill_ShareKeyAndArtwork(string label)
        {
            ApplyConsumerCharacterSkillFactory factory =
                ScriptableObject.CreateInstance<ApplyConsumerCharacterSkillFactory>();
            _asset = factory;
            factory.name = "Different asset name";
            factory.data = new ApplyConsumerCharacterSkillData { name = label };
            DataIconDescriptor expected = DataIconDescriptor.From(factory.data);

            DataIconDescriptor fromFactory = DataIconDescriptor.From(factory);
            DataIconDescriptor fromSkill = DataIconDescriptor.From(factory.Create());

            Assert.AreEqual(expected.key, fromFactory.key);
            Assert.AreEqual(expected.key, fromSkill.key);
            CollectionAssert.AreEqual(ProceduralDataIcon.Render(expected, 32), ProceduralDataIcon.Render(fromFactory, 32));
        }

        [Test]
        public void From_FactoryAndRuntimeItem_ShareKey()
        {
            ItemFactory factory = ScriptableObject.CreateInstance<ItemFactory>();
            _asset = factory;
            factory.data = new ItemData { name = "Shield" };
            DataIconDescriptor expected = DataIconDescriptor.From(factory.data);

            DataIconDescriptor fromFactory = DataIconDescriptor.From(factory);
            DataIconDescriptor fromItem = DataIconDescriptor.From(factory.GetItem());

            Assert.AreEqual(expected.key, fromFactory.key);
            Assert.AreEqual(expected.key, fromItem.key);
        }

        [Test]
        public void StableHash_Hello_MatchesFnvVector()
        {
            uint hash = DataIconDescriptor.StableHash("hello");

            Assert.AreEqual(0x4f9f2cabu, hash);
        }

        [TestCase("Heal group", DataIconKind.Spell, DataIconSymbol.Heal)]
        [TestCase("Poison single target", DataIconKind.Spell, DataIconSymbol.Poison)]
        [TestCase("Fire bolt", DataIconKind.Spell, DataIconSymbol.Flame)]
        [TestCase("Buff attack speed", DataIconKind.Spell, DataIconSymbol.Shield)]
        [TestCase("Damage single entity", DataIconKind.Spell, DataIconSymbol.Bolt)]
        [TestCase("Soldier", DataIconKind.Creature, DataIconSymbol.Soldier)]
        [TestCase("Swarm", DataIconKind.Creature, DataIconSymbol.Swarm)]
        [TestCase("TripleShoot", DataIconKind.Creature, DataIconSymbol.Archer)]
        [TestCase("ChainLightning", DataIconKind.Creature, DataIconSymbol.Mage)]
        [TestCase("Forest fox", DataIconKind.Creature, DataIconSymbol.Fox)]
        [TestCase("Dragon", DataIconKind.Creature, DataIconSymbol.Dragon)]
        [TestCase("Slime", DataIconKind.Creature, DataIconSymbol.Slime)]
        [TestCase("Unregistered content", DataIconKind.Data, DataIconSymbol.Generic)]
        public void Symbol_LabelAndKind_ReflectsContent(string label, DataIconKind kind, DataIconSymbol expected)
        {
            DataIconDescriptor descriptor = new DataIconDescriptor("key", label, kind);

            Assert.AreEqual(expected, descriptor.symbol);
        }

        [TestCase("HealerLike.Render.Creatures", DataIconKind.Creature)]
        [TestCase("HealerLike.Render.Creatures.Future", DataIconKind.Creature)]
        [TestCase("HealerLike.Render.Spells", DataIconKind.Spell)]
        [TestCase("HealerLike.Render.Spells.Future", DataIconKind.Spell)]
        [TestCase("HealerLike.Render.SpellsOther", DataIconKind.Data)]
        [TestCase(null, DataIconKind.Data)]
        public void KindFromNamespace_RenderNamespace_MatchesCategory(string dataNamespace, DataIconKind expected)
        {
            DataIconKind kind = DataIconDescriptor.KindFromNamespace(dataNamespace);

            Assert.AreEqual(expected, kind);
        }
    }
}
