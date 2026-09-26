using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GrassComputeGroundStateTests : AGrassComputeFixture
    {
        [Test]
        public void Dispatch_GroundAsh_BurnsTheTuftShortAndStiff()
        {
            _compute.SetInt("_HLZoneCount", 0);
            SetGround(new Vector4(0.4f, 0f, 0f, 0f), 0f);
            Dispatch();
            ReadStates();
            Vector4 green = _states[0].leanHeightSpike;

            SetGround(new Vector4(0.4f, 0f, 0f, 0f), 0f, new Vector4(1f, 0f, 0f, 0f));
            Dispatch();
            ReadStates();
            Vector4 burnt = _states[0].leanHeightSpike;

            Assert.That(burnt.z, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(burnt.x, Is.EqualTo(green.x * 0.3f).Within(0.001f), "Ash barely moves in the air.");
        }

        [Test]
        public void Dispatch_GroundVitality_LiftsLushGrassAndDroopsDeadGrass()
        {
            _compute.SetInt("_HLZoneCount", 0);
            _layout[0].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0.2f);
            _seedBuffer.SetData(_layout);
            SetGround(Vector4.zero, 0f, new Vector4(0f, 1f, 0f, 0f));
            Dispatch();
            ReadStates();
            Vector4 lush = _states[0].leanHeightSpike;

            SetGround(Vector4.zero, 0f, new Vector4(0f, -1f, 0f, 0f));
            Dispatch();
            ReadStates();
            Vector4 dead = _states[0].leanHeightSpike;

            Assert.That(lush.z, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(dead.z, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(dead.y, Is.EqualTo(0.2f + 0.8f).Within(0.001f), "Dead grass droops along its rest heading.");
        }

        [Test]
        public void Dispatch_Frost_FreezesTheTuftAgainstTheAir()
        {
            _compute.SetInt("_HLZoneCount", 0);
            SetGround(new Vector4(0.4f, 0f, 0f, 0f), 0f, new Vector4(0f, 0f, -1f, 0f));

            Dispatch();
            ReadStates();

            Vector4 frozen = _states[0].leanHeightSpike;
            SetGround(new Vector4(0.4f, 0f, 0f, 0f), 0f);
            Dispatch();
            ReadStates();
            Assert.That(frozen.x, Is.EqualTo(_states[0].leanHeightSpike.x * 0.2f).Within(0.001f));
            Assert.That(frozen.z, Is.EqualTo(0.9f).Within(0.001f));
        }

        [Test]
        public void Dispatch_Blight_SinksAndDroopsTheTuft()
        {
            _compute.SetInt("_HLZoneCount", 0);
            _layout[0].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0.2f);
            _seedBuffer.SetData(_layout);
            SetGround(Vector4.zero, 0f, new Vector4(0f, 0f, 0f, 1f));

            Dispatch();
            ReadStates();

            Vector4 blighted = _states[0].leanHeightSpike;
            Assert.That(blighted.z, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(blighted.y, Is.EqualTo(0.2f + 0.5f).Within(0.001f));
        }
    }
}
