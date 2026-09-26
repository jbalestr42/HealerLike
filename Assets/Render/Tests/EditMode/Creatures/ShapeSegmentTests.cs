using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class ShapeSegmentTests
{
    [TestCase(0f)]
    [TestCase(0.7f)]
    public void Fit_WithAxialOverlap_PreservesTheCentreAndAddsTheRequestedSpan(float distance)
    {
        Vector3 from = new Vector3(2f, 3f, 4f);
        Vector3 to = from + Vector3.up * distance;
        ShapeSegment pose = ShapeSegment.Fit(Vector3.down * 0.5f, Vector3.up * 0.5f, from, to, 0.2f, 0.2f);
        Assert.AreEqual((from + to) * 0.5f, pose.position);
        Assert.AreEqual(0.2f, pose.scale.x);
        Assert.AreEqual(distance + 0.2f, pose.scale.y, 0.000001f);
        Assert.AreEqual(0.2f, pose.scale.z);
    }

    [Test]
    public void Fit_ZeroRootSpanWithoutOverlap_KeepsTheRootCollapsed()
    {
        Vector3 point = new Vector3(2f, 3f, 4f);
        ShapeSegment pose = ShapeSegment.Fit(Vector3.down * 0.5f, Vector3.up * 0.5f, point, point, 0.2f);
        Assert.AreEqual(point, pose.position);
        Assert.AreEqual(Vector3.zero, pose.scale);
    }
}
}
