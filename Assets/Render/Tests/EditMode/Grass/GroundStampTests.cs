using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundStampTests
{
    [Test]
    public void Stride_MatchesTheMarshalledSize()
    {
        Assert.AreEqual(GroundStamp.Stride, System.Runtime.InteropServices.Marshal.SizeOf<GroundStamp>());
    }

    [Test]
    public void Disc_Inside_LeansOutwardAndFlattens()
    {
        GroundStamp stamp = GroundStamp.Disc(new Vector2(1f, 1f), 2f, 0.4f, 0.8f, 0.2f, 0f);

        Vector3 east = stamp.Sample(new Vector2(2f, 1f));
        Vector3 north = stamp.Sample(new Vector2(1f, 2f));

        Assert.That(east.x, Is.EqualTo(0.4f).Within(1e-5f));
        Assert.That(east.y, Is.EqualTo(0f).Within(1e-5f));
        Assert.That(north.y, Is.EqualTo(0.4f).Within(1e-5f));
        Assert.That(east.z, Is.EqualTo(0.8f).Within(1e-5f));
    }

    [Test]
    public void Disc_Edge_FadesToNothingAtTheRadius()
    {
        GroundStamp stamp = GroundStamp.Disc(Vector2.zero, 2f, 0.4f, 1f, 0.25f, 0f);

        float inner = stamp.Sample(new Vector2(1.4f, 0f)).z;
        float fading = stamp.Sample(new Vector2(1.75f, 0f)).z;
        float outside = stamp.Sample(new Vector2(2.01f, 0f)).z;

        Assert.AreEqual(1f, inner);
        Assert.That(fading, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.AreEqual(0f, outside);
    }

    [Test]
    public void Disc_Wobble_OnlyPullsTheRimInward()
    {
        GroundStamp stamp = GroundStamp.Disc(Vector2.zero, 1f, 0f, 1f, 0.1f, 0.3f);

        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Assert.AreEqual(0f, stamp.Sample(direction * 1.001f).z, "Nothing past the radius.");
            Assert.AreEqual(1f, stamp.Sample(direction * 0.6f).z, "The core stays full.");
        }
    }

    [Test]
    public void Front_AheadOfTheCentre_LeansAlongTheHeadingOnlyNearTheFront()
    {
        GroundStamp stamp = GroundStamp.Front(Vector2.zero, 2f, 0.3f, new Vector2(0f, 2f), 0.6f);

        Vector3 onFront = stamp.Sample(new Vector2(0f, 2f));
        Vector3 behindFront = stamp.Sample(new Vector2(0f, 1f));
        Vector3 behindCentre = stamp.Sample(new Vector2(0f, -2f));

        Assert.That(onFront.y, Is.EqualTo(0.6f).Within(1e-5f));
        Assert.AreEqual(0f, onFront.x);
        Assert.AreEqual(0f, onFront.z, "A front never flattens.");
        Assert.AreEqual(Vector3.zero, behindFront);
        Assert.AreEqual(Vector3.zero, behindCentre);
    }

    [Test]
    public void Reach_CoversTheFrontBand()
    {
        Assert.AreEqual(2.3f, GroundStamp.Front(Vector2.zero, 2f, 0.3f, Vector2.right, 1f).reach, 1e-6f);
        Assert.AreEqual(1.5f, GroundStamp.Disc(Vector2.zero, 1.5f, 0f, 1f, 0.2f, 0f).reach, 1e-6f);
    }

    [Test]
    public void SmoothStep_MatchesHlsl()
    {
        Assert.AreEqual(0f, GroundStamp.SmoothStep(1f, 2f, 0.5f));
        Assert.AreEqual(0.5f, GroundStamp.SmoothStep(1f, 2f, 1.5f), 1e-6f);
        Assert.AreEqual(1f, GroundStamp.SmoothStep(1f, 2f, 3f));
        Assert.AreEqual(0.15625f, GroundStamp.SmoothStep(0f, 4f, 1f), 1e-6f);
    }
}

}
