using UnityEngine;
using HealerLike.Render.Stones;

namespace HealerLike.Render.Creatures
{
    // Shared meshes without textures, baked by PrimitiveMeshBaker. Cylinder height and sphere diameter are one.
    public class PrimitiveMeshes : ScriptableObject
    {
        public Mesh sphere;
        public Mesh capsule;
        public Mesh cone;
        public Mesh cylinder;
        public Mesh torus;
        // Grass tuft from GrassTuft, base on the ground and apex at one, and the flat socle under it
        public Mesh tuft;
        public Mesh socle;
        public Mesh pyramid;
        public Mesh leaf;
        public Mesh boulder;
        public Mesh disc;
        public Mesh annulus;
        public StoneVariants stoneVariants;

        // A stone part takes one of the seeded variants, the boulder until any are baked
        public Mesh GetMesh(Primitive primitive, int variant)
        {
            if (primitive != Primitive.Stone)
            {
                return GetMesh(primitive);
            }

            if (stoneVariants == null || stoneVariants.meshes == null || stoneVariants.meshes.Length == 0)
            {
                return boulder;
            }

            int count = stoneVariants.meshes.Length;
            return stoneVariants.meshes[((variant % count) + count) % count];
        }

        // Recipes only author torus parts at the baked 0.2 tube ratio
        public Mesh GetMesh(Primitive primitive)
        {
            switch (primitive)
            {
                case Primitive.Capsule:
                    return capsule;
                case Primitive.Cone:
                    return cone;
                case Primitive.Torus:
                    return torus;
                case Primitive.CylinderSegment:
                    return cylinder;
                case Primitive.Leaf:
                    return leaf;
                case Primitive.Boulder:
                case Primitive.Stone:
                    return boulder;
                case Primitive.Pyramid:
                    return pyramid;
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
            block.SetColor(RenderObjects.BaseColorId, Brighten(colour, glow));
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
