using HealerLike.Render.Environment;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class BattleFocusBoundsTests
{
    [TestCase(0.5625f, 4f, 3f, 3f)]
    [TestCase(1.777778f, 4f, 3f, 3f)]
    [TestCase(0.5625f, 18f, 5f, 12f)]
    [TestCase(1.777778f, 18f, 5f, 12f)]
    [TestCase(0.5625f, 2f, 7f, 2f)]
    [TestCase(1.777778f, 2f, 7f, 2f)]
    public void Fit_BodyBounds_KeepsEveryCornerInsideTheHudMargin(float aspect, float width, float height, float depth)
    {
        Bounds bounds = new Bounds(new Vector3(7f, 2f, -4f), new Vector3(width, height, depth));

        Pose pose = BattleFocusBounds.Fit(bounds, 52f, 40f, aspect);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    Vector2 point = EnvironmentForeground.ToViewport(corner, pose.position, pose.rotation, 40f, aspect);
                    Assert.That(point.x, Is.InRange(0.0799f, 0.9201f));
                    Assert.That(point.y, Is.InRange(0.1999f, 0.8201f));
                }
            }
        }
    }

    [Test]
    public void Fit_WiderOrMovedBounds_WidensViewAndTranslationOnlyMovesPose()
    {
        Bounds small = new Bounds(Vector3.zero, new Vector3(3f, 3f, 3f));
        Bounds wide = new Bounds(Vector3.zero, new Vector3(18f, 3f, 12f));

        Pose smallPose = BattleFocusBounds.Fit(small, 52f, 40f, 0.5625f);
        Pose widePose = BattleFocusBounds.Fit(wide, 52f, 40f, 0.5625f);

        Assert.That(widePose.position.magnitude, Is.GreaterThan(smallPose.position.magnitude * 2f));
        Vector3 offset = new Vector3(13f, 4f, -7f);
        small.center += offset;
        Pose movedPose = BattleFocusBounds.Fit(small, 52f, 40f, 0.5625f);
        Assert.That(Vector3.Distance(movedPose.position, smallPose.position + offset), Is.LessThan(0.0001f));
    }
}

}
