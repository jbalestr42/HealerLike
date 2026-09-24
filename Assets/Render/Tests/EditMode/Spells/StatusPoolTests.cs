using System.Collections.Generic;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class StatusPoolTests
{
    GameObject _host;
    GameObject _target;
    GameObject _other;
    StatusPool _pool;
    BuffHandlerFactory _factory;
    BuffHandlerFactory _second;
    readonly List<Object> _created = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Host");
        _target = new GameObject("Target");
        _other = new GameObject("Other");
        _pool = new StatusPool();
        _pool.Init(_host.transform, RenderTestAssets.LoadEffectVocabulary(),
                   AssetDatabase.LoadAssetAtPath<SpellLooks>(SpellSinkFixture.LooksPath), RenderTestAssets.LoadMeshes(),
                   RenderTestAssets.LoadLookMaterial());
        _factory = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
        _second = SpellSinkFixture.Modifier(AttributeType.Damage, 3f, _created);
    }

    [TearDown]
    public void TearDown()
    {
        _pool.Clear();
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
    public void Set_DerivedBoon_BuildsTheOrbitWithoutAPrefab()
    {
        _pool.Set(null, _target, _factory, 1, 0.25f, 4f);

        SpellEffect effect = _pool.Get(_target, _factory);
        Assert.AreEqual(EffectElement.Orbit, effect.element);
        Assert.AreEqual(_target.transform, effect.transform.parent);
        Assert.IsNull(PrefabUtility.GetCorrespondingObjectFromSource(effect.gameObject));
        foreach (Transform part in effect.parts)
        {
            Assert.AreSame(RenderTestAssets.LoadLookMaterial(), part.GetComponent<Renderer>().sharedMaterial);
        }
    }

    [Test]
    public void Set_HarmfulModifierFromAnEnemy_PressesInTheBaneAccent()
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

        _pool.Set(_other, _target, harm, 1, 0f, 4f);

        SpellEffect effect = _pool.Get(_target, harm);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        effect.shapes[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreEqual(EffectElement.Press, effect.element);
        Assert.Less(Vector4.Distance(RenderTestAssets.LoadPalette().bane, block.GetColor("_BaseColor")), 0.0001f);
    }

    // BoostCellItem: the handler the item puts on its carrier (a positional buff no row maps) and the boost its
    // cells put on the allies standing on them
    [TestCase("BuffHandlerFactory")]
    [TestCase("BoostBuffHandlerFactory")]
    public void Set_BoostCellHandlers_DeriveAFamilyWithoutThrowing(string handlerName)
    {
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
            "Assets/Data/EntityItems/BoostCellItem/" + handlerName + ".asset");
        Entity caster = null;
        Entity recipient = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            caster = _other.AddComponent<Entity>();
            recipient = _target.AddComponent<Entity>();
        });
        caster.entityType = Entity.EntityType.Player;
        recipient.entityType = Entity.EntityType.Player;

        _pool.Set(_other, _target, handler, 1, 0f, float.PositiveInfinity);

        Assert.AreEqual(EffectFamily.Boon, EffectDerivation.Family(handler, true));
        Assert.IsNotNull(_pool.Get(_target, handler));
    }

    [Test]
    public void Set_TwoBoonHandlers_OneOrbitWithTwoStacks()
    {
        _pool.Set(null, _target, _factory, 1, 0f, 4f);
        _pool.Set(null, _target, _second, 1, 0f, 4f);

        SpellEffect orbit = _pool.Get(_target, EffectElement.Orbit);
        Assert.AreEqual(1, _pool.count);
        Assert.AreSame(orbit.gameObject, _pool.Get(_target, _second).gameObject);
        Assert.AreEqual(2, orbit.stacks);
        Assert.AreEqual(3, orbit.count); // two tori, one more for the second stack

        _pool.Remove(_target, _factory);
        Assert.AreEqual(1, _pool.count);
        Assert.AreEqual(1, orbit.stacks);

        _pool.Remove(_target, _second);
        Assert.AreEqual(0, _pool.count);
    }

    [Test]
    public void Set_SameTargetAndFactory_KeepsOneStatus()
    {
        _pool.Set(null, _target, _factory, 1, 0f, 4f);
        GameObject first = _pool.Get(_target, _factory).gameObject;

        _pool.Set(_other, _target, _factory, 3, 2f, 4f);
        _pool.Set(null, _other, _factory, 1, 0f, 4f);

        Assert.AreSame(first, _pool.Get(_target, _factory).gameObject);
        Assert.AreEqual(3, first.GetComponent<SpellEffect>().stacks);
        Assert.AreEqual(2, _pool.count);

        _pool.Remove(_target, _factory);
        _pool.Remove(_target, _factory);

        Assert.AreEqual(1, _pool.count);
    }

    [Test]
    public void SetCharges_BoonDefenceAndHitArmor_ShareThePlates()
    {
        BuffHandlerFactory armor = SpellSinkFixture.Modifier(AttributeType.HitArmor, 2f, _created);
        _pool.Set(null, _target, armor, 1, 0f, 4f);

        _pool.SetCharges(_target, 3f);

        SpellEffect plates = _pool.Get(_target, EffectElement.Plates);
        Assert.AreEqual(1, _pool.count);
        Assert.AreEqual(3, plates.count);

        _pool.Remove(_target, armor);
        Assert.AreEqual(1, _pool.count);
        _pool.SetCharges(_target, 0f);
        Assert.AreEqual(0, _pool.count);
    }

    [Test]
    public void Remove_Removed_KeepsOnlyTheCosmeticTail()
    {
        _pool.Set(null, _target, _factory, 1, 0.25f, 4f);
        SpellEffect effect = _pool.Get(_target, _factory);

        _pool.Remove(_target, _factory);
        effect.Advance(0.25f);

        Assert.AreEqual(0, _pool.count);
        Assert.IsTrue(effect);
        Assert.IsTrue(effect.removalComplete);
    }

    [Test]
    public void Tick_DestroyedTarget_ReleasesTheStatus()
    {
        _pool.Set(null, _target, _factory, 1, 0f, 4f);
        Object.DestroyImmediate(_target);

        _pool.Tick();

        Assert.AreEqual(0, _pool.count);
    }
}

}
