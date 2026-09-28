using System.Collections.Generic;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The editor tools read a class's own skills and items as that class casts them, the atlas's rule, over the
    // project's classes: Shield (Cleric, +0.5 PercentArmor, its own share) is Heavy where the plain reading is Light
    public class SpellToolClassTests
    {
        const string Data = "Assets/Data/";
        const string ShieldHandler = Data + "CharacterSkills/Shield/BuffHandlerFactory.asset";
        const string ShieldSkill = Data + "CharacterSkills/Shield/Shield.asset";
        const string HealSkill = Data + "CharacterSkills/HealSingleTarget/HealSingleTarget.asset";
        const string UnownedSkill = Data + "CharacterSkills/DamageAllEnemy/DamageAllEnemy.asset";
        const string CreatureHandler = Data + "EntityItems/ZealItem/BuffHandlerFactory.asset";

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, path);
            return asset;
        }

        // The runtime's editor scan and the atlas list are the same classes in the same order
        [Test]
        public void ProjectClasses_MatchTheAtlasCharacters()
        {
            CollectionAssert.AreEqual(AtlasAssetCatalog.Characters(), PlayerClassContext.ProjectClasses());
        }

        [Test]
        public void Census_ClassHandler_SizedByItsClass()
        {
            ABuffHandlerFactory shield = Load<ABuffHandlerFactory>(ShieldHandler);
            Assert.AreEqual(EffectMagnitude.Heavy, SpellLookCensus.Channels(shield, AtlasAssetCatalog.Characters()).magnitude);
            Assert.AreEqual(EffectMagnitude.Light, EffectDerivation.Channels(shield, true).magnitude);
        }

        [Test]
        public void Census_CreatureHandler_KeepsThePlainReading()
        {
            ABuffHandlerFactory zeal = Load<ABuffHandlerFactory>(CreatureHandler);
            Assert.AreEqual(EffectDerivation.Channels(zeal, true),
                SpellLookCensus.Channels(zeal, AtlasAssetCatalog.Characters()));
        }

        // The icon capture's Shield is Heavy, its Heal (25 x 1.5 = 37.5 against 100) Solid
        [TestCase(ShieldSkill, EffectMagnitude.Heavy)]
        [TestCase(HealSkill, EffectMagnitude.Solid)]
        public void IconCapture_ClassSkill_SizedByItsClass(string path, EffectMagnitude expected)
        {
            SpellIconRecipe icon = SpellIconRun.Compose(Load<ACharacterSkillFactory>(path),
                RenderTestAssets.LoadEffectVocabulary(), null, AtlasAssetCatalog.Characters());
            Assert.IsNotNull(icon, path);
            Assert.AreEqual(expected, icon.layers[0].channels.magnitude, path);
        }

        [Test]
        public void IconCapture_UnownedSkill_KeepsThePlainReading()
        {
            ACharacterSkillFactory skill = Load<ACharacterSkillFactory>(UnownedSkill);
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            SpellIconRecipe icon = SpellIconRun.Compose(skill, vocabulary, null, AtlasAssetCatalog.Characters());
            SpellIconRecipe plain = SpellIconComposer.Compose(skill, vocabulary, null);
            Assert.AreEqual(plain.layers.Count, icon.layers.Count);
            for (int i = 0; i < plain.layers.Count; i++)
            {
                Assert.AreEqual(plain.layers[i].channels, icon.layers[i].channels);
            }
        }

        // The polish capture resolves the element of the class-sized channels, and a creature's the plain one
        [TestCase(ShieldHandler)]
        [TestCase(CreatureHandler)]
        public void PolishCapture_Resolve_UsesTheClassSizedChannels(string path)
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            List<CharacterData> characters = AtlasAssetCatalog.Characters();
            ABuffHandlerFactory handler = Load<ABuffHandlerFactory>(path);
            Assert.AreEqual(EffectComposer.Element(vocabulary, PlayerClassContext.Channels(handler, true, characters)),
                SpellPolishRun.Resolve(vocabulary, path, characters));
        }
    }
}
