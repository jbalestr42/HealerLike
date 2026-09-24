using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneImpactsTests
{
    GameObject _owner;
    GameObject _part;
    GameObject _source;
    Mesh _mesh;
    StoneEffects _fx;
    StoneImpacts _impacts;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Stone");
        _source = new GameObject("Source");
        _source.transform.position = Vector3.up * 5f;
        _part = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        _part.transform.SetParent(_owner.transform, false);
        _mesh = StoneMesh.CreateMesh(1, StonePresets.Boulder);
        _part.GetComponent<MeshFilter>().sharedMesh = _mesh;
        _fx = RenderTestAssets.CreateStoneEffects();
        _impacts = new StoneImpacts();
        _impacts.Init(_owner.transform, _fx, 1);
        _impacts.ReadParts(new Transform[] { _part.transform });
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_fx.gameObject);
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_mesh);
    }

    // Every live fragment sits within the distance of the point
    void AssertFragmentsNear(Vector3 point, float distance)
    {
        foreach (MeshFilter filter in _fx.GetComponentsInChildren<MeshFilter>())
        {
            Assert.That(Vector3.Distance(filter.transform.position, point), Is.LessThan(distance));
        }
    }

    [Test]
    public void Resolve_ReportedContact_HitsWhereItWasReported()
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };
        Vector3 point = new Vector3(23f, 7f, 4f);
        _impacts.Record(modifier, new StoneImpact(point, Vector3.up));
        _fx.Advance(1f);

        _impacts.Resolve(modifier, true, false);

        Assert.AreEqual(StoneEmitters.HitChips, _fx.liveCount); // the dust rose when it was reported
        AssertFragmentsNear(point, 0.01f);
    }

    [Test]
    public void Resolve_UnreportedHit_EstimatesOnThePartAndRaisesDust()
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };

        _impacts.Resolve(modifier, true, false);

        Assert.AreEqual(StoneEffects.DustPuffs + StoneEmitters.HitChips, _fx.liveCount);
        AssertFragmentsNear(Vector3.zero, 3f);
    }

    [Test]
    public void Resolve_NotAHit_DropsTheContactWithoutEmitting()
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };
        Vector3 point = new Vector3(23f, 7f, 4f);
        _impacts.Record(modifier, new StoneImpact(point, Vector3.up));
        _fx.Advance(1f);

        _impacts.Resolve(modifier, false, false);
        Assert.AreEqual(0, _fx.liveCount);

        _impacts.Resolve(modifier, true, false);
        AssertFragmentsNear(Vector3.zero, 3f); // estimated, the contact is gone
    }

    [Test]
    public void CompleteFrame_TwoFramesUnresolved_DropsTheContact()
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };
        Vector3 point = new Vector3(23f, 7f, 4f);
        _impacts.Record(modifier, new StoneImpact(point, Vector3.up));
        _impacts.CompleteFrame();
        _impacts.CompleteFrame();
        _fx.Advance(1f);

        _impacts.Resolve(modifier, true, false);

        Assert.AreEqual(StoneEffects.DustPuffs + StoneEmitters.HitChips, _fx.liveCount);
        AssertFragmentsNear(Vector3.zero, 3f);
    }

    [Test]
    public void Estimate_QueryAboveThePart_LandsOnItsTop()
    {
        Bounds bounds = _part.GetComponent<Renderer>().bounds;

        StoneImpact impact = _impacts.Estimate(Vector3.up * 5f);

        Assert.Greater(impact.point.y, bounds.center.y);
        Assert.Greater(impact.normal.y, 0f);
    }
}

}
