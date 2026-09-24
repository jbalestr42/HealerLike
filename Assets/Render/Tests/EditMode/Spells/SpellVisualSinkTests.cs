using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

// A sink wired like the shipped prefab, with the meshes the RenderManager would hand it
public static class SpellSinkFixture
{
    public static readonly string SinkPath = "Assets/Render/Spells/Prefabs/SpellVisualSink.prefab";
    public static readonly string MeshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

    public static SpellVisualSink Add(GameObject host)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SinkPath);
        SpellVisualSink shipped = prefab.GetComponent<SpellVisualSink>();
        SpellVisualSink sink = host.AddComponent<SpellVisualSink>();
        sink.looks = shipped.looks;
        sink.vocabulary = shipped.vocabulary;
        sink.material = shipped.material;
        sink.meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(MeshesPath);
        return sink;
    }

    public static BuffHandlerFactory Modifier(AttributeType type, float value, List<Object> created)
    {
        FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
        modifier.data = new FlatModifierData { type = type, modifierType = AttributeModifierType.Add, value = value };
        return Handler(modifier, false, 0f, created);
    }

    public static BuffHandlerFactory Consumer(float value, float period, List<Object> created)
    {
        ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
        FlatValue flat = new FlatValue();
        flat.data = new FlatValueData { value = value };
        consumer.data = new ConsumerData { value = flat };
        created.Add(consumer);
        ApplyConsumerBuffFactory buff = ScriptableObject.CreateInstance<ApplyConsumerBuffFactory>();
        buff.data = new ApplyConsumerBuffData { consumerFactory = consumer };
        return Handler(buff, period > 0f, period, created);
    }

    public static BuffHandlerFactory Invincible(List<Object> created)
    {
        return Handler(ScriptableObject.CreateInstance<InvincibilityBuffFactory>(), false, 0f, created);
    }

    static BuffHandlerFactory Handler(ABuffFactory buff, bool isPeriodic, float period, List<Object> created)
    {
        created.Add(buff);
        BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            isPeriodic = isPeriodic,
            periodDuration = period,
            buffFactoryList = new List<ABuffFactory> { buff }
        };
        created.Add(handler);
        return handler;
    }
}

public class SpellVisualSinkTests
{
    GameObject _host;
    GameObject _target;
    GameObject _other;
    SpellVisualSink _sink;
    BuffHandlerFactory _factory;
    BuffHandlerFactory _second;
    readonly List<Object> _created = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Host");
        _target = new GameObject("Target");
        _other = new GameObject("Other");
        _sink = SpellSinkFixture.Add(_host);
        _factory = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
        _second = SpellSinkFixture.Modifier(AttributeType.Damage, 3f, _created);
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_sink, "OnDestroy");
        Object.DestroyImmediate(_host);
        if (_target != null)
        {
            Object.DestroyImmediate(_target);
        }
        Object.DestroyImmediate(_other);
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    [Test]
    public void Looks_ShippedPrefab_CarriesLooksVocabularyAndTheLookMaterial()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpellSinkFixture.SinkPath);
        SpellVisualSink shipped = prefab.GetComponent<SpellVisualSink>();

        string data = "Assets/Render/Spells/Data/";
        SpellLooks looks = AssetDatabase.LoadAssetAtPath<SpellLooks>(data + "SpellLooks.asset");
        EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(data + "EffectVocabulary.asset");
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        Assert.AreSame(looks, shipped.looks);
        Assert.AreSame(vocabulary, shipped.vocabulary);
        Assert.AreSame(material, shipped.material);
    }

    [Test]
    public void SetStatus_DerivedBoon_BuildsTheOrbitWithoutAPrefab()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0.25f, 4f, ClockKind.Simulation);

        SpellEffect effect = _sink.GetStatus(_target, _factory).GetComponent<SpellEffect>();
        Assert.AreEqual(EffectElement.Orbit, effect.element);
        Assert.AreEqual(_target.transform, effect.transform.parent);
        Assert.IsNull(PrefabUtility.GetCorrespondingObjectFromSource(effect.gameObject));
        foreach (Transform part in effect.parts)
        {
            Assert.AreSame(_sink.material, part.GetComponent<Renderer>().sharedMaterial);
        }
    }

    [Test]
    public void SetStatus_HarmfulModifierFromAnEnemy_PressesInTheBaneAccent()
    {
        BuffHandlerFactory harm = SpellSinkFixture.Modifier(AttributeType.Damage, -2f, _created);
        Entity caster = null;
        Entity recipient = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            caster = _other.AddComponent<Entity>();
            recipient = _target.AddComponent<Entity>();
        });
        caster.entityType = Entity.EntityType.Computer;
        recipient.entityType = Entity.EntityType.Player;

        _sink.SetStatus(_other, _target, harm, 1, 0f, 4f, ClockKind.Simulation);

        SpellEffect effect = _sink.GetStatus(_target, harm).GetComponent<SpellEffect>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        effect.shapes[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreEqual(EffectElement.Press, effect.element);
        Assert.Less(Vector4.Distance(_sink.vocabulary.palette.bane, block.GetColor("_BaseColor")), 0.0001f);
    }

    [Test]
    public void SetStatus_TwoBoonHandlers_OneOrbitWithTwoStacks()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);
        _sink.SetStatus(null, _target, _second, 1, 0f, 4f, ClockKind.Simulation);

        SpellEffect orbit = _sink.GetElement(_target, EffectElement.Orbit);
        Assert.AreEqual(1, _sink.statusCount);
        Assert.AreSame(orbit.gameObject, _sink.GetStatus(_target, _second));
        Assert.AreEqual(2, orbit.stacks);
        Assert.AreEqual(3, orbit.count); // two tori, one more for the second stack

        _sink.RemoveStatus(null, _target, _factory);
        Assert.AreEqual(1, _sink.statusCount);
        Assert.AreEqual(1, orbit.stacks);

        _sink.RemoveStatus(null, _target, _second);
        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void SetStatus_SameTargetAndFactory_KeepsOneStatus()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);
        GameObject first = _sink.GetStatus(_target, _factory);

        _sink.SetStatus(_other, _target, _factory, 3, 2f, 4f, ClockKind.Realtime);
        _sink.SetStatus(null, _other, _factory, 1, 0f, 4f, ClockKind.Simulation);

        Assert.AreSame(first, _sink.GetStatus(_target, _factory));
        Assert.AreEqual(3, first.GetComponent<SpellEffect>().stacks);
        Assert.AreEqual(2, _sink.statusCount);

        _sink.RemoveStatus(_other, _target, _factory);
        _sink.RemoveStatus(_other, _target, _factory);

        Assert.AreEqual(1, _sink.statusCount);
    }

    [Test]
    public void SetCharges_BoonDefenceAndHitArmor_ShareThePlates()
    {
        BuffHandlerFactory armor = SpellSinkFixture.Modifier(AttributeType.HitArmor, 2f, _created);
        _sink.SetStatus(null, _target, armor, 1, 0f, 4f, ClockKind.Simulation);

        _sink.SetCharges(_target, 3f);

        SpellEffect plates = _sink.GetElement(_target, EffectElement.Plates);
        Assert.AreEqual(1, _sink.statusCount);
        Assert.AreEqual(3, plates.count);

        _sink.RemoveStatus(null, _target, armor);
        Assert.AreEqual(1, _sink.statusCount);
        _sink.SetCharges(_target, 0f);
        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void RemoveStatus_Removed_KeepsOnlyTheCosmeticTail()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0.25f, 4f, ClockKind.Simulation);
        SpellEffect effect = _sink.GetStatus(_target, _factory).GetComponent<SpellEffect>();

        _sink.RemoveStatus(null, _target, _factory);
        effect.Advance(0.25f);

        Assert.AreEqual(0, _sink.statusCount);
        Assert.IsTrue(effect);
        Assert.IsTrue(effect.removalComplete);
    }

    [Test]
    public void SetStatus_DisabledSink_ShowsNothing()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);
        GameObject status = _sink.GetStatus(_target, _factory);
        _sink.enabled = false;
        TestHelpers.InvokePrivate(_sink, "OnDisable");

        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);
        _sink.ShowImpact(null, _target, ResourceKind.Health, 2f, false);
        _sink.PulseArea(Vector3.zero, 1f, ZoneKind.Heal, 1f);

        Assert.IsFalse(status);
        Assert.AreEqual(0, _sink.statusCount);
        Assert.AreEqual(0, _sink.impactCount);
    }

    [Test]
    public void OnEnable_ReEnabled_StartsClean()
    {
        _sink.enabled = false;
        TestHelpers.InvokePrivate(_sink, "OnDisable");
        _sink.enabled = true;
        TestHelpers.InvokePrivate(_sink, "OnEnable");
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);
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
            _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, ClockKind.Simulation);
        }
        GameObject status = _sink.GetStatus(_target, _factory);
        // A delegate over the private method, because InvokePrivate allocates and this test measures allocation
        System.Reflection.MethodInfo method = typeof(SpellVisualSink).GetMethod("LateUpdate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        System.Action lateUpdate =
            (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), _sink, method);
        for (int i = 0; i < 32; i++)
        {
            if (isPopulated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, ClockKind.Simulation);
            }
            lateUpdate();
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 32; i++)
        {
            if (isPopulated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f, ClockKind.Simulation);
            }
            lateUpdate();
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
        Assert.AreSame(status, _sink.GetStatus(_target, _factory));
    }

    [Test]
    public void PulseArea_Hostile_SpawnsTheLitterInTheBaneAccent()
    {
        _sink.PulseArea(Vector3.one, 2f, ZoneKind.Hostile, 1f);

        SpellEffect effect = _host.GetComponentInChildren<SpellEffect>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        effect.shapes[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreEqual(EffectElement.Litter, effect.element);
        Assert.AreEqual(SpellVisualSink.PulseSeconds, effect.lifetime);
        Assert.AreEqual(Vector3.one, effect.transform.position);
        Assert.Less(Vector4.Distance(_sink.vocabulary.palette.bane, block.GetColor("_BaseColor")), 0.0001f);
    }

    [Test]
    public void PulseArea_Heal_KeepsTheExactRadius()
    {
        _sink.PulseArea(Vector3.right, 2f, ZoneKind.Heal, 0.3f);

        SpellEffect ring = _host.GetComponentInChildren<SpellEffect>();
        Assert.AreEqual(EffectElement.Ring, ring.element);
        Assert.AreEqual(Vector3.right, ring.transform.position);
        Assert.AreEqual(Vector3.one * 2f, ring.transform.localScale);
    }

    [Test]
    public void PulseArea_Forwarded_KeepsRadiusAndKind()
    {
        float forwardedRadius = 0f;
        ZoneKind forwardedKind = ZoneKind.Hostile;
        _sink.areaPulse = (center, radius, kind, strength) =>
        {
            forwardedRadius = radius;
            forwardedKind = kind;
        };

        _sink.PulseArea(Vector3.zero, 2f, ZoneKind.Heal, 0.5f);

        Assert.AreEqual(2f, forwardedRadius);
        Assert.AreEqual(ZoneKind.Heal, forwardedKind);
    }

    [Test]
    public void PulseArea_NegativeRadius_ForwardsNothing()
    {
        int calls = 0;
        _sink.areaPulse = (center, radius, kind, strength) => calls++;

        _sink.PulseArea(Vector3.zero, -1f, ZoneKind.Heal, 0.5f);

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void ShowContactLink_Contacts_DrawsAThreadWithoutBeads()
    {
        SpellEffect thread = _sink.ShowContactLink(Vector3.zero, Vector3.right);

        Assert.IsTrue(thread.isContactThread);
        Assert.AreEqual(EffectElement.Beam, thread.element);
        Assert.IsFalse(thread.shapes[0].gameObject.activeSelf);
        Assert.IsTrue(thread.stalks[0].gameObject.activeSelf);
    }

    [Test]
    public void ShowImpact_LargerAmount_DrawsALargerBurst()
    {
        _sink.ShowImpact(null, _target, ResourceKind.Health, -1f, false);
        float small = _host.GetComponentInChildren<SpellEffect>().transform.localScale.x;
        _sink.Clear();

        _sink.ShowImpact(null, _target, ResourceKind.Health, -100f, false);

        Assert.Greater(_host.GetComponentInChildren<SpellEffect>().transform.localScale.x, small);
    }

    [Test]
    public void ShowImpact_LargerHeal_RaisesMoreSpheres()
    {
        _sink.ShowImpact(null, _target, ResourceKind.Health, 1f, false);
        int few = _host.GetComponentInChildren<SpellEffect>().count;
        _sink.Clear();

        _sink.ShowImpact(null, _target, ResourceKind.Health, 100f, false);

        Assert.AreEqual(3, few);
        Assert.AreEqual(8, _host.GetComponentInChildren<SpellEffect>().count);
    }

    [Test]
    public void ShowImpact_SignedFiniteAmounts_PickTheElement()
    {
        _sink.ShowImpact(null, _target, ResourceKind.Health, 0f, false);
        _sink.ShowImpact(null, _target, ResourceKind.Health, float.NaN, false);
        Assert.AreEqual(0, _sink.impactCount);

        _sink.ShowImpact(null, _target, ResourceKind.Health, 5f, false);
        _sink.ShowImpact(null, _target, ResourceKind.Health, -5f, false);
        _sink.ShowImpact(null, _target, ResourceKind.Mana, 5f, false);
        _sink.ShowImpact(null, _target, ResourceKind.Mana, -5f, false);

        SpellEffect[] effects = _host.GetComponentsInChildren<SpellEffect>();
        Assert.AreEqual(4, _sink.impactCount);
        Assert.AreEqual(EffectElement.Rise, effects[0].element);
        Assert.AreEqual(EffectElement.Burst, effects[1].element);
        Assert.AreEqual(EffectElement.ManaUp, effects[2].element);
        Assert.AreEqual(EffectElement.ManaDown, effects[3].element);
    }

    [Test]
    public void ShowImpact_Critical_ShowsTheRings()
    {
        _sink.ShowImpact(null, _target, ResourceKind.Health, -3f, true);

        SpellEffect effect = _host.GetComponentInChildren<SpellEffect>();
        Assert.Greater(effect.rings.Count, 0);
        foreach (Transform ring in effect.rings)
        {
            Assert.IsTrue(ring.gameObject.activeSelf, ring.name);
        }
    }

    [Test]
    public void ShowImpact_NoVocabulary_ShowsNothing()
    {
        _sink.vocabulary = null;

        _sink.ShowImpact(null, _target, ResourceKind.Health, 3f, false);
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);

        Assert.AreEqual(0, _sink.impactCount);
        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void FlushLinks_OneCharacterHealingTwoRecipients_LinksEachOnceInLime()
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
        _sink.ShowImpact(_host, _target, ResourceKind.Health, 3f, false);
        _sink.ShowImpact(_host, _target, ResourceKind.Health, 4f, false);
        _sink.ShowImpact(_host, _other, ResourceKind.Health, 2f, false);
        _sink.ShowImpact(_other, _target, ResourceKind.Health, 2f, false);
        _sink.ShowImpact(_host, _target, ResourceKind.Mana, 2f, false);
        Assert.AreEqual(0, links);

        _sink.FlushLinks();
        _sink.FlushLinks();

        Assert.AreEqual(2, links); // one per recipient, the second flush has nothing left
        SpellEffect beam = null;
        foreach (SpellEffect effect in _host.GetComponentsInChildren<SpellEffect>())
        {
            if (effect.element == EffectElement.Beam)
            {
                beam = effect;
            }
        }
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        beam.stalks[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.Less(Vector4.Distance(_sink.vocabulary.palette.heal, block.GetColor("_BaseColor")), 0.0001f);
    }

    [Test]
    public void FlushLinks_SingleRecipient_DrawsNoBeam()
    {
        int links = 0;
        _sink.isCharacterSource = source => source == _host;
        _sink.linkObserved = (start, end) => links++;
        _sink.ShowImpact(_host, _target, ResourceKind.Health, 3f, false);

        _sink.FlushLinks();

        Assert.AreEqual(0, links);
    }

    [Test]
    public void FlushLinks_OneHealAndOneHit_DrawsNoBeam()
    {
        int links = 0;
        _sink.isCharacterSource = source => source == _host;
        _sink.linkObserved = (start, end) => links++;
        _sink.ShowImpact(_host, _target, ResourceKind.Health, 3f, false);
        _sink.ShowImpact(_host, _other, ResourceKind.Health, -3f, false);

        _sink.FlushLinks();

        Assert.AreEqual(0, links);
    }

    [Test]
    public void LateUpdate_DestroyedTarget_ReleasesTheStatus()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f, ClockKind.Simulation);
        Object.DestroyImmediate(_target);

        TestHelpers.InvokePrivate(_sink, "LateUpdate");

        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void Clear_Impacts_ReleasesThem()
    {
        _sink.ShowImpact(null, _other, ResourceKind.Health, 1f, false);

        _sink.Clear();

        Assert.AreEqual(0, _sink.impactCount);
    }
}

}
