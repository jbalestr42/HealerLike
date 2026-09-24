using HealerLike.Render.Creatures.Studio;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureGrammarPresetTests
    {
        CreatureGrammarPreset preset;
        [SetUp] public void SetUp()
        {
            preset = ScriptableObject.CreateInstance<CreatureGrammarPreset>();
            preset.vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>("Assets/Render/Creatures/Data/LookVocabulary.asset");
            Assert.NotNull(preset.vocabulary);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(preset); }

        [TestCase(LookSide.Plant, HeadKind.Bud, CountBand.One, MassBand.Light)]
        [TestCase(LookSide.Plant, HeadKind.Arch, CountBand.Many, MassBand.Sturdy)]
        [TestCase(LookSide.Stone, HeadKind.Ward, CountBand.Few, MassBand.Heavy)]
        public void Compose_UsesExactProductionGrammar(LookSide side, HeadKind head, CountBand count, MassBand mass)
        {
            preset.side = side; preset.head = head; preset.count = count; preset.mass = mass;
            Assert.IsEmpty(preset.Validate(), string.Join("; ", preset.Validate()));
            CreatureRecipe actual = preset.Compose();
            CreatureRecipe expected = LookComposer.Compose(preset.Channels(), preset.vocabulary);
            try
            {
                Assert.NotNull(actual); Assert.NotNull(expected);
                CollectionAssert.AreEqual(expected.parts, actual.parts);
                CollectionAssert.AreEqual(expected.sourceLocal, actual.sourceLocal);
                Assert.AreEqual(expected.roots, actual.roots);
                Assert.AreEqual(expected.idle, actual.idle);
                Assert.AreEqual(expected.neckLocal, actual.neckLocal);
                Assert.AreEqual(expected.wiltColour, actual.wiltColour);
                Assert.AreEqual(expected.stoneOchre, actual.stoneOchre);
                Assert.AreEqual(expected.arms.Length, actual.arms.Length);
                for (int i = 0; i < expected.arms.Length; i++)
                    CollectionAssert.AreEqual(expected.arms[i].restJoints, actual.arms[i].restJoints);
            }
            finally { if (actual != null) Object.DestroyImmediate(actual); if (expected != null) Object.DestroyImmediate(expected); }
        }

        [Test]
        public void Compose_StoneWiltComesFromCentralPaletteAndInvalidColourIsReported()
        {
            LookVocabulary custom = Object.Instantiate(preset.vocabulary);
            LookPalette customPalette = Object.Instantiate(preset.vocabulary.palette);
            try
            {
                custom.palette = customPalette;
                customPalette.stoneWilt = new Color(.2f, .4f, .6f, 1f);
                preset.vocabulary = custom; preset.side = LookSide.Stone;
                CreatureRecipe result = preset.Compose();
                try
                {
                    Assert.NotNull(result);
                    Assert.AreEqual(customPalette.Colour(ColourRole.Wilt, preset.accent, LookSide.Stone), result.wiltColour);
                }
                finally { if (result) Object.DestroyImmediate(result); }
                customPalette.stoneWilt = new Color(float.NaN, 0f, 0f, 1f);
                Assert.That(string.Join(" ", preset.Validate()), Does.Contain("Palette colours"));
                Assert.IsNull(preset.Compose());
            }
            finally { Object.DestroyImmediate(custom); Object.DestroyImmediate(customPalette); }
        }

        [Test]
        public void Channels_DerivesFromEntityAndDetachesWithoutEditingSource()
        {
            EntityData entity = ScriptableObject.CreateInstance<EntityData>();
            EntityData soldier = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/SoldierEntity/SoldierEntity.asset");
            Assert.NotNull(soldier);
            // A real primary skill is required by the production derivation; the fixture owns only its editable attributes.
            entity.skillFactories = new System.Collections.Generic.List<ASkillFactory>(soldier.skillFactories);
            entity.attributes[AttributeType.HealthMax] = 300f;
            entity.attributes[AttributeType.Range] = 2f;
            try
            {
                preset.sourceEntity = entity; preset.sourceSide = Entity.EntityType.Computer; preset.deriveFromEntity = true;
                UnitChannels expected = LookDerivation.Channels(entity, preset.sourceSide);
                Assert.AreEqual(expected, preset.Channels());
                Assert.AreEqual(MassBand.Heavy, preset.Channels().mass);
                Assert.IsTrue(preset.ReadFromEntity());
                Assert.IsFalse(preset.deriveFromEntity);
                Assert.AreEqual(expected, preset.Channels());
                preset.mass = MassBand.Light;
                Assert.AreEqual(300f, entity.attributes[AttributeType.HealthMax]);
                entity.attributes[AttributeType.HealthMax] = 500f;
                Assert.AreEqual(MassBand.Light, preset.Channels().mass);
                Assert.AreSame(entity, preset.sourceEntity);
            }
            finally { Object.DestroyImmediate(entity); }
        }

        [Test]
        public void Compose_ReturnsIndependentBakesWithoutChangingVocabulary()
        {
            LookPart original = preset.vocabulary.heads[preset.head].plant[0];
            CreatureRecipe a = preset.Compose(); CreatureRecipe b = preset.Compose();
            try
            {
                Assert.NotNull(a); Assert.NotNull(b);
                Vector3 before = b.parts[0].dimensions;
                a.parts[0].dimensions = Vector3.one * 99f;
                Assert.AreEqual(before, b.parts[0].dimensions);
                Assert.AreEqual(original, preset.vocabulary.heads[preset.head].plant[0]);
                if (a.arms.Length > 0)
                {
                    a.arms[0].restJoints[0] = Vector3.up;
                    Assert.AreEqual(Vector3.zero, b.arms[0].restJoints[0]);
                }
            }
            finally { if (a != null) Object.DestroyImmediate(a); if (b != null) Object.DestroyImmediate(b); }
        }

        [Test]
        public void Validate_MissingSourcesAndVocabularyTablesAreSafe()
        {
            preset.deriveFromEntity = true;
            Assert.IsNotEmpty(preset.Validate()); Assert.IsNull(preset.Compose()); Assert.IsFalse(preset.ReadFromEntity());
            preset.deriveFromEntity = false; preset.vocabulary = null;
            Assert.IsNotEmpty(preset.Validate()); Assert.IsNull(preset.Compose());
            LookVocabulary malformed = ScriptableObject.CreateInstance<LookVocabulary>();
            try
            {
                malformed.bodies = null; malformed.heads = null; malformed.stems = null; malformed.roots = null;
                preset.vocabulary = malformed;
                Assert.IsNotEmpty(preset.Validate()); Assert.IsNull(preset.Compose());
            }
            finally { Object.DestroyImmediate(malformed); }
        }

        [Test]
        public void Validate_EntityWithoutPrimarySkillReportsActionableErrorWithoutRuntimeLogging()
        {
            EntityData entity = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                preset.sourceEntity = entity;
                preset.deriveFromEntity = true;
                Assert.That(string.Join(" ", preset.Validate()), Does.Contain("primary skill"));
                Assert.IsNull(preset.Compose());
                Assert.IsFalse(preset.ReadFromEntity());
            }
            finally { Object.DestroyImmediate(entity); }
        }

        [Test]
        public void Validate_NullSelectedFragmentIsRejectedBeforeComposerAccessesIt()
        {
            LookVocabulary malformed = ScriptableObject.CreateInstance<LookVocabulary>();
            LookVocabulary original = preset.vocabulary;
            try
            {
                malformed.palette = original.palette;
                malformed.heads = original.heads;
                malformed.stems = original.stems;
                malformed.roots = original.roots;
                malformed.bodies = new System.Collections.Generic.Dictionary<MassBand, LookVocabulary.BodyEntry>(original.bodies);
                malformed.bodies[preset.mass] = new LookVocabulary.BodyEntry { plant = null };
                preset.vocabulary = malformed;
                Assert.That(string.Join(" ", preset.Validate()), Does.Contain("body"));
                Assert.IsNull(preset.Compose());
                Assert.NotNull(original.bodies[preset.mass].plant);
            }
            finally { Object.DestroyImmediate(malformed); }
        }

        [Test]
        public void Validate_InvalidChannelsAreRejectedWithoutMutatingThem()
        {
            preset.head = (HeadKind)999;
            Assert.IsNotEmpty(preset.Validate()); Assert.IsNull(preset.Compose());
            Assert.AreEqual((HeadKind)999, preset.head);
        }

        [Test]
        public void Diagnostics_ExplainsVocabularyBudgetAndPinnedReach()
        {
            Assert.That(string.Join(" ", preset.Diagnostics()), Does.Contain("maxParts"));
            if (preset.vocabulary.isReachPinned)
            {
                Assert.That(string.Join(" ", preset.Diagnostics()), Does.Contain("pinned"));
                preset.reach = ReachBand.Short; CreatureRecipe shortRecipe = preset.Compose();
                preset.reach = ReachBand.Long; CreatureRecipe longRecipe = preset.Compose();
                try { Assert.AreEqual(shortRecipe.roots.footRadius, longRecipe.roots.footRadius); }
                finally { Object.DestroyImmediate(shortRecipe); Object.DestroyImmediate(longRecipe); }
            }
        }

        [Test]
        public void Validate_StoneIgnoresUnusedPlantAndRootSettings()
        {
            LookVocabulary custom = Object.Instantiate(preset.vocabulary);
            try
            {
                // Selected stone data stays valid; these fields are never read by the stone composition path.
                custom.roots = null;
                custom.pinnedReach = float.NaN;
                custom.plantScale = float.NaN;
                var original = custom.stems[preset.stem];
                custom.stems = new System.Collections.Generic.Dictionary<StemBand, LookVocabulary.StemEntry>(custom.stems);
                custom.stems[preset.stem] = new LookVocabulary.StemEntry
                    { length = float.NaN, thickness = float.NaN, limbLength = original.limbLength };
                preset.vocabulary = custom; preset.side = LookSide.Stone;
                Assert.IsEmpty(preset.Validate(), string.Join("; ", preset.Validate()));
                CreatureRecipe actual = preset.Compose();
                CreatureRecipe expected = LookComposer.Compose(preset.Channels(), custom);
                try { Assert.NotNull(actual); CollectionAssert.AreEqual(expected.parts, actual.parts); }
                finally { if (actual) Object.DestroyImmediate(actual); if (expected) Object.DestroyImmediate(expected); }
            }
            finally { Object.DestroyImmediate(custom); }
        }

        [Test]
        public void Validate_UnpinnedPlantIgnoresUnusedFallbackAndRootBand()
        {
            LookVocabulary custom = Object.Instantiate(preset.vocabulary);
            try
            {
                custom.isReachPinned = false; custom.pinnedReach = float.NaN;
                custom.roots = new System.Collections.Generic.Dictionary<ReachBand, LookVocabulary.RootEntry>(custom.roots);
                custom.roots[ReachBand.Short] = null;
                custom.roots[ReachBand.Mid] = new LookVocabulary.RootEntry { reach = 0.8f };
                custom.roots[ReachBand.Long] = new LookVocabulary.RootEntry { reach = 1f };
                preset.vocabulary = custom; preset.reach = ReachBand.Long;
                Assert.IsEmpty(preset.Validate(), string.Join("; ", preset.Validate()));
                CreatureRecipe actual = preset.Compose();
                try { Assert.NotNull(actual); Assert.AreEqual(custom.Unit(LookSide.Plant), actual.roots.footRadius); }
                finally { if (actual) Object.DestroyImmediate(actual); }
            }
            finally { Object.DestroyImmediate(custom); }
        }

        [Test]
        public void Validate_MeasuresAccessoryAfterMaxPartsReducesFannedHeadCopies()
        {
            LookVocabulary custom = ScriptableObject.CreateInstance<LookVocabulary>();
            try
            {
                custom.palette = preset.vocabulary.palette;
                custom.bodyUnit = 1f; custom.plantScale = 1f; custom.maxParts = 4;
                var body = new LookPart { id = "Body", primitive = Primitive.Sphere, role = PartRole.Body,
                    colour = ColourRole.Body, size = Vector3.one };
                var crown = new LookPart { id = "Head", primitive = Primitive.Sphere, role = PartRole.Head,
                    colour = ColourRole.Accent, size = Vector3.one };
                custom.bodies[MassBand.Light] = new LookVocabulary.BodyEntry { plant = new[] { body } };
                custom.heads[HeadKind.Bud] = new LookVocabulary.HeadEntry { plant = new[] { crown }, carriesCount = false };
                custom.stems[StemBand.Steady] = new LookVocabulary.StemEntry { length = 1f, thickness = .1f, limbLength = .4f };
                // This bead lies inside the outermost five-copy head but clearly outside the final single head.
                var bead = new LookPart { id = "Bead", primitive = Primitive.Sphere, role = PartRole.Accessory,
                    colour = ColourRole.Accent, size = Vector3.one * .05f,
                    position = Quaternion.Euler(0f, 0f, -56f) * Vector3.up * 1.2f };
                custom.accessories[AccessoryKind.SmallTorus] = new LookVocabulary.AccessoryEntry
                    { socket = AccessorySocket.NeckOrbit, plant = new[] { bead } };
                preset.vocabulary = custom; preset.count = CountBand.Many; preset.accessory = AccessoryKind.SmallTorus;
                Assert.Less(LookMeasure.AccessoryReach(preset.Channels(), custom), LookComposer.PlantAccessoryReach);
                Assert.IsEmpty(preset.Validate(), string.Join("; ", preset.Validate()));
                CreatureRecipe actual = preset.Compose();
                CreatureRecipe expected = LookComposer.Compose(preset.Channels(), custom);
                try
                {
                    Assert.NotNull(actual); Assert.AreEqual(4, actual.parts.Length);
                    CollectionAssert.AreEqual(expected.parts, actual.parts);
                }
                finally { if (actual) Object.DestroyImmediate(actual); if (expected) Object.DestroyImmediate(expected); }
            }
            finally { Object.DestroyImmediate(custom); }
        }

        [Test]
        public void SavedPreset_ReloadsAllGrammarChannelsAndReferences()
        {
            string path = "Assets/__CreatureGrammarTest_" + System.Guid.NewGuid().ToString("N") + ".asset";
            CreatureGrammarPreset saved = Object.Instantiate(preset);
            try
            {
                saved.displayName = "Persistent ward"; saved.description = "Production channel inputs";
                saved.side = LookSide.Stone; saved.head = HeadKind.Ward; saved.count = CountBand.Many;
                saved.stem = StemBand.Slow; saved.mass = MassBand.Heavy; saved.reach = ReachBand.Long;
                saved.accessory = AccessoryKind.MiniHead; saved.accessoryHead = HeadKind.Spear; saved.accent = EffectFamily.Boon;
                saved.sourceSide = Entity.EntityType.Computer;
                saved.sourceEntity = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/SoldierEntity/SoldierEntity.asset");
                Assert.NotNull(saved.sourceEntity);
                saved.deriveFromEntity = true;
                UnitChannels expectedManual = new UnitChannels { side = saved.side, head = saved.head, count = saved.count,
                    stem = saved.stem, mass = saved.mass, reach = saved.reach, accessory = saved.accessory,
                    accessoryHead = saved.accessoryHead, accent = saved.accent };
                AssetDatabase.CreateAsset(saved, path); AssetDatabase.SaveAssetIfDirty(saved);
                Resources.UnloadAsset(saved); saved = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                saved = AssetDatabase.LoadAssetAtPath<CreatureGrammarPreset>(path);
                Assert.NotNull(saved);
                Assert.AreEqual("Persistent ward", saved.displayName);
                Assert.AreEqual("Production channel inputs", saved.description);
                Assert.AreSame(preset.vocabulary, saved.vocabulary);
                Assert.IsTrue(saved.deriveFromEntity); Assert.NotNull(saved.sourceEntity);
                Assert.AreEqual(Entity.EntityType.Computer, saved.sourceSide);
                Assert.AreEqual(LookDerivation.Channels(saved.sourceEntity, saved.sourceSide), saved.Channels());
                saved.deriveFromEntity = false;
                Assert.AreEqual(expectedManual, saved.Channels());
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (saved != null && !EditorUtility.IsPersistent(saved)) Object.DestroyImmediate(saved);
            }
        }
    }
}
