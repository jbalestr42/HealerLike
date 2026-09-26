using System.Text.RegularExpressions;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{
    public class GrassFieldTests : AGrassFieldFixture
    {
        [Test]
        public void BladeSegments_OutsideRange_Clamps()
        {
            Assert.AreEqual(4, _field.bladeSegments);

            _field.bladeSegments = 0;
            Assert.AreEqual(1, _field.bladeSegments);

            _field.bladeSegments = 99;
            Assert.AreEqual(GrassBladeMesh.MaxSegments, _field.bladeSegments);
        }

        [Test]
        public void TuftBudget_Default_IsTheMaximum()
        {
            Assert.AreEqual(GrassLayout.MaxBudget, _field.tuftBudget);
        }

        [Test]
        public void TuftBudget_OutsideRange_Clamps()
        {
            _field.tuftBudget = int.MaxValue;

            Assert.AreEqual(GrassLayout.MaxBudget, _field.tuftBudget);

            _field.tuftBudget = -1;

            Assert.AreEqual(0, _field.tuftBudget);
        }

        [Test]
        public void BladeHeightScale_OutsideRange_ClampsAndResetsNaN()
        {
            _field.bladeHeightScale = 0.8f;
            Assert.AreEqual(0.8f, _field.bladeHeightScale);

            _field.bladeHeightScale = 3f;
            Assert.AreEqual(1f, _field.bladeHeightScale);

            _field.bladeHeightScale = -1f;
            Assert.AreEqual(0.25f, _field.bladeHeightScale);

            _field.bladeHeightScale = float.NaN;
            Assert.AreEqual(1f, _field.bladeHeightScale);
        }

        [Test]
        public void SetZoneSnapshot_CountWithoutBuffer_KeepsPreviousSnapshot()
        {
            _field.SetZoneSnapshot(null, 0);

            TestHelpers.WithLoggingDisabled(() => _field.SetZoneSnapshot(null, 1));
            TestHelpers.WithLoggingDisabled(() => _field.SetZoneSnapshot(null, ZonePacker.MaxZones + 1));

            Assert.AreEqual(0, _field.activeZoneCount);
        }

        [Test]
        public void Init_WithoutZoneBuffer_LogsAndStaysUnbuilt()
        {
            string message = "[GrassField] Borrow a live zone buffer with the 32-byte stride and capacity 1..64.";
            LogAssert.Expect(LogType.Error, message);

            _field.Init(oneCell, 1f, 0.5f, null, null, ZonePacker.MaxZones);
            _field.UpdateField(null, 0);

            Assert.IsFalse(_field.isReady);
            Assert.AreEqual(0, _field.tuftCount);
        }

        [Test]
        public void Init_NonFiniteCellSize_LogsAndStaysUnbuilt()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
            LogAssert.Expect(LogType.Error, new Regex(@"^\[GrassField\] Rejected area .* with cell size NaN"));

            _field.Init(oneCell, float.NaN, 0.5f, null, _borrowedZones, ZonePacker.MaxZones);

            Assert.IsFalse(_field.isReady);
            Assert.AreEqual(0, _field.activeZoneCount);
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(0f, float.PositiveInfinity)]
        public void Init_NonFiniteOrigin_LogsAndStaysUnbuilt(float x, float y)
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
            LogAssert.Expect(LogType.Error, new Regex(@"^\[GrassField\] Rejected area"));

            _field.Init(new Rect(x, y, 1f, 1f), 1f, 0.5f, null, _borrowedZones, ZonePacker.MaxZones);

            Assert.IsFalse(_field.isReady);
            Assert.AreEqual(0, _field.activeZoneCount);
        }

        [Test]
        public void UpdateField_WithCamera_BuildsFromTheInitArea()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            Camera camera = _go.AddComponent<Camera>();
            _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
            _field.Init(new Rect(2f, -3f, 4f, 2f), 1f, 0.5f, camera, _borrowedZones, ZonePacker.MaxZones);
            _field.tuftBudget = 80;
            SetAssets();

            _field.UpdateField(_borrowedZones, 3);

            Assert.IsTrue(_field.isReady);
            Assert.AreEqual(72, _field.tuftCount); // 12 by 6 on the 4 by 2 area, the widest grid of at most 80
            Assert.AreEqual(3, _field.activeZoneCount);
        }
    }
}
