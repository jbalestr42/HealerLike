using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellIconsTests
    {
        class FakeCapture : ISpellIconCapture
        {
            public int calls;
            public bool disposed;
            public bool fail;
            public Texture2D Capture(SpellIconRecipe recipe)
            {
                calls++;
                return fail ? null : new Texture2D(4, 4);
            }
            public void Dispose() { disposed = true; }
        }

        [Test]
        public void Capacity_DoesNotEvictBorrowedImagesOrCollideOnIdenticalNames()
        {
            ApplyConsumerCharacterSkillFactory source = AssetDatabase.LoadAssetAtPath<ApplyConsumerCharacterSkillFactory>(
                "Assets/Data/CharacterSkills/HealSingleTarget/HealSingleTarget.asset");
            FakeCapture capture = new FakeCapture();
            SpellIcons icons = new SpellIcons(RenderTestAssets.LoadEffectVocabulary(), null, capture);
            Texture2D first = null;
            try
            {
                for (int i = 0; i <= SpellIcons.Capacity; i++)
                {
                    ApplyConsumerCharacterSkillData data = new ApplyConsumerCharacterSkillData
                    {
                        name = "Same display name", consumer = source.data.consumer, isSingle = true,
                        entityType = Entity.EntityType.Player
                    };
                    Texture2D image = icons.GetIcon(data);
                    if (i == 0)
                    {
                        first = image;
                    }
                    Assert.AreEqual(i < SpellIcons.Capacity, image != null);
                }
                Assert.AreEqual(SpellIcons.Capacity, capture.calls);
                Assert.IsTrue(first, "A displayed image must survive capacity exhaustion.");
                icons.Dispose();
                Assert.IsTrue(first == null);
            }
            finally
            {
                icons.Dispose();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Cache_SharesFactoryAndDataIncludingFailedCaptures(bool fail)
        {
            ACharacterSkillFactory source = AssetDatabase.LoadAssetAtPath<ACharacterSkillFactory>(
                "Assets/Data/CharacterSkills/HealSingleTarget/HealSingleTarget.asset");
            FakeCapture capture = new FakeCapture { fail = fail };
            SpellIcons icons = new SpellIcons(RenderTestAssets.LoadEffectVocabulary(), null, capture);
            try
            {
                Texture2D image = icons.GetIcon(source);
                Assert.AreSame(image, icons.GetIcon(SpellIconDerivation.Source(source)));
                Assert.AreEqual(1, capture.calls);
                Assert.AreEqual(1, icons.cachedCount);
                int events = 0;
                icons.Changed += () => events++;
                icons.Invalidate();
                Assert.IsTrue(image == null, "Invalidation releases the owned texture.");
                Assert.AreEqual(1, events);
                icons.GetIcon(source);
                Assert.AreEqual(2, capture.calls);
                icons.Dispose();
                Assert.IsTrue(capture.disposed);
                Assert.IsNull(icons.GetIcon(source));
                icons.Dispose();
                Assert.AreEqual(2, events, "Disposal notification is sent only once.");
            }
            finally
            {
                icons.Dispose();
            }
        }
    }
}
