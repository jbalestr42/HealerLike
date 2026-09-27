using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellIconComposerTests
    {
        static readonly string root = "Assets/Data/CharacterSkills/";

        [TestCase("HealSingleTarget/HealSingleTarget", EffectOperation.Heal, EffectReach.Single)]
        [TestCase("HealMultiTarget/HealMultiTarget", EffectOperation.Heal, EffectReach.All)]
        [TestCase("DamageSingleEntity/DamageSingleEntity", EffectOperation.Damage, EffectReach.Single)]
        [TestCase("DamageAllEnemy/DamageAllEnemy", EffectOperation.Damage, EffectReach.All)]
        [TestCase("PoisonSingleTarget/PoisonSingleTarget", EffectOperation.Damage, EffectReach.Single)]
        public void ShippedSpells_ReadDataRatherThanNames(string path, EffectOperation operation, EffectReach reach)
        {
            ACharacterSkillFactory source = AssetDatabase.LoadAssetAtPath<ACharacterSkillFactory>(root + path + ".asset");
            Assert.IsNotNull(source);
            SpellIconRecipe icon = SpellIconComposer.Compose(source, RenderTestAssets.LoadEffectVocabulary(), null);
            Assert.IsNotNull(icon);
            Assert.AreEqual(operation, icon.layers[0].channels.operation);
            Assert.AreEqual(reach, icon.reach);
            Assert.AreEqual(EffectOrigin.Healer, icon.origin);
            CharacterSkillData data = SpellIconDerivation.Source(source) as CharacterSkillData;
            string label = data.name;
            try
            {
                data.name = "Unrelated label that says poison and shield";
                SpellIconRecipe renamed = SpellIconComposer.Compose(source, RenderTestAssets.LoadEffectVocabulary(), null);
                Assert.AreEqual(icon.layers[0].element, renamed.layers[0].element);
                Assert.AreEqual(icon.layers[0].channels, renamed.layers[0].channels);
            }
            finally
            {
                data.name = label;
            }
        }

        [Test]
        public void CompoundHandler_PreservesLayersAndAuthoredOverrideSnapshots()
        {
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            SpellLooks looks = ScriptableObject.CreateInstance<SpellLooks>();
            EffectRecipeAsset asset = ScriptableObject.CreateInstance<EffectRecipeAsset>();
            try
            {
                EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
                asset.recipe = EffectComposer.Compose(vocabulary, EffectElement.Burst, EffectFamily.Damage,
                    EffectTempo.Once, 0, 1, 0, .5f);
                asset.recipe.additions = new[] { EffectComposer.Compose(vocabulary, EffectElement.Plates,
                    EffectFamily.Boon, EffectTempo.ForDuration, 0, 1, 3, 0) };
                looks.buffs.Add(handler, new SpellLook { recipe = asset });
                SpellIconRecipe icon = SpellIconComposer.Compose(handler, vocabulary, looks);
                Assert.AreEqual(2, icon.layers.Count);
                Assert.AreEqual(EffectElement.Plates, icon.layers[1].element);
                Assert.AreEqual(0, icon.layers[0].additions.Length);
                icon.layers[1].entry.parts[0].size = Vector3.one * 8;
                Assert.AreNotEqual(icon.layers[1].entry.parts[0].size, asset.recipe.additions[0].entry.parts[0].size);
                asset.recipe.additions = new[] { asset.recipe };
                Assert.IsNull(SpellIconComposer.Compose(handler, vocabulary, looks));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(looks);
                Object.DestroyImmediate(handler);
            }
        }

        [Test]
        public void UnsupportedData_PreservesTheUiFallback()
        {
            Assert.IsNull(SpellIconComposer.Compose(new object(), RenderTestAssets.LoadEffectVocabulary(), null));
            Assert.IsNull(SpellIconComposer.Compose(null, RenderTestAssets.LoadEffectVocabulary(), null));
        }
    }
}
