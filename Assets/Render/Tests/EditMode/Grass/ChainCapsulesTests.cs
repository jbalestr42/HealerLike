using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class ChainCapsulesTests
{
    static Vector3[] Line(int count, float height)
    {
        Vector3[] joints = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            joints[i] = new Vector3(i * 0.2f, height, i * 0.2f);
        }

        return joints;
    }

    [Test]
    public void Append_LowChain_BrushesAlongItsJointsWithoutPressing()
    {
        Vector3[] joints = Line(5, 0.1f);
        BodyCapsule[] into = new BodyCapsule[8];

        int count = ChainCapsules.Append(joints, 5, 0.05f, 1f, into, 1, 12);

        Assert.AreEqual(4, count);
        Assert.AreEqual(joints[0], into[1].start);
        Assert.AreEqual(joints[1], into[1].end);
        Assert.AreEqual(joints[4], into[4].end, "The chain runs diagonally, joint to joint.");
        Assert.AreEqual(0f, into[1].press, "A liana parts the grass, it does not flatten it.");
    }

    [Test]
    public void Append_LongChain_GroupsJointsToFitTheLimit()
    {
        Vector3[] joints = Line(25, 0.1f);
        BodyCapsule[] into = new BodyCapsule[32];

        int count = ChainCapsules.Append(joints, 25, 0.05f, 1f, into, 0, 12);

        Assert.LessOrEqual(count, 12);
        Assert.AreEqual(joints[0], into[0].start);
        Assert.AreEqual(joints[24], into[count - 1].end, "The groups still reach the tip.");
    }

    [Test]
    public void Append_HighChainOrNoRoom_WritesNothing()
    {
        Assert.AreEqual(0, ChainCapsules.Append(Line(5, 3f), 5, 0.05f, 1f, new BodyCapsule[8], 0, 12));
        Assert.AreEqual(0, ChainCapsules.Append(Line(5, 0f), 5, 0.05f, 1f, new BodyCapsule[2], 2, 12));
        Assert.AreEqual(0, ChainCapsules.Append(Line(1, 0f), 1, 0.05f, 1f, new BodyCapsule[8], 0, 12));
        Assert.AreEqual(0, ChainCapsules.Append(null, 5, 0.05f, 1f, new BodyCapsule[8], 0, 12));
    }
}

}
