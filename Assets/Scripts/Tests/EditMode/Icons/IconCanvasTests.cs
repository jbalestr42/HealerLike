using NUnit.Framework;

namespace Icons
{
    public class IconCanvasTests
    {
        static readonly IconColor Red = new IconColor(1f, 0f, 0f);
        static readonly IconColor Blue = new IconColor(0f, 0f, 1f);

        [Test]
        public void EmptyCanvas_IsTransparent()
        {
            var canvas = new IconCanvas(8);

            Assert.AreEqual(0f, canvas.GetPixel(3, 3).a);
        }

        [Test]
        public void FillCircle_CoversItsCentre_NotTheCorners()
        {
            var canvas = new IconCanvas(32);

            canvas.FillCircle(16f, 16f, 10f, Red);

            IconColor centre = canvas.GetPixel(16, 16);
            Assert.AreEqual(1f, centre.a, 1e-4f);
            Assert.AreEqual(1f, centre.r, 1e-4f);
            Assert.AreEqual(0f, canvas.GetPixel(0, 0).a);
            Assert.AreEqual(0f, canvas.GetPixel(31, 31).a);
        }

        [Test]
        public void FillCircle_PartlyCoversItsEdgePixels()
        {
            var canvas = new IconCanvas(32);

            canvas.FillCircle(16f, 16f, 10.5f, Red);

            // The pixel spanning x 26 to 27 is cut in half by the edge at x 26.5
            float edge = canvas.GetPixel(26, 16).a;
            Assert.Greater(edge, 0f);
            Assert.Less(edge, 1f);
        }

        [Test]
        public void FillPolygon_CoversItsInside_NotItsOutside()
        {
            var canvas = new IconCanvas(32);

            canvas.FillPolygon(new[] { (0f, 0f), (32f, 0f), (0f, 32f) }, Red);

            Assert.AreEqual(1f, canvas.GetPixel(4, 4).a, 1e-4f);
            Assert.AreEqual(0f, canvas.GetPixel(28, 28).a);
        }

        [Test]
        public void FillLine_CoversItsWidth_WithRoundEnds()
        {
            var canvas = new IconCanvas(32);

            canvas.FillLine(8f, 16f, 24f, 16f, 6f, Red);

            Assert.AreEqual(1f, canvas.GetPixel(16, 14).a, 1e-4f);
            Assert.AreEqual(0f, canvas.GetPixel(16, 21).a);
            // Round: past the end by less than half the width, beside the axis
            Assert.Greater(canvas.GetPixel(25, 16).a, 0f);
            Assert.AreEqual(0f, canvas.GetPixel(26, 13).a);
        }

        [Test]
        public void FillEllipseRing_LeavesItsInsideEmpty()
        {
            var canvas = new IconCanvas(32);

            canvas.FillEllipseRing(16f, 16f, 12f, 12f, 3f, Red);

            Assert.AreEqual(1f, canvas.GetPixel(16, 5).a, 1e-4f);
            Assert.AreEqual(0f, canvas.GetPixel(16, 16).a);
        }

        [Test]
        public void FillRoundRect_RoundsItsCorners()
        {
            var canvas = new IconCanvas(32);

            canvas.FillRoundRect(0f, 0f, 32f, 32f, 10f, Red);

            Assert.AreEqual(1f, canvas.GetPixel(16, 16).a, 1e-4f);
            Assert.AreEqual(0f, canvas.GetPixel(0, 0).a);
            Assert.AreEqual(1f, canvas.GetPixel(16, 0).a, 1e-4f);
        }

        [Test]
        public void HalfTransparentColour_BlendsOverWhatIsBelow()
        {
            var canvas = new IconCanvas(8);

            canvas.FillRoundRect(0f, 0f, 8f, 8f, 0f, Red);
            canvas.FillRoundRect(0f, 0f, 8f, 8f, 0f, new IconColor(0f, 0f, 1f, 0.5f));

            IconColor pixel = canvas.GetPixel(4, 4);
            Assert.AreEqual(1f, pixel.a, 1e-4f);
            Assert.AreEqual(0.5f, pixel.r, 1e-4f);
            Assert.AreEqual(0.5f, pixel.b, 1e-4f);
        }

        [Test]
        public void LaterShape_PaintsOverEarlierOne()
        {
            var canvas = new IconCanvas(8);

            canvas.FillCircle(4f, 4f, 4f, Red);
            canvas.FillCircle(4f, 4f, 2f, Blue);

            Assert.AreEqual(1f, canvas.GetPixel(4, 4).b, 1e-4f);
            Assert.AreEqual(0f, canvas.GetPixel(4, 4).r, 1e-4f);
        }

        [Test]
        public void ToRgba32_PutsTheBottomRowFirst_WhenAsked()
        {
            var canvas = new IconCanvas(4);
            // The top row only
            canvas.FillRoundRect(0f, 0f, 4f, 1f, 0f, Red);

            byte[] topFirst = canvas.ToRgba32(false);
            byte[] bottomFirst = canvas.ToRgba32(true);

            Assert.AreEqual(255, topFirst[3]);
            Assert.AreEqual(0, bottomFirst[3]);
            Assert.AreEqual(255, bottomFirst[(3 * 4) * 4 + 3]);
            Assert.AreEqual(255, bottomFirst[(3 * 4) * 4]);
        }

        [Test]
        public void Hex_ReadsChannelsAndOptionalAlpha()
        {
            IconColor color = IconColor.Hex("#FF8000");
            IconColor translucent = IconColor.Hex("#00000080");

            Assert.AreEqual(1f, color.r, 1e-4f);
            Assert.AreEqual(128f / 255f, color.g, 1e-4f);
            Assert.AreEqual(0f, color.b, 1e-4f);
            Assert.AreEqual(1f, color.a, 1e-4f);
            Assert.AreEqual(128f / 255f, translucent.a, 1e-4f);
        }

        [Test]
        public void Lighten_GoesTowardsWhite_Darken_TowardsBlack()
        {
            var grey = new IconColor(0.5f, 0.5f, 0.5f);

            Assert.AreEqual(0.75f, grey.Lighten(0.5f).r, 1e-4f);
            Assert.AreEqual(0.25f, grey.Darken(0.5f).r, 1e-4f);
            Assert.AreEqual(1f, grey.Lighten(1f).g, 1e-4f);
            Assert.AreEqual(0f, grey.Darken(1f).b, 1e-4f);
        }
    }
}
