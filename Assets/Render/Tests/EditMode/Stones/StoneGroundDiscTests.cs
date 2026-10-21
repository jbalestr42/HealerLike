using HealerLike.Render.Stage;
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

        shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), new Vector3(-1f, 2f, 0f), null);

        Assert.Greater(shadow.transform.position.x, 0);
        Assert.Greater(shadow.transform.position.y, 0);
        Assert.Greater(Vector3.Dot(shadow.transform.forward, Vector3.right), 0.999f);
        Assert.Greater(shadow.transform.localScale.z, shadow.transform.localScale.x);
        Assert.Less(shadow.transform.localScale.y, 0.01f);

        shadow.Show(false);
        Assert.False(shadow.gameObject.activeSelf);

        shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), Vector3.zero, null);
        shadow.Show(true);
        Assert.True(shadow.gameObject.activeSelf);
        Assert.Greater(Vector3.Dot(shadow.transform.forward, Vector3.forward), 0.999f);
        Assert.AreEqual(1, _owner.transform.childCount);
    }

    [Test]
    public void Refresh_KeyLightWithRealShadows_HidesTheShadowWhileTheyAreDrawn()
    {
        StageKeyLight keyLight = _owner.AddComponent<StageKeyLight>();
        TestHelpers.SetPrivateField(keyLight, "_realShadows", true);
        StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
        shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), Vector3.up, keyLight);

        shadow.Show(true);
        Assert.IsFalse(shadow.gameObject.activeSelf, "the owner cannot show a shadow the key light draws");

        TestHelpers.SetPrivateField(keyLight, "_realShadows", false);
        shadow.Refresh();
        Assert.IsTrue(shadow.gameObject.activeSelf);

        shadow.Show(false);
        shadow.Refresh();
        Assert.IsFalse(shadow.gameObject.activeSelf);
    }

    [Test]
    public void Refresh_BareEarthUnderRealShadows_StaysShown()
    {
        StageKeyLight keyLight = _owner.AddComponent<StageKeyLight>();
        TestHelpers.SetPrivateField(keyLight, "_realShadows", true);
        StoneGroundDisc earth = RenderTestAssets.CreateGroundDisc(_owner.transform, false);
        earth.Init(new Bounds(Vector3.up, Vector3.one * 2f), Vector3.up, keyLight);

        earth.Show(true);
        earth.Refresh();

        Assert.IsTrue(earth.gameObject.activeSelf);
    }
}

}
