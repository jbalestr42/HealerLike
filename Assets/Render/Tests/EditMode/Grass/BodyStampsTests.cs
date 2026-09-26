using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class BodyStampsTests
{
    [Test]
    public void Create_Capsule_MeasuresHeightsFromTheGrassSurface()
    {
        BodyCapsule capsule = new BodyCapsule
        {
            start = new Vector3(1f, 0.7f, 2f), end = new Vector3(3f, 0.6f, 2f), radius = 0.1f, press = 1f
        };

        GroundStamp stamp = BodyStamps.Create(capsule, 0.5f, 2f);

        Assert.AreEqual(GroundStampKind.Body, stamp.kind);
        Assert.That(Vector4.Distance(new Vector4(1f, 2f, 0.2f, 0.1f), stamp.centreRadius), Is.LessThan(1e-5f));
        Assert.AreEqual(3f, stamp.push.x);
        Assert.AreEqual(0.1f, stamp.push.z, 1e-5f);
        Assert.AreEqual(BodyStamps.Margin * 2f, stamp.push.w, 1e-6f);
        Assert.AreEqual(GrassLayout.TuftHeight * 2f, stamp.response.x, 1e-6f);
        Assert.Greater(stamp.Sample(new Vector2(2f, 2f)).z, 0.95f, "A root on the ground lays the grass flat.");
    }

    [Test]
    public void Create_BrushingCapsule_PartsTheGrassWithoutFlatteningIt()
    {
        BodyCapsule liana = new BodyCapsule
        {
            start = new Vector3(0f, 0.1f, 0f), end = new Vector3(2f, 0.1f, 0f), radius = 0.05f, press = 0f
        };

        GroundStamp stamp = BodyStamps.Create(liana, 0f, 1f);

        Assert.AreEqual(0f, stamp.Sample(new Vector2(1f, 0f)).z, "A brushing liana flattens nothing.");
        Assert.Greater(stamp.Sample(new Vector2(1f, 0.2f)).y, 0.2f, "It parts the grass beside it.");
    }

    [Test]
    public void Create_BrushingCapsuleOverTheGrass_StillStirsItFadingWithHeight()
    {
        BodyCapsule low = new BodyCapsule
        {
            start = new Vector3(0f, 1f, 0f), end = new Vector3(2f, 1f, 0f), radius = 0.05f, press = 0f
        };
        BodyCapsule high = low;
        high.start.y = high.end.y = 1.5f;
        BodyCapsule solid = low;
        solid.press = 1f;

        float lowLean = BodyStamps.Create(low, 0f, 1f).Sample(new Vector2(1f, 0.2f)).y;
        float highLean = BodyStamps.Create(high, 0f, 1f).Sample(new Vector2(1f, 0.2f)).y;

        Assert.Greater(lowLean, 0.1f, "A liana passing a cell up still parts the grass under it.");
        Assert.Less(highLean, lowLean, "Less the higher it flies.");
        Assert.AreEqual(0f, BodyStamps.Create(solid, 0f, 1f).Sample(new Vector2(1f, 0.2f)).y, 1e-5f,
            "A solid body only presses what it reaches.");
    }

    [Test]
    public void Append_Capsules_WritesOnePerCapsuleAsFarAsItFits()
    {
        BodyCapsule[] capsules = new BodyCapsule[4];
        GroundStamp[] into = new GroundStamp[3];

        Assert.AreEqual(2, BodyStamps.Append(capsules, 4, 0f, 1f, into, 1));
        Assert.AreEqual(GroundStampKind.Body, into[2].kind);
        Assert.AreEqual(0, BodyStamps.Append(null, 4, 0f, 1f, into, 0));
        Assert.AreEqual(1, BodyStamps.Append(capsules, 1, 0f, 1f, into, 0));
    }
}

}
