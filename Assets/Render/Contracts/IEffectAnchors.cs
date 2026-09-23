using UnityEngine;

namespace HealerLike.Render
{
    // Where an effect may sit on a unit, in world space, read from the recipe it was built from
    public struct EffectAnchors
    {
        public Vector3 foot;
        public Vector3 bodyCentre;
        public float bodyRadius;
        public Vector3 neck;
        public Vector3 headCentre;
        public float headRadius;
    }

    // A view that knows its body and head, so a status can keep clear of the head
    public interface IEffectAnchors
    {
        bool TryGetAnchors(out EffectAnchors anchors);
    }
}
