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
                Assert.AreEqual(expected.arms.Length, actual.arms.Length);
                for (int i = 0; i < expected.arms.Length; i++)
                    CollectionAssert.AreEqual(expected.arms[i].restJoints, actual.arms[i].restJoints);
            }
            finally { if (actual != null) Object.DestroyImmediate(actual); if (expected != null) Object.DestroyImmediate(expected); }
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
