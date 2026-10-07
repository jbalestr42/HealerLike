using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public class DeliveryPresentationTests
    {
        [Test]
        public void ScaleAt_PulseBreathesWithoutShrinkingBelowAuthoredSize()
        {
            var look = new DeliveryPresentation { size = 1.3f, pulseAmount = 0.2f, pulseFrequency = 2f };
            Assert.AreEqual(1.3f, look.ScaleAt(0f), 0.0001f);
            Assert.AreEqual(1.56f, look.ScaleAt(0.25f), 0.0001f);
            Assert.AreEqual(1.3f, look.ScaleAt(0.5f), 0.0001f);
            Assert.AreEqual(1.3f, look.ScaleAt(-1f), 0.0001f);
        }

        [Test]
        public void Presets_CoverAllDeliveriesAndAreIdempotent()
        {
            var vocabulary = ScriptableObject.CreateInstance<DeliveryVocabulary>();
            try
            {
                DeliveryPresentationPresets.Apply(vocabulary);
                DeliveryPresentationPresets.Apply(vocabulary);
                foreach (DeliveryStyle style in System.Enum.GetValues(typeof(DeliveryStyle)))
                {
                    Assert.IsTrue(vocabulary.presentation.ContainsKey(style), style.ToString());
                }

                Assert.AreEqual(0.28f, vocabulary.bulletSize);
                Assert.Greater(vocabulary.GetPresentation(DeliveryStyle.Direct).trailSeconds, 0f);
            }
            finally { Object.DestroyImmediate(vocabulary); }
        }

        [Test]
        public void MissingProfile_PreservesTheAuthoredGeometry()
        {
            var vocabulary = ScriptableObject.CreateInstance<DeliveryVocabulary>();
            try
            {
                var look = vocabulary.GetPresentation(DeliveryStyle.Direct);
                Assert.AreEqual(1f, look.ScaleAt(1f));
                Assert.AreEqual(0f, look.trailSeconds);
            }
            finally { Object.DestroyImmediate(vocabulary); }
        }
        [Test]
        public void InvalidAuthoredProfiles_FallBackWithoutNonfiniteTransforms()
        {
            var vocabulary = ScriptableObject.CreateInstance<DeliveryVocabulary>();
            try
            {
                System.Action<DeliveryPresentation>[] invalid =
                {
                    value => value.size = float.NaN,
                    value => value.pulseAmount = float.PositiveInfinity,
                    value => value.pulseFrequency = -1f,
                    value => value.trailSeconds = float.NaN,
                    value => value.trailWidth = 2f,
                    value => value.trailBreakDistance = 0f
                };
                foreach (var change in invalid)
                {
                    var look = new DeliveryPresentation();
                    change(look);
                    Assert.IsFalse(look.IsValid());
                    Assert.AreEqual(1f, look.ScaleAt(1f));
                    vocabulary.presentation[DeliveryStyle.Direct] = look;
                    Assert.IsTrue(vocabulary.GetPresentation(DeliveryStyle.Direct).IsValid());
                }
                Assert.AreEqual(1f, new DeliveryPresentation().ScaleAt(float.NaN));
            }
            finally { Object.DestroyImmediate(vocabulary); }
        }

    }
}
