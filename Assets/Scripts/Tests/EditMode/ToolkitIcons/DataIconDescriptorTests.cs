using NUnit.Framework;
using UnityEngine;
namespace HealerLike.UI.Toolkit.Icons
{
    public class DataIconDescriptorTests
    {
        [Test] public void NullHasUsableMetadata()
        {
            var descriptor = DataIconDescriptor.From(null);
            Assert.That(descriptor.label, Is.EqualTo("Unknown"));
            Assert.That(descriptor.kind, Is.EqualTo(DataIconKind.Data));
        }
        [Test] public void CreatureUsesTitleAndCreatureCategory()
        {
            var data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                data.name = "Asset"; data.title = "Forest guardian";
                var descriptor = DataIconDescriptor.From(data);
                Assert.That(descriptor.label, Is.EqualTo("Forest guardian"));
                Assert.That(descriptor.kind, Is.EqualTo(DataIconKind.Creature));
            }
            finally { Object.DestroyImmediate(data); }
        }
        [Test] public void SpellDataUsesNameAndSpellCategory()
        {
            var descriptor = DataIconDescriptor.From(new CharacterSkillData { name = "Heal" });
            Assert.That(descriptor.label, Is.EqualTo("Heal"));
            Assert.That(descriptor.kind, Is.EqualTo(DataIconKind.Spell));
        }
        [Test] public void StableHashMatchesKnownFnvVector()
        {
            Assert.That(DataIconDescriptor.StableHash("hello"), Is.EqualTo(0x4f9f2cabu));
        }
        [Test] public void ArbitraryDataDoesNotNeedRegistration()
        {
            var descriptor = DataIconDescriptor.From(new ExampleData { title = "New content" });
            Assert.That(descriptor.label, Is.EqualTo("New content"));
            Assert.That(descriptor.kind, Is.EqualTo(DataIconKind.Data));
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
        public void SymbolReflectsContentMeaning(string label, DataIconKind kind, DataIconSymbol expected)
        {
            Assert.That(new DataIconDescriptor("key", label, kind).symbol, Is.EqualTo(expected));
        }
        [TestCase("Heal")]
        [TestCase("")]
        public void FactoryAndRuntimeSkillShareDescriptor(string label)
        {
            var factory = ScriptableObject.CreateInstance<ApplyConsumerCharacterSkillFactory>();
            try
            {
                factory.name = "Different asset name";
                factory.data = new ApplyConsumerCharacterSkillData { name = label };
                var expected = DataIconDescriptor.From(factory.data);
                Assert.That(DataIconDescriptor.From(factory).key, Is.EqualTo(expected.key));
                Assert.That(DataIconDescriptor.From(factory.Create()).key, Is.EqualTo(expected.key));
                CollectionAssert.AreEqual(ProceduralDataIcon.Render(DataIconDescriptor.From(factory), 32),
                    ProceduralDataIcon.Render(expected, 32));
            }
            finally { Object.DestroyImmediate(factory); }
        }
        [Test] public void FactoryAndRuntimeItemShareDescriptor()
        {
            var factory = ScriptableObject.CreateInstance<ItemFactory>();
            try
            {
                factory.data = new ItemData { name = "Shield" };
                var expected = DataIconDescriptor.From(factory.data);
                Assert.That(DataIconDescriptor.From(factory).key, Is.EqualTo(expected.key));
                Assert.That(DataIconDescriptor.From(factory.GetItem()).key, Is.EqualTo(expected.key));
            }
            finally { Object.DestroyImmediate(factory); }
        }
        [TestCase("HealerLike.Render.Creatures", DataIconKind.Creature)]
        [TestCase("HealerLike.Render.Creatures.Future", DataIconKind.Creature)]
        [TestCase("HealerLike.Render.Spells", DataIconKind.Spell)]
        [TestCase("HealerLike.Render.Spells.Future", DataIconKind.Spell)]
        [TestCase("HealerLike.Render.SpellsOther", DataIconKind.Data)]
        [TestCase(null, DataIconKind.Data)]
        public void RenderDataUsesCategoryWithoutAssemblyDependency(string dataNamespace, DataIconKind expected)
        {
            Assert.That(DataIconDescriptor.KindFromNamespace(dataNamespace), Is.EqualTo(expected));
        }
        sealed class ExampleData { public string title; }
    }
}
