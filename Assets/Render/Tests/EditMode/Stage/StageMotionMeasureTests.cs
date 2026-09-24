using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageMotionMeasureTests
    {
        [Test]
        public void Difference_MotionOutsideTheGrassRegion_DoesNotCount()
        {
            Color32[] first = new Color32[16];
            Color32[] second = new Color32[16];
            second[15] = new Color32(255, 255, 255, 255);
            Rect region = new Rect(0f, 0f, 0.5f, 0.5f);
            Assert.AreEqual(Vector2.zero, StageMotionMeasure.Difference(first, second, 4, 4, region));
            second[0] = new Color32(60, 30, 0, 255);
            Assert.AreEqual(new Vector2(7.5f, 0.25f), StageMotionMeasure.Difference(first, second, 4, 4, region));
        }

        [Test]
        public void Difference_BadDimensionsOrEmptyRegion_IsRejected()
        {
            Color32[] pixels = new Color32[16];
            Assert.Less(StageMotionMeasure.Difference(pixels, pixels, 5, 5, Rect.zero).x, 0f);
            Assert.Less(StageMotionMeasure.Difference(pixels, pixels, 4, 4, Rect.zero).x, 0f);
        }

        [Test]
        public void Pass_CameraDriftAndFrozenControlNoise_CannotPassForWind()
        {
            Assert.IsFalse(StageMotionMeasure.Pass(0f, new Vector2(5f, 0.2f), false));
            Assert.IsFalse(StageMotionMeasure.Pass(2f, new Vector2(5f, 0.2f), true));
            Assert.IsFalse(StageMotionMeasure.Pass(0f, new Vector2(5f, 0.001f), true));
            Assert.IsTrue(StageMotionMeasure.Pass(0f, new Vector2(5f, 0.2f), true));
        }
    }
}
