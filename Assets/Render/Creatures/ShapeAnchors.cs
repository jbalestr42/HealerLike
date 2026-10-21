using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public static class ShapeAnchors
    {
        // A curved profile's pole need not be on the box's Y axis. Use the same generated vertices as the mesh,
        // before flat-face splitting, so an attachment follows edits without creating a temporary Unity object.
        public static bool TryGet(ShapeProfile shape, ShapeAnchor anchor, int variant, out Vector3 point)
        {
            point = Vector3.zero;
            if (anchor < ShapeAnchor.Center || anchor > ShapeAnchor.Top || !shape.IsValid())
            {
                return false;
            }

            if (anchor == ShapeAnchor.Center)
            {
                return true;
            }

            if (!shape.isProcedural)
            {
                point = Vector3.up * (anchor == ShapeAnchor.Top ? 0.5f : -0.5f);
                return true;
            }

            ShapeGeometry.Build(
                shape,
                variant,
                out List<Vector3> vertices,
                out List<int> indices,
                out List<int> facets
            );
            bool top = anchor == ShapeAnchor.Top;
            // A blade's attachment is its apex, even when clipping leaves a tiny slanted crown.
            if (facets != null && !(top && shape.kind == ShapeKind.Shard))
            {
                point = FacetAnchor(vertices, indices, facets, top);
                return true;
            }

            point = ExtremeAnchor(vertices, top);
            return true;
        }

        static Vector3 ExtremeAnchor(List<Vector3> vertices, bool top)
        {
            float extreme = top ? float.MinValue : float.MaxValue;
            foreach (Vector3 point in vertices)
            {
                extreme = top ? Mathf.Max(extreme, point.y) : Mathf.Min(extreme, point.y);
            }

            Vector3 total = Vector3.zero;
            int count = 0;
            foreach (Vector3 point in vertices)
            {
                if (Mathf.Abs(point.y - extreme) <= 0.000001f)
                {
                    total += point;
                    count++;
                }
            }

            return total / count;
        }

        // Broad crowns attach through their cap centre. A fully clipped cap leaves an apex, not a side socket.
        static Vector3 FacetAnchor(List<Vector3> vertices, List<int> indices, List<int> facets, bool top)
        {
            int selected = top ? 5 : 4;
            Vector3 centre = Vector3.zero;
            float total = 0f;
            for (int triangle = 0; triangle < facets.Count; triangle++)
            {
                if (facets[triangle] != selected)
                {
                    continue;
                }

                Vector3 a = vertices[indices[triangle * 3]];
                Vector3 b = vertices[indices[triangle * 3 + 1]];
                Vector3 c = vertices[indices[triangle * 3 + 2]];
                float area = Vector3.Cross(b - a, c - a).magnitude;
                centre += (a + b + c) * (area / 3f);
                total += area;
            }

            return total > 0f ? centre / total : ExtremeAnchor(vertices, top);
        }
    }
}
