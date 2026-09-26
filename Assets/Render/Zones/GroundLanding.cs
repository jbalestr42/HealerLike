using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grass;
using HealerLike.Render.Stage;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Landing reads the accepted root layout, even while an edited recipe is waiting for a valid rebuild.
    public static class GroundLanding
    {
        public static float Footprint(Transform root, CreatureRig rig)
        {
            if (rig == null || rig.recipe == null)
            {
                return StageCalibration.CellSize * 0.5f
                    * Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z));
            }

            RootDefinition roots = rig.roots;
            float extent = roots.count > 0 ? (roots.footRadius + roots.thickness) * rig.cellSize : 0f;
            for (int i = 0; i < rig.parts.Count; i++)
            {
                CreaturePart part = rig.parts[i];
                bool isBase = part.role == PartRole.Body || (roots.count == 0 && part.role == PartRole.Limb);
                if (!isBase)
                {
                    continue;
                }
                Renderer renderer = rig.partTransforms[i].GetComponent<Renderer>();
                if (!renderer)
                {
                    continue;
                }
                Bounds bounds = renderer.bounds;
                Vector3 offset = bounds.center - root.position;
                float x = Mathf.Abs(offset.x) + bounds.extents.x;
                float z = Mathf.Abs(offset.z) + bounds.extents.z;
                extent = Mathf.Max(extent, Mathf.Sqrt(x * x + z * z));
            }
            return Mathf.Max(extent, StageCalibration.CellSize * 0.25f);
        }

        public static void Play(Ground ground, CreatureRig rig, Vector3 position, float footprint, List<Vector3> feet)
        {
            float cell = rig != null ? rig.cellSize : StageCalibration.CellSize;
            feet.Clear();
            if (rig != null && rig.root)
            {
                Feet(rig.root, rig.roots, cell, feet);
            }
            foreach (Vector3 foot in feet)
            {
                ground.Play(ground.vocabulary.footRing, foot, TrampleZone.FootRingRadius * cell);
            }
            ground.Play(ground.vocabulary.bodyRing, position, footprint * TrampleZone.BodyRingScale);
        }

        public static void Feet(Transform root, RootDefinition roots, float cellSize, List<Vector3> into)
        {
            for (int i = 0; i < roots.count; i++)
            {
                float angle = i * Mathf.PI * 2f / roots.count;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                into.Add(root.TransformPoint(radial * roots.footRadius * cellSize));
            }
        }
    }
}
