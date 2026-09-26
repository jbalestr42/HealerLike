using UnityEngine;

namespace HealerLike.Render.Grass
{
    // One piece of a body on the ground, in world space: the segment between two points, swollen by a radius. A
    // sphere has both ends at its centre. A solid piece presses the grass flat under it; a brushing one, like a
    // liana sweeping low, only parts the grass beside it.
    public struct BodyCapsule
    {
        public Vector3 start;
        public Vector3 end;
        public float radius;
        // One flattens the grass under the capsule, zero only pushes it aside
        public float press;

        // The lowest point of the capsule's surface
        public float bottom
        {
            get { return Mathf.Min(start.y, end.y) - radius; }
        }

        // The capsule a mesh's local bounds make under a transform: its longest world axis becomes the segment,
        // the mean of the other two its radius. Returns false for an empty or non-finite box.
        public static bool TryFromBounds(Matrix4x4 localToWorld, Bounds localBounds, out BodyCapsule capsule)
        {
            capsule = new BodyCapsule();
            Vector3 centre = localToWorld.MultiplyPoint3x4(localBounds.center);
            Vector3 extents = localBounds.extents;
            Vector3 x = localToWorld.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 y = localToWorld.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 z = localToWorld.MultiplyVector(new Vector3(0f, 0f, extents.z));
            if (!RenderMath.IsFinite(centre) || !RenderMath.IsFinite(x) || !RenderMath.IsFinite(y)
                || !RenderMath.IsFinite(z))
            {
                return false;
            }

            Vector3 axis = x;
            float first = y.magnitude;
            float second = z.magnitude;
            if (y.sqrMagnitude > axis.sqrMagnitude)
            {
                axis = y;
                first = x.magnitude;
                second = z.magnitude;
            }

            if (z.sqrMagnitude > axis.sqrMagnitude)
            {
                axis = z;
                first = x.magnitude;
                second = y.magnitude;
            }

            float radius = 0.5f * (first + second);
            float length = axis.magnitude;
            if (length <= 1e-6f)
            {
                return false;
            }

            // A capsule's caps add its radius past each end, so the segment stops a radius short of the box
            Vector3 half = axis * (Mathf.Max(0f, length - radius) / length);
            capsule = new BodyCapsule { start = centre - half, end = centre + half, radius = radius, press = 1f };
            return true;
        }
    }

    // Something with a shape on the ground that the grass parts around, asked once a frame
    public interface IGroundBody
    {
        // Writes this frame's capsules from start on, as many as fit, and returns how many it wrote
        int AppendCapsules(BodyCapsule[] into, int start);
    }
}
