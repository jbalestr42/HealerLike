using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneEmittersTests
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
    public void Dust_Advanced_RisesFadesExpiresAndReusesWithoutAllocating()
    {
        StoneEmitters.Dust(_fx, Vector3.zero, 1);
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

        StoneEmitters.Dust(_fx, Vector3.zero, 1);
        _fx.Advance(0.01f);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++)
        {
            _fx.Advance(1f);
            StoneEmitters.Dust(_fx, Vector3.zero, 1);
            _fx.Advance(0.01f);
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, allocated);
        Assert.AreEqual(StoneEffects.DustPuffs, _go.transform.childCount);
    }

    [Test]
    public void Dust_EnabledThenDisabled_EmitsOnlyWhileEnabled()
    {
        StoneEmitters.Dust(_fx, Vector3.zero, 1);

        Assert.AreEqual(StoneEffects.DustPuffs, _fx.liveCount);

        _fx.Advance(1f);
        _fx.enabled = false;
        TestHelpers.InvokePrivate(_fx, "OnDisable");
        StoneEmitters.Dust(_fx, Vector3.zero, 1);
        StoneEmitters.ThrownContact(_fx, Vector3.zero, 1);

        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void Hit_ManyHits_CapsFragmentsAndExpires()
    {
        StoneImpact impact = new StoneImpact(Vector3.up, Vector3.up);

        StoneEmitters.Hit(_fx, impact, false, 1);
        Assert.AreEqual(StoneEmitters.HitChips, _fx.liveCount);

        _fx.Advance(0.6f);
        Assert.AreEqual(0, _fx.liveCount);

        StoneEmitters.Hit(_fx, impact, true, 1);
        Assert.AreEqual(StoneEmitters.CriticalHitChips, _fx.liveCount);

        for (uint i = 0; i < 60; i++)
        {
            StoneEmitters.Hit(_fx, impact, true, i);
        }
        Assert.AreEqual(StoneEffects.MaxLiveFragments, _fx.liveCount); // 61 * 5 fragments asked, 256 kept

        _fx.Advance(1f);
        Assert.AreEqual(0, _fx.liveCount);
        Assert.AreEqual(0, _go.GetComponentsInChildren<Collider>().Length);
        Assert.AreEqual(0, _go.GetComponentsInChildren<Rigidbody>().Length);
    }

    [Test]
    public void DetachedPart_SourceMeshDestroyed_CopySurvivesAndSplitsIntoThree()
    {
        Matrix4x4 pose = Matrix4x4.TRS(Vector3.up, Quaternion.identity, Vector3.one);
        StoneEmitters.DetachedPart(_fx, _mesh, null, pose, Vector3.zero, 0f, 1);
        Object.DestroyImmediate(_mesh);
        _mesh = null;

        _fx.Advance(0.24f);
        Assert.AreEqual(1, _fx.liveCount);
        Assert.IsNotNull(_go.GetComponentInChildren<MeshFilter>().sharedMesh);

        _fx.Advance(0.01f);
        Assert.AreEqual(StoneEmitters.SplitPieces, _fx.liveCount);

        _fx.Advance(0.25f);
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void ThrownContact_Burst_ExpiresAndReusesThePool()
    {
        StoneEmitters.ThrownContact(_fx, Vector3.one, 91);
        int count = _fx.liveCount;
        Assert.That(count, Is.InRange(StoneEmitters.MinThrownChips, StoneEmitters.MinThrownChips + 2));

        _fx.Advance(0.4f);
        Assert.AreEqual(count, _fx.liveCount);

        _fx.Advance(0.1f);
        Assert.AreEqual(0, _fx.liveCount);

        StoneEmitters.ThrownContact(_fx, Vector3.one, 91);
        Assert.AreEqual(count, _fx.liveCount);
        Assert.AreEqual(count, _go.transform.childCount);
    }

    [Test]
    public void Collapse_StandingAndHiddenParts_BreaksOnlyTheStandingOnes()
    {
        GameObject standing = new GameObject("Standing", typeof(MeshFilter), typeof(MeshRenderer));
        GameObject hidden = new GameObject("Hidden", typeof(MeshFilter), typeof(MeshRenderer));
        standing.transform.SetParent(_source.transform, false);
        hidden.transform.SetParent(_source.transform, false);
        hidden.transform.position = Vector3.right * 50f;
        hidden.SetActive(false);

        StoneEmitters.Collapse(_fx, new Transform[] { standing.transform, hidden.transform }, Vector3.zero, 0f, 1);

        Assert.AreEqual(StoneEffects.CollapseDebris + StoneEffects.DustPuffs, _fx.liveCount); // 12 debris and 5 dust
        foreach (MeshFilter filter in _go.GetComponentsInChildren<MeshFilter>())
        {
            Assert.Less(filter.transform.position.x, 25f);
        }
        Assert.IsTrue(standing.activeSelf); // the caller hides its own parts
    }

    [Test]
    public void Collapse_NothingStanding_EmitsNothing()
    {
        GameObject hidden = new GameObject("Hidden", typeof(MeshFilter), typeof(MeshRenderer));
        hidden.transform.SetParent(_source.transform, false);
        hidden.SetActive(false);

        StoneEmitters.Collapse(_fx, new Transform[] { hidden.transform }, Vector3.zero, 0f, 1);

        Assert.AreEqual(0, _fx.liveCount);
    }
}

}
