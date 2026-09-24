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

    [Test]
    public void EmitDust_Advanced_RisesFadesExpiresAndReusesWithoutAllocating()
    {
        _fx.EmitDust(Vector3.zero, 1);
        Assert.AreEqual(StoneEffects.DustPuffs, _fx.liveCount);

        MeshRenderer renderer = _go.GetComponentInChildren<MeshRenderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _fx.Advance(0.1f);
        renderer.GetPropertyBlock(block);
        float alpha = block.GetColor("_BaseColor").a;
        Assert.Greater(renderer.transform.position.y, 0);

        _fx.Advance(0.2f);
        renderer.GetPropertyBlock(block);
        Assert.Less(block.GetColor("_BaseColor").a, alpha);

        _fx.Advance(1f);
        Assert.AreEqual(0, _fx.liveCount);

        _fx.EmitDust(Vector3.zero, 1);
        _fx.Advance(0.01f);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++)
        {
            _fx.Advance(1f);
            _fx.EmitDust(Vector3.zero, 1);
            _fx.Advance(0.01f);
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, allocated);
        Assert.AreEqual(StoneEffects.DustPuffs, _go.transform.childCount);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OnDisable_LiveEffects_ClearsCopiesAndRejectsEveryEmission(bool deactivateObject)
    {
        GameObject partGo = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        partGo.transform.SetParent(_source.transform, false);
        Transform[] parts = new Transform[] { partGo.transform };
        _fx.EmitDetachedPart(_mesh, null, Matrix4x4.identity, Vector3.zero, 0f, 1);
        Mesh copy = _go.GetComponentInChildren<MeshFilter>().sharedMesh;
        _fx.EmitThrownContact(Vector3.zero, 1);

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

        _fx.EmitHit(default, false, 1);
        _fx.EmitThrownContact(Vector3.zero, 1);
        _fx.EmitDetachedPart(_mesh, null, Matrix4x4.identity, Vector3.zero, 0f, 1);
        _fx.CollapseParts(parts, Vector3.zero, 0f, 1);
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
        _fx.EmitThrownContact(Vector3.zero, 1);
        Assert.Greater(_fx.liveCount, 0);
        Assert.AreEqual(pooled, _go.transform.childCount);
    }

    [Test]
    public void RecordImpact_EnabledThenDisabled_EmitsDustOnlyWhileEnabled()
    {
        _fx.RecordImpact(Vector3.zero, 1);

        Assert.AreEqual(StoneEffects.DustPuffs, _fx.liveCount);

        _fx.Advance(1f);
        _fx.enabled = false;
        TestHelpers.InvokePrivate(_fx, "OnDisable");
        _fx.RecordImpact(Vector3.zero, 1);
        _fx.EmitThrownContact(Vector3.zero, 1);

        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void EmitHit_ManyHits_CapsFragmentsAndExpires()
    {
        StoneImpact impact = new StoneImpact(Vector3.up, Vector3.up);

        _fx.EmitHit(impact, false, 1);
        Assert.AreEqual(StoneEffects.HitSparks + StoneEffects.HitChips, _fx.liveCount);

        _fx.Advance(0.6f);
        Assert.AreEqual(0, _fx.liveCount);

        _fx.EmitHit(impact, true, 1);
        Assert.AreEqual(StoneEffects.CriticalHitSparks + StoneEffects.CriticalHitChips, _fx.liveCount);

        for (uint i = 0; i < 40; i++)
        {
            _fx.EmitHit(impact, true, i);
        }
        Assert.AreEqual(StoneEffects.MaxLiveFragments, _fx.liveCount); // 41 * 14 fragments asked, 256 kept

        _fx.Advance(1f);
        Assert.AreEqual(0, _fx.liveCount);
        Assert.AreEqual(0, _go.GetComponentsInChildren<Collider>().Length);
        Assert.AreEqual(0, _go.GetComponentsInChildren<Rigidbody>().Length);
    }

    [Test]
    public void EmitDetachedPart_SourceMeshDestroyed_CopySurvivesAndSplitsIntoThree()
    {
        Matrix4x4 pose = Matrix4x4.TRS(Vector3.up, Quaternion.identity, Vector3.one);
        _fx.EmitDetachedPart(_mesh, null, pose, Vector3.zero, 0f, 1);
        Object.DestroyImmediate(_mesh);
        _mesh = null;

        _fx.Advance(0.24f);
        Assert.AreEqual(1, _fx.liveCount);
        Assert.IsNotNull(_go.GetComponentInChildren<MeshFilter>().sharedMesh);

        _fx.Advance(0.01f);
        Assert.AreEqual(StoneEffects.SplitPieces, _fx.liveCount);

        _fx.Advance(0.25f);
        Assert.AreEqual(0, _fx.liveCount);
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

        _fx.EmitDust(Vector3.zero, 1);

        Assert.AreEqual(StoneEffects.DustPuffs, _fx.transform.childCount); // the returned shard is reused
    }

    [Test]
    public void EmitThrownContact_Burst_ExpiresAndReusesThePool()
    {
        _fx.EmitThrownContact(Vector3.one, 91);
        int count = _fx.liveCount;
        int minimum = StoneEffects.MinThrownChips + StoneEffects.StarRays;
        Assert.That(count, Is.InRange(minimum, minimum + 2));

        _fx.Advance(0.2f);
        Assert.AreEqual(count - StoneEffects.StarRays, _fx.liveCount);

        _fx.Advance(0.3f);
        Assert.AreEqual(0, _fx.liveCount);

        _fx.EmitThrownContact(Vector3.one, 91);
        Assert.AreEqual(count, _fx.liveCount);
        Assert.AreEqual(count, _go.transform.childCount);
    }

    [Test]
    public void CollapseParts_StandingAndHiddenParts_BreaksOnlyTheStandingOnes()
    {
        GameObject standing = new GameObject("Standing", typeof(MeshFilter), typeof(MeshRenderer));
        GameObject hidden = new GameObject("Hidden", typeof(MeshFilter), typeof(MeshRenderer));
        standing.transform.SetParent(_source.transform, false);
        hidden.transform.SetParent(_source.transform, false);
        hidden.transform.position = Vector3.right * 50f;
        hidden.SetActive(false);

        _fx.CollapseParts(new Transform[] { standing.transform, hidden.transform }, Vector3.zero, 0f, 1);

        Assert.AreEqual(StoneEffects.CollapseDebris + StoneEffects.DustPuffs, _fx.liveCount); // 12 debris and 5 dust
        foreach (MeshFilter filter in _go.GetComponentsInChildren<MeshFilter>())
        {
            Assert.Less(filter.transform.position.x, 25f);
        }
        Assert.IsTrue(standing.activeSelf); // the caller hides its own parts
    }

    [Test]
    public void CollapseParts_NothingStanding_EmitsNothing()
    {
        GameObject hidden = new GameObject("Hidden", typeof(MeshFilter), typeof(MeshRenderer));
        hidden.transform.SetParent(_source.transform, false);
        hidden.SetActive(false);

        _fx.CollapseParts(new Transform[] { hidden.transform }, Vector3.zero, 0f, 1);

        Assert.AreEqual(0, _fx.liveCount);
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
