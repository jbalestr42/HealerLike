using UnityEngine;
using UnityEditor;

namespace HealerLike.Render.Creatures
{
    // Writes the shared primitive meshes as assets, and the PrimitiveMeshes asset that references them
    public static class PrimitiveMeshBaker
    {
        static readonly string creaturesFolder = "Assets/Render/Creatures";
        static readonly string meshesFolder = "Assets/Render/Creatures/Meshes";
        static readonly string meshesAssetPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

        [MenuItem("Tools/Render/Bake Primitive Meshes")]
        public static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(meshesFolder))
            {
                AssetDatabase.CreateFolder(creaturesFolder, "Meshes");
            }

            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesAssetPath);
            if (meshes == null)
            {
                meshes = ScriptableObject.CreateInstance<PrimitiveMeshes>();
                AssetDatabase.CreateAsset(meshes, meshesAssetPath);
            }

            meshes.sphere = Save(RevolvedMeshes.Create("Sphere", Primitive.Sphere, 12, 6, 0.2f));
            meshes.capsule = Save(RevolvedMeshes.Create("Capsule", Primitive.Capsule, 12, 6, 0.2f));
            meshes.cone = Save(RevolvedMeshes.Create("Cone", Primitive.Cone, 12, 6, 0.2f));
            meshes.cylinder = Save(RevolvedMeshes.Create("Cylinder", Primitive.CylinderSegment, 6, 6, 0.2f));
            meshes.torus = Save(RevolvedMeshes.Create("Torus", Primitive.Torus, 12, 6, 0.2f));
            meshes.tuft = Save(FacetedMeshes.CreatePyramid("Tuft", false));
            meshes.socle = Save(FacetedMeshes.CreateSocle());
            meshes.pyramid = Save(FacetedMeshes.CreatePyramid("Pyramid", true));
            meshes.leaf = Save(FacetedMeshes.CreateLeaf());
            meshes.boulder = Save(FacetedMeshes.CreateBoulder());
            meshes.disc = Save(RingMeshes.CreateDisc(32));
            meshes.annulus = Save(RingMeshes.CreateAnnulus(128));
            EditorUtility.SetDirty(meshes);
            AssetDatabase.SaveAssetIfDirty(meshes);
            Debug.Log($"[PrimitiveMeshBaker] Baked 12 meshes into {meshesFolder}");
        }

        static Mesh Save(Mesh mesh)
        {
            string path = meshesFolder + "/" + mesh.name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        // Mesh from raw vertices and triangles, normals recalculated
        public static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
        {
            return CreateMesh(name, vertices, triangles, null, null);
        }

        // Every baked mesh is built here: the normals given, or recalculated when there are none, and the uv
        // when there are some
        public static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles, Vector3[] normals, Vector2[] uv)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            if (uv != null)
            {
                mesh.uv = uv;
            }

            mesh.triangles = triangles;
            if (normals != null)
            {
                mesh.normals = normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
