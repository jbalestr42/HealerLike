using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Explicit surface anchors resolve in geometry space, including appearance and charge scale.
    public static class CreatureSources
    {
        public static bool Valid(ShapeAnchor anchor) => anchor == ShapeAnchor.Top || anchor == ShapeAnchor.Bottom;

        public static Vector3 Local(CreaturePart part)
        {
            ShapeAnchors.TryGet(part.shape, part.sourceAnchor, part.variant, out Vector3 point);
            if (!part.shape.isProcedural)
            {
                PrimitiveMeshes.GetSpan(part.primitive, out Vector3 span, out float middle);
                point = Vector3.Scale(point, span) + Vector3.up * middle;
            }
            return point;
        }

        public static ARigHost Host(GameObject owner)
        {
            Entity entity = owner ? owner.GetComponent<Entity>() : null;
            ARigHost host = entity && entity.model ? entity.model.GetComponentInChildren<ARigHost>() : null;
            return host ? host : owner ? owner.GetComponentInChildren<ARigHost>() : null;
        }

        public static bool HasExplicit(GameObject owner, bool armless = false)
        {
            ARigHost host = Host(owner);
            return host && host.rig != null && (!armless || host.rig.armCount == 0) && HasExplicit(host.rig);
        }

        public static bool HasExplicit(CreatureRig rig)
        {
            foreach (CreaturePart part in rig.parts) if (part.isSource) return true;
            return false;
        }

        // Stable authored identity plus occurrence within the primary head copies, never renderer ordering.
        public static string Select(CreatureRig rig, uint sequence)
        {
            int count = 0;
            foreach (CreaturePart part in rig.parts) if (part.isSource) count++;
            if (count == 0) return null;
            int selected = (int)(sequence % (uint)count);
            foreach (CreaturePart part in rig.parts)
                if (part.isSource && selected-- == 0) return part.sourceId;
            return null;
        }

        public static bool Resolve(CreatureRig rig, string id, out Vector3 point)
        {
            point = default;
            if (rig == null || !rig.root || !rig.root.gameObject.activeInHierarchy) return false;
            for (int i = 0; i < rig.parts.Count; i++)
            {
                CreaturePart part = rig.parts[i];
                if (!part.isSource || part.sourceId != id) continue;
                Transform geometry = rig.partTransforms[i];
                if (!geometry || !geometry.gameObject.activeInHierarchy) return false;
                point = geometry.TransformPoint(rig.SourceLocal(i));
                return RenderMath.IsFinite(point);
            }
            return false;
        }
    }
}
