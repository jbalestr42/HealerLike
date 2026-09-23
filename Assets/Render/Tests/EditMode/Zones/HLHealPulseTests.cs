using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLHealPulseTests
    {
        GameObject _ownerGo;
        GameObject _source;
        GameObject _target;
        HLZoneRegistry _owner;
        HLHealPulse _pulse;
        HLRenderRegistry _previous;

        [SetUp]
        public void SetUp()
        {
            _previous = HLRenderRegistry.current;
            HLRenderRegistry.current = new HLRenderRegistry();
            _ownerGo = new GameObject("zones");
            _owner = _ownerGo.AddComponent<HLZoneRegistry>();
            _owner.Init(new HLZoneFakeUpload());
            _source = new GameObject("source");
            _target = new GameObject("target");
            _target.transform.position = new Vector3(1f, 2f, 3f);
            _pulse = _source.AddComponent<HLHealPulse>();
            _pulse.cellSize = 2f;
            _pulse.Initialize(_source);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_ownerGo);
            HLRenderRegistry.current = _previous;
        }

        [Test]
        public void NotifyCreatesOneTargetPulseInCellUnitsAndExpires()
        {
            _pulse.Initialize(_source);

            HLRenderRegistry.current.NotifyHeal(_source, _target, 4, true);
            _owner.PublishFrame(0f);

            Assert.AreEqual(1, _owner.count);
            Assert.AreEqual((int)HLZoneKind.Heal, _owner.snapshot[0].kind);
            Assert.AreEqual(_target.transform.position, _owner.snapshot[0].position);
            Assert.AreEqual(1.2f, _owner.snapshot[0].radius);

            _target.transform.position = Vector3.zero;
            _owner.PublishFrame(0.225f);

            Assert.AreEqual(0.5f, _owner.snapshot[0].strength, 0.0001f);
            Assert.AreEqual(Vector3.zero, _owner.snapshot[0].position);

            _owner.PublishFrame(0.225f);
            Assert.AreEqual(0, _owner.count);
        }

        [Test]
        public void DisableUnsubscribesAndEnableResubscribesWithoutDuplicates()
        {
            _pulse.enabled = false;
            TestHelpers.InvokePrivate(_pulse, "OnDisable");
            HLRenderRegistry.current.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(0, _owner.liveCount);

            _pulse.enabled = true;
            TestHelpers.InvokePrivate(_pulse, "OnEnable");
            HLRenderRegistry.current.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(1, _owner.liveCount);
        }

        [Test]
        public void RegistryReplacementAndSourceReinitializationDetachOldSubscriptions()
        {
            HLRenderRegistry oldRegistry = HLRenderRegistry.current;
            HLRenderRegistry.current = new HLRenderRegistry();
            TestHelpers.InvokePrivate(_pulse, "Update");
            oldRegistry.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(0, _owner.liveCount);

            _pulse.Initialize(_target);
            HLRenderRegistry.current.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(0, _owner.liveCount);

            HLRenderRegistry.current.NotifyHeal(_target, _source, 1, false);
            Assert.AreEqual(1, _owner.liveCount);
        }

        [Test]
        public void TransformPulseSurvivesSourceDisableButEndsWithTarget()
        {
            _pulse.Pulse(_target.transform);
            _pulse.enabled = false;
            _owner.PublishFrame(0.1f);
            Assert.AreEqual(1, _owner.count);

            Object.DestroyImmediate(_target);
            _owner.PublishFrame(0f);
            Assert.AreEqual(0, _owner.count);
        }

        [Test]
        public void DamageZeroNonFiniteAndMissingTargetsAreIgnored()
        {
            foreach (float value in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
            {
                HLRenderRegistry.current.NotifyHeal(_source, _target, value, false);
            }

            HLRenderRegistry.current.NotifyHeal(_source, null, 1, false);

            Assert.AreEqual(0, _owner.liveCount);
        }


        [Test]
        public void Init_WithRegistryAndZones_PulsesOnlyForTheGivenRegistry()
        {
            HLRenderRegistry registry = new HLRenderRegistry();

            _pulse.Init(_source, registry, _owner);
            HLRenderRegistry.current.NotifyHeal(_source, _target, 1f, false);

            Assert.AreEqual(0, _owner.liveCount);

            registry.NotifyHeal(_source, _target, 1f, false);

            Assert.AreEqual(1, _owner.liveCount);
        }

        [Test]
        public void Init_WithoutZones_IgnoresTheStaticZoneRegistry()
        {
            HLRenderRegistry registry = new HLRenderRegistry();

            _pulse.Init(_source, registry, null);
            registry.NotifyHeal(_source, _target, 1f, false);

            Assert.AreEqual(0, _owner.liveCount);
        }

        [Test]
        public void Update_AfterInit_KeepsTheGivenRegistry()
        {
            HLRenderRegistry registry = new HLRenderRegistry();
            _pulse.Init(_source, registry, _owner);

            HLRenderRegistry.current = new HLRenderRegistry();
            TestHelpers.InvokePrivate(_pulse, "Update");
            registry.NotifyHeal(_source, _target, 1f, false);

            Assert.AreEqual(1, _owner.liveCount);
        }
    }
}
