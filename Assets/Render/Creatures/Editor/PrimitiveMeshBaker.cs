using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grass;

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
            meshes.thinTorus = Save(RingMeshes.CreateThinTorus());
            meshes.bladeCone = Save(RevolvedMeshes.Create("BladeCone", Primitive.Cone, GrassField.BladeSides, 2, 0.2f));
            meshes.pyramid = Save(FacetedMeshes.CreatePyramid());
            meshes.star = Save(FacetedMeshes.CreateStar());
            meshes.boulder = Save(FacetedMeshes.CreateBoulder());
            meshes.disc = Save(RingMeshes.CreateDisc(32));
            meshes.annulus = Save(RingMeshes.CreateAnnulus(128));

            EditorUtility.SetDirty(meshes);
            AssetDatabase.SaveAssets();
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
            Object.DestroyImmediate(mesh);
            return existing;
        }

        // Mesh from raw vertices and triangles, normals recalculated
        public static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
