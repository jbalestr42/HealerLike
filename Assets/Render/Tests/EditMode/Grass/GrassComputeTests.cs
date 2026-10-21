using System.Collections.Generic;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GrassComputeTests : AGrassComputeFixture
    {
        static readonly uint quarterTurn = 1073741824u;

        [Test]
        public void Dispatch_ZoneLanes_ShapesVisibleTuftsAndClearsRemovedInfluence()
        {
            Assert.AreEqual(65u, Dispatch());

            ReadStates();
            Assert.That(_states[0].leanHeightSpike.z, Is.EqualTo(1.32f).Within(0.0001)); // 1 + 0.8 * heal 0.4
            Assert.That(_states[1].leanHeightSpike.w, Is.EqualTo(0.759375f).Within(0.0001));
            float spikeHeight = 0.759375f * 0.648f; // weight * rise
            float spikeTall = _states[1].leanHeightSpike.z * _layout[1].heightWidthLean.x;
            Assert.That(spikeTall, Is.InRange(0.26f * spikeHeight - 0.0001f, 0.63f * spikeHeight + 0.0001f));
            Assert.AreEqual(Vector4.one * 123f, _states[65].leanHeightSpike);
            uint[] ids = new uint[65];
            _visible.GetData(ids, 0, 0, 65);
            HashSet<uint> unique = new HashSet<uint>(ids);
            Assert.AreEqual(65, unique.Count);
            Assert.IsTrue(unique.Contains(1), "The spike is drawn from the same list.");

            TuftState hostileState = _states[1];
            Dispatch();
            ReadStates();
            Assert.AreEqual(hostileState, _states[1], "Spikes stay still from frame to frame.");

            _compute.SetInt("_HLZoneCount", ZonePacker.MaxZones);
            Assert.AreEqual(65u, Dispatch());
            ReadStates();
            Assert.GreaterOrEqual(_states[1].leanHeightSpike.w, 0.5f);

            _compute.SetInt("_HLZoneCount", 0);
            Assert.AreEqual(65u, Dispatch());
            ReadStates();
            Assert.AreEqual(0f, _states[1].leanHeightSpike.w);
            Assert.AreEqual(1f, _states[0].leanHeightSpike.z);

            TuftState range = Sample(3, Vector3.right, 3f, 1f);
            Assert.That(range.leanHeightSpike.z, Is.InRange(1.008f, 1.2801f)); // heal 0.01 to 0.35
            Assert.AreEqual(0f, range.leanHeightSpike.w);

            TuftState bruise = Sample(4, Vector3.zero, 3f, 1f);
            Assert.Less(bruise.leanHeightSpike.z, 1f);
            Assert.AreEqual(0f, bruise.leanHeightSpike.w);

            // Launch and trample move the grass through the ground now; the tuft pass ignores them
            TuftState launch = Sample(5, Vector3.forward * 2f, 4f, 0.2f, heading: quarterTurn);
            Assert.AreEqual(new Vector4(0f, 0f, 1f, 0f), launch.leanHeightSpike);
            Assert.AreEqual(new Vector4(0f, 0f, 1f, 0f), Sample(6, Vector3.zero, 1f, 1f).leanHeightSpike);

            Assert.AreEqual(1f, Sample(1, Vector3.right, 2f, 0f).leanHeightSpike.z);
            // grown, heal 1
            Assert.That(Sample(1, Vector3.right, 2f, 0.3f).leanHeightSpike.z, Is.EqualTo(1.8f).Within(0.0001));

            float rising = Sample(2, Vector3.zero, 3f, 0.075f).leanHeightSpike.z;
            float peak = Sample(2, Vector3.zero, 3f, 0.15f).leanHeightSpike.z;
            float sinking = Sample(2, Vector3.zero, 3f, 0.7f, 0.125f).leanHeightSpike.z;
            Assert.Less(rising, peak);
            Assert.Less(sinking, rising);
            Assert.GreaterOrEqual(_states[0].leanHeightSpike.w, 0.5f, "Sinking spikes retain their geometry until expiry.");

            _planes[0] = new Vector4(1f, 0f, 0f, -100f);
            _compute.SetVectorArray("_HLFrustumPlanes", _planes);
            Assert.AreEqual(0u, Dispatch());
            foreach (ShaderMessage message in ShaderUtil.GetComputeShaderMessages(_compute))
            {
                Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
            }
        }

        [Test]
        public void Dispatch_RootOutsideAPlane_IsKeptOnlyWithinTheCullMargin()
        {
            float margin = GrassBounds.Envelope(1f);
            _planes[0] = new Vector4(1f, 0f, 0f, 2f - 0.5f * margin);
            _compute.SetVectorArray("_HLFrustumPlanes", _planes);

            uint kept = Dispatch();

            _planes[0] = new Vector4(1f, 0f, 0f, 2f - 1.5f * margin);
            _compute.SetVectorArray("_HLFrustumPlanes", _planes);

            Assert.AreEqual(kept - 1, Dispatch()); // tuft 0 at x -2 falls outside the margin
        }

        [Test]
        public void Dispatch_NoZones_KeepsEachTuftsRestLean()
        {
            for (int i = 0; i < _layout.Length; i++)
            {
                _layout[i].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, i / 128f);
            }
            _seedBuffer.SetData(_layout);
            _compute.SetInt("_HLZoneCount", 0);

            Dispatch();
            ReadStates();
            TuftState[] first = (TuftState[])_states.Clone();
            Dispatch();
            ReadStates();

            for (int i = 0; i < 65; i++)
            {
                Assert.AreEqual(new Vector4(0f, i / 128f, 1f, 0f), _states[i].leanHeightSpike);
                Assert.AreEqual(first[i], _states[i], "No wind, nothing moves between frames.");
            }
        }

        [Test]
        public void Dispatch_Wind_ChangesLeanOnlyAndRepeatsAtTheSameTime()
        {
            _compute.SetInt("_HLZoneCount", 0);
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(1f, 0f));
            Dispatch();
            ReadStates();
            Vector4 first = _states[0].leanHeightSpike;
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(1f, 0.8f));
            Dispatch();
            ReadStates();
            Vector4 second = _states[0].leanHeightSpike;
            Assert.Greater(Vector2.Distance(first, second), 0.01f);
            Assert.That(new Vector2(second.x, second.y).magnitude, Is.LessThan(0.16f));
            Assert.AreEqual(1f, second.z);
            Assert.AreEqual(0f, second.w);
            Assert.AreEqual(Vector4.one * 123f, _states[65].leanHeightSpike);
            Dispatch();
            ReadStates();
            Assert.AreEqual(second, _states[0].leanHeightSpike);
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(1f, 0f));
            Dispatch();
            ReadStates();
            Assert.AreEqual(first, _states[0].leanHeightSpike);
        }
    }
}
