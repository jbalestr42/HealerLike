using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // How much larger than the board's calibration a unit's author drew its model, read from the root scale of the
    // authored model prefab and from nothing else. The rig still divides out every ancestor scale, so a body is
    // calibrated to the board whatever a transform above it does; this is the one signal that division would erase,
    // given back as a multiplier on the rig's cell size.
    public static class ModelScale
    {
        // A root this close to one, or below it, makes no statement of size: the body stays at the board's calibration
        public static readonly float Tolerance = 0.001f;

        // A guard against a typo in authored data, not a design limit
        public static readonly float MaxBody = 4f;

        // The multiplier for a unit's cell size, exactly 1 for every model whose root is not larger than one
        public static float Body(EntityData data)
        {
            if (data == null || data.model == null)
            {
                return 1f;
            }

            return Body(data.model.transform.localScale);
        }

        public static float Body(Vector3 authoredRoot)
        {
            float scale = authoredRoot.x;
            if (!float.IsFinite(scale) || scale <= 1f + Tolerance)
            {
                return 1f;
            }

            return Mathf.Min(scale, MaxBody);
        }
    }
}
