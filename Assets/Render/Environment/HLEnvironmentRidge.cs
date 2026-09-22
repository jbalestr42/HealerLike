using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    /// <summary>Static, bounded silhouettes in a stage-calibrated last fog band. No per-frame work.</summary>
    public sealed class HLEnvironmentRidge : MonoBehaviour
    {
        Transform root;
        public Transform Root => root;
        public const int MaximumCount = 48;
        /// <summary>Place root silhouettes at the middle of the last non-opaque radial fog band.
        /// Rebuild after camera framing or fog calibration changes, never every frame.</summary>
        public void BuildInFogBand(Camera camera, float surfaceY, float fogStart, float fogEnd, int bands, Material material, int seed = 1707, int count = 24)
        {
            if (!camera || !(fogEnd > fogStart) || bands < 1) { Clear(); return; }
            float distance = fogEnd - (fogEnd - fogStart) / bands * .5f;
            float altitude = camera.transform.position.y - surfaceY;
            if (distance <= Mathf.Abs(altitude)) { Clear(); return; }
            Vector3 centre = camera.transform.position; centre.y = surfaceY;
            Build(centre, Mathf.Sqrt(distance * distance - altitude * altitude), material, seed, count);
        }
        public void Build(Vector3 centre, float radius, Material material, int seed = 1707, int count = 24)
        {
            Clear();
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0) return;
            HLPrimitiveMeshes.Retain();
            root = new GameObject("HLFarRidge").transform; root.SetParent(transform, false); root.position = centre;
            var random = new HLStoneRandom((uint)seed);
            Color colour = new Color32(114, 145, 155, 255);
            count = Mathf.Clamp(count, 0, MaximumCount);
            for (int i = 0; i < count; i++)
            {
                float angle = (i + random.Range(-.2f, .2f)) * Mathf.PI * 2 / count;
                var pivot = new GameObject("HLSilhouette").transform; pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                float height = random.Range(3.5f, 7);
                bool mushroom = i % 3 != 0;
                var stem = HLPrimitiveMeshes.Geometry("HLStem", pivot, mushroom ? HLPrimitive.Capsule : HLPrimitive.Cone, material, colour.linear);
                stem.localScale = new Vector3(mushroom ? .3f : 1.2f, height, mushroom ? .3f : .9f);
                stem.localPosition = Vector3.up * height * .5f;
                stem.localRotation = Quaternion.Euler(0, random.Range(0, 360), random.Range(-7, 7));
                if (mushroom)
                {
                    var cap = HLPrimitiveMeshes.Geometry("HLCap", pivot, HLPrimitive.Sphere, material, colour.linear);
                    cap.localPosition = Vector3.up * height; cap.localScale = new Vector3(2.5f, .35f, 2.5f);
                }
            }
        }
        public void Clear()
        {
            if (!root) return;
            root.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(root.gameObject); else DestroyImmediate(root.gameObject);
            root = null; HLPrimitiveMeshes.Release();
        }
        void OnDestroy() => Clear();
    }
}
