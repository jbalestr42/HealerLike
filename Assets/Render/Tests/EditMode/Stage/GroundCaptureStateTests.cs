using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class GroundCaptureStateTests
    {
        GameObject _enabledObject;
        GameObject _disabledObject;
        GameObject _sunObject;
        RenderPipelineAsset _pipeline;
        Light _sun;
        LookShaderProperties.Snapshot _look;

        [SetUp]
        public void SetUp()
        {
            _pipeline = QualitySettings.renderPipeline;
            _sun = RenderSettings.sun;
            _look = LookShaderProperties.Capture();
            _enabledObject = new GameObject("Existing look");
            _disabledObject = new GameObject("Disabled look");
            _sunObject = new GameObject("Original sun");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enabledObject);
            Object.DestroyImmediate(_disabledObject);
            Object.DestroyImmediate(_sunObject);
            QualitySettings.renderPipeline = _pipeline;
            RenderSettings.sun = _sun;
            _look.Restore();
        }

        [Test]
        public void Dispose_FailedCapture_RestoresBorrowedLightingAndReleasesFixture()
        {
            LookController enabled = _enabledObject.AddComponent<LookController>();
            LookController disabled = _disabledObject.AddComponent<LookController>();
            disabled.enabled = false;
            Light sun = _sunObject.AddComponent<Light>();
            RenderSettings.sun = sun;
            enabled.ApplyGlobals();
            Shader.SetGlobalFloat("_HLFogStart", 123.5f);
            Shader.SetGlobalVector("_HLShadowTint", new Vector4(0.2f, 0.3f, 0.4f, 0.7f));
            GameObject fixture = null;

            Assert.Throws<InvalidOperationException>(() =>
            {
                using (GroundCaptureState state = new GroundCaptureState(new[] { enabled, disabled }))
                {
                    Assert.IsFalse(enabled.enabled);
                    QualitySettings.renderPipeline = null;
                    RenderSettings.sun = null;
                    fixture = new GameObject("Temporary fixture");
                    state.created.Add(fixture);
                    fixture.AddComponent<LookController>();
                    throw new InvalidOperationException("Simulated capture failure");
                }
            });

            Assert.IsTrue(enabled.enabled);
            Assert.IsFalse(disabled.enabled);
            Assert.AreSame(_pipeline, QualitySettings.renderPipeline);
            Assert.AreSame(sun, RenderSettings.sun);
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_HLLookApplied"));
            Assert.AreEqual(123.5f, Shader.GetGlobalFloat("_HLFogStart"));
            Assert.AreEqual(new Vector4(0.2f, 0.3f, 0.4f, 0.7f), Shader.GetGlobalVector("_HLShadowTint"));
            Assert.IsTrue(fixture == null);
        }
    }
}
