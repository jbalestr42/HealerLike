using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class BattleBodyBoundsTests
{
    GameObject _root;
    GameObject _body;
    EntityManager _manager;
    BattleBodyBounds _bounds;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Bounds fixture");
        _manager = _root.AddComponent<EntityManager>();
        _body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _body.transform.SetParent(_root.transform);
        TestHelpers.SetPrivateField(_manager, "_entities", new Dictionary<Entity.EntityType, List<GameObject>>
        {
            { Entity.EntityType.Player, new List<GameObject> { _body } },
            { Entity.EntityType.Computer, new List<GameObject>() }
        });
        _bounds = new BattleBodyBounds();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void TryRead_DisabledOrRemovedCombatant_DropsItsCachedBounds()
    {
        Assert.IsTrue(_bounds.TryRead(_manager, out Bounds first));
        Assert.IsTrue(first.Contains(_body.transform.position));

        _body.SetActive(false);

        Assert.IsFalse(_bounds.TryRead(_manager, out _));
        Assert.AreEqual(0, _bounds.count);
        _body.SetActive(true);
        Assert.IsTrue(_bounds.TryRead(_manager, out _));
        Object.DestroyImmediate(_body);
        Assert.IsFalse(_bounds.TryRead(_manager, out _));
    }

    [Test]
    public void Clear_CollectedBodies_ReleasesThePreviousScene()
    {
        Assert.IsTrue(_bounds.TryRead(_manager, out _));

        _bounds.Clear();

        Assert.AreEqual(0, _bounds.count);
        Assert.IsFalse(_bounds.TryRead(null, out _));
    }
}

}
