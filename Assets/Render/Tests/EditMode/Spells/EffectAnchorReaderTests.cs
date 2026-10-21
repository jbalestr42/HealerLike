using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class EffectAnchorReaderTests
{
    GameObject _target;
    FakeEffectAnchors _view;

    [SetUp]
    public void SetUp()
    {
        _target = new GameObject("Recipient");
        _view = _target.AddComponent<FakeEffectAnchors>();
        _view.anchors = new EffectAnchors
        {
            foot = Vector3.one,
            bodyCentre = Vector3.one * 2f,
            bodyRadius = 0.5f,
            neck = Vector3.one * 3f,
            headCentre = Vector3.one * 4f,
            headRadius = 0.2f,
            castPoint = Vector3.one * 5f
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_target);
    }

    [Test]
    public void Read_ValidView_KeepsItsFullAnchors()
    {
        EffectAnchors anchors = EffectAnchorReader.Read(_target);

        Assert.AreEqual(_view.anchors.castPoint, anchors.castPoint);
        Assert.AreEqual(_view.anchors.headRadius, anchors.headRadius);
    }

    [Test]
    public void Read_NonfinitePosition_UsesTheFiniteTargetFallback(
        [Values(0, 1, 2, 3, 4)] int position, [Values(0, 1, 2)] int axis)
    {
        Vector3 invalid = Vector3.one;
        invalid[axis] = float.NaN;
        switch (position)
        {
            case 0:
                _view.anchors.foot = invalid;
                break;
            case 1:
                _view.anchors.bodyCentre = invalid;
                break;
            case 2:
                _view.anchors.neck = invalid;
                break;
            case 3:
                _view.anchors.headCentre = invalid;
                break;
            case 4:
                _view.anchors.castPoint = invalid;
                break;
        }

        EffectAnchors anchors = EffectAnchorReader.Read(_target);

        Assert.IsTrue(RenderMath.IsFinite(anchors.foot));
        Assert.IsTrue(RenderMath.IsFinite(anchors.bodyCentre));
        Assert.IsTrue(RenderMath.IsFinite(anchors.neck));
        Assert.IsTrue(RenderMath.IsFinite(anchors.headCentre));
        Assert.IsTrue(RenderMath.IsFinite(anchors.castPoint));
        Assert.AreEqual(EffectPlacement.FallbackBodyRadius, anchors.bodyRadius);
    }

    [TestCase(-1f)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NaN)]
    public void Read_InvalidHeadRadius_UsesTheFallback(float radius)
    {
        _view.anchors.headRadius = radius;

        EffectAnchors anchors = EffectAnchorReader.Read(_target);

        Assert.AreEqual(EffectPlacement.FallbackBodyRadius, anchors.bodyRadius);
    }
}

}
