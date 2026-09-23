using System.Collections.Generic;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellVisualSinkTests
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

        GameObject _host;
        GameObject _target;
        GameObject _other;
        HLSpellVisualSink _sink;
        BuffHandlerFactory _factory;
        BuffHandlerFactory _second;
        FlatModifierFactory _modifier;

        [SetUp]
        public void Setup()
        {
            _host = new GameObject("HLHost");
            _target = new GameObject("HLTarget");
            _other = new GameObject("HLOther");
            _sink = _host.AddComponent<HLSpellVisualSink>();
            _factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            _second = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            _modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            _modifier.data = new FlatModifierData { type = AttributeType.Damage, value = 2f };
            _factory.data = new BuffHandlerData
            {
                durationType = DurationType.Duration,
                duration = 4f,
                buffFactoryList = new List<ABuffFactory> { _modifier }
            };
            _second.data = _factory.data;
        }

        [TearDown]
        public void Cleanup()
        {
            DestroyHost(_host);
            DestroyHost(_target);
            DestroyHost(_other);
            Object.DestroyImmediate(_factory);
            Object.DestroyImmediate(_second);
            Object.DestroyImmediate(_modifier);
        }

        [Test]
        public void HostilePulseSpawnsSlateLitterAndContactLinkUsesThreads()
        {
            GameObject go = new GameObject("HLBeautySink");
            try
            {
                HLSpellVisualSink sink = go.AddComponent<HLSpellVisualSink>();
                TestHelpers.InvokePrivate(sink, "OnEnable");
                sink.areaPulse = (center, radius, kind, strength) => { };
                sink.PulseArea(Vector3.one, 2f, HealerLike.Render.Zones.HLZoneKind.Hostile, 1f);
                HLSpellEffect effect = go.GetComponentInChildren<HLSpellEffect>();
                Assert.AreEqual(HLSpellEffectKind.Litter, effect.kind);
                Assert.AreEqual(HLSpellVisualSink.PulseSeconds, effect.lifetime);
                Assert.AreEqual(Vector3.one, effect.transform.position);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                effect.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.Less(
                    Vector4.Distance((Color)new Color32(58, 66, 87, 255), block.GetColor("_BaseColor")),
                    0.00001f
                );
                HLSpellEffect thread = sink.ShowContactLink(Vector3.zero, Vector3.right);
                Assert.IsTrue(thread.contactThread);
                Assert.IsFalse(thread.parts[1].gameObject.activeSelf);
                TestHelpers.InvokePrivate(sink, "OnDestroy");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RegistryCallsCannotRepopulateDisabledSinkAndEnableStartsClean()
        {
            HLRenderRegistry previous = HLRenderRegistry.current;
            try
            {
                HLRenderRegistry.current = new HLRenderRegistry { spellSink = _sink };
                _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
                GameObject root = _sink.GetStatus(_target, _factory);
                _sink.enabled = false;
                TestHelpers.InvokePrivate(_sink, "OnDisable");
                Assert.IsFalse(root);
                IHLSpellVisualSink registered = HLRenderRegistry.current.spellSink;
                registered.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
                registered.ShowImpact(null, _target, HLResourceKind.Health, 2f, false);
                _sink.PulseArea(Vector3.zero, 1f, HLZoneKind.Heal, 1f);
                Assert.IsNull(_sink.ShowLink(Vector3.zero, Vector3.one));
                Assert.AreEqual(0, _sink.statusCount);
                Assert.AreEqual(0, _sink.impactCount);
                _sink.enabled = true;
                TestHelpers.InvokePrivate(_sink, "OnEnable");
                Assert.AreSame(_sink, HLRenderRegistry.current.spellSink);
                Assert.AreEqual(0, _sink.statusCount);
                _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
                Assert.AreEqual(1, _sink.statusCount);
                TestHelpers.InvokePrivate(_sink, "OnEnable");
                Assert.AreEqual(0, _sink.statusCount);
            }
            finally
            {
                HLRenderRegistry.current = previous;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedStatusAndIdleSinkFramesAllocateNothing(bool populated)
        {
            if (populated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, HLClockKind.Simulation);
            }
            GameObject root = _sink.GetStatus(_target, _factory);
            System.Reflection.MethodInfo method = typeof(HLSpellVisualSink).GetMethod(
                "LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            System.Action update = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), _sink, method);
            for (int i = 0; i < 32; i++)
            {
                if (populated)
                {
                    _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, HLClockKind.Simulation);
                }
                update();
            }
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 32; i++)
            {
                if (populated)
                {
                    _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, HLClockKind.Simulation);
                }
                update();
            }
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, allocated);
            Assert.AreSame(root, _sink.GetStatus(_target, _factory));
        }

        [Test]
        public void SpeedStatusSitsLowAndRemovalKeepsOnlyCosmeticTail()
        {
            _modifier.data.type = AttributeType.Speed;
            _sink.SetStatus(null, _target, _factory, 1, 0.25f, 4f, HLClockKind.Simulation);
            GameObject root = _sink.GetStatus(_target, _factory);
            HLSpellEffect effect = root.GetComponentInChildren<HLSpellEffect>();
            Assert.AreEqual(-0.35f, effect.transform.localPosition.y);
            _sink.RemoveStatus(null, _target, _factory);
            Assert.AreEqual(0, _sink.statusCount);
            Assert.IsTrue(effect);
            effect.Advance(0.25f);
            Assert.IsTrue(effect.removalComplete);
        }

        [Test]
        public void AreaRingKeepsExactRadiusAndResolvedAmountScalesImpact()
        {
            _sink.PulseArea(Vector3.right, 2f, HLZoneKind.Heal, 0.3f);
            HLSpellEffect ring = _host.GetComponentInChildren<HLSpellEffect>();
            Assert.AreEqual(HLSpellEffectKind.Area, ring.kind);
            Assert.AreEqual(Vector3.right, ring.transform.position);
            Assert.AreEqual(Vector3.one * 2f, ring.transform.localScale);
            _sink.Clear();
            _sink.ShowImpact(null, _target, HLResourceKind.Health, 1f, false);
            float small = _host.GetComponentInChildren<HLSpellEffect>().transform.localScale.x;
            _sink.Clear();
            _sink.ShowImpact(null, _target, HLResourceKind.Health, 100f, false);
            Assert.Greater(_host.GetComponentInChildren<HLSpellEffect>().transform.localScale.x, small);
        }

        [Test]
        public void SameFrameCharacterHealsPairEachRecipientAndIgnoreOtherOutcomes()
        {
            _sink.isCharacterSource = source => source == _host;
            _sink.healerAnchor = source => _other.transform;
            _other.transform.position = Vector3.up * 2f;
            int links = 0;
            _sink.linkObserved = (a, b) =>
            {
                links++;
                Assert.AreEqual(_other.transform.position, a);
            };
            _sink.ShowImpact(_host, _target, HLResourceKind.Health, 3f, false);
            _sink.ShowImpact(_host, _target, HLResourceKind.Health, 4f, false);
            _sink.ShowImpact(_host, _other, HLResourceKind.Health, 2f, false);
            _sink.ShowImpact(_other, _target, HLResourceKind.Health, 2f, false);
            _sink.ShowImpact(_host, _target, HLResourceKind.Health, -2f, false);
            _sink.ShowImpact(_host, _target, HLResourceKind.Mana, 2f, false);
            Assert.AreEqual(0, links);
            _sink.FlushHealLinks();
            Assert.AreEqual(2, links);
            _sink.FlushHealLinks();
            Assert.AreEqual(2, links);
            foreach (HLSpellEffect effect in _host.GetComponentsInChildren<HLSpellEffect>())
            {
                if (effect.kind == HLSpellEffectKind.Chain)
                {
                    Assert.AreEqual(0.6f, effect.lifetime);
                }
            }
        }

        [Test]
        public void UnknownMappedSignatureFailsClosedAndLogsOnce()
        {
            _sink.styles = ScriptableObject.CreateInstance<HLSpellStyleTable>();
            try
            {
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Warning,
                    "HL unmapped spell signature: Resource/Positive/HealthMax/Single/Instant/Immediate/v0"
                );
                _sink.ShowImpact(null, _target, HLResourceKind.Health, 3f, false);
                _sink.ShowImpact(null, _target, HLResourceKind.Health, 3f, false);
                Assert.AreEqual(0, _sink.impactCount);
            }
            finally
            {
                Object.DestroyImmediate(_sink.styles);
            }
        }

        [Test]
        public void KeyIsTargetAndFactoryNotSourceOrSignature()
        {
            _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
            GameObject first = _sink.GetStatus(_target, _factory);
            _sink.SetStatus(_other, _target, _factory, 3, 2f, 4f, HLClockKind.Realtime);
            Assert.AreSame(first, _sink.GetStatus(_target, _factory));
            Assert.AreEqual(3, first.GetComponentInChildren<HLSpellEffect>().stacks);
            _sink.SetStatus(null, _other, _factory, 1, 0f, 4f, HLClockKind.Simulation);
            _sink.SetStatus(null, _target, _second, 1, 0f, 4f, HLClockKind.Simulation);
            Assert.AreEqual(3, _sink.statusCount);
            _sink.RemoveStatus(_other, _target, _factory);
            Assert.AreEqual(2, _sink.statusCount);
            _sink.RemoveStatus(_other, _target, _factory);
            Assert.AreEqual(2, _sink.statusCount);
        }

        [Test]
        public void AnchorUsesTargetPointAndPlainObjectsFallBack()
        {
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = _target.AddComponent<Entity>());
            GameObject anchor = new GameObject("HLAnchor");
            anchor.transform.SetParent(_target.transform);
            TestHelpers.SetPrivateField(entity, "_targetPoint", anchor);
            _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
            Assert.AreEqual(anchor.transform, _sink.GetStatus(_target, _factory).transform.parent);
            _sink.SetStatus(null, _other, _factory, 1, 0f, 4f, HLClockKind.Simulation);
            Assert.AreEqual(_other.transform, _sink.GetStatus(_other, _factory).transform.parent);
        }

        [Test]
        public void OnlySignedFiniteOutcomesEmitAndResourceChoosesShape()
        {
            _sink.ShowImpact(null, _target, HLResourceKind.Health, 0f, false);
            _sink.ShowImpact(null, _target, HLResourceKind.Health, float.NaN, false);
            Assert.AreEqual(0, _sink.impactCount);
            _sink.ShowImpact(null, _target, HLResourceKind.Health, 5f, false);
            _sink.ShowImpact(null, _target, HLResourceKind.Health, -5f, false);
            _sink.ShowImpact(null, _target, HLResourceKind.Mana, -5f, false);
            Assert.AreEqual(3, _sink.impactCount);
            HLSpellEffect[] fx = _host.GetComponentsInChildren<HLSpellEffect>();
            Assert.AreEqual(HLSpellEffectKind.Heal, fx[0].kind);
            Assert.AreEqual(HLSpellEffectKind.Impact, fx[1].kind);
            Assert.AreEqual(HLSpellEffectKind.Mana, fx[2].kind);
        }

        [Test]
        public void PulseForwardsExactRadiusAndRejectsInvalidGeometry()
        {
            int calls = 0;
            _sink.areaPulse = (p, r, k, s) =>
            {
                calls++;
                Assert.AreEqual(2, r);
                Assert.AreEqual(HLZoneKind.Heal, k);
            };
            _sink.PulseArea(Vector3.zero, 2f, HLZoneKind.Heal, 0.5f);
            _sink.PulseArea(Vector3.zero, -1f, HLZoneKind.Heal, 0.5f);
            Assert.AreEqual(1, calls);
        }

        class HLOwnerSpy : IHLZoneOwner
        {
            public int calls;
            public float seconds;
            public float radius;
            public HLZoneKind kind;

            public int AddPulse(HLZoneKind k, Vector3 c, float r, float s, float t)
            {
                calls++;
                kind = k;
                radius = r;
                seconds = t;
                return calls;
            }
        }

        [Test]
        public void NullDelegateFallsBackToRegistryZoneOwner()
        {
            HLRenderRegistry previous = HLRenderRegistry.current;
            HLOwnerSpy owner = new HLOwnerSpy();
            try
            {
                HLRenderRegistry.current = new HLRenderRegistry { zoneOwner = owner };
                _sink.PulseArea(Vector3.one, 3f, HLZoneKind.Hostile, 0.5f);
                Assert.AreEqual(1, owner.calls);
                Assert.AreEqual(3, owner.radius);
                Assert.AreEqual(HLZoneKind.Hostile, owner.kind);
                Assert.AreEqual(HLSpellVisualSink.PulseSeconds, owner.seconds);
                _sink.PulseArea(Vector3.one, -1f, HLZoneKind.Hostile, 0.5f);
                Assert.AreEqual(1, owner.calls);
                int injected = 0;
                _sink.areaPulse = (p, r, k, s) => injected++;
                _sink.PulseArea(Vector3.one, 3f, HLZoneKind.Heal, 0.5f);
                Assert.AreEqual(1, injected);
                Assert.AreEqual(1, owner.calls);
                _sink.areaPulse = null;
                HLRenderRegistry.current = null;
                Assert.DoesNotThrow(() => _sink.PulseArea(Vector3.one, 3f, HLZoneKind.Heal, 0.5f));
            }
            finally
            {
                HLRenderRegistry.current = previous;
            }
        }

        [Test]
        public void DestroyedTargetAndDisableReleaseVisuals()
        {
            _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
            DestroyHost(_target);
            TestHelpers.InvokePrivate(_sink, "LateUpdate");
            Assert.AreEqual(0, _sink.statusCount);
            _sink.ShowImpact(null, _other, HLResourceKind.Health, 1f, false);
            _sink.Clear();
            Assert.AreEqual(0, _sink.impactCount);
        }
    }
}
