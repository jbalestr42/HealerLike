using System.Collections.Generic;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class HLSpellVisualSinkTests
{
    GameObject _host;
    GameObject _target;
    GameObject _other;
    HLSpellVisualSink _sink;
    BuffHandlerFactory _factory;
    BuffHandlerFactory _second;
    FlatModifierFactory _modifier;

    static SpellLooks LoadLooks()
    {
        return AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
    }

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Host");
        _target = new GameObject("Target");
        _other = new GameObject("Other");
        _sink = _host.AddComponent<HLSpellVisualSink>();
        _sink.looks = LoadLooks();
        _modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
        _modifier.data = new FlatModifierData { type = AttributeType.Damage, value = 2f };
        _factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _factory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 4f,
            buffFactoryList = new List<ABuffFactory> { _modifier }
        };
        _second = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _second.data = _factory.data;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_host);
        if (_target != null)
        {
            Object.DestroyImmediate(_target);
        }
        Object.DestroyImmediate(_other);
        Object.DestroyImmediate(_factory);
        Object.DestroyImmediate(_second);
        Object.DestroyImmediate(_modifier);
    }

    [Test]
    public void PulseArea_Hostile_SpawnsSlateLitter()
    {
        _sink.PulseArea(Vector3.one, 2f, HLZoneKind.Hostile, 1f);

        HLSpellEffect effect = _host.GetComponentInChildren<HLSpellEffect>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        effect.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreEqual(HLSpellEffectKind.Litter, effect.kind);
        Assert.AreEqual(HLSpellVisualSink.PulseSeconds, effect.lifetime);
        Assert.AreEqual(Vector3.one, effect.transform.position);
        Assert.Less(Vector4.Distance((Color)new Color32(58, 66, 87, 255), block.GetColor("_BaseColor")), 0.00001f);
    }

    [Test]
    public void ShowContactLink_Contacts_DrawsAThread()
    {
        HLSpellEffect thread = _sink.ShowContactLink(Vector3.zero, Vector3.right);

        Assert.IsTrue(thread.contactThread);
        Assert.IsFalse(thread.parts[1].gameObject.activeSelf);
    }

    [Test]
    public void SetStatus_DisabledSink_ShowsNothingAndEnableStartsClean()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        GameObject status = _sink.GetStatus(_target, _factory);
        _sink.enabled = false;
        TestHelpers.InvokePrivate(_sink, "OnDisable");
        Assert.IsFalse(status);

        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        _sink.ShowImpact(null, _target, HLResourceKind.Health, 2f, false);
        _sink.PulseArea(Vector3.zero, 1f, HLZoneKind.Heal, 1f);
        Assert.IsNull(_sink.ShowLink(Vector3.zero, Vector3.one));
        Assert.AreEqual(0, _sink.statusCount);
        Assert.AreEqual(0, _sink.impactCount);

        _sink.enabled = true;
        TestHelpers.InvokePrivate(_sink, "OnEnable");
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        Assert.AreEqual(1, _sink.statusCount);

        TestHelpers.InvokePrivate(_sink, "OnEnable");
        Assert.AreEqual(0, _sink.statusCount);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LateUpdate_RepeatedStatus_AllocatesNothing(bool isPopulated)
    {
        if (isPopulated)
        {
            _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, HLClockKind.Simulation);
        }
        GameObject status = _sink.GetStatus(_target, _factory);
        System.Reflection.MethodInfo method = typeof(HLSpellVisualSink).GetMethod("LateUpdate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        System.Action lateUpdate = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), _sink, method);
        for (int i = 0; i < 32; i++)
        {
            if (isPopulated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, HLClockKind.Simulation);
            }
            lateUpdate();
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 32; i++)
        {
            if (isPopulated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, HLClockKind.Simulation);
            }
            lateUpdate();
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
        Assert.AreSame(status, _sink.GetStatus(_target, _factory));
    }

    [Test]
    public void SetStatus_UnmappedBuff_UsesTheBoonLook()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0.25f, 4f, HLClockKind.Simulation);

        HLSpellEffect effect = _sink.GetStatus(_target, _factory).GetComponent<HLSpellEffect>();
        Assert.AreEqual(_sink.looks.boon.effectPrefab.kind, effect.kind);
        Assert.AreEqual(_target.transform, effect.transform.parent);
    }

    [Test]
    public void SetStatus_MappedSpeedBuff_SitsAtTheLookOffset()
    {
        ABuffHandlerFactory slow = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
            "Assets/Data/EntityItems/SlowItem/BuffHandlerFactory.asset");

        _sink.SetStatus(null, _target, slow, 1, 0.25f, 4f, HLClockKind.Simulation);

        GameObject status = _sink.GetStatus(_target, slow);
        Assert.AreEqual(_sink.looks.GetLook(slow, true).offset, status.transform.localPosition);
        Assert.Less(status.transform.localPosition.y, 0f);
    }

    [Test]
    public void SetStatus_EnemyCasterOnAlly_UsesTheBaneLook()
    {
        Entity caster = null;
        Entity recipient = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            caster = _other.AddComponent<Entity>();
            recipient = _target.AddComponent<Entity>();
        });
        caster.entityType = Entity.EntityType.Computer;
        recipient.entityType = Entity.EntityType.Player;

        _sink.SetStatus(_other, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        HLSpellEffect effect = _sink.GetStatus(_target, _factory).GetComponent<HLSpellEffect>();
        effect.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreEqual(_sink.looks.bane.tint.g * 1.12f, block.GetColor("_BaseColor").g, 0.001f); // first glow frame
    }

    [Test]
    public void RemoveStatus_Removed_KeepsOnlyTheCosmeticTail()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0.25f, 4f, HLClockKind.Simulation);
        HLSpellEffect effect = _sink.GetStatus(_target, _factory).GetComponent<HLSpellEffect>();

        _sink.RemoveStatus(null, _target, _factory);
        effect.Advance(0.25f);

        Assert.AreEqual(0, _sink.statusCount);
        Assert.IsTrue(effect);
        Assert.IsTrue(effect.removalComplete);
    }

    [Test]
    public void PulseArea_Heal_KeepsTheExactRadius()
    {
        _sink.PulseArea(Vector3.right, 2f, HLZoneKind.Heal, 0.3f);

        HLSpellEffect ring = _host.GetComponentInChildren<HLSpellEffect>();
        Assert.AreEqual(HLSpellEffectKind.Area, ring.kind);
        Assert.AreEqual(Vector3.right, ring.transform.position);
        Assert.AreEqual(Vector3.one * 2f, ring.transform.localScale);
    }

    [Test]
    public void ShowImpact_LargerAmount_DrawsALargerImpact()
    {
        _sink.ShowImpact(null, _target, HLResourceKind.Health, 1f, false);
        float small = _host.GetComponentInChildren<HLSpellEffect>().transform.localScale.x;
        _sink.Clear();

        _sink.ShowImpact(null, _target, HLResourceKind.Health, 100f, false);

        Assert.Greater(_host.GetComponentInChildren<HLSpellEffect>().transform.localScale.x, small);
    }

    [Test]
    public void FlushHealLinks_SameFrameCharacterHeals_LinksEachRecipientOnce()
    {
        int links = 0;
        _sink.isCharacterSource = source => source == _host;
        _sink.healerAnchor = source => _other.transform;
        _other.transform.position = Vector3.up * 2f;
        _sink.linkObserved = (start, end) =>
        {
            links++;
            Assert.AreEqual(_other.transform.position, start);
        };
        _sink.ShowImpact(_host, _target, HLResourceKind.Health, 3f, false);
        _sink.ShowImpact(_host, _target, HLResourceKind.Health, 4f, false);
        _sink.ShowImpact(_host, _other, HLResourceKind.Health, 2f, false);
        _sink.ShowImpact(_other, _target, HLResourceKind.Health, 2f, false);
        _sink.ShowImpact(_host, _target, HLResourceKind.Health, -2f, false);
        _sink.ShowImpact(_host, _target, HLResourceKind.Mana, 2f, false);
        Assert.AreEqual(0, links);

        _sink.FlushHealLinks();
        _sink.FlushHealLinks();

        Assert.AreEqual(2, links); // one per recipient, the second flush has nothing left
    }

    [Test]
    public void ShowImpact_Critical_ShowsTheRings()
    {
        _sink.ShowImpact(null, _target, HLResourceKind.Health, -3f, true);

        int rings = 0;
        foreach (Transform child in _host.GetComponentInChildren<HLSpellEffect>().transform)
        {
            if (child.name == "CriticalRing" && child.gameObject.activeSelf)
            {
                rings++;
            }
        }
        Assert.AreEqual(2, rings);
    }

    [Test]
    public void ShowImpact_NoLooks_ShowsNothing()
    {
        _sink.looks = null;

        _sink.ShowImpact(null, _target, HLResourceKind.Health, 3f, false);
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);

        Assert.AreEqual(0, _sink.impactCount);
        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void SetStatus_SameTargetAndFactory_KeepsOneStatus()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        GameObject first = _sink.GetStatus(_target, _factory);

        _sink.SetStatus(_other, _target, _factory, 3, 2f, 4f, HLClockKind.Realtime);
        _sink.SetStatus(null, _other, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        _sink.SetStatus(null, _target, _second, 1, 0f, 4f, HLClockKind.Simulation);

        Assert.AreSame(first, _sink.GetStatus(_target, _factory));
        Assert.AreEqual(3, first.GetComponent<HLSpellEffect>().stacks);
        Assert.AreEqual(3, _sink.statusCount);

        _sink.RemoveStatus(_other, _target, _factory);
        _sink.RemoveStatus(_other, _target, _factory);

        Assert.AreEqual(2, _sink.statusCount);
    }

    [Test]
    public void SetStatus_TargetPoint_AnchorsOnIt()
    {
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = _target.AddComponent<Entity>());
        GameObject anchor = new GameObject("Anchor");
        anchor.transform.SetParent(_target.transform);
        TestHelpers.SetPrivateField(entity, "_targetPoint", anchor);

        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        _sink.SetStatus(null, _other, _factory, 1, 0f, 4f, HLClockKind.Simulation);

        Assert.AreEqual(anchor.transform, _sink.GetStatus(_target, _factory).transform.parent);
        Assert.AreEqual(_other.transform, _sink.GetStatus(_other, _factory).transform.parent);
    }

    [Test]
    public void ShowImpact_SignedFiniteAmounts_PickTheResourceLook()
    {
        _sink.ShowImpact(null, _target, HLResourceKind.Health, 0f, false);
        _sink.ShowImpact(null, _target, HLResourceKind.Health, float.NaN, false);
        Assert.AreEqual(0, _sink.impactCount);

        _sink.ShowImpact(null, _target, HLResourceKind.Health, 5f, false);
        _sink.ShowImpact(null, _target, HLResourceKind.Health, -5f, false);
        _sink.ShowImpact(null, _target, HLResourceKind.Mana, -5f, false);

        HLSpellEffect[] effects = _host.GetComponentsInChildren<HLSpellEffect>();
        Assert.AreEqual(3, _sink.impactCount);
        Assert.AreEqual(HLSpellEffectKind.Heal, effects[0].kind);
        Assert.AreEqual(HLSpellEffectKind.Impact, effects[1].kind);
        Assert.AreEqual(HLSpellEffectKind.Mana, effects[2].kind);
    }

    [Test]
    public void PulseArea_Forwarded_KeepsRadiusAndRejectsInvalidGeometry()
    {
        int calls = 0;
        _sink.areaPulse = (center, radius, kind, strength) =>
        {
            calls++;
            Assert.AreEqual(2f, radius);
            Assert.AreEqual(HLZoneKind.Heal, kind);
        };

        _sink.PulseArea(Vector3.zero, 2f, HLZoneKind.Heal, 0.5f);
        _sink.PulseArea(Vector3.zero, -1f, HLZoneKind.Heal, 0.5f);

        Assert.AreEqual(1, calls);
    }

    [Test]
    public void LateUpdate_DestroyedTarget_ReleasesTheStatus()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, HLClockKind.Simulation);
        Object.DestroyImmediate(_target);

        TestHelpers.InvokePrivate(_sink, "LateUpdate");

        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void Clear_Impacts_ReleasesThem()
    {
        _sink.ShowImpact(null, _other, HLResourceKind.Health, 1f, false);

        _sink.Clear();

        Assert.AreEqual(0, _sink.impactCount);
    }
}
}
