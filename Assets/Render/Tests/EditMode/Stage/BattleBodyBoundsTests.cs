using System.Collections.Generic;
using HealerLike.Render.Creatures;
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
    public void TryRead_DetachedRigTracksFinalSpawnPositionAndRecomposition()
    {
        _body.GetComponent<Renderer>().enabled = false;
        CreatureRecipe recipe = RenderTestAssets.CreateRecipe();
        Material material = new Material(RenderTestAssets.LoadLookMaterial());
        recipe.roots.count = 0;
        CreaturePart head = recipe.parts[0];
        head.id = "High head";
        head.parent = 0;
        head.localPosition = Vector3.up * 5f;
        recipe.parts = new[] { recipe.parts[0], head };
        Entity entity = RenderTestAssets.CreateStoneEntity(_body, null);
        CreatureBuilder host = _body.AddComponent<CreatureBuilder>();
        RenderTestAssets.SetRecipe(host, recipe, material, RenderTestAssets.LoadMeshes());
        try
        {
            host.Init(entity);
            Assert.IsTrue(_bounds.TryRead(_manager, out Bounds first));
            Assert.Greater(first.max.y, 5f);
            _body.transform.position = Vector3.right * 20f;
            Assert.IsTrue(_bounds.TryRead(_manager, out Bounds moved));
            Assert.That(moved.center.x - first.center.x, Is.EqualTo(20f).Within(0.0001f));
            recipe.parts[1].localPosition = Vector3.up * 7f;
            Assert.IsTrue(host.Rebuild(null));
            Assert.IsTrue(_bounds.TryRead(_manager, out Bounds rebuilt));
            Assert.Greater(rebuilt.max.y, 7f);
            Assert.IsTrue(_body.GetComponent<Collider>().enabled);
        }
        finally
        {
            TestHelpers.InvokePrivate(host, "OnDestroy");
            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(material);
        }
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
