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
        // Grass tuft, an open pyramid with its base on the ground and apex at one, and the flat socle under it
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

        // The span of a baked mesh at unit scale and the height of its middle over its pivot, for the meshes that are
        // not a unit box around their pivot: the boulder spans two units across and 1.7 from -0.7 to 1, and a stone
        // variant falls back to it; the pyramid stands on its base, apex one unit up
        public static void GetSpan(Primitive primitive, out Vector3 span, out float middle)
        {
            switch (primitive)
            {
                case Primitive.Boulder:
                case Primitive.Stone:
                    span = new Vector3(2f, 1.7f, 2f);
                    middle = 0.15f;
                    return;
                case Primitive.Pyramid:
                    span = Vector3.one;
                    middle = 0.5f;
                    return;
                default:
                    span = Vector3.one;
                    middle = 0f;
                    return;
            }
        }

        // The scale and the pivot that fit a part's mesh to the box it is authored as, centre and size in any unit
        public static void Fit(Primitive primitive, Vector3 centre, Vector3 size, Quaternion rotation,
            out Vector3 dimensions, out Vector3 pivot)
        {
            GetSpan(primitive, out Vector3 span, out float middle);
            dimensions = new Vector3(size.x / span.x, size.y / span.y, size.z / span.z);
            pivot = centre - rotation * (Vector3.up * (middle * dimensions.y));
        }

        // Generated shapes already occupy a centered unit box, including mineral blocks.
        public static void Fit(Primitive primitive, ShapeProfile shape, Vector3 centre, Vector3 size,
            Quaternion rotation, out Vector3 dimensions, out Vector3 pivot)
        {
            if (shape.isProcedural)
            {
                dimensions = size;
                pivot = centre;
                return;
            }
            Fit(primitive, centre, size, rotation, out dimensions, out pivot);
        }

        public static Transform Geometry(string name, Transform parent, Mesh mesh, Material material, Color colour,
            float glow = 0f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Geometry(go, mesh, material, colour, glow, new MaterialPropertyBlock());
            return go.transform;
        }

        // Draws the mesh on an object that already exists; the block may be shared, the renderer copies it
        public static MeshRenderer Geometry(GameObject go, Mesh mesh, Material material, Color colour, float glow,
            MaterialPropertyBlock block)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            block.SetColor(RenderObjects.BaseColorId, Brighten(colour, glow));
            renderer.SetPropertyBlock(block);
            return renderer;
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
