using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Separate growing segments and joints, posed from the hip through a raised knee to the foot.
    public class RootChain
    {
        // A root thins to this share of its thickness at the foot, its joints are this many radii wide
        static readonly float taper = 0.65f;
        static readonly float jointWidth = 2.8f;
        // Lighter knuckles fill the bends between segments
        static readonly float jointGlow = 0.35f;
        // The hips ring the stem this far out, the knee rises over this share of the way to the foot, in cells
        static readonly float hipSpread = 0.08f;
        static readonly float kneeReach = 0.6f;

        RootDefinition _definition;
        readonly ShapeMeshCache _shapeMeshes = new ShapeMeshCache();
        Transform[] _segments = new Transform[0];
        Transform[] _joints = new Transform[0];

        public void Clear()
        {
            foreach (Transform segment in _segments)
            {
                if (segment)
                {
                    segment.gameObject.SetActive(false);
                    RenderObjects.Release(segment.gameObject);
                }
            }
            foreach (Transform joint in _joints)
            {
                if (joint)
                {
                    joint.gameObject.SetActive(false);
                    RenderObjects.Release(joint.gameObject);
                }
            }
            _segments = new Transform[0];
            _joints = new Transform[0];
            _shapeMeshes.Dispose();
        }

        public void Init(RootDefinition definition, Transform parent, PrimitiveMeshes meshes, Material material,
            Color colour)
        {
            Clear();
            _definition = definition;
            int segments = definition.segments;
            _segments = new Transform[definition.count * segments];
            _joints = new Transform[definition.count * (segments - 1)];
            if (definition.count == 0)
            {
                return;
            }
            Mesh segmentMesh = definition.segmentShape.isProcedural
                ? _shapeMeshes.Get(definition.segmentShape) : meshes.cylinder;
            Mesh jointMesh = definition.jointShape.isProcedural
                ? _shapeMeshes.Get(definition.jointShape) : meshes.sphere;
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i] = PrimitiveMeshes.Geometry("Root", parent, segmentMesh, material, colour);
            }

            for (int i = 0; i < _joints.Length; i++)
            {
                _joints[i] = PrimitiveMeshes.Geometry("RootJoint", parent, jointMesh, material, colour,
                    jointGlow);
            }
        }

        // The hips follow the swaying body, the knees and feet stay on the ground under the root
        public void Place(Transform sway, Transform root, float cellSize)
        {
            RootDefinition roots = _definition;
            for (int i = 0; i < roots.count; i++)
            {
                float angle = i * Mathf.PI * 2f / roots.count;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 hip = sway.TransformPoint((radial * hipSpread + Vector3.up * roots.hipHeight) * cellSize);
                Vector3 kneeLocal = radial * (roots.footRadius * kneeReach) + Vector3.up * roots.kneeHeight;
                Vector3 knee = root.TransformPoint(kneeLocal * cellSize);
                Vector3 foot = root.TransformPoint(radial * roots.footRadius * cellSize);

                // A curve from hip to foot through the raised knee, cut into equal steps, thinning toward the foot
                Vector3 bend = knee * 2f - (hip + foot) * 0.5f;
                Vector3 start = hip;
                for (int k = 0; k < roots.segments; k++)
                {
                    float t = (k + 1f) / roots.segments;
                    Vector3 end = (1f - t) * (1f - t) * hip + 2f * t * (1f - t) * bend + t * t * foot;
                    float tipRatio = roots.taper > 0f ? roots.taper : taper;
                    float thinning = Mathf.Lerp(1f, tipRatio, (float)k / Mathf.Max(1, roots.segments - 1));
                    float radius = roots.thickness * thinning * cellSize;
                    PrimitiveMeshes.Segment(_segments[i * roots.segments + k], start, end, radius);
                    if (k > 0)
                    {
                        Transform joint = _joints[i * (roots.segments - 1) + k - 1];
                        joint.position = start;
                        float width = roots.jointScale > 0f ? roots.jointScale : jointWidth;
                        joint.localScale = Vector3.one * (radius * width / root.lossyScale.x);
                    }

                    start = end;
                }
            }
        }
    }
}
