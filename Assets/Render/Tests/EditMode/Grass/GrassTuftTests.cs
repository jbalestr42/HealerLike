using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassTuftTests
{
    Mesh _mesh;

    [SetUp]
    public void SetUp()
    {
        _mesh = GrassBladeMesh.Create(4);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_mesh);
    }

    [Test]
    public void Tilt_KnownLean_MatchesAngleAxis()
    {
        Vector2 lean = new Vector2(0.2f, -0.25f);
        float degrees = lean.magnitude * Mathf.Rad2Deg;
        Vector3 axis = new Vector3(lean.y, 0f, -lean.x).normalized;
        Vector3 tip = new Vector3(0.1f, 1f, -0.2f);

        Vector3 tilted = GrassTuft.Tilt(tip, lean);

        Vector3 expected = Quaternion.AngleAxis(degrees, axis) * tip;
        Assert.That(Vector3.Distance(expected, tilted), Is.LessThan(0.00001f));
    }

    [Test]
    public void Tilt_LeanTowardX_MovesTheTipTowardX()
    {
        Vector3 tip = GrassTuft.Tilt(Vector3.up, new Vector2(0.3f, 0f));

        Assert.That(tip.x, Is.EqualTo(Mathf.Sin(0.3f)).Within(0.00001f));
        Assert.That(tip.y, Is.EqualTo(Mathf.Cos(0.3f)).Within(0.00001f));
        Assert.That(tip.z, Is.EqualTo(0f).Within(0.00001f));
    }

    [Test]
    public void Tilt_NoLean_KeepsTheVector()
    {
        Vector3 v = new Vector3(0.3f, 0.7f, -0.4f);

        Assert.That(Vector3.Distance(v, GrassTuft.Tilt(v, Vector2.zero)), Is.LessThan(0.00001f));
    }

    [Test]
    public void Place_NoLean_ScalesYawsAndLiftsTheUnitTuft()
    {
        Vector3 root = new Vector3(2f, 0.505f, -3f);
        Quaternion yaw = Quaternion.Euler(0f, 1.1f * Mathf.Rad2Deg, 0f);

        foreach (Vector3 vertex in _mesh.vertices)
        {
            Vector3 placed = GrassTuft.Place(vertex, root, 1.1f, 0.085f, 0.13f, Vector2.zero);

            Vector3 expected = root + yaw * Vector3.Scale(vertex, new Vector3(0.085f, 0.13f, 0.085f));
            Assert.That(Vector3.Distance(expected, placed), Is.LessThan(0.00001f));
        }
    }

    [Test]
    public void Place_Lean_KeepsTheRootAndTiltsTheChordByTheLean()
    {
        Vector3 root = new Vector3(0f, 0.5f, 0f);
        Vector2 lean = new Vector2(0.35f, 0f);

        Vector3 upright = GrassTuft.Place(Vector3.up, root, 0.4f, 0.085f, 0.13f, Vector2.zero);
        Vector3 leaning = GrassTuft.Place(Vector3.up, root, 0.4f, 0.085f, 0.13f, lean);

        Assert.That(Vector3.Distance(root, GrassTuft.Place(Vector3.zero, root, 0.4f, 0.085f, 0.13f, lean)),
            Is.LessThan(0.00001f));
        Assert.That(Vector3.Angle(upright - root, leaning - root), Is.EqualTo(0.35f * Mathf.Rad2Deg).Within(0.01f));
        Assert.Greater(leaning.x, root.x, "The tip goes where the lean points.");
    }

    [Test]
    public void Spine_StrongLean_KeepsTheTuftsLength()
    {
        Vector2 lean = new Vector2(0.9f, -0.6f);
        float length = 0f;
        Vector3 previous = GrassTuft.Spine(lean, 0.7f, 0f);
        for (int i = 1; i <= 200; i++)
        {
            Vector3 next = GrassTuft.Spine(lean, 0.7f, i / 200f);
            length += Vector3.Distance(previous, next);
            previous = next;
        }

        Assert.That(length, Is.EqualTo(0.7f).Within(0.001f), "Bending curls the tuft, it never stretches it.");
        Assert.Less(previous.magnitude, 0.7f, "The chord of a bent tuft is shorter than the tuft.");
    }

    [Test]
    public void Spine_Lean_CurlsMoreAtTheTipThanAtTheRoot()
    {
        Vector2 lean = new Vector2(0f, 0.5f);
        Vector3 nearRoot = GrassTuft.Spine(lean, 1f, 0.01f) - GrassTuft.Spine(lean, 1f, 0f);
        Vector3 nearTip = GrassTuft.Spine(lean, 1f, 1f) - GrassTuft.Spine(lean, 1f, 0.99f);

        float rootAngle = Vector3.Angle(Vector3.up, nearRoot) * Mathf.Deg2Rad;
        float tipAngle = Vector3.Angle(Vector3.up, nearTip) * Mathf.Deg2Rad;

        Assert.That(rootAngle, Is.EqualTo(0.5f * GrassTuft.RootBend).Within(0.01f));
        Assert.That(tipAngle, Is.EqualTo(0.5f * (GrassTuft.RootBend + GrassTuft.TipBend)).Within(0.01f));
    }

    [Test]
    public void PlaceNormal_UnscaledTuft_TurnsWithTheTangentAtItsHeight()
    {
        Vector2 lean = new Vector2(0.2f, 0.15f);
        Vector2 tangent = GrassTuft.LeanAt(lean, 0.5f);
        Vector3 axis = new Vector3(tangent.y, 0f, -tangent.x).normalized;
        Quaternion rotation = Quaternion.AngleAxis(tangent.magnitude * Mathf.Rad2Deg, axis)
                              * Quaternion.Euler(0f, 40f, 0f);
        Vector3 normal = new Vector3(0f, 0.447f, -0.894f).normalized;

        Vector3 placed = GrassTuft.PlaceNormal(normal, 0.5f, 40f * Mathf.Deg2Rad, 1f, 1f, lean);

        Assert.That(Vector3.Distance(rotation * normal, placed), Is.LessThan(0.00001f));
    }

    [Test]
    public void Place_HalfRadianLean_MovesTheApexAlongTheShortenedChord()
    {
        Vector3 root = new Vector3(1f, 0.505f, 2f);
        float chord = 0.25f * GrassTuft.Sinc(0.5f * 0.5f * GrassTuft.TipBend);

        Vector3 apex = GrassTuft.Place(Vector3.up, root, 0.7f, 0.08f, 0.25f, new Vector2(0f, 0.5f));

        Assert.That(apex.z - root.z, Is.EqualTo(chord * Mathf.Sin(0.5f)).Within(0.00001f));
        Assert.That(apex.y - root.y, Is.EqualTo(chord * Mathf.Cos(0.5f)).Within(0.00001f));
        Assert.That(apex.x - root.x, Is.EqualTo(0f).Within(0.00001f));
    }

    [Test]
    public void Sinc_NearZero_IsOneAndMatchesTheQuotientBeyond()
    {
        Assert.AreEqual(1f, GrassTuft.Sinc(0f));
        Assert.That(GrassTuft.Sinc(0.00005f), Is.EqualTo(1f).Within(1e-6f));
        Assert.That(GrassTuft.Sinc(0.8f), Is.EqualTo(Mathf.Sin(0.8f) / 0.8f).Within(1e-6f));
    }
}

}
