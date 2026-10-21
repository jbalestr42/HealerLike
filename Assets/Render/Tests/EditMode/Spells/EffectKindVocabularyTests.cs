using System.Collections.Generic;
using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // The kind table is read before the cells: a kind with an entry draws it, a kind with no entry for the caster's
    // material draws its Plant entry, and a kind with no entry at all draws its cell exactly as before
    public class EffectKindVocabularyTests
    {
        static readonly EffectKind[] Authored = { EffectKind.Projectile, EffectKind.Volume, EffectKind.Rate,
            EffectKind.Conditional, EffectKind.Positional, EffectKind.Flat, EffectKind.Reactive, EffectKind.Echo,
            EffectKind.Link, EffectKind.Summon, EffectKind.Growth };
        // The kinds with a Stone entry of their own; the six Boon kinds before them draw Plant for a stone caster
        static readonly EffectKind[] StoneAuthored = { EffectKind.Reactive, EffectKind.Echo, EffectKind.Link,
            EffectKind.Summon, EffectKind.Growth };

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
        public void KindCell_WithoutSide_IsThePlantCell()
        {
            Assert.AreEqual(new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Flat, LookSide.Plant),
                new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Flat));
            Assert.AreNotEqual(new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Flat, LookSide.Stone),
                new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Flat));
        }

        [Test]
        public void KindKey_EveryKindButPlain_HasItsOwnElementAndReadsBack()
        {
            HashSet<EffectKey> keys = new HashSet<EffectKey>();
            foreach (EffectKind kind in Authored)
            {
                EffectKey key = EffectVocabulary.KindKey(kind);
                Assert.IsTrue(keys.Add(key), kind.ToString());
                Assert.AreEqual(kind, EffectVocabulary.KindOf(key));
            }
            Assert.AreEqual(EffectKind.Plain, EffectVocabulary.KindOf(EffectKey.Orbit));
        }

        [Test]
        public void Element_KindWithAnEntry_DrawsThatEntry()
        {
            EffectVocabulary vocabulary = Synthetic(out ElementEntry orbit, out ElementEntry dart);
            EffectChannels channels = Boon(EffectKind.Projectile, LookSide.Plant);
            Assert.AreEqual(EffectKey.Dart, EffectComposer.Element(vocabulary, channels));
            Assert.AreSame(dart, vocabulary.GetEntry(EffectKey.Dart, LookSide.Plant, out LookSide drawn));
            Assert.AreEqual(LookSide.Plant, drawn);
        }

        [Test]
        public void Element_KindWithAStoneEntry_DrawsStone()
        {
            EffectVocabulary vocabulary = Synthetic(out _, out _);
            ElementEntry stoneDart = Entry("Stone dart");
            vocabulary.kinds[new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Projectile,
                LookSide.Stone)] = stoneDart;
            Assert.AreEqual(EffectKey.Dart, EffectComposer.Element(vocabulary, Boon(EffectKind.Projectile, LookSide.Stone)));
            Assert.AreSame(stoneDart, vocabulary.GetEntry(EffectKey.Dart, LookSide.Stone, out LookSide drawn));
            Assert.AreEqual(LookSide.Stone, drawn);
        }

        [Test]
        public void Element_KindWithNoEntryForItsMaterial_DrawsThePlantEntry()
        {
            EffectVocabulary vocabulary = Synthetic(out _, out ElementEntry dart);
            Assert.AreEqual(EffectKey.Dart, EffectComposer.Element(vocabulary, Boon(EffectKind.Projectile, LookSide.Stone)));
            Assert.AreSame(dart, vocabulary.GetEntry(EffectKey.Dart, LookSide.Stone, out LookSide drawn));
            Assert.AreEqual(LookSide.Plant, drawn);
        }

        [TestCase(EffectKind.Rate)]
        [TestCase(EffectKind.Plain)]
        public void Element_KindWithNoEntryAtAll_FallsThroughToTheCell(EffectKind kind)
        {
            EffectVocabulary vocabulary = Synthetic(out ElementEntry orbit, out _);
            Assert.AreEqual(EffectKey.Orbit, EffectComposer.Element(vocabulary, Boon(kind, LookSide.Plant)));
            Assert.AreEqual(EffectKey.Orbit, EffectComposer.Element(vocabulary, Boon(kind, LookSide.Stone)));
            Assert.AreSame(orbit, vocabulary.GetEntry(EffectKey.Orbit, LookSide.Plant, out _));
        }

        [Test]
        public void Element_KindEntryOfAnotherCell_LeavesThisCellAlone()
        {
            EffectVocabulary vocabulary = Synthetic(out _, out _);
            vocabulary.kinds[new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Flat)] =
                Entry("Boon canopy");
            EffectChannels bane = new EffectChannels { operation = EffectOperation.Bane, aspect = EffectAspect.Offence,
                family = EffectFamily.Bane, kind = EffectKind.Flat };
            Assert.AreEqual(EffectKey.Press, EffectComposer.Element(vocabulary, bane));
        }

        [Test]
        public void Shipped_AuthoredKinds_ResolveInBoonGoldInTheirOwnMaterialOrPlant()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            EffectRecipe orbit = EffectComposer.Compose(vocabulary, Boon(EffectKind.Plain, LookSide.Plant), 1, 0);
            foreach (EffectKind kind in Authored)
            {
                foreach (LookSide material in new[] { LookSide.Plant, LookSide.Stone })
                {
                    EffectRecipe recipe = EffectComposer.Compose(vocabulary, Boon(kind, material), 1, 0);
                    Assert.IsNotNull(recipe, kind + " " + material);
                    Assert.AreEqual(EffectVocabulary.KindKey(kind), recipe.element, kind.ToString());
                    LookSide drawn = material == LookSide.Stone && StoneAuthored.Contains(kind) ? LookSide.Stone
                        : LookSide.Plant;
                    Assert.AreEqual(drawn, recipe.material, kind + " draws " + drawn + " for " + material);
                    Assert.AreEqual(orbit.colour, recipe.colour, kind + " keeps the boon's hue");
                }
            }
        }

        [Test]
        public void Shipped_AuthoredKinds_AreValidPlantKitParts()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            foreach (EffectKind kind in Authored)
            {
                ElementEntry entry = vocabulary.GetEntry(EffectVocabulary.KindKey(kind));
                Assert.IsNotNull(entry, kind.ToString());
                foreach (LookPart part in entry.parts)
                {
                    string name = kind + "/" + part.id;
                    Assert.IsTrue(part.shape.isProcedural && part.shape.IsValid(), name);
                    Assert.AreNotEqual(Primitive.Boulder, part.primitive, name);
                    Assert.AreNotEqual(Primitive.Pyramid, part.primitive, name);
                    Assert.AreNotEqual(Primitive.Stone, part.primitive, name);
                    Assert.IsFalse(part.shape.faceted, name + " is smooth");
                }
            }
        }

        // Structurally different, not six variations of one ring: no two kinds share their motion, socket, part
        // count and primitive make-up
        [Test]
        public void Shipped_AuthoredKinds_HaveDistinctConstructions()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            HashSet<string> signatures = new HashSet<string>();
            HashSet<string> shapes = new HashSet<string>();
            foreach (EffectKind kind in Authored)
            {
                ElementEntry entry = vocabulary.GetEntry(EffectVocabulary.KindKey(kind));
                string make = string.Join(",", entry.parts.GroupBy(part => part.primitive).OrderBy(group => group.Key)
                    .Select(group => group.Key + "x" + group.Count()));
                Assert.IsTrue(shapes.Add(make), kind + " repeats the construction " + make);
                Assert.IsTrue(signatures.Add(entry.motion + "/" + entry.socket + "/" + make), kind.ToString());
            }
        }

        [Test]
        public void Shipped_LiveBoonOffenceHandlers_DrawTheirKindNotOrbit()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            HashSet<string> awaiting = new HashSet<string>();
            foreach (string path in HealerLike.Render.Grammar.SpellChannelAssetPinningTests.AwaitingTreatment)
            {
                awaiting.Add("Assets/Data/" + path + ".asset");
            }
            // On the holder of its item: a growing item's handler draws its growth
            EffectContext growth = HealerLike.Render.Grammar.EffectKindDerivationTests.GrowthContext();
            foreach (string guid in AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // A handler with no look by design is a listed gap, not a vocabulary promise
                if (awaiting.Contains(path))
                {
                    continue;
                }

                EffectChannels channels = EffectDerivation.Channels(
                    AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path), true, growth);
                if (channels.operation != EffectOperation.Boon || channels.aspect != EffectAspect.Offence)
                {
                    continue;
                }

                Assert.AreEqual(EffectVocabulary.KindKey(channels.kind), EffectComposer.Element(vocabulary, channels), path);
            }
        }

        // The Hungering Mask's battle growth costs health: its growth draws the Stem in the Bane defence cell too
        [Test]
        public void Shipped_GrowthOnABaneDefence_DrawsTheStemNotCrack()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            EffectChannels bane = new EffectChannels { operation = EffectOperation.Bane, aspect = EffectAspect.Defence,
                family = EffectFamily.Bane, group = AttributeGroup.Defence, tempo = EffectTempo.ForDuration,
                kind = EffectKind.Growth };
            Assert.AreEqual(EffectKey.Stem, EffectComposer.Element(vocabulary, bane));
            bane.material = LookSide.Stone;
            EffectRecipe stone = EffectComposer.Compose(vocabulary, bane, 1, 0);
            Assert.AreEqual(EffectKey.Stem, stone.element);
            Assert.AreEqual(LookSide.Stone, stone.material, "a stone caster's growth draws the Stone stem");
            bane.material = LookSide.Plant;
            bane.kind = EffectKind.Plain;
            Assert.AreEqual(EffectKey.Crack, EffectComposer.Element(vocabulary, bane));
        }

        // Each stack the holder's item grows shows one more segment of the stem, up to its bud
        [Test]
        public void Shipped_Stem_GrowsWithStacks()
        {
            ElementEntry stem = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectKey.Stem);
            Assert.AreEqual(EffectCount.Stacks, stem.count);
            Assert.Less(EffectComposer.Count(stem, 1, 0, 0), EffectComposer.Count(stem, 3, 0, 0));
            Assert.AreEqual(EffectComposer.Shapes(stem), EffectComposer.Count(stem, 99, 0, 0));
        }

        static EffectChannels Boon(EffectKind kind, LookSide material)
        {
            return new EffectChannels { operation = EffectOperation.Boon, aspect = EffectAspect.Offence,
                family = EffectFamily.Boon, group = AttributeGroup.Offence, tempo = EffectTempo.ForDuration,
                kind = kind, material = material };
        }

        static ElementEntry Entry(string label)
        {
            return new ElementEntry { label = label, cycleSeconds = 1f, parts = new[] { new LookPart { id = label,
                primitive = Primitive.Sphere, shape = ShapeProfile.Bulb(), size = Vector3.one } } };
        }

        EffectVocabulary Synthetic(out ElementEntry orbit, out ElementEntry dart)
        {
            EffectVocabulary vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            _owned.Add(vocabulary);
            orbit = Entry("Plant orbit");
            dart = Entry("Plant dart");
            vocabulary.entries[EffectKey.Orbit] = orbit;
            vocabulary.entries[EffectKey.Press] = Entry("Plant press");
            vocabulary.cells[new EffectCell(EffectOperation.Boon, EffectAspect.Offence)] =
                new EffectCellEntries(orbit, null, false);
            vocabulary.cells[new EffectCell(EffectOperation.Bane, EffectAspect.Offence)] =
                new EffectCellEntries(vocabulary.entries[EffectKey.Press], null, false);
            vocabulary.kinds[new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, EffectKind.Projectile)] = dart;
            return vocabulary;
        }
    }
}
