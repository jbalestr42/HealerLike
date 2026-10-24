using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace UI.Toolkit
{
#pragma warning disable CS0618 // The scene's FreeLook rigs still use the legacy composer
    public class ToolkitWorldFramingTests
    {
        // The battle HUD on a phone: the header above, the party and spells below
        static readonly Rect battleViewport = new Rect(0.02f, 0.36f, 0.96f, 0.57f);

        GameObject _go;
        CinemachineComposer _composer;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Rig");
            _composer = _go.AddComponent<CinemachineComposer>();
            _composer.m_ScreenX = 0.5f;
            _composer.m_ScreenY = 0.55f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void ScreenPoint_WholeScreen_KeepsTheAuthoredPoint()
        {
            Vector2 point = ToolkitWorldFraming.ScreenPoint(new Vector2(0.5f, 0.55f), new Rect(0f, 0f, 1f, 1f));

            Assert.AreEqual(0.5f, point.x, 0.0001f);
            Assert.AreEqual(0.55f, point.y, 0.0001f);
        }

        [Test]
        public void ScreenPoint_FreeAreaBetweenTheBars_PlacesThePointInsideIt()
        {
            Vector2 point = ToolkitWorldFraming.ScreenPoint(new Vector2(0.5f, 0.5f), battleViewport);

            // Centered in the free area: 0.07 from the top of the screen down to 0.64
            Assert.AreEqual(0.5f, point.x, 0.0001f);
            Assert.AreEqual(0.355f, point.y, 0.0001f);
        }

        [Test]
        public void Update_AimsTheComposerInTheFreeAreaAndRestorePutsItBack()
        {
            ToolkitWorldFraming framing = new ToolkitWorldFraming();

            framing.Update(battleViewport);

            Assert.AreEqual(0.07f + 0.55f * 0.57f, _composer.m_ScreenY, 0.0001f);
            framing.Restore();
            Assert.AreEqual(0.55f, _composer.m_ScreenY, 0.0001f);
            Assert.AreEqual(0.5f, _composer.m_ScreenX, 0.0001f);
        }

        [Test]
        public void Update_TheViewportChanges_StartsFromTheAuthoredPointAgain()
        {
            ToolkitWorldFraming framing = new ToolkitWorldFraming();

            framing.Update(battleViewport);
            framing.Update(new Rect(0f, 0f, 1f, 1f));

            Assert.AreEqual(0.55f, _composer.m_ScreenY, 0.0001f);
            framing.Restore();
        }

        [Test]
        public void Update_NoMeasuredViewport_LeavesTheComposer()
        {
            ToolkitWorldFraming framing = new ToolkitWorldFraming();

            framing.Update(Rect.zero);

            Assert.AreEqual(0.55f, _composer.m_ScreenY, 0.0001f);
        }
    }
#pragma warning restore CS0618
}
