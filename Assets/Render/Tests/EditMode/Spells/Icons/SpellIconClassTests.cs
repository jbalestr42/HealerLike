using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // The HUD's skill-slot icons are sized as the live healer's class casts them, the size the atlas and the in-world
    // look give the same skill (Cleric HealPower 25, no class carries HealthMax so a heal reads against 100)
    public class SpellIconClassTests
    {
        const string Data = "Assets/Data/";
        const string Shield = "CharacterSkills/Shield/Shield";
        const string Heal = "CharacterSkills/HealSingleTarget/HealSingleTarget";
        const string SacredTome = "PlayerItems/HealPowerItem/BuffHandlerFactory";
        // A legacy skill no class lists
        const string Unowned = "CharacterSkills/DamageAllEnemy/DamageAllEnemy";
        // A creature's item: never the healer's stats, even with a live caster
        const string Creature = "EntityItems/ZealItem/BuffHandlerFactory";
        GameObject _caster;

        [TearDown]
        public void TearDown()
        {
            if (_caster) Object.DestroyImmediate(_caster);
        }

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(Data + path + ".asset");
            Assert.IsNotNull(asset, path);
            return asset;
        }

        static object SkillData(string path)
        {
            return SpellIconDerivation.Source(Load<ACharacterSkillFactory>(path));
        }

        Character Caster(string className)
        {
            _caster = new GameObject("Live healer");
            Character character = null;
            // Reset() runs on AddComponent and reaches a BuffManager only Init() wires
            TestHelpers.WithLoggingDisabled(() => character = _caster.AddComponent<Character>());
            character.data = Load<CharacterData>("Characters/" + className + "/" + className);
            return character;
        }

        // The recipe the HUD would capture for the source, through the same owner rule StageInterface wires
        SpellIconRecipe HudIcon(object source, Character caster)
        {
            RecordingCapture capture = new RecordingCapture();
            using (SpellIcons icons = new SpellIcons(RenderTestAssets.LoadEffectVocabulary(), null, capture,
                key => StageInterface.IconOwner(key, caster)))
            {
                Assert.IsNotNull(icons.GetIcon(source));
            }
            Assert.IsNotNull(capture.last);
            return capture.last;
        }

        static SpellIconRecipe Plain(object source)
        {
            SpellIconRecipe icon = SpellIconComposer.Compose(source, RenderTestAssets.LoadEffectVocabulary(), null);
            Assert.IsNotNull(icon);
            return icon;
        }

        // Shield: +0.5 PercentArmor, its own share against damage taken, 0.5 > 0.4 Heavy; plain it reads Light
        [Test]
        public void HudIcon_ShieldForTheCleric_ReadsHeavy()
        {
            object shield = SkillData(Shield);
            SpellIconRecipe icon = HudIcon(shield, Caster("ClericCharacter"));
            Assert.AreEqual(EffectMagnitude.Heavy, icon.layers[0].channels.magnitude);
            Assert.AreEqual(EffectMagnitude.Light, Plain(shield).layers[0].channels.magnitude);
        }

        // Heal: 1 x HealPower x 1.5 on the Cleric's 25 = 37.5, 0.375 Solid; plain 1.5 / 100 Light
        [Test]
        public void HudIcon_HealForTheCleric_ReadsSolid()
        {
            object heal = SkillData(Heal);
            SpellIconRecipe icon = HudIcon(heal, Caster("ClericCharacter"));
            Assert.AreEqual(EffectOperation.Heal, icon.layers[0].channels.operation);
            Assert.AreEqual(EffectMagnitude.Solid, icon.layers[0].channels.magnitude);
            Assert.AreEqual(EffectMagnitude.Light, Plain(heal).layers[0].channels.magnitude);
        }

        // Sacred Tome, the Cleric's starting item: +10 HealPower against its 25, 0.40 on the inclusive Solid bound
        [Test]
        public void HudIcon_SacredTomeForTheCleric_ReadsSolid()
        {
            ABuffHandlerFactory tome = Load<ABuffHandlerFactory>(SacredTome);
            SpellIconRecipe icon = HudIcon(tome, Caster("ClericCharacter"));
            Assert.AreEqual(EffectMagnitude.Solid, icon.layers[0].channels.magnitude);
            Assert.AreEqual(EffectMagnitude.Light, Plain(tome).layers[0].channels.magnitude);
        }

        // No caster and no class listing it: the icon is the one the plain composer gives
        [Test]
        public void HudIcon_UnownedSkillWithoutCaster_KeepsThePlainReading()
        {
            object skill = SkillData(Unowned);
            Assert.IsNull(StageInterface.IconOwner(skill, null));
            SpellIconRecipe icon = HudIcon(skill, null);
            SpellIconRecipe plain = Plain(skill);
            Assert.AreEqual(plain.layers.Count, icon.layers.Count);
            for (int i = 0; i < plain.layers.Count; i++)
            {
                Assert.AreEqual(plain.layers[i].channels, icon.layers[i].channels);
                Assert.AreEqual(plain.layers[i].scale, icon.layers[i].scale);
            }
        }

        // A creature's handler read beside a live Cleric takes no class: its icon is unchanged
        [Test]
        public void HudIcon_CreatureSourceWithACaster_IsUnchanged()
        {
            ABuffHandlerFactory zeal = Load<ABuffHandlerFactory>(Creature);
            Character cleric = Caster("ClericCharacter");
            Assert.IsNull(StageInterface.IconOwner(zeal, cleric));
            SpellIconRecipe icon = HudIcon(zeal, cleric);
            SpellIconRecipe plain = Plain(zeal);
            Assert.AreEqual(plain.layers.Count, icon.layers.Count);
            for (int i = 0; i < plain.layers.Count; i++)
            {
                Assert.AreEqual(plain.layers[i].channels, icon.layers[i].channels);
                Assert.AreEqual(plain.layers[i].scale, icon.layers[i].scale);
            }
        }

        // The live caster sizes what it casts, as the in-world look does: its class, not the first listing class
        [Test]
        public void IconOwner_SkillInTheSlots_IsTheLiveCastersClass()
        {
            Character druid = Caster("DruidCharacter");
            Assert.AreSame(druid.data, StageInterface.IconOwner(SkillData(Shield), druid));
        }

        // The class's size reaches the icon recipe: a presented layer grows by the vocabulary's Heavy over Light scale.
        // (SpellIconSubject then fits every glyph to the tile, so the captured image is framed the same)
        [Test]
        public void Compose_ShieldForTheCleric_DrawsAtTheHeavyScale()
        {
            object shield = SkillData(Shield);
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            CharacterData cleric = Load<CharacterData>("Characters/ClericCharacter/ClericCharacter");
            SpellIconRecipe owned = SpellIconComposer.Compose(shield, vocabulary, null, cleric);
            SpellIconRecipe plain = Plain(shield);
            EffectRecipe layer = owned.layers[0];
            float growth = layer.presentation != null && layer.presentation.enabled
                ? vocabulary.MagnitudeScale(EffectMagnitude.Heavy) / vocabulary.MagnitudeScale(EffectMagnitude.Light)
                : 1f;
            Assert.AreEqual(plain.layers[0].scale * growth, layer.scale, 1e-4f);
        }

        // One skill cast by two classes is two images, never the first class's cached one
        [Test]
        public void GetIcon_SameSourceForAnotherClass_IsCachedSeparately()
        {
            object shield = SkillData(Shield);
            CharacterData owner = Load<CharacterData>("Characters/ClericCharacter/ClericCharacter");
            RecordingCapture capture = new RecordingCapture();
            using (SpellIcons icons = new SpellIcons(RenderTestAssets.LoadEffectVocabulary(), null, capture,
                key => owner))
            {
                icons.GetIcon(shield);
                owner = null;
                icons.GetIcon(shield);
                Assert.AreEqual(2, icons.cachedCount);
                Assert.AreEqual(2, capture.calls);
                icons.GetIcon(shield);
                Assert.AreEqual(2, capture.calls, "The same source and class reuse their image");
            }
        }

        class RecordingCapture : ISpellIconCapture
        {
            public int calls;
            public SpellIconRecipe last;

            public Texture2D Capture(SpellIconRecipe recipe)
            {
                calls++;
                last = recipe;
                return new Texture2D(4, 4);
            }

            public void Dispose()
            {
            }
        }
    }
}
