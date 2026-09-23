using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellEffectTests
    {
        static void DestroyHost(GameObject go)
        {
            if (!go)
            {
                return;
            }
            foreach (HLSpellEffect effect in go.GetComponentsInChildren<HLSpellEffect>(true))
            {
                TestHelpers.InvokePrivate(effect, "OnDestroy");
            }
            foreach (HLSpellVisualSink sink in go.GetComponentsInChildren<HLSpellVisualSink>(true))
            {
                TestHelpers.InvokePrivate(sink, "OnDestroy");
            }
            Object.DestroyImmediate(go);
        }

        [TestCase(HLSpellEffectKind.Heal)]
        [TestCase(HLSpellEffectKind.Impact)]
        [TestCase(HLSpellEffectKind.Buff)]
        [TestCase(HLSpellEffectKind.Shield)]
        [TestCase(HLSpellEffectKind.Chain)]
        [TestCase(HLSpellEffectKind.Drip)]
        [TestCase(HLSpellEffectKind.Litter)]
        public void BeautyAnimationAllocatesNothingAfterWarmup(HLSpellEffectKind kind)
        {
            GameObject go = new GameObject("HLBeauty");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = kind;
                fx.Initialize();
                if (kind == HLSpellEffectKind.Chain)
                {
                    fx.SetEndpoints(Vector3.zero, Vector3.right * 4);
                }
                for (int i = 0; i < 16; i++)
                {
                    fx.Advance(0.001f);
                }
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 64; i++)
                {
                    fx.Advance(0.001f);
                }
                long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void HealBudsGrowThenPopAndStalksStayConnected()
        {
            GameObject go = new GameObject("HLHeal");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Heal;
                fx.Advance(0);
                float seed = fx.parts[0].localScale.x;
                fx.Advance(fx.lifetime * 0.6f);
                Assert.Greater(fx.parts[0].localScale.x, seed);
                for (int i = 0; i < fx.parts.Length; i++)
                {
                    Assert.AreEqual(
                        fx.parts[i].localPosition.y,
                        fx.stalks[i].localPosition.y + fx.stalks[i].localScale.y,
                        0.00001f
                    );
                    Assert.AreEqual(-0.12f, fx.stalks[i].localPosition.y - fx.stalks[i].localScale.y, 0.00001f);
                }
                fx.Advance(fx.lifetime * 0.35f);
                Assert.AreEqual(0, fx.parts[0].localScale.x, 0.00001f);
                Assert.AreEqual(0, fx.stalks[0].localScale.x, 0.00001f);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void RegenerationBudsReadOnlyObservedTickTime()
        {
            GameObject go = new GameObject("HLRegen");
            try
            {
                HLSpellSignature signature = new HLSpellSignature
                {
                    operation = HLOperation.Resource,
                    hasAttribute = true,
                    attribute = AttributeType.HealthMax,
                    sign = HLSign.Positive,
                    tempo = HLTempo.HandlerTick,
                };
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellPrimitives.Kind(signature);
                fx.periodSeconds = 2;
                Assert.AreEqual(HLSpellEffectKind.Heal, fx.kind);
                fx.SetStatus(1, 1, 10, HLClockKind.Simulation, signature);
                Assert.AreEqual(Vector3.zero, fx.parts[0].localScale);
                fx.SetStatus(1, 2.6f, 10, HLClockKind.Simulation, signature);
                Vector3 p = fx.parts[0].localPosition;
                Vector3 size = fx.parts[0].localScale;
                Assert.Greater(size.x, 0);
                fx.Advance(1);
                Assert.AreEqual(p, fx.parts[0].localPosition);
                Assert.AreEqual(size, fx.parts[0].localScale);
                fx.SetStatus(1, 3.9f, 10, HLClockKind.Simulation, signature);
                Assert.AreEqual(Vector3.zero, fx.parts[0].localScale);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void ImpactFacesCameraAndShardsFallUnderGravity()
        {
            GameObject go = new GameObject("HLImpact");
            GameObject camera = new GameObject("HLCamera");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Impact;
                fx.Initialize();
                camera.transform.rotation = Quaternion.Euler(35, 20, 0);
                fx.FaceCamera(camera.transform);
                Assert.Less(Quaternion.Angle(camera.transform.rotation, fx.parts[0].rotation), 0.001f);
                float start = fx.parts[1].localPosition.y;
                fx.Advance(0.2f);
                float peak = fx.parts[1].localPosition.y;
                fx.Advance(0.4f);
                Assert.Greater(peak, start);
                Assert.Less(fx.parts[1].localPosition.y, start);
                Assert.AreEqual(5, fx.parts.Length);
            }
            finally
            {
                DestroyHost(go);
                Object.DestroyImmediate(camera);
            }
        }

        [Test]
        public void BuffTiltsOrbitAndGlowPulsesFromObservedTime()
        {
            GameObject go = new GameObject("HLBuff");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Buff;
                fx.SetStatus(1, 0, 10, HLClockKind.Simulation, default);
                Vector3 normal = fx.parts[0].up;
                Vector3 scale = fx.parts[0].localScale;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                fx.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
                Color color = block.GetColor("_BaseColor");
                fx.SetStatus(1, 1, 10, HLClockKind.Simulation, default);
                Assert.AreNotEqual(normal, fx.parts[0].up);
                Assert.AreNotEqual(scale, fx.parts[0].localScale);
                fx.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.AreNotEqual(color, block.GetColor("_BaseColor"));
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void LitterEmergesThenSinksAndDripsWaitForFirstTick()
        {
            GameObject go = new GameObject("HLLitter");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Litter;
                fx.Advance(0);
                float buried = fx.parts[0].localPosition.y;
                fx.Advance(0.2f);
                float emerged = fx.parts[0].localPosition.y;
                Assert.Greater(emerged, buried);
                fx.Advance(fx.lifetime);
                Assert.Less(fx.parts[0].localPosition.y, emerged);
            }
            finally
            {
                DestroyHost(go);
            }
            go = new GameObject("HLDrip");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Drip;
                fx.periodSeconds = 2;
                fx.SetStatus(1, 1.9f, 10, HLClockKind.Simulation, default);
                Assert.AreEqual(Vector3.zero, fx.parts[0].localScale);
                fx.SetStatus(1, 2, 10, HLClockKind.Simulation, default);
                Assert.Greater(fx.parts[0].localScale.y, 0);
                Vector3 p = fx.parts[0].localPosition;
                fx.Advance(1);
                Assert.AreEqual(p, fx.parts[0].localPosition);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void ContactThreadUsesConfirmedEndpointsAndHidesHealDots()
        {
            GameObject go = new GameObject("HLThread");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Chain;
                fx.contactThread = true;
                fx.SetEndpoints(Vector3.zero, Vector3.right * 4);
                Assert.IsFalse(fx.parts[1].gameObject.activeSelf);
                Assert.AreEqual(0, fx.parts[0].position.y, 0.00001f);
                Transform last = fx.parts[30];
                Assert.Less(Vector3.Distance(Vector3.right * 4, last.position + last.up * last.localScale.y), 0.00001f);
                fx.SetEndpoints(Vector3.one, Vector3.one);
                foreach (Transform part in fx.parts)
                {
                    Assert.IsFalse(float.IsNaN(part.position.x));
                }
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void RepeatedStatusSideAndShieldUpdatesAllocateNothing()
        {
            GameObject go = new GameObject("HLShield");
            try
            {
                HLSpellEffect effect = go.AddComponent<HLSpellEffect>();
                effect.kind = HLSpellEffectKind.Shield;
                HLSpellSignature signature = new HLSpellSignature
                {
                    operation = HLOperation.Attribute,
                    attribute = AttributeType.HitArmor,
                    hasAttribute = true,
                };
                for (int i = 0; i < 32; i++)
                {
                    effect.SetStatus(2, 1, 4, HLClockKind.Simulation, signature);
                    effect.SetSide(Entity.EntityType.Player);
                    effect.SetShieldState(2);
                }
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 32; i++)
                {
                    effect.SetStatus(2, 1, 4, HLClockKind.Simulation, signature);
                    effect.SetSide(Entity.EntityType.Player);
                    effect.SetShieldState(2);
                }
                long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated);
                effect.SetShieldState(1);
                Assert.IsFalse(effect.parts[1].gameObject.activeSelf);
                effect.SetShieldState(3);
                Assert.IsTrue(effect.parts[2].gameObject.activeSelf);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void StatusPoseUsesObserverTimeAndRemovalOpensPlates()
        {
            GameObject go = new GameObject("HLStatus");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Buff;
                fx.SetStatus(1, 1, 4, HLClockKind.Simulation, default);
                Quaternion pose = fx.parts[0].localRotation;
                fx.Advance(7);
                Assert.AreEqual(pose, fx.parts[0].localRotation);
                fx.SetStatus(2, 2, 4, HLClockKind.Simulation, default);
                Assert.AreNotEqual(pose, fx.parts[0].localRotation);
            }
            finally
            {
                DestroyHost(go);
            }
            go = new GameObject("HLShield");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Shield;
                fx.SetStatus(1, 0.25f, 4, HLClockKind.Simulation, default);
                Quaternion closed = fx.parts[0].localRotation;
                float closedRadius = fx.parts[0].localPosition.magnitude;
                fx.BeginRemoval();
                fx.Advance(0.25f);
                Assert.IsTrue(fx.RemovalComplete);
                Assert.AreNotEqual(closed, fx.parts[0].localRotation);
                Assert.Greater(fx.parts[0].localPosition.magnitude, closedRadius);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void DripsFollowPeriodAndTintResetsOnRemoval()
        {
            GameObject go = new GameObject("HLDrip");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Drip;
                fx.periodSeconds = 2;
                Color tint = Color.clear;
                fx.OnTint.AddListener(c => tint = c);
                fx.SetStatus(1, 2, 6, HLClockKind.Simulation, default);
                Vector3 position = fx.parts[0].localPosition;
                Assert.AreEqual((Color)new Color32(242, 96, 122, 255), tint);
                fx.SetStatus(1, 3, 6, HLClockKind.Simulation, default);
                Assert.Less(fx.parts[0].localPosition.y, position.y);
                fx.SetStatus(1, 4, 6, HLClockKind.Simulation, default);
                Assert.AreEqual(position, fx.parts[0].localPosition);
                fx.BeginRemoval();
                Assert.AreEqual(Color.white, tint);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void LinkDotsTravelAndExpireAtAuthoredLifetime()
        {
            GameObject go = new GameObject("HLBeam");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Chain;
                fx.SetEndpoints(Vector3.zero, Vector3.right * 4);
                fx.Advance(0.15f);
                Assert.Greater(fx.parts[1].position.x, 0);
                Assert.Greater(fx.parts[1].position.y, 0);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void SideRimUpdatesWithoutDuplicatingAndCriticalUsesTwoRings()
        {
            GameObject go = new GameObject("HLSide");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Heal;
                fx.Initialize();
                int count = go.transform.childCount;
                fx.SetSide(Entity.EntityType.Player);
                fx.SetSide(Entity.EntityType.Computer);
                Assert.AreEqual(count + 1, go.transform.childCount);
                HLSpellPrimitives.AddCritical(fx);
                Assert.AreEqual(count + 3, go.transform.childCount);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void HealRisesAndStatusDoesNotAutoExpire()
        {
            GameObject go = new GameObject("HLHeal");
            GameObject status = new GameObject("HLStatus");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Heal;
                fx.Initialize();
                float before = fx.parts[0].localPosition.y;
                fx.Advance(0.2f);
                Assert.Greater(fx.parts[0].localPosition.y, before);
                HLSpellEffect s = status.AddComponent<HLSpellEffect>();
                s.SetStatus(3, 2, 4, HLClockKind.Realtime, default);
                s.Advance(10);
                Assert.AreEqual(3, s.Stacks);
                Assert.AreEqual(2, s.ElapsedSeconds);
                Assert.IsTrue(status);
            }
            finally
            {
                DestroyHost(go);
                DestroyHost(status);
            }
        }

        [Test]
        public void ChargePlatesUseObservedChargeCountAndPreserveStackData()
        {
            GameObject go = new GameObject("HLShield");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Shield;
                fx.SetStatus(
                    9,
                    0,
                    4,
                    HLClockKind.Simulation,
                    new HLSpellSignature
                    {
                        operation = HLOperation.Attribute,
                        attribute = AttributeType.HitArmor,
                        hasAttribute = true,
                    }
                );
                fx.SetShieldState(2);
                int count = 0;
                foreach (Transform part in fx.parts)
                {
                    if (part.gameObject.activeSelf)
                    {
                        count++;
                    }
                }
                Assert.AreEqual(2, count);
                Assert.AreEqual(9, fx.Stacks);
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void ChainUsesSuppliedEndpointsAndCurvesAboveChord()
        {
            GameObject go = new GameObject("HLChain");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Chain;
                fx.SetEndpoints(Vector3.zero, Vector3.right * 4);
                Assert.AreEqual(Vector3.zero, fx.parts[1].position);
                Assert.Greater(fx.parts[17].position.y, 0);
                Assert.AreEqual(32, fx.parts.Length);
            }
            finally
            {
                DestroyHost(go);
            }
        }
    }
}
