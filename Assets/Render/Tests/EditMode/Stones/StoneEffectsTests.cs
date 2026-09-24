using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneEffectsTests
{
    StoneEffects _fx;
    GameObject _go;
    GameObject _source;
    Mesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _fx = RenderTestAssets.CreateStoneEffects();
        _go = _fx.gameObject;
        _source = new GameObject("SourceVisual");
        _mesh = StoneMesh.CreateMesh(1, StonePresets.Boulder);
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_source);
        if (_mesh != null)
        {
            Object.DestroyImmediate(_mesh);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OnDisable_LiveEffects_ClearsCopiesAndRejectsEveryEmission(bool deactivateObject)
    {
        GameObject partGo = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        partGo.transform.SetParent(_source.transform, false);
        Transform[] parts = new Transform[] { partGo.transform };
        StoneEmitters.DetachedPart(_fx, _mesh, null, Matrix4x4.identity, Vector3.zero, 0f, 1);
        Mesh copy = _go.GetComponentInChildren<MeshFilter>().sharedMesh;
        StoneEmitters.ThrownContact(_fx, Vector3.zero, 1);

        if (deactivateObject)
        {
            _go.SetActive(false);
        }
        else
        {
            _fx.enabled = false;
        }
        TestHelpers.InvokePrivate(_fx, "OnDisable");

        Assert.AreEqual(0, _fx.liveCount);
        Assert.IsTrue(copy == null);
        foreach (MeshFilter filter in _go.GetComponentsInChildren<MeshFilter>(true))
        {
            Assert.IsFalse(filter.gameObject.activeSelf);
            Assert.IsNull(filter.sharedMesh);
        }

        StoneEmitters.Hit(_fx, default, false, 1);
        StoneEmitters.ThrownContact(_fx, Vector3.zero, 1);
        StoneEmitters.DetachedPart(_fx, _mesh, null, Matrix4x4.identity, Vector3.zero, 0f, 1);
        StoneEmitters.Collapse(_fx, parts, Vector3.zero, 0f, 1);
        Assert.AreEqual(0, _fx.liveCount);
        Assert.IsTrue(partGo.activeSelf);

        int pooled = _go.transform.childCount;
        if (deactivateObject)
        {
            _go.SetActive(true);
        }
        else
        {
            _fx.enabled = true;
        }
        StoneEmitters.ThrownContact(_fx, Vector3.zero, 1);
        Assert.Greater(_fx.liveCount, 0);
        Assert.AreEqual(pooled, _go.transform.childCount);
    }

    [Test]
    public void TakeShard_ThenReturned_ComesFromAndGoesBackToThePool()
    {
        Transform shard = _fx.TakeShard(_mesh, Color.white);

        Assert.IsTrue(shard.gameObject.activeSelf);
        Assert.AreSame(_mesh, shard.GetComponent<MeshFilter>().sharedMesh);
        Assert.AreEqual(0, _fx.liveCount);

        _fx.ReturnShard(shard);
        Assert.IsFalse(shard.gameObject.activeSelf);
        Assert.AreEqual(1, _fx.transform.childCount);

        StoneEmitters.Dust(_fx, Vector3.zero, 1);

        Assert.AreEqual(StoneEffects.DustPuffs, _fx.transform.childCount); // the returned shard is reused
    }

    [Test]
    public void PositionAt_Bouncing_NeverFallsBelowGround()
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 position = StoneEffects.PositionAt(Vector3.up, Vector3.right, i * 0.02f, 0f, true);
            Assert.That(position.y, Is.GreaterThanOrEqualTo(0));
        }
        Assert.That(StoneEffects.PositionAt(Vector3.up, Vector3.right, 1f, 0f, false).y, Is.LessThan(0));
    }
}

}
