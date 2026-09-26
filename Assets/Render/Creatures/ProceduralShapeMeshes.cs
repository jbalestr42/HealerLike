using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Closed solids in a centered unit box. Size belongs to the recipe; these parameters change the profile.
    public static class ProceduralShapeMeshes
    {
        public static Mesh Create(ShapeProfile shape, int variant = 0)
        {
            if (!shape.isProcedural || !shape.IsValid())
            {
                return null;
            }

            ShapeGeometry.Build(
                shape,
                variant,
                out List<Vector3> vertices,
                out List<int> indices,
                out List<int> facets
            );
            bool mineral =
                shape.kind == ShapeKind.Block
                || shape.kind == ShapeKind.Shard
                || (shape.kind == ShapeKind.Ring && shape.faceted);
            return ShapeMeshOutput.Create(shape, variant, vertices, indices, mineral, facets);
        }

        public static Vector3 Anchor(ShapeProfile shape, ShapeAnchor anchor, int variant = 0)
        {
            if (!ShapeAnchors.TryGet(shape, anchor, variant, out Vector3 point))
            {
                Debug.LogError("[ProceduralShapeMeshes] Invalid shape profile or attachment anchor.");
            }

            return point;
        }
    }
}
