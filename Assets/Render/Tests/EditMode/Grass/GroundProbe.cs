using System;
using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{

// What a Ground would hand the grass this frame, for tests to read
public static class GroundProbe
{
    public static List<GroundStamp> Stamps(Ground ground)
    {
        GroundStamp[] into = new GroundStamp[GroundSimulation.StampCapacity];
        int count = ground.Collect(into, 0, ReadOnlySpan<Zone>.Empty, new BodyCapsule[256], 0f, 1f);
        return new List<GroundStamp>(new ArraySegment<GroundStamp>(into, 0, count));
    }

    // The auras' state summed at a point: x ash, y vitality, z light, w blight
    public static Vector4 State(Ground ground, Vector3 at)
    {
        Vector4 sum = Vector4.zero;
        foreach (GroundStamp stamp in Stamps(ground))
        {
            sum += stamp.State(new Vector2(at.x, at.z));
        }

        return sum;
    }

    public static int Count(Ground ground, GroundStampKind kind)
    {
        int count = 0;
        foreach (GroundStamp stamp in Stamps(ground))
        {
            count += stamp.kind == kind ? 1 : 0;
        }

        return count;
    }
}

}
