using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Owns only its generated part objects and mesh leases, never gameplay anchors.
    public class StoneAssembly : IDisposable
    {
        public class Part
        {
            public Color baseColor;
            public Transform transform;
            public MeshRenderer renderer;
            public StoneMeshCache.Lease lease;
        }

        public static readonly Color[] Palette =
        {
            new Color32(201, 196, 180, 255),
            new Color32(142, 147, 161, 255),
            new Color32(100, 121, 150, 255),
            new Color32(199, 154, 75, 255)
        };

        public readonly List<Part> parts = new List<Part>();
        StoneMeshCache _meshes;
        MaterialPropertyBlock _fractureBlock;

        public Bounds localBounds { get; private set; }

        public void Init(StoneMeshCache meshes)
        {
            Dispose();
            _meshes = meshes;
        }

        public bool Add(Transform parent, uint seed, StonePart recipe, Material material)
        {
            if (_meshes == null)
            {
                Debug.LogError("[StoneAssembly] Init the assembly with a StoneMeshCache before adding parts.");
                return false;
            }

            uint partSeed = StoneSeed.ForPart(seed, recipe.seedSalt);
            StoneMeshCache.Lease lease = _meshes.Acquire(partSeed, recipe.shape);
            if (lease == null)
            {
                return false;
            }

            GameObject partGo = new GameObject("StonePart");
            partGo.layer = parent.gameObject.layer;
            partGo.transform.SetParent(parent, false);
            partGo.transform.localPosition = recipe.localPosition;
            partGo.transform.localRotation = Quaternion.Euler(recipe.localEulerAngles);
            partGo.AddComponent<MeshFilter>().sharedMesh = lease.mesh;
            MeshRenderer renderer = partGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // Palette and fracture colors are already linear. SetColor would convert them a second time.
            Color baseColor = Palette[Mathf.Clamp(recipe.paletteIndex, 0, 3)].linear;
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetVector("_BaseColor", baseColor);
            renderer.SetPropertyBlock(properties);
            Part part = new Part();
            part.baseColor = baseColor;
            part.transform = partGo.transform;
            part.renderer = renderer;
            part.lease = lease;
            parts.Add(part);
            return true;
        }

        public void BuildEnemy(Transform parent, uint seed, StonePreset preset, Material material,
            StoneAssemblyProfile profile = null)
        {
            Dispose();
            if (profile != null)
            {
                foreach (StonePart recipe in profile.parts)
                {
                    if (!Add(parent, seed, recipe, material))
                    {
                        return;
                    }
                }
            }
            else if (preset == StonePreset.Monolith)
            {
                StonePart recipe = new StonePart();
                recipe.shape = StonePresets.Monolith;
                recipe.seedSalt = 1;
                recipe.paletteIndex = 2;
                if (!Add(parent, seed, recipe, material))
                {
                    return;
                }
            }
            else if (!BuildCluster(parent, seed, preset, material))
            {
                return;
            }

            float height = 0.8f;
            if (preset == StonePreset.Cairn)
            {
                height = 1.05f;
            }
            else if (preset == StonePreset.Monolith)
            {
                height = 1.45f;
            }
            Fit(0.9f, height);
        }

        bool BuildCluster(Transform parent, uint seed, StonePreset preset, Material material)
        {
            float[] cairnSizes = { 0.55f, 0.42f, 0.29f };
            float[] cairnElongations = { 0.65f, 0.75f, 0.95f };
            float[] boulderSizes = { 0.58f, 0.33f, 0.23f };
            float[] boulderX = { -0.08f, 0.23f, -0.22f };
            float[] boulderZ = { 0f, 0.06f, -0.16f };
            bool isCairn = preset == StonePreset.Cairn;
            float top = 0f;
            for (int i = 0; i < 3; i++)
            {
                StonePart recipe = new StonePart();
                if (isCairn)
                {
                    recipe.shape = StonePresets.Shape(cairnSizes[i], cairnElongations[i]);
                    // The top stone takes the ochre accent.
                    recipe.paletteIndex = i == 2 ? 3 : 2 - i;
                }
                else
                {
                    recipe.shape = StonePresets.Shape(boulderSizes[i], i == 0 ? 0.85f : 1.15f, 0.9f, 0.14f);
                    recipe.paletteIndex = i;
                }
                recipe.seedSalt = (uint)i + 1;
                recipe.localEulerAngles = new Vector3(0f, StoneSeed.ForPart(seed, (uint)i + 31) % 360, 0f);
                if (!Add(parent, seed, recipe, material))
                {
                    return false;
                }

                Part part = parts[i];
                Bounds bounds = PartBounds(part);
                if (isCairn)
                {
                    part.transform.localPosition = new Vector3(0f, top - bounds.min.y, 0f);
                    top += bounds.size.y * 0.92f;
                }
                else
                {
                    float lift = i == 0 ? 0f : 0.22f;
                    part.transform.localPosition = new Vector3(boulderX[i], -bounds.min.y + lift, boulderZ[i]);
                }
            }
            return true;
        }

        public void Fit(float width, float height)
        {
            RecalculateBounds();
            float xz = Mathf.Min(1f, width / Mathf.Max(localBounds.size.x, localBounds.size.z));
            float y = height / localBounds.size.y;
            Vector3 scale = new Vector3(xz, y, xz);
            Vector3 offset = new Vector3(localBounds.center.x, localBounds.min.y, localBounds.center.z);
            foreach (Part part in parts)
            {
                part.transform.localPosition = Vector3.Scale(part.transform.localPosition - offset, scale);
                part.transform.localScale = Vector3.Scale(part.transform.localScale, scale);
            }
            RecalculateBounds();
        }

        public void ApplyFracture(float healthFraction, uint seed, Color? statusTint = null)
        {
            float damage = 1f - Mathf.Clamp01(float.IsFinite(healthFraction) ? healthFraction : 1f);
            if (_fractureBlock == null)
            {
                _fractureBlock = new MaterialPropertyBlock();
            }

            Color crackColor = ((Color)new Color32(66, 89, 138, 255)).linear;
            for (int i = 0; i < parts.Count; i++)
            {
                // A seeded connected run across the cluster, leaving one boulder uncracked.
                int order = (i + (int)(seed % (uint)Mathf.Max(1, parts.Count))) % parts.Count;
                float weight;
                if (parts.Count == 1)
                {
                    weight = 1f;
                }
                else if (order == parts.Count - 1)
                {
                    weight = 0f;
                }
                else
                {
                    weight = 1f - order / (float)parts.Count;
                }

                Part part = parts[i];
                part.renderer.GetPropertyBlock(_fractureBlock);
                Color healthColor = Color.Lerp(part.baseColor, crackColor, damage * weight * 0.85f);
                if (statusTint.HasValue && statusTint.Value != Color.white)
                {
                    healthColor = Color.Lerp(healthColor, statusTint.Value, 0.42f);
                }
                _fractureBlock.SetVector("_BaseColor", healthColor);
                part.renderer.SetPropertyBlock(_fractureBlock);
            }
        }

        public void RecalculateBounds()
        {
            if (parts.Count == 0)
            {
                localBounds = default;
                return;
            }

            Bounds bounds = PartBounds(parts[0]);
            for (int i = 1; i < parts.Count; i++)
            {
                bounds.Encapsulate(PartBounds(parts[i]));
            }
            localBounds = bounds;
        }

        static Bounds PartBounds(Part part)
        {
            Transform partTransform = part.transform;
            Matrix4x4 matrix = Matrix4x4.TRS(partTransform.localPosition, partTransform.localRotation,
                partTransform.localScale);
            Vector3[] vertices = part.lease.data.vertices;
            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            foreach (Vector3 vertex in vertices)
            {
                bounds.Encapsulate(matrix.MultiplyPoint3x4(vertex));
            }
            return bounds;
        }

        public void Dispose()
        {
            foreach (Part part in parts)
            {
                if (part.transform != null)
                {
                    part.transform.gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(part.transform.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(part.transform.gameObject);
                    }
                }
                part.lease.Dispose();
            }
            parts.Clear();
        }
    }
}
