using UnityEngine;

namespace HealerLike.Render.Spells
{
    // Accept complete finite anchors from a view, otherwise use the shared gameplay target point.
    public static class EffectAnchorReader
    {
        public static EffectAnchors Read(GameObject target)
        {
            IEffectAnchors source = null;
            Entity entity = target.GetComponent<Entity>();
            if (entity != null && entity.model != null)
            {
                source = entity.model.GetComponentInChildren<IEffectAnchors>();
            }

            if (source == null)
            {
                source = target.GetComponentInChildren<IEffectAnchors>();
            }

            if (source != null && source.TryGetAnchors(out EffectAnchors anchors) && IsValid(anchors))
            {
                return anchors;
            }
            return Fallback(target);
        }

        static bool IsValid(EffectAnchors anchors)
        {
            return RenderMath.IsPositive(anchors.bodyRadius) && float.IsFinite(anchors.headRadius)
                && anchors.headRadius >= 0f && RenderMath.IsFinite(anchors.bodyCentre)
                && RenderMath.IsFinite(anchors.headCentre) && RenderMath.IsFinite(anchors.foot)
                && RenderMath.IsFinite(anchors.neck) && RenderMath.IsFinite(anchors.castPoint)
                && (anchors.castSources == null || AllFinite(anchors.castSources));
        }

        static bool AllFinite(Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (!RenderMath.IsFinite(points[i])) return false;
            }
            return true;
        }

        static EffectAnchors Fallback(GameObject target)
        {
            float radius = EffectPlacement.FallbackBodyRadius;
            Vector3 foot = target.transform.position;
            Vector3 centre = foot + Vector3.up * radius;
            Transform anchor = RenderTargets.Anchor(target);
            if (anchor != target.transform)
            {
                centre = anchor.position;
            }

            EffectAnchors anchors = new EffectAnchors();
            anchors.foot = new Vector3(centre.x, Mathf.Min(foot.y, centre.y - radius), centre.z);
            anchors.bodyCentre = centre;
            anchors.bodyRadius = radius;
            anchors.neck = centre + Vector3.up * radius;
            anchors.headCentre = centre + Vector3.up * (1.5f * radius);
            anchors.headRadius = 0.5f * radius;
            anchors.castPoint = anchors.headCentre;
            anchors.castSources = new[] { anchors.castPoint };
            return anchors;
        }
    }
}
