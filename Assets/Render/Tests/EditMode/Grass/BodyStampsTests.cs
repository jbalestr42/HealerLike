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
            start = new Vector3(1f, 0.7f, 2f), end = new Vector3(3f, 0.6f, 2f), radius = 0.1f
        };

        GroundStamp stamp = BodyStamps.Create(capsule, 0.5f, 2f);

        Assert.AreEqual(GroundStampKind.Body, stamp.kind);
        Assert.That(Vector4.Distance(new Vector4(1f, 2f, 0.2f, 0.1f), stamp.centreRadius), Is.LessThan(1e-5f));
        Assert.AreEqual(3f, stamp.push.x);
        Assert.AreEqual(0.1f, stamp.push.z, 1e-5f);
        Assert.AreEqual(BodyStamps.Margin * 2f, stamp.push.w, 1e-6f);
        Assert.AreEqual(GrassLayout.TuftHeight * 2f, stamp.body.x, 1e-6f);
        Assert.Greater(stamp.Sample(new Vector2(2f, 2f)).z, 0.95f, "A root on the ground lays the grass flat.");
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
