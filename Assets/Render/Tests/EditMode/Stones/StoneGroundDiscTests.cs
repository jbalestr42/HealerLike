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

    [Test]
    public void Init_Shadow_PointsAwayFromLightStaysFlatAndCanBeHidden()
    {
        StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);

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
    public void IsAllowed_Off_HidesAShownDiscAndOnRestoresOnlyWhatTheOwnerShows()
    {
        StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
        shadow.Show(true);

        shadow.isAllowed = false;
        Assert.IsFalse(shadow.gameObject.activeSelf);

        shadow.Show(true);
        Assert.IsFalse(shadow.gameObject.activeSelf, "the owner cannot show a disc the key light turned off");

        shadow.Show(false);
        shadow.isAllowed = true;
        Assert.IsFalse(shadow.gameObject.activeSelf);

        shadow.Show(true);
        Assert.IsTrue(shadow.gameObject.activeSelf);
    }
}

}
