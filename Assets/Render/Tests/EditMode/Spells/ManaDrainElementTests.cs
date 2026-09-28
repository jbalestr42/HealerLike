using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;

namespace HealerLike.Render.Spells
{
    // A mana gain and a mana drain are different operations, so the shipped vocabulary draws them apart
    public class ManaDrainElementTests
    {
        const string DrainHandler = "Assets/Data/EntityItems/SiphonItem/BuffHandlerFactory.asset";
        const string GainHandler = "Assets/Data/PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory.asset";

        [TestCase(true)]
        [TestCase(false)]
        public void ShippedDrainHandler_ResolvesToManaDown(bool isSameSide)
        {
            ABuffHandlerFactory handler = Load(DrainHandler);
            Assert.IsTrue(HasBuff<DrainCharacterManaBuffFactory>(handler), "Siphon no longer drains mana");

            EffectChannels channels = EffectDerivation.Channels(handler, isSameSide);

            Assert.AreEqual(EffectOperation.ManaDrain, channels.operation);
            Assert.AreEqual(EffectKey.ManaDown, EffectComposer.Element(RenderTestAssets.LoadEffectVocabulary(), channels));
            foreach (EffectChannels layer in EffectDerivation.Layers(handler, isSameSide))
                Assert.AreEqual(EffectKey.ManaDown, EffectComposer.Element(RenderTestAssets.LoadEffectVocabulary(), layer));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ShippedGainHandler_ResolvesToManaUp(bool isSameSide)
        {
            ABuffHandlerFactory handler = Load(GainHandler);
            Assert.IsTrue(HasBuff<ManaOnRoundEndBuffFactory>(handler), "Mana item no longer grants mana");

            EffectChannels channels = EffectDerivation.Channels(handler, isSameSide);

            Assert.AreEqual(EffectOperation.Mana, channels.operation);
            Assert.AreEqual(EffectKey.ManaUp, EffectComposer.Element(RenderTestAssets.LoadEffectVocabulary(), channels));
            foreach (EffectChannels layer in EffectDerivation.Layers(handler, isSameSide))
                Assert.AreEqual(EffectKey.ManaUp, EffectComposer.Element(RenderTestAssets.LoadEffectVocabulary(), layer));
        }

        [Test]
        public void ShippedTable_EveryManaDrainCell_DrawsManaDown()
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            foreach (EffectAspect aspect in System.Enum.GetValues(typeof(EffectAspect)))
                foreach (EffectTempo tempo in System.Enum.GetValues(typeof(EffectTempo)))
                {
                    Assert.IsTrue(vocabulary.TryGetElement(EffectOperation.ManaDrain, aspect, tempo, out EffectKey element),
                        aspect + "/" + tempo);
                    Assert.AreEqual(EffectKey.ManaDown, element, aspect + "/" + tempo);
                }
        }

        static ABuffHandlerFactory Load(string path)
        {
            ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);
            Assert.IsNotNull(handler, path);
            return handler;
        }

        static bool HasBuff<T>(ABuffHandlerFactory handler) where T : ABuffFactory
        {
            foreach (ABuffFactory buff in EffectDerivation.Buffs(handler))
                if (buff is T) return true;
            return false;
        }
    }
}
