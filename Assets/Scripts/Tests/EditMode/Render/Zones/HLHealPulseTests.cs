using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLHealPulseTests
    {
        GameObject _ownerGo, _source, _target;
        HLZoneRegistry _owner;
        HLHealPulse _pulse;
        HLRenderRegistry _previous;
        [SetUp] public void SetUp()
        {
            _previous = HLRenderRegistry.Current;
            HLRenderRegistry.Current = new HLRenderRegistry();
            _ownerGo = new GameObject("zones");
            _owner = _ownerGo.AddComponent<HLZoneRegistry>();
            _owner.Initialize(new HLZoneFakeUpload());
            _source = new GameObject("source");
            _target = new GameObject("target");
            _target.transform.position = new Vector3(1, 2, 3);
            _pulse = _source.AddComponent<HLHealPulse>();
            _pulse.CellSize = 2;
            _pulse.Initialize(_source);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(_source); Object.DestroyImmediate(_target); Object.DestroyImmediate(_ownerGo);
            HLRenderRegistry.Current = _previous;
        }
        [Test] public void NotifyCreatesOneTargetPulseInCellUnitsAndExpires()
        {
            _pulse.Initialize(_source);
            HLRenderRegistry.Current.NotifyHeal(_source, _target, 4, true);
            _owner.PublishFrame(0);
            Assert.AreEqual(1, _owner.Count);
            Assert.AreEqual((int)HLZoneKind.Heal, _owner.Snapshot[0].kind);
            Assert.AreEqual(_target.transform.position, _owner.Snapshot[0].position);
            Assert.AreEqual(1.2f, _owner.Snapshot[0].radius);
            _target.transform.position = Vector3.zero;
            _owner.PublishFrame(0.225f);
            Assert.AreEqual(0.5f, _owner.Snapshot[0].strength, 0.0001f);
            Assert.AreEqual(new Vector3(1, 2, 3), _owner.Snapshot[0].position);
            _owner.PublishFrame(0.225f);
            Assert.AreEqual(0, _owner.Count);
        }
        [Test] public void DisableUnsubscribesAndEnableResubscribesWithoutDuplicates()
        {
            _pulse.enabled = false;
            TestHelpers.InvokePrivate(_pulse, "OnDisable");
            HLRenderRegistry.Current.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(0, _owner.LiveCount);
            _pulse.enabled = true;
            TestHelpers.InvokePrivate(_pulse, "OnEnable");
            HLRenderRegistry.Current.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(1, _owner.LiveCount);
        }
        [Test] public void RegistryReplacementAndSourceReinitializationDetachOldSubscriptions()
        {
            var oldRegistry = HLRenderRegistry.Current;
            HLRenderRegistry.Current = new HLRenderRegistry();
            TestHelpers.InvokePrivate(_pulse, "Update");
            oldRegistry.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(0, _owner.LiveCount);
            _pulse.Initialize(_target);
            HLRenderRegistry.Current.NotifyHeal(_source, _target, 1, false);
            Assert.AreEqual(0, _owner.LiveCount);
            HLRenderRegistry.Current.NotifyHeal(_target, _source, 1, false);
            Assert.AreEqual(1, _owner.LiveCount);
        }
        [Test] public void DamageZeroNonFiniteAndMissingTargetsAreIgnored()
        {
            foreach (float value in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
                HLRenderRegistry.Current.NotifyHeal(_source, _target, value, false);
            HLRenderRegistry.Current.NotifyHeal(_source, null, 1, false);
            Assert.AreEqual(0, _owner.LiveCount);
        }
    }
}
