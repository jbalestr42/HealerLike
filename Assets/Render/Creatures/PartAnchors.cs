using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Where effects sit on a built unit: its body, neck, head and foot, read from the recipe's roles and the live
    // renderers
    public static class PartAnchors
    {
        public static bool TryMeasure(
            CreatureRecipe recipe,
            Transform root,
            Transform firstPivot,
            IReadOnlyList<Renderer> renderers,
            float cellSize,
            out EffectAnchors anchors
        )
        {
            return TryMeasure(
                recipe.parts,
                recipe.neckLocal,
                recipe.sourceLocal,
                root,
                firstPivot,
                renderers,
                cellSize,
                out anchors
            );
        }

        public static bool TryMeasure(
            CreaturePart[] parts,
            Vector3 neckLocal,
            Vector3[] sourceLocal,
            Transform root,
            Transform firstPivot,
            IReadOnlyList<Renderer> renderers,
            float cellSize,
            out EffectAnchors anchors
        )
        {
            anchors = new EffectAnchors();
            if (!root || renderers == null || renderers.Count == 0)
            {
                return false;
            }

            int body = -1;
            int head = -1;
            for (int i = 0; i < renderers.Count; i++)
            {
                PartRole role = parts[i].role;
                if (role == PartRole.Body && body < 0)
                {
                    body = i;
                }

                bool isHead = role == PartRole.Head || role == PartRole.Tip;
                if (isHead && (head < 0 || renderers[i].transform.position.y > renderers[head].transform.position.y))
                {
                    head = i;
                }
            }

            Vector3 neck = neckLocal;
            if (neck == Vector3.zero && sourceLocal.Length > 0)
            {
                foreach (Vector3 source in sourceLocal)
                {
                    neck += source;
                }

                neck /= sourceLocal.Length;
            }

            Bounds bodyBounds = renderers[Mathf.Max(0, body)].bounds;
            anchors.foot = root.position;
            anchors.bodyCentre = bodyBounds.center;
            anchors.bodyRadius = Mathf.Max(bodyBounds.extents.x, bodyBounds.extents.z);
            anchors.neck = firstPivot.TransformPoint((neck - parts[0].localPosition) * cellSize);
            if (head < 0)
            {
                anchors.headCentre = anchors.neck;
                anchors.headRadius = 0f;
                anchors.castPoint = anchors.neck;
                return true;
            }

            Bounds headBounds = renderers[head].bounds;
            anchors.headCentre = headBounds.center;
            anchors.headRadius = Mathf.Max(headBounds.extents.x, Mathf.Max(headBounds.extents.y, headBounds.extents.z));
            // The tip of the top head
            anchors.castPoint = headBounds.center + Vector3.up * headBounds.extents.y;
            return true;
        }
    }
}
