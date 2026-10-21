using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GrassComputeGroundMotionTests : AGrassComputeFixture
    {
        [Test]
        public void Dispatch_GroundLean_AddsToTheRestLeanByEachTuftsOwnShare()
        {
            _compute.SetInt("_HLZoneCount", 0);
            _layout[0].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0.2f);
            for (int i = 2; i < 65; i++)
            {
                _layout[i].positionYaw = new Vector4(3f + i * 0.37f, 0.505f, i * 0.11f, 0f);
            }
            _seedBuffer.SetData(_layout);
            SetGround(new Vector4(0.4f, 0f, 0f, 0f), 0f);

            Dispatch();
            ReadStates();

            Vector4 state = _states[0].leanHeightSpike;
            Assert.That(state.x, Is.InRange(0.4f * 0.75f - 0.001f, 0.4f * 1.25f + 0.001f));
            Assert.That(state.y, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.AreEqual(1f, state.z);
            float[] shares = new float[63];
            for (int i = 2; i < 65; i++)
            {
                shares[i - 2] = _states[i].leanHeightSpike.x;
            }

            Assert.Greater(Mathf.Max(shares) - Mathf.Min(shares), 0.05f, "Neighbours answer the same push unevenly.");
        }

        [Test]
        public void Dispatch_GroundCrush_FoldsTheTuftDownAlongItsPushAndShortensIt()
        {
            _compute.SetInt("_HLZoneCount", 0);
            SetGround(new Vector4(0f, 0.3f, 0f, 0f), 1f);

            Dispatch();
            ReadStates();

            Vector4 state = _states[0].leanHeightSpike;
            Assert.That(new Vector2(state.x, state.y).magnitude, Is.EqualTo(1.2f).Within(0.001f));
            Assert.Greater(state.y, 1.19f, "The fold follows the push.");
            Assert.That(state.z * GrassLayout.TuftHeight, Is.LessThanOrEqualTo(0.1401f));
        }

        [Test]
        public void Dispatch_EveryLeanTogether_StaysShortOfLyingDown()
        {
            _compute.SetInt("_HLZoneCount", 0);
            _layout[0].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0.5f);
            _seedBuffer.SetData(_layout);
            // A push along the rest heading, dead and blighted grass, all at once
            SetGround(new Vector4(0f, 1.25f, 0f, 0f), 0f, new Vector4(0f, -1f, 0f, 1f));

            Dispatch();
            ReadStates();

            Vector2 lean = _states[0].leanHeightSpike;
            Assert.That(lean.magnitude, Is.EqualTo(GrassLayout.MaxTuftLean).Within(0.001f));
            Assert.GreaterOrEqual(GrassTuft.Spine(lean, 1f, 1f).y, 0f, "The tip stays above the ground.");
        }

        [Test]
        public void Dispatch_HealOnLushGrass_StaysWithinTheHealLift()
        {
            SetGround(Vector4.zero, 0f, new Vector4(0f, 1f, 0f, 0f));

            TuftState healed = Sample(1, Vector3.right, 2f, 1f);

            Assert.That(healed.leanHeightSpike.z, Is.EqualTo(GrassLayout.HealLift).Within(0.0001f));
        }

        [Test]
        public void Dispatch_GroundCrush_FlattensHostileSpikes()
        {
            SetGround(new Vector4(0f, 0f, 0f, 0f), 1f);

            TuftState trampled = Sample(2, Vector3.zero, 3f, 1f);

            Assert.AreEqual(0f, trampled.leanHeightSpike.w, "Obstacles remain flattened through hostile overlap.");
        }

        [Test]
        public void Dispatch_GroundActiveOutsideItsRect_FallsBackToTheWind()
        {
            _compute.SetInt("_HLZoneCount", 0);
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(1f, 0.5f));
            Dispatch();
            ReadStates();
            Vector4 windOnly = _states[0].leanHeightSpike;

            SetGround(new Vector4(0.5f, 0.5f, 0f, 0f), 1f);
            _compute.SetVector("_HLGroundRect", new Vector4(30f, 30f, 1f, 1f));
            Dispatch();
            ReadStates();

            Assert.AreEqual(windOnly, _states[0].leanHeightSpike);
        }

        [Test]
        public void Dispatch_Wind_DoesNotMoveStoneSpikesOrTrampledRoots()
        {
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(1f, 0f));
            TuftState spike = Sample(2, Vector3.zero, 3f, 1f);
            SetGround(new Vector4(0.2f, 0f, 0f, 0f), 1f);
            TuftState trample = Sample(0, Vector3.zero, 3f, 1f);
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(1f, 1.25f));
            Assert.AreEqual(trample, Sample(0, Vector3.zero, 3f, 1f));
            SetGround(null, 0f);
            Assert.AreEqual(spike, Sample(2, Vector3.zero, 3f, 1f));
        }
    }
}
