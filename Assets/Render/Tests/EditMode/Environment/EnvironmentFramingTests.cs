using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public class EnvironmentFramingTests
    {
        [Test]
        public void CrownScale_TallCentralScenery_FitsWholeCrownWithoutMovingGroundAnchor()
        {
            Vector3 eye = new Vector3(0f, 10f, -10f);
            Quaternion rotation = Quaternion.Euler(45f, 0f, 0f);
            Vector3 anchor = new Vector3(0f, 0f, 3f);
            Bounds bounds = new Bounds(anchor + Vector3.up * 3f, new Vector3(2f, 6f, 2f));
            float scale = EnvironmentFraming.CrownScale(bounds, anchor, eye, rotation, 40f, 9f / 16f);
            Assert.That(scale, Is.GreaterThan(0.25f).And.LessThan(1f));
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = anchor + (RenderMath.Corner(bounds, i) - anchor) * scale;
                Vector2 viewport = EnvironmentForeground.ToViewport(point, eye, rotation, 40f, 9f / 16f);
                Assert.That(viewport.y, Is.LessThanOrEqualTo(EnvironmentFraming.CrownCeiling + 0.0001f));
            }
        }

        [Test]
        public void CrownScale_AlreadyInsideFrame_KeepsAuthoredSize()
        {
            Vector3 eye = new Vector3(0f, 10f, -10f);
            Quaternion rotation = Quaternion.Euler(45f, 0f, 0f);
            Bounds bounds = new Bounds(Vector3.up, Vector3.one * 2f);
            Assert.AreEqual(1f, EnvironmentFraming.CrownScale(bounds, Vector3.zero, eye, rotation, 40f, 9f / 16f));
        }

        [Test]
        public void CrownScale_BaseAboveCeiling_LeavesTheVisibleFrameToTheFogRidge()
        {
            Vector3 eye = new Vector3(0f, 10f, -10f);
            Quaternion rotation = Quaternion.Euler(45f, 0f, 0f);
            Vector3 anchor = new Vector3(0f, 0f, 12f);
            Bounds bounds = new Bounds(anchor + Vector3.up * 4f, new Vector3(2f, 8f, 2f));
            Assert.AreEqual(0f, EnvironmentFraming.CrownScale(bounds, anchor, eye, rotation, 40f, 9f / 16f));
        }
    }
}
