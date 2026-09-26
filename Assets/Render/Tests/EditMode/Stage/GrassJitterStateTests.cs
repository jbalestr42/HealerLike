using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class GrassJitterStateTests
    {
        GameObject _fixture;
        Camera _camera;
        BattleFocus _focus;
        GrassField _field;
        EnvironmentSway _enabledSway;
        EnvironmentSway _disabledSway;
        float _captureDelta;
        Vector3 _position;
        Quaternion _rotation;

        [SetUp]
        public void SetUp()
        {
            _captureDelta = Time.captureDeltaTime;
            _fixture = new GameObject("Jitter state fixture");
            _camera = _fixture.AddComponent<Camera>();
            _focus = _fixture.AddComponent<BattleFocus>();
            _field = _fixture.AddComponent<GrassField>();
            _enabledSway = _fixture.AddComponent<EnvironmentSway>();
            _disabledSway = _fixture.AddComponent<EnvironmentSway>();
            _disabledSway.enabled = false;
            _field.windStrength = 0.37f;
            _camera.aspect = 1.7f;
            _position = new Vector3(1f, 2f, 3f);
            _rotation = Quaternion.Euler(17f, 38f, 4f);
            _camera.transform.SetPositionAndRotation(_position, _rotation);
        }

        [TearDown]
        public void TearDown()
        {
            _field.Release();
            Object.DestroyImmediate(_fixture);
            Time.captureDeltaTime = _captureDelta;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Dispose_FailedMeasurement_RestoresBorrowedPresentation(bool focusEnabled)
        {
            _focus.enabled = focusEnabled;
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (GrassJitterState state = CreateState())
                {
                    Change(state);
                    throw new InvalidOperationException("Measurement failed");
                }
            });
            AssertRestored(focusEnabled);
        }

        [Test]
        public void Dispose_SuspendedMeasurement_RestoresOnCancellation()
        {
            IEnumerator measurement = SuspendedMeasurement();
            Assert.IsTrue(measurement.MoveNext());
            Assert.IsFalse(_enabledSway.enabled);
            ((IDisposable)measurement).Dispose();
            AssertRestored(true);
        }

        [Test]
        public void RestoreEnvironment_PreservesAuthoredShadowMode()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing
                || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                Assert.Ignore("Requires the grass compute and indirect draw path.");
            }
            using (GraphicsBuffer zones = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                       ZonePacker.MaxZones, Zone.Stride))
            {
                try
                {
                    EnvironmentAuthoring.SetGrass(_field);
                    _field.tuftBudget = 1;
                    _field.Init(new Rect(0f, 0f, 1f, 1f), 1f, 0f, _camera, zones, ZonePacker.MaxZones);
                    _field.UpdateField(zones, 0);
                    GrassDraw draw = _field.tuftDraw;
                    Assert.IsNotNull(draw);
                    draw.shadowCastingMode = ShadowCastingMode.TwoSided;
                    using (GrassJitterState state = CreateState())
                    {
                        state.SetWind(0f);
                        state.SetShadows(ShadowCastingMode.Off);
                        Assert.AreEqual(ShadowCastingMode.Off, draw.shadowCastingMode);
                        state.RestoreEnvironment();
                        Assert.AreEqual(0.37f, _field.windStrength);
                        Assert.AreEqual(ShadowCastingMode.TwoSided, draw.shadowCastingMode);
                        state.SetShadows(ShadowCastingMode.Off);
                    }
                    Assert.AreEqual(ShadowCastingMode.TwoSided, draw.shadowCastingMode);
                }
                finally
                {
                    _field.Release();
                }
            }
        }

        GrassJitterState CreateState()
        {
            return new GrassJitterState(_camera, _focus, new[] { _field });
        }

        IEnumerator SuspendedMeasurement()
        {
            using (GrassJitterState state = CreateState())
            {
                Change(state);
                yield return null;
            }
        }

        void Change(GrassJitterState state)
        {
            _focus.enabled = !_focus.enabled;
            _camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _camera.aspect = 0.5f;
            Time.captureDeltaTime = 1f / 20f;
            state.SetWind(0f);
            state.StopSways(new[] { _enabledSway, _disabledSway });
            state.StopSways(new[] { _enabledSway });
        }

        void AssertRestored(bool focusEnabled)
        {
            Assert.AreEqual(focusEnabled, _focus.enabled);
            Assert.AreEqual(_position, _camera.transform.position);
            Assert.Less(Quaternion.Angle(_rotation, _camera.transform.rotation), 0.001f);
            Assert.AreEqual(1.7f, _camera.aspect);
            Assert.AreEqual(_captureDelta, Time.captureDeltaTime);
            Assert.AreEqual(0.37f, _field.windStrength);
            Assert.IsTrue(_enabledSway.enabled);
            Assert.IsFalse(_disabledSway.enabled);
        }
    }
}
