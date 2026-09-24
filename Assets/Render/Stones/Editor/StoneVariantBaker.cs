using System.Collections.Generic;
using System.IO;
using HealerLike.Render.Stage;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Bakes the seeded stones a Stone part draws: 20 flat facets, grey in submesh 0 and ochre in submesh 1
    // Each variant fills the baked boulder's box, two across and 1.7 from -0.7 to 1, so it can replace a boulder
    public static class StoneVariantBaker
    {
        public static readonly int VariantCount = 12;
        public static readonly uint VariantSeed = 356414;
        public static readonly string VariantsPath = "Assets/Render/Stones/Data/StoneVariants.asset";

        static readonly string meshesFolder = "Assets/Render/Stones/Meshes";
        static readonly Vector3 boxCentre = new Vector3(0f, 0.15f, 0f);
        static readonly Vector3 boxExtents = new Vector3(1f, 0.85f, 1f);
        // A face this turned toward the portrait camera is seen at every yaw, so it can carry the ochre
        static readonly float ochreFacing = 0.35f;

        [MenuItem("Tools/Render/Bake Stone Variants")]
        public static void Bake()
        {
            StoneVariants variants = AssetDatabase.LoadAssetAtPath<StoneVariants>(VariantsPath);
            if (variants == null)
            {
                Debug.LogError($"[StoneVariantBaker] Missing {VariantsPath}");
                return;
            }

            Directory.CreateDirectory(meshesFolder);
            Mesh[] meshes = new Mesh[VariantCount];
            for (int i = 0; i < VariantCount; i++)
            {
                Mesh mesh = CreateVariant(SeededRandom.ForPart(VariantSeed, (uint)i));
                if (mesh == null)
                {
                    return;
                }

                mesh.name = "StoneVariant" + i.ToString("00");
                meshes[i] = Save(mesh);
            }

            variants.meshes = meshes;
            EditorUtility.SetDirty(variants);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StoneVariantBaker] Baked {VariantCount} stones into {meshesFolder}");
        }

        // The direction from a stone toward the portrait camera
        public static Vector3 ViewDirection()
        {
            float pitch = StageCalibration.PortraitPitch * Mathf.Deg2Rad;
            return new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch));
        }

        public static Mesh CreateVariant(uint seed)
        {
            SeededRandom random = new SeededRandom(SeededRandom.ForPart(seed, 1));
            StoneSettings settings = StonePresets.Shape(1f, random.Range(0.8f, 1f), random.Range(0.85f, 1f),
                random.Range(0.1f, 0.16f), 0);
            StoneMeshData data;
            if (!StoneMesh.TryGenerate(seed, settings, out data))
            {
                Debug.LogError($"[StoneVariantBaker] No stone for seed {seed}.");
                return null;
            }

            // One uniform scale keeps the stone's own proportions inside the boulder's box
            Vector3 size = data.bounds.extents;
            float scale = Mathf.Min(boxExtents.x / size.x, Mathf.Min(boxExtents.y / size.y, boxExtents.z / size.z));
            Vector3 offset = boxCentre - data.bounds.center * scale;
            Vector3[] vertices = new Vector3[data.vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = data.vertices[i] * scale + offset;
            }

            // The outline pushes along the normals shared at each corner, so the edges stay closed
            Vector3[] corners = new Vector3[data.weldedVertices.Length];
            for (int i = 0; i < data.weldedIndices.Length; i += 3)
            {
                Vector3 normal = data.normals[i];
                for (int j = 0; j < 3; j++)
                {
                    corners[data.weldedIndices[i + j]] += normal;
                }
            }

            List<Vector3> outlineNormals = new List<Vector3>(vertices.Length);
            for (int i = 0; i < data.weldedIndices.Length; i++)
            {
                outlineNormals.Add(corners[data.weldedIndices[i]].normalized);
            }

            HashSet<int> ochre = PickOchreFaces(data.normals, ref random);
            List<int> grey = new List<int>();
            List<int> warm = new List<int>();
            for (int face = 0; face < data.indices.Length / 3; face++)
            {
                List<int> target = ochre.Contains(face) ? warm : grey;
                for (int j = 0; j < 3; j++)
                {
                    target.Add(data.indices[face * 3 + j]);
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = data.normals;
            mesh.SetUVs(3, outlineNormals);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(grey, 0);
            mesh.SetTriangles(warm, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Three to five of the faces turned up or toward the camera, shuffled by the seed
        static HashSet<int> PickOchreFaces(Vector3[] normals, ref SeededRandom random)
        {
            Vector3 view = ViewDirection();
            List<int> candidates = new List<int>();
            for (int face = 0; face < normals.Length / 3; face++)
            {
                if (Vector3.Dot(normals[face * 3], view) > ochreFacing)
                {
                    candidates.Add(face);
                }
            }

            int count = Mathf.Min(candidates.Count, 3 + (int)(random.Next() % 3));
            HashSet<int> picked = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                int index = i + (int)(random.Next() % (uint)(candidates.Count - i));
                int face = candidates[index];
                candidates[index] = candidates[i];
                candidates[i] = face;
                picked.Add(face);
            }

            if (picked.Count < 3)
            {
                Debug.LogError($"[StoneVariantBaker] Only {picked.Count} faces turn toward the camera.");
            }
            return picked;
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
    }
}
