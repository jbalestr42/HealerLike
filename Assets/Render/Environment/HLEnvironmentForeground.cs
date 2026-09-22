using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    /// <summary>Camera-relative, cropped corner framing. Configure with the stage camera and primitive material.</summary>
    public sealed class HLEnvironmentForeground : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Material material;
        [SerializeField, Range(2, 4)] int stoneCount = 3;
        [SerializeField] int seed = 1707;
        Transform root;
        readonly Transform[] stones = new Transform[4];
        readonly Transform[] rosettes = new Transform[2];
        public Transform Root => root;
        public void Configure(Camera camera, Material sharedMaterial, int count = 3, int layoutSeed = 1707)
        { viewCamera = camera; material = sharedMaterial; stoneCount = Mathf.Clamp(count, 2, 4); seed = layoutSeed; Build(); }
        void Start() { if (!root && viewCamera) Build(); }
        public void Build()
        {
            Clear();
            if (!viewCamera) return;
            HLPrimitiveMeshes.Retain();
            root = new GameObject("HLForeground").transform; root.SetParent(transform, false);
            var random = new HLStoneRandom((uint)seed);
            Color tint = new Color32(43, 75, 143, 255);
            stoneCount = Mathf.Clamp(stoneCount, 2, 4);
            for (int i = 0; i < stoneCount; i++)
            {
                stones[i] = HLPrimitiveMeshes.Geometry("HLRoundedStone", root, HLPrimitive.Sphere, material, HLEnvironmentScatter.VaryColor(tint, (uint)seed + (uint)i).linear);
                stones[i].localScale = new Vector3(random.Range(1.2f, 1.7f), random.Range(.7f, .95f), .85f);
                stones[i].localRotation = Quaternion.Euler(0, 0, random.Range(-25, 25));
            }
            for (int side = 0; side < 2; side++)
            {
                var fan = new GameObject("HLGiantRosette").transform; fan.SetParent(root, false); rosettes[side] = fan;
                for (int i = 0; i < 9; i++)
                {
                    var blade = HLPrimitiveMeshes.Geometry("HLBlade", fan, HLPrimitive.Cone, material, tint.linear);
                    blade.localRotation = Quaternion.Euler(0, i * 137.5f, 22 + i * 6);
                    blade.localScale = new Vector3(.16f, random.Range(.9f, 1.4f), .035f);
                    blade.localPosition = blade.localRotation * Vector3.up * blade.localScale.y * .5f;
                }
            }
            Tick(Time.timeAsDouble);
        }
        void LateUpdate() => Tick(Time.timeAsDouble);
        public void Tick(double time)
        {
            if (!root || !viewCamera) return;
            float depth = Mathf.Max(viewCamera.nearClipPlane * 5, 2);
            Vector3 bottom = viewCamera.ViewportToWorldPoint(new Vector3(0, 0, depth));
            Vector3 top = viewCamera.ViewportToWorldPoint(new Vector3(0, 1, depth));
            float height = Vector3.Distance(bottom, top);
            root.SetPositionAndRotation(viewCamera.transform.position, viewCamera.transform.rotation);
            root.localScale = Vector3.one;
            float drift = Mathf.Sin((float)(time * .08 % (2 * System.Math.PI))) * .008f;
            for (int i = 0; i < stoneCount; i++)
            {
                float x = i % 2 == 0 ? .025f + i * .025f : .975f - (i - 1) * .025f;
                stones[i].position = viewCamera.ViewportToWorldPoint(new Vector3(x + drift, -.025f, depth));
                // Size is assigned absolutely so repeated sampling never compounds scale.
                float size = height * (i < 2 ? .29f : .22f);
                var random = new HLStoneRandom((uint)seed + (uint)i);
                stones[i].localScale = new Vector3(random.Range(1.2f, 1.7f), .8f, .85f) * size;
            }
            for (int side = 0; side < 2; side++)
            {
                rosettes[side].position = viewCamera.ViewportToWorldPoint(new Vector3(side == 0 ? -.02f + drift : 1.02f + drift, -.06f, depth + height * .03f));
                rosettes[side].localScale = Vector3.one * height * .23f;
                rosettes[side].localRotation = Quaternion.Euler(0, 0, side == 0 ? -25 : 25);
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
