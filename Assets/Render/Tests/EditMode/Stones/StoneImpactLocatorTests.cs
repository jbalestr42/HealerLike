using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneImpactLocatorTests
{
    [Test]
    public void ClosestFacePointUnderRotationAndNonuniformScale()
    {
        Vector3[] vertices = { Vector3.zero, Vector3.right, Vector3.up };
        Vector3[] normals = { Vector3.forward, Vector3.forward, Vector3.forward };
        StoneMeshData data = new StoneMeshData(vertices, normals, new int[] { 0, 1, 2 }, default);
        Quaternion rotation = Quaternion.Euler(21f, 53f, 17f);
        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(1f, 2f, 3f), rotation, new Vector3(2f, 3f, 0.5f));
        Vector3 expected = matrix.MultiplyPoint3x4(new Vector3(0.2f, 0.3f, 0f));
        Vector3 faceNormal = matrix.MultiplyVector(Vector3.forward).normalized;

        Vector3 query = expected + faceNormal * 2f;
        bool found = StoneImpactLocator.TryClosestPoint(data, matrix, query,
            out Vector3 point, out Vector3 normal);
        Assert.IsTrue(found);
        Assert.That(Vector3.Distance(expected, point), Is.LessThan(1e-5));
        Assert.That(Vector3.Dot(faceNormal, normal), Is.GreaterThan(0.99999f));

        query = new Vector3(-1f, -1f, 0f);
        Assert.IsTrue(StoneImpactLocator.TryClosestPoint(data, Matrix4x4.identity, query, out point, out normal));
        Assert.AreEqual(Vector3.zero, point);

        Assert.IsFalse(StoneImpactLocator.TryClosestPoint(default, matrix, Vector3.zero, out point, out normal));
    }
}

}
