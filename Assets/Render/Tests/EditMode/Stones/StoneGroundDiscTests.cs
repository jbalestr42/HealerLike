using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneGroundDiscTests
{
    GameObject _owner;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("DiscOwner");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
    }

    static StoneGroundDisc CreateDisc(Transform parent, bool isShadow)
    {
        GameObject discGo = new GameObject("Disc", typeof(MeshFilter), typeof(MeshRenderer));
        discGo.transform.SetParent(parent, false);
        StoneGroundDisc disc = discGo.AddComponent<StoneGroundDisc>();
        TestHelpers.SetPrivateField(disc, "_renderer", discGo.GetComponent<MeshRenderer>());
        TestHelpers.SetPrivateField(disc, "_isShadow", isShadow);
        return disc;
    }

    [Test]
    public void Init_Shadow_PointsAwayFromLightStaysFlatAndCanBeHidden()
    {
        StoneGroundDisc shadow = CreateDisc(_owner.transform, true);

        shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), new Vector3(-1f, 2f, 0f));

        Assert.Greater(shadow.transform.position.x, 0);
        Assert.Greater(shadow.transform.position.y, 0);
        Assert.Greater(Vector3.Dot(shadow.transform.forward, Vector3.right), 0.999f);
        Assert.Greater(shadow.transform.localScale.z, shadow.transform.localScale.x);
        Assert.Less(shadow.transform.localScale.y, 0.01f);

        shadow.Show(false);
        Assert.False(shadow.gameObject.activeSelf);

        shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), Vector3.zero);
        shadow.Show(true);
        Assert.True(shadow.gameObject.activeSelf);
        Assert.Greater(Vector3.Dot(shadow.transform.forward, Vector3.forward), 0.999f);
        Assert.AreEqual(1, _owner.transform.childCount);
    }

    [Test]
    public void Init_BareEarth_RadiusCoversScaledClumpAndColourGoesThroughTheBlock()
    {
        StoneGroundDisc ring = CreateDisc(_owner.transform, false);

        ring.Init(new Bounds(Vector3.up, new Vector3(2f, 2f, 1f)), Vector3.up);

        Assert.AreEqual(1.18f, ring.radius, 0.001f);
        Assert.AreEqual(0.006f, ring.center.y, 0.0001f);

        _owner.transform.localScale = new Vector3(2f, 1f, 3f);
        Assert.AreEqual(3.54f, ring.radius, 0.001f);

        Color colour = new Color(0.2f, 0.6f, 0.3f, 1f);
        ring.colour = colour;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        ring.GetComponent<MeshRenderer>().GetPropertyBlock(block);
        Assert.IsFalse(block.isEmpty);
        Assert.AreEqual(colour, ring.colour);
        Assert.AreEqual(0, _owner.GetComponentsInChildren<Collider>().Length);
    }
}

}
