using UnityEngine;

namespace HealerLike.Render
{
    // Where an effect, a contact or a status lands on a unit
    public static class RenderTargets
    {
        // The entity's target point when it has one, else the skill target point tag, else the unit itself
        public static Transform Anchor(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Entity entity = target.GetComponent<Entity>();
            if (entity != null && entity.targetPoint != null)
            {
                return entity.targetPoint.transform;
            }

            SkillTargetPointTag tag = target.GetComponentInChildren<SkillTargetPointTag>();
            if (tag != null)
            {
                return tag.transform;
            }

            return target.transform;
        }

        public static Vector3 Point(GameObject target)
        {
            Transform anchor = Anchor(target);
            if (anchor == null)
            {
                return Vector3.zero;
            }

            return anchor.position;
        }
    }
}
