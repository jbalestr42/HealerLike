using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // A spell is built from its caster's material: a stone caster's Burst, Rise and Press are stone, every other
    // element still draws its Plant entry until it has a Stone one
    public class SpellMaterialTests
    {
        static readonly EffectKey[] StoneElements = { EffectKey.Burst, EffectKey.Rise, EffectKey.Press, EffectKey.Spark,
            EffectKey.Echo, EffectKey.Tether, EffectKey.Sprout, EffectKey.Stem };
        static readonly EffectKey[] PlantOnly = { EffectKey.Stalks, EffectKey.Drips, EffectKey.Orbit,
            EffectKey.Plates, EffectKey.Bud, EffectKey.Crack, EffectKey.ManaUp, EffectKey.ManaDown };

        readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned)
            {
                if (owned)
                {
                    Object.DestroyImmediate(owned);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void EffectCell_WithoutSide_IsThePlantCell()
        {
            Assert.AreEqual(new EffectCell(EffectOperation.Heal, EffectAspect.Offence, LookSide.Plant),
                new EffectCell(EffectOperation.Heal, EffectAspect.Offence));
            Assert.AreNotEqual(new EffectCell(EffectOperation.Heal, EffectAspect.Offence, LookSide.Stone),
                new EffectCell(EffectOperation.Heal, EffectAspect.Offence));
        }

        [TestCase(Entity.EntityType.Computer, LookSide.Stone)]
        [TestCase(Entity.EntityType.Player, LookSide.Plant)]
        public void CasterSide_ReadsTheCastersSide(Entity.EntityType type, LookSide expected)
        {
            Assert.AreEqual(expected, LookDerivation.CasterSide(Unit(type)));
        }

        [Test]
        public void CasterSide_WithoutACaster_IsPlant()
        {
            Assert.AreEqual(LookSide.Plant, LookDerivation.CasterSide(null));
        }

        [Test]
        public void Context_StoneCasterOnPlantTarget_IsStone()
        {
            EffectContext context = EffectDerivation.Context(Unit(Entity.EntityType.Computer),
                Unit(Entity.EntityType.Player));
            Assert.AreEqual(LookSide.Stone, context.material);
            context = EffectDerivation.Context(Unit(Entity.EntityType.Player), Unit(Entity.EntityType.Computer));
            Assert.AreEqual(LookSide.Plant, context.material);
        }

        [Test]
        public void Vocabulary_StoneCell_ResolvesItsOwnEntry()
        {
            EffectVocabulary vocabulary = Synthetic(out ElementEntry plantBurst, out ElementEntry stoneBurst);
            Assert.AreSame(stoneBurst, vocabulary.GetEntry(EffectKey.Burst, LookSide.Stone, out LookSide drawn));
            Assert.AreEqual(LookSide.Stone, drawn);
            Assert.AreSame(plantBurst, vocabulary.GetEntry(EffectKey.Burst, LookSide.Plant, out drawn));
            Assert.AreEqual(LookSide.Plant, drawn);
            Assert.AreSame(plantBurst, vocabulary.GetEntry(EffectKey.Burst), "The side-less lookup stays Plant");
        }

        [Test]
        public void Vocabulary_NoStoneCell_FallsBackToThePlantEntry()
        {
            EffectVocabulary vocabulary = Synthetic(out _, out _);
            ElementEntry plantRise = vocabulary.entries[EffectKey.Rise];
            Assert.AreSame(plantRise, vocabulary.GetEntry(EffectKey.Rise, LookSide.Stone, out LookSide drawn));
            Assert.AreEqual(LookSide.Plant, drawn);
        }

        [Test]
        public void Shipped_StoneElements_DrawStoneEntries()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            foreach (EffectKey element in StoneElements)
            {
                EffectRecipe plant = Compose(vocabulary, element, LookSide.Plant);
                EffectRecipe stone = Compose(vocabulary, element, LookSide.Stone);
                Assert.IsNotNull(stone, element.ToString());
                Assert.AreEqual(LookSide.Stone, stone.material, element.ToString());
                Assert.AreNotSame(plant.entry, stone.entry, element.ToString());
                Assert.AreEqual(plant.motion, stone.motion, element + " keeps its motion");
                Assert.AreEqual(plant.socket, stone.socket, element + " keeps its socket");
                Assert.AreEqual(plant.colour, stone.colour, element + " keeps its operation's hue");
            }
        }

        [Test]
        public void Shipped_OtherElements_FallBackToPlantAndStillDraw()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            foreach (EffectKey element in PlantOnly)
            {
                EffectRecipe plant = Compose(vocabulary, element, LookSide.Plant);
                EffectRecipe stone = Compose(vocabulary, element, LookSide.Stone);
                Assert.IsNotNull(stone, element + " must draw, not vanish");
                Assert.AreSame(plant.entry, stone.entry, element.ToString());
                Assert.AreEqual(LookSide.Plant, stone.material, element + " reports that it draws Plant");
                Assert.Greater(EffectComposer.Shapes(stone.entry), 0, element.ToString());
            }
        }

        [Test]
        public void Shipped_StoneEntries_HaveNoGrownParts()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            foreach (EffectKey element in StoneElements)
            {
                ElementEntry entry = vocabulary.GetEntry(element, LookSide.Stone, out _);
                foreach (LookPart[] group in new[] { entry.parts, entry.stackBeads, entry.criticalRings, entry.sideRim })
                {
                    foreach (LookPart part in group)
                    {
                        string name = element + "/" + part.id;
                        Assert.AreNotEqual(Primitive.Sphere, part.primitive, name);
                        Assert.IsTrue(part.shape.kind == ShapeKind.Block || part.shape.kind == ShapeKind.Shard
                            || (part.shape.kind == ShapeKind.Ring && part.shape.faceted), name + " is " + part.shape.kind);
                        Assert.AreEqual(0f, part.shape.bend, name + " is straight");
                        Assert.AreEqual(0f, part.shape.bow, name + " is straight");
                    }
                }
            }
        }

        [Test]
        public void Shipped_StoneCasterImpact_DrawsStone()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            EffectRecipe hit = EffectComposer.Impact(vocabulary, ResourceKind.Health, false, 0.3f, LookSide.Stone);
            EffectRecipe heal = EffectComposer.Impact(vocabulary, ResourceKind.Health, true, 0.3f, LookSide.Stone);
            Assert.AreEqual(EffectKey.Burst, hit.element);
            Assert.AreEqual(LookSide.Stone, hit.material);
            Assert.AreEqual(EffectKey.Rise, heal.element);
            Assert.AreEqual(LookSide.Stone, heal.material);
            Assert.AreEqual(LookSide.Plant, EffectComposer.Impact(vocabulary, ResourceKind.Health, true, 0.3f).material);
        }

        [Test]
        public void Shipped_StoneCasterChannels_DrawStonePress()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            EffectChannels channels = new EffectChannels { operation = EffectOperation.Bane,
                aspect = EffectAspect.Offence, family = EffectFamily.Bane, material = LookSide.Stone };
            EffectRecipe recipe = EffectComposer.Compose(vocabulary, channels, 1, 0);
            Assert.AreEqual(EffectKey.Press, recipe.element);
            Assert.AreEqual(LookSide.Stone, recipe.material);
        }

        static EffectRecipe Compose(EffectVocabulary vocabulary, EffectKey element, LookSide material)
        {
            return EffectComposer.Compose(vocabulary, element, EffectFamily.Damage, EffectTempo.Once, 0f, 3, 3, 0.5f,
                material: material);
        }

        GameObject Unit(Entity.EntityType type)
        {
            GameObject go = new GameObject(type.ToString());
            _owned.Add(go);
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
            entity.entityType = type;
            return go;
        }

        EffectVocabulary Synthetic(out ElementEntry plantBurst, out ElementEntry stoneBurst)
        {
            EffectVocabulary vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            _owned.Add(vocabulary);
            plantBurst = new ElementEntry { label = "Plant burst" };
            stoneBurst = new ElementEntry { label = "Stone burst" };
            vocabulary.entries[EffectKey.Burst] = plantBurst;
            vocabulary.entries[EffectKey.Rise] = new ElementEntry { label = "Plant rise" };
            vocabulary.cells[new EffectCell(EffectOperation.Damage, EffectAspect.Offence)] =
                new EffectCellEntries(plantBurst, null, false);
            vocabulary.cells[new EffectCell(EffectOperation.Heal, EffectAspect.Offence)] =
                new EffectCellEntries(vocabulary.entries[EffectKey.Rise], null, false);
            vocabulary.cells[new EffectCell(EffectOperation.Damage, EffectAspect.Offence, LookSide.Stone)] =
                new EffectCellEntries(stoneBurst, null, false);
            return vocabulary;
        }
    }
}
