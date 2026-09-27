using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public class DeliveryWakeTests
    {
        GameObject _parent;
        Material _material;
        DeliveryWake _wake;
        readonly DeliveryPresentation _look = new DeliveryPresentation { trailSeconds = 0.2f, trailWidth = 0.3f };

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("WakeTest");
            _material = new Material(RenderTestAssets.LoadLookMaterial());
            _wake = new DeliveryWake();
        }

        [TearDown]
        public void TearDown()
        {
            _wake.Dispose();
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void Draw_BorrowsMaterialAndScalesWidthInWorldUnits()
        {
            _parent.transform.localScale = Vector3.one * 5f;
            _wake.Draw(_parent.transform, _material, _look, Color.green, 0.5f);
            var trail = _parent.GetComponentInChildren<TrailRenderer>();
            Assert.AreSame(_material, trail.sharedMaterial);
            Assert.AreEqual(0.15f, trail.widthMultiplier, 0.0001f);
            Assert.IsTrue(trail.generateLightingData);
        }

        [Test]
        public void HideAndTeleport_ClearPreviousPathBeforeReusingRenderer()
        {
            _wake.Draw(_parent.transform, _material, _look, Color.green, 1f);
            var trail = _parent.GetComponentInChildren<TrailRenderer>();
            trail.AddPosition(Vector3.zero);
            trail.AddPosition(Vector3.right);
            _parent.transform.position = Vector3.right * 20f;
            _wake.Draw(_parent.transform, _material, _look, Color.green, 1f);
            Assert.AreEqual(0, trail.positionCount);
            trail.AddPosition(Vector3.zero);
            _wake.Hide();
            Assert.AreEqual(0, trail.positionCount);
            Assert.IsFalse(trail.emitting);
            Assert.IsFalse(trail.gameObject.activeSelf);
            _wake.Draw(_parent.transform, _material, _look, Color.green, 1f);
            Assert.AreSame(trail, _parent.GetComponentInChildren<TrailRenderer>());
            _wake.Dispose();
            Assert.AreEqual(0, _parent.transform.childCount);
            Assert.IsTrue(_material);
        }
    }
}
