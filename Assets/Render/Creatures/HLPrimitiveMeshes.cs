using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Shared meshes without textures, baked by PrimitiveMeshBaker. Cylinder height and sphere diameter are one.
    public class HLPrimitiveMeshes : ScriptableObject
    {
        public Mesh sphere;
        public Mesh capsule;
        public Mesh cone;
        public Mesh cylinder;
        public Mesh torus;
        public Mesh thinTorus;
        public Mesh bladeCone;
        public Mesh pyramid;
        public Mesh star;
        public Mesh boulder;
        public Mesh disc;
        public Mesh annulus;

        // Recipes only author torus parts at the baked 0.2 tube ratio
        public Mesh GetMesh(HLPrimitive primitive)
        {
            switch (primitive)
            {
                case HLPrimitive.Capsule:
                    return capsule;
                case HLPrimitive.Cone:
                    return cone;
                case HLPrimitive.Torus:
                    return torus;
                case HLPrimitive.CylinderSegment:
                    return cylinder;
                default:
                    return sphere;
            }
        }

        public static Transform Geometry(string name, Transform parent, Mesh mesh, Material material, Color colour,
            float glow = 0f)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Brighten(colour, glow));
            renderer.SetPropertyBlock(block);
            return go.transform;
        }

        public static Color Brighten(Color colour, float glow)
        {
            float brightness = 1f + Mathf.Max(0f, glow);
            return new Color(colour.r * brightness, colour.g * brightness, colour.b * brightness, colour.a);
        }

        public static void Segment(Transform segment, Vector3 a, Vector3 b, float radius)
        {
            Vector3 delta = b - a;
            Quaternion rotation = Quaternion.identity;
            if (delta.sqrMagnitude > 0.000000000001f)
            {
                rotation = Quaternion.FromToRotation(Vector3.up, delta);
            }

            segment.SetPositionAndRotation((a + b) * 0.5f, rotation);
            float parentScale = segment.parent ? segment.parent.lossyScale.x : 1f;
            segment.localScale = new Vector3(radius * 2f, delta.magnitude, radius * 2f) / parentScale;
        }
    }
}
