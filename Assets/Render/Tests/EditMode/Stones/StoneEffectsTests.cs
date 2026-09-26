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
    public void OnDisable_BorrowedShard_RevokesItBeforeTheFragmentIsReused()
    {
        StoneFragmentPool.ShardLease previous = _fx.BorrowShard(_mesh, Color.white);
        Transform firstShard = previous.shard;

        _fx.enabled = false;
        TestHelpers.InvokePrivate(_fx, "OnDisable");

        Assert.IsNull(previous.shard);
        Assert.IsFalse(firstShard.gameObject.activeSelf);
        Assert.IsNull(firstShard.GetComponent<MeshFilter>().sharedMesh);
        _fx.enabled = true;
        StoneFragmentPool.ShardLease current = _fx.BorrowShard(_mesh, Color.red);
        Assert.AreSame(firstShard, current.shard);

        previous.Dispose();

        Assert.IsNotNull(current.shard);
        Assert.IsTrue(current.shard.gameObject.activeSelf);
        current.Dispose();
        Assert.IsFalse(firstShard.gameObject.activeSelf);
    }

    [Test]
    public void Advance_CollapseDebris_NeverFallsBelowTheGround()
    {
        GameObject part = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(_source.transform, false);
        part.transform.position = Vector3.up;
        StoneEmitters.Collapse(_fx, new Transform[] { part.transform }, Vector3.right, 0f, 1);

        for (int i = 0; i < 40; i++)
        {
            _fx.Advance(0.02f);
            foreach (MeshFilter filter in _go.GetComponentsInChildren<MeshFilter>())
            {
                Assert.That(filter.transform.position.y, Is.GreaterThanOrEqualTo(0f));
            }
        }
    }
}

}
