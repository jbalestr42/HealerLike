using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

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
    public void Init_ShippedPrefab_DrawsWithItsVocabularyAndTheLookMaterial()
    {
        GameObject sinkGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SpellSinkFixture.SinkPath));
        _created.Add(sinkGo);
        SpellVisualSink shipped = sinkGo.GetComponent<SpellVisualSink>();

        SpellSinkFixture.Init(shipped);
        shipped.SetStatus(null, _target, _factory, 1, 0f, 4f);

        SpellEffect effect = shipped.GetElement(_target, EffectElement.Orbit);
        Assert.IsNotNull(effect);
        Assert.AreSame(RenderTestAssets.LoadLookMaterial(), effect.parts[0].GetComponent<Renderer>().sharedMaterial);
        Assert.IsNull(new SerializedObject(shipped).FindProperty("_looks")); // the looks come from the manager
    }

    [Test]
    public void SetStatus_DisabledSink_ShowsNothing()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f);
        GameObject status = _sink.GetStatus(_target, _factory);
        _sink.enabled = false;
        TestHelpers.InvokePrivate(_sink, "OnDisable");

        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f);
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
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f);
        Assert.AreEqual(1, _sink.statusCount);

        TestHelpers.InvokePrivate(_sink, "OnEnable");

        Assert.AreEqual(0, _sink.statusCount);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Tick_RepeatedStatus_AllocatesNothing(bool isPopulated)
    {
        if (isPopulated)
        {
            _sink.SetStatus(null, _target, _factory, 1, 1f, 4f);
        }

        GameObject status = _sink.GetStatus(_target, _factory);
        for (int i = 0; i < 32; i++)
        {
            if (isPopulated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f);
            }

            _sink.Tick();
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 32; i++)
        {
            if (isPopulated)
            {
                _sink.SetStatus(null, _target, _factory, 1, 1f, 4f);
            }

            _sink.Tick();
        }

        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
        Assert.AreSame(status, _sink.GetStatus(_target, _factory));
    }

    [Test]
    public void IsShowing_StatusClearedByTheSink_ReturnsFalse()
    {
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f);
        Assert.IsTrue(_sink.IsShowing(_target, _factory));

        _sink.Clear();

        Assert.IsFalse(_sink.IsShowing(_target, _factory));
    }

    [Test]
    public void ShowImpact_ZeroOrNaNAmount_ShowsNothing()
    {
        _sink.ShowImpact(null, _target, ResourceKind.Health, 0f, false);
        _sink.ShowImpact(null, _target, ResourceKind.Health, float.NaN, false);

        Assert.AreEqual(0, _sink.impactCount);
    }

    [Test]
    public void ShowImpact_NoVocabulary_ShowsNothing()
    {
        TestHelpers.SetPrivateField(_sink, "_vocabulary", null);
        SpellSinkFixture.Init(_sink);

        _sink.ShowImpact(null, _target, ResourceKind.Health, 3f, false);
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f);

        Assert.AreEqual(0, _sink.impactCount);
        Assert.AreEqual(0, _sink.statusCount);
    }

    [Test]
    public void Clear_ImpactsAndTails_ReleasesThem()
    {
        _sink.ShowImpact(null, _other, ResourceKind.Health, 1f, false);
        _sink.SetStatus(null, _target, _factory, 1, 0f, 4f);
        _sink.RemoveStatus(null, _target, _factory);

        _sink.Clear();

        Assert.AreEqual(0, _sink.impactCount);
        Assert.AreEqual(0, _host.GetComponentsInChildren<SpellEffect>().Length);
    }
}

}
