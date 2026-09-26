using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class BodyCapsuleTests
{
    [Test]
    public void TryFromBounds_Cube_IsASphereAtItsCentre()
    {
        Matrix4x4 place = Matrix4x4.Translate(new Vector3(1f, 2f, 3f));

        Assert.IsTrue(BodyCapsule.TryFromBounds(place, new Bounds(Vector3.zero, Vector3.one), out BodyCapsule sphere));

        Assert.AreEqual(new Vector3(1f, 2f, 3f), sphere.start);
        Assert.AreEqual(sphere.start, sphere.end);
        Assert.AreEqual(0.5f, sphere.radius, 1e-6f);
        Assert.AreEqual(1.5f, sphere.bottom, 1e-6f);
        Assert.AreEqual(1f, sphere.press, "A mesh of the body is solid.");
    }

    [Test]
    public void TryFromBounds_TippedCylinder_RunsAlongItsLongAxis()
    {
        // A unit-wide cylinder two units tall, tipped over onto the x axis
        Matrix4x4 place = Matrix4x4.TRS(new Vector3(0f, 0.5f, 0f), Quaternion.Euler(0f, 0f, 90f), Vector3.one);

        Assert.IsTrue(BodyCapsule.TryFromBounds(place, new Bounds(Vector3.zero, new Vector3(1f, 2f, 1f)),
                                                out BodyCapsule root));

        Assert.AreEqual(0.5f, root.radius, 1e-5f);
        Assert.AreEqual(0.5f, Mathf.Abs(root.start.x), 1e-5f, "The segment stops a radius short of each end.");
        Assert.AreEqual(-root.start.x, root.end.x, 1e-5f);
        Assert.AreEqual(0.5f, root.start.y, 1e-5f);
        Assert.AreEqual(0f, root.bottom, 1e-5f);
    }

    [Test]
    public void TryFromBounds_Scale_ScalesTheCapsule()
    {
        Matrix4x4 place = Matrix4x4.Scale(new Vector3(0.2f, 3f, 0.4f));

        BodyCapsule.TryFromBounds(place, new Bounds(Vector3.zero, Vector3.one), out BodyCapsule capsule);

        Assert.AreEqual(0.15f, capsule.radius, 1e-5f);
        Assert.AreEqual(1.5f - 0.15f, capsule.end.y, 1e-5f);
    }

    [Test]
    public void TryFromBounds_EmptyOrNonFinite_Fails()
    {
        Assert.IsFalse(BodyCapsule.TryFromBounds(Matrix4x4.identity, new Bounds(Vector3.zero, Vector3.zero), out _));
        Assert.IsFalse(BodyCapsule.TryFromBounds(Matrix4x4.Translate(new Vector3(float.NaN, 0f, 0f)),
                                                 new Bounds(Vector3.zero, Vector3.one), out _));
    }
}

}
