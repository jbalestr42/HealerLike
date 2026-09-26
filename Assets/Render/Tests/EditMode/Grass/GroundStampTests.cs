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
    public void Bounds_CoverTheFrontBandTheDiscAndTheWholeBody()
    {
        GroundStamp.Front(Vector2.one, 2f, 0.3f, Vector2.right, 1f).Bounds(out Vector2 frontCentre, out float front);
        GroundStamp.Disc(Vector2.zero, 1.5f, 0f, 1f, 0.2f, 0f).Bounds(out _, out float disc);
        GroundStamp.Body(new Vector3(-1f, 0.2f, 0f), new Vector3(1f, 0.4f, 0f), 0.3f, 0.2f, 0.7f, 0.9f, 1f)
                   .Bounds(out Vector2 bodyCentre, out float body);

        Assert.AreEqual(Vector2.one, frontCentre);
        Assert.AreEqual(2.3f, front, 1e-6f);
        Assert.AreEqual(1.5f, disc, 1e-6f);
        Assert.AreEqual(Vector2.zero, bodyCentre);
        Assert.AreEqual(1.5f, body, 1e-6f);
    }

    [Test]
    public void Kind_EachFactory_TagsItsStamp()
    {
        Assert.AreEqual(GroundStampKind.Disc, GroundStamp.Disc(Vector2.zero, 1f, 0f, 1f, 0.2f, 0f).kind);
        Assert.AreEqual(GroundStampKind.Front, GroundStamp.Front(Vector2.zero, 1f, 0.2f, Vector2.up, 1f).kind);
        Assert.AreEqual(GroundStampKind.Body,
            GroundStamp.Body(Vector3.zero, Vector3.one, 0.2f, 0.2f, 0.7f, 0.9f, 1f).kind);
    }

    [Test]
    public void Body_FootOnTheGround_FlattensUnderItAndPushesTheGrassBesideItAway()
    {
        // A root lying along x, its axis 0.1 above the ground
        GroundStamp root = GroundStamp.Body(new Vector3(-1f, 0.1f, 0f), new Vector3(1f, 0.1f, 0f), 0.15f, 0.3f,
                                            0.7f, 0.9f, 1f);

        Vector3 under = root.Sample(new Vector2(0.3f, 0f));
        Vector3 beside = root.Sample(new Vector2(0.3f, 0.25f));
        Vector3 far = root.Sample(new Vector2(0.3f, 0.6f));

        Assert.AreEqual(1f, under.z, 1e-5f);
        Assert.AreEqual(0f, beside.z, "Only the capsule's own shadow lies flat.");
        Assert.Greater(beside.y, 0.3f, "The grass beside a root leans away from it.");
        Assert.AreEqual(0f, beside.x, 1e-5f);
        Assert.AreEqual(Vector3.zero, far);
    }

    [Test]
    public void Body_Hovering_OnlyBrushesTheGrassItReaches()
    {
        // A sphere of radius 0.3 with its underside at 0.5, over grass 0.7 tall
        GroundStamp belly = GroundStamp.Body(new Vector3(0f, 0.8f, 0f), new Vector3(0f, 0.8f, 0f), 0.3f, 0.3f,
                                             0.7f, 0.9f, 1f);
        GroundStamp high = GroundStamp.Body(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 1.5f, 0f), 0.3f, 0.3f,
                                            0.7f, 0.9f, 1f);

        Vector3 under = belly.Sample(Vector2.zero);

        Assert.AreEqual(0.2f / 0.7f, under.z, 1e-4f, "Pressed by the share of the grass above the underside.");
        Assert.AreEqual(Vector3.zero, high.Sample(Vector2.zero), "Nothing reaches grass it floats above.");
        Assert.AreEqual(Vector3.zero, high.Sample(new Vector2(0.35f, 0f)));
    }

    [Test]
    public void Body_SlopedCapsule_PressesHarderWhereItComesDown()
    {
        GroundStamp leg = GroundStamp.Body(new Vector3(0f, 0.9f, 0f), new Vector3(1f, 0.05f, 0f), 0.1f, 0.2f,
                                           0.7f, 0.9f, 1f);

        float hip = leg.Sample(new Vector2(0.05f, 0f)).z;
        float foot = leg.Sample(new Vector2(0.95f, 0f)).z;

        Assert.AreEqual(0f, hip);
        Assert.AreEqual(1f, foot, 1e-5f);
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
