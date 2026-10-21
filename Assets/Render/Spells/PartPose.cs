using UnityEngine;

namespace HealerLike.Render.Spells
{
    // Where a shape part stands at one point of its element's motion, in the element's own space
    public struct PartPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
}
