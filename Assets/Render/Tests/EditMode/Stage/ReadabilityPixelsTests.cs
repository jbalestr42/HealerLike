using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class ReadabilityPixelsTests
    {
        static readonly Color32 grass = new Color32(55, 191, 104, 255);
        static readonly Color32 body = new Color32(120, 200, 60, 255);
        static readonly Color32 burst = new Color32(250, 120, 90, 255);

        static Color32[] Fill(int count, Color32 colour)
        {
            Color32[] pixels = new Color32[count];
            for (int i = 0; i < count; i++) pixels[i] = colour;
            return pixels;
        }

        [Test]
        public void Changed_OnlyPixelsPastThreshold_AreMarked()
        {
            Color32[] a = { grass, grass, grass };
            Color32[] b = { grass, new Color32(55, 191 + ReadabilityPixels.Threshold, 104, 255),
                new Color32(55, 191 + ReadabilityPixels.Threshold + 1, 104, 255) };

            CollectionAssert.AreEqual(new[] { false, false, true }, ReadabilityPixels.Changed(a, b));
        }

        [Test]
        public void Survival_EffectCoveringHalfTheBody_KeepsHalf()
        {
            bool[] mask = { true, true, true, true, false, false };
            Color32[] without = { body, body, body, body, grass, grass };
            Color32[] with = { burst, burst, body, body, burst, burst };

            Assert.AreEqual(.5, ReadabilityPixels.Survival(mask, with, without), 1e-9);
        }

        [Test]
        public void Survival_EffectOnlyOnTheField_KeepsTheWholeBody()
        {
            bool[] mask = { true, true, false };
            Color32[] without = { body, body, grass };
            Color32[] with = { body, body, burst };

            Assert.AreEqual(1.0, ReadabilityPixels.Survival(mask, with, without), 1e-9);
        }

        [Test]
        public void Survival_EmptyMask_IsWhole()
        {
            Assert.AreEqual(1.0, ReadabilityPixels.Survival(new bool[2], new[] { burst, burst }, new[] { grass, grass }));
        }

        [Test]
        public void Grow_SinglePixel_CoversItsSquareOnly()
        {
            bool[] mask = new bool[25];
            mask[12] = true;

            bool[] grown = ReadabilityPixels.Grow(mask, 5, 5, 1);

            Assert.AreEqual(9, ReadabilityPixels.Count(grown));
            Assert.IsTrue(grown[6] && grown[8] && grown[16] && grown[18]);
            Assert.IsFalse(grown[0] || grown[4] || grown[20] || grown[24]);
        }

        [Test]
        public void Measure_BodyOnField_ReadsBodyAgainstFieldBehindAndAround()
        {
            // A 3x3 frame, the centre pixel is the body; the frame without it is plain grass
            Color32[] withBody = Fill(9, grass);
            withBody[4] = body;
            Color32[] without = Fill(9, grass);
            bool[] shape = new bool[9];
            shape[4] = true;

            ReadabilityPixels.Contrast contrast = ReadabilityPixels.Measure(shape, withBody, without, 3, 3, 1);

            Assert.AreEqual(1, contrast.pixels);
            CollectionAssert.AreEqual(new double[] { 120, 200, 60 }, contrast.shape);
            CollectionAssert.AreEqual(new double[] { 55, 191, 104 }, contrast.field);
            double expected = System.Math.Sqrt(65 * 65 + 9 * 9 + 44 * 44);
            Assert.AreEqual(expected, contrast.rgbDistance, 1e-9);
            Assert.AreEqual(.2126 * 65 + .7152 * 9 - .0722 * 44, contrast.lumaDifference, 1e-9);
        }

        [Test]
        public void Luma_White_IsFullScale()
        {
            Assert.AreEqual(255.0, ReadabilityPixels.Luma(new double[] { 255, 255, 255 }), 1e-9);
        }
    }
}
