using UnityEngine;

namespace HealerLike.Render.Spells
{
    // Puts an element on the socket its entry names, read from the target's anchors
    public static class EffectPlacement
    {
        // A view without anchors is read as a body of this radius around its target point
        public static readonly float FallbackBodyRadius = 0.3f;
        // Press sits a quarter of a body unit (the body's diameter) over the head, in body radii
        public static readonly float AboveHeadGap = 0.5f;

        public static EffectAnchors Anchors(GameObject target)
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
            return Fallback(target, entity);
        }

        public static Vector3 Socket(EffectAnchors anchors, EffectSocket socket)
        {
            float radius = anchors.bodyRadius;
            switch (socket)
            {
                case EffectSocket.AboveHead:
                    return anchors.headCentre + Vector3.up * (anchors.headRadius + AboveHeadGap * radius);
                case EffectSocket.UnderHead:
                    // On the right of the body, just under the head, so the drops fall clear of both
                    return new Vector3(anchors.bodyCentre.x + 1.15f * radius,
                                       anchors.headCentre.y - anchors.headRadius - 0.55f * radius,
                                       anchors.bodyCentre.z);
                case EffectSocket.Feet:
                    return anchors.foot;
                default:
                    return anchors.bodyCentre;
            }
        }

        // Lays the element at its socket in body radii, the parent keeps it on a moving unit
        public static void Place(SpellEffect effect, Transform parent, EffectAnchors anchors)
        {
            if (effect == null || effect.recipe == null)
            {
                return;
            }

            Transform root = effect.transform;
            root.SetParent(parent, false);
            root.position = Socket(anchors, effect.recipe.socket);
            root.rotation = Quaternion.identity;
            float parentScale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.x) : 1f;
            root.localScale = Vector3.one * (anchors.bodyRadius / parentScale);
            if (effect.recipe.socket == EffectSocket.UnderHead)
            {
                effect.SetFallDistance((root.position.y - anchors.foot.y) / anchors.bodyRadius);
            }

            if (effect.recipe.element == EffectElement.Bud)
            {
                FitToNeck(effect, anchors);
            }
            effect.Advance(0f);
        }

        // The bud closes up to the neck, never over the head
        static void FitToNeck(SpellEffect effect, EffectAnchors anchors)
        {
            Transform root = effect.transform;
            effect.Pose(1f, effect.recipe.cycleSeconds);
            float top = float.NegativeInfinity;
            foreach (Transform shape in effect.shapes)
            {
                if (shape.gameObject.activeSelf)
                {
                    top = Mathf.Max(top, WorldBounds(shape).max.y);
                }
            }

            float reach = top - root.position.y;
            float wanted = anchors.neck.y - root.position.y;
            if (reach > 0.0001f && wanted > 0f)
            {
                Vector3 scale = root.localScale;
                scale.y *= Mathf.Clamp(wanted / reach, 0.3f, 1.5f);
                root.localScale = scale;
            }
        }

        public static Bounds WorldBounds(Transform part)
        {
            MeshFilter filter = part.GetComponent<MeshFilter>();
            Bounds local = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
            Matrix4x4 matrix = part.localToWorldMatrix;
            Bounds world = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                world.Encapsulate(matrix.MultiplyPoint3x4(corner));
            }
            return world;
        }

        static bool IsValid(EffectAnchors anchors)
        {
            return float.IsFinite(anchors.bodyRadius) && anchors.bodyRadius > 0f && float.IsFinite(anchors.headRadius)
                   && float.IsFinite(anchors.bodyCentre.y) && float.IsFinite(anchors.headCentre.y) && float.IsFinite(anchors.foot.y);
        }

        static EffectAnchors Fallback(GameObject target, Entity entity)
        {
            float radius = FallbackBodyRadius;
            Vector3 foot = target.transform.position;
            Vector3 centre = foot + Vector3.up * radius;
            if (entity != null && entity.targetPoint != null)
            {
                centre = entity.targetPoint.transform.position;
            }

            EffectAnchors anchors = new EffectAnchors();
            anchors.foot = new Vector3(centre.x, Mathf.Min(foot.y, centre.y - radius), centre.z);
            anchors.bodyCentre = centre;
            anchors.bodyRadius = radius;
            anchors.neck = centre + Vector3.up * radius;
            anchors.headCentre = centre + Vector3.up * (1.5f * radius);
            anchors.headRadius = 0.5f * radius;
            return anchors;
        }
    }
}
