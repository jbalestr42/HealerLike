using System.Collections.Generic;
using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    // The one owner of stone debris, dust and thrown shards, and of the stone meshes shared by every stone
    public class HLStoneEffects : MonoBehaviour
    {
        class Fragment
        {
            public GameObject gameObject;
            public MeshFilter filter;
            public MeshRenderer renderer;
            public Vector3 start;
            public Vector3 velocity;
            public Vector3 spin;
            public Quaternion rotation;
            public float age;
            public float life;
            public float ground;
            public bool bounce;
            public bool split;
            public bool isDust;
            public Vector3 scale;
            public MaterialPropertyBlock block = new MaterialPropertyBlock();
            public uint seed;
            public Mesh ownedMesh;
        }

        public static readonly int MaxLiveFragments = 256;
        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");

        [HideInInspector] public UnityEvent<Vector3> OnImpactRecorded = new UnityEvent<Vector3>();

        [FormerlySerializedAs("stoneMaterial")]
        [SerializeField] Material _stoneMaterial;
        [SerializeField] Material _coralMaterial;
        [SerializeField] Material _dustMaterial;
        [SerializeField] HLPrimitiveMeshes _meshes;
        [SerializeField] GameObject _fragmentPrefab;

        readonly List<Fragment> _active = new List<Fragment>(MaxLiveFragments);
        readonly Stack<Fragment> _pool = new Stack<Fragment>(MaxLiveFragments);
        readonly Dictionary<Transform, Fragment> _shards = new Dictionary<Transform, Fragment>();
        readonly List<HLStoneAssembly.Part> _surviving = new List<HLStoneAssembly.Part>(8);
        StoneMeshCache.Lease _dustLease;

        readonly StoneMeshCache _stoneMeshes = new StoneMeshCache();
        public StoneMeshCache stoneMeshes { get { return _stoneMeshes; } }

        public Material stoneMaterial { get { return _stoneMaterial; } }

        public int liveCount { get { return _active.Count; } }

        public void RecordImpact(Vector3 position, uint seed)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            EmitDust(position, seed);
            OnImpactRecorded.Invoke(position);
        }

        bool HasAssets()
        {
            if (_fragmentPrefab == null || _meshes == null || _stoneMaterial == null || _coralMaterial == null
                || _dustMaterial == null)
            {
                Debug.LogError("[HLStoneEffects] Wire the fragment prefab, meshes and materials on the effects prefab.");
                return false;
            }

            if (_dustLease == null)
            {
                _dustLease = _stoneMeshes.Acquire(123, HLStonePresets.Shape(1f, 1f, 1f, 0f, 1));
            }
            return true;
        }

        Fragment Take()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            GameObject fragmentGo = Instantiate(_fragmentPrefab, transform, false);
            Fragment fragment = new Fragment();
            fragment.gameObject = fragmentGo;
            fragment.filter = fragmentGo.GetComponent<MeshFilter>();
            fragment.renderer = fragmentGo.GetComponent<MeshRenderer>();
            return fragment;
        }

        void Return(Fragment fragment)
        {
            // Scene teardown can destroy the children before the owner receives OnDestroy
            if (fragment.gameObject == null)
            {
                return;
            }

            fragment.gameObject.SetActive(false);
            fragment.filter.sharedMesh = null;
            _pool.Push(fragment);
        }

        Fragment Spawn(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale,
            Vector3 velocity, float life, float ground, bool bounce, uint seed, Mesh owned = null, bool split = false)
        {
            while (_active.Count >= MaxLiveFragments)
            {
                Release(_active[0]);
            }

            Fragment fragment = Take();

            fragment.filter.sharedMesh = mesh;
            fragment.renderer.sharedMaterial = material;
            fragment.gameObject.transform.SetPositionAndRotation(position, rotation);
            fragment.gameObject.transform.localScale = scale;
            fragment.gameObject.SetActive(true);
            fragment.start = position;
            fragment.rotation = rotation;
            fragment.velocity = velocity;
            fragment.spin = new Vector3(70f, 120f, 45f);
            fragment.age = 0f;
            fragment.life = life;
            fragment.ground = ground;
            fragment.bounce = bounce;
            fragment.seed = seed;
            fragment.ownedMesh = owned;
            fragment.split = split;
            fragment.isDust = false;
            fragment.scale = scale;
            fragment.renderer.SetPropertyBlock(null);
            _active.Add(fragment);
            return fragment;
        }

        void Release(Fragment fragment)
        {
            _active.Remove(fragment);
            if (fragment.ownedMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(fragment.ownedMesh);
                }
                else
                {
                    DestroyImmediate(fragment.ownedMesh);
                }
                fragment.ownedMesh = null;
            }
            Return(fragment);
        }

        // A shard that follows a thrown projectile; it is not simulated and goes back with ReturnShard
        public Transform TakeShard(Mesh mesh, Color colour)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return null;
            }

            Fragment fragment = Take();
            fragment.filter.sharedMesh = mesh;
            fragment.renderer.sharedMaterial = _stoneMaterial;
            fragment.block.Clear();
            fragment.block.SetVector(baseColorId, colour.linear);
            fragment.renderer.SetPropertyBlock(fragment.block);
            fragment.gameObject.transform.localScale = Vector3.one;
            fragment.gameObject.SetActive(true);
            _shards.Add(fragment.gameObject.transform, fragment);
            return fragment.gameObject.transform;
        }

        public void ReturnShard(Transform shard)
        {
            if (shard == null || !_shards.TryGetValue(shard, out Fragment fragment))
            {
                return;
            }

            _shards.Remove(shard);
            Return(fragment);
        }

        static Vector3 Direction(ref HLStoneRandom random, Vector3 normal)
        {
            Vector3 direction = new Vector3(random.Range(-1f, 1f), random.Range(0.2f, 1f), random.Range(-1f, 1f));
            if (Vector3.Dot(direction, normal) < 0f)
            {
                direction -= 2f * Vector3.Dot(direction, normal) * normal;
            }
            Vector3 upwardTangent = Vector3.up - normal * Vector3.Dot(Vector3.up, normal);
            return (direction + normal * 0.5f + upwardTangent * 0.3f).normalized;
        }

        public void EmitDust(Vector3 position, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            HLStoneRandom random = new HLStoneRandom(seed);
            for (int i = 0; i < 5; i++)
            {
                Vector3 scale = Vector3.one * random.Range(0.09f, 0.17f);
                float x = random.Range(-0.24f, 0.24f);
                float y = random.Range(0.25f, 0.5f);
                float z = random.Range(-0.24f, 0.24f);
                Vector3 velocity = new Vector3(x, y, z);
                Fragment fragment = Spawn(_dustLease.mesh, _dustMaterial, position, Quaternion.identity, scale, velocity,
                    0.65f, position.y, false, random.Next());
                fragment.isDust = true;
                fragment.spin = Vector3.zero;
            }
        }

        public void EmitTrickle(Vector3 position, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            HLStoneRandom random = new HLStoneRandom(seed);
            Quaternion rotation = Quaternion.Euler(15f, seed % 360, 30f);
            Vector3 scale = Vector3.one * random.Range(0.025f, 0.045f);
            Vector3 velocity = new Vector3(random.Range(-0.25f, 0.25f), 0.08f, random.Range(-0.25f, 0.25f));
            Spawn(_meshes.pyramid, _stoneMaterial, position, rotation, scale, velocity, 0.7f, position.y - 0.5f, true,
                seed);
        }

        public void EmitHit(HLStoneImpact impact, bool critical, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            HLStoneRandom random = new HLStoneRandom(seed);
            int sparks = critical ? 9 : 6;
            int shards = critical ? 5 : 3;
            Vector3 normal = impact.normalWS.sqrMagnitude > 0f ? impact.normalWS.normalized : Vector3.up;
            for (int i = 0; i < sparks + shards; i++)
            {
                bool isSpark = i < sparks;
                float size = random.Range(0.025f, 0.07f);
                Material material = isSpark || i % 3 == 0 ? _coralMaterial : _stoneMaterial;
                Vector3 scale = isSpark ? new Vector3(size * 0.25f, size, size * 0.25f) : Vector3.one * size;
                // Keep the random draws in order: direction, speed, then life.
                Vector3 velocity = Direction(ref random, normal) * random.Range(0.6f, 1.4f);
                float life = isSpark ? random.Range(0.12f, 0.22f) : random.Range(0.35f, 0.55f);
                Vector3 position = impact.pointWS + normal * 0.005f;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
                Spawn(_meshes.pyramid, material, position, rotation, scale, velocity, life, impact.pointWS.y - 1f, false,
                    random.Next());
            }
        }

        public void EmitThrownContact(Vector3 contact, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            HLStoneRandom random = new HLStoneRandom(seed);
            int count = 3 + (int)(random.Next() % 3);
            for (int i = 0; i < count; i++)
            {
                Quaternion rotation = Quaternion.Euler(random.Range(0f, 180f), random.Range(0f, 360f), 0f);
                Vector3 scale = Vector3.one * random.Range(0.055f, 0.11f);
                Vector3 velocity = Direction(ref random, Vector3.up) * random.Range(0.6f, 1.3f);
                Spawn(_meshes.pyramid, _stoneMaterial, contact, rotation, scale, velocity, 0.45f, contact.y, false,
                    random.Next());
            }

            // Five coral rays share a center: one star silhouette at the resolved contact.
            for (int i = 0; i < 5; i++)
            {
                Vector3 ray = Quaternion.AngleAxis(i * 72f, Vector3.forward) * Vector3.up;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, ray);
                Fragment star = Spawn(_meshes.pyramid, _coralMaterial, contact, rotation, new Vector3(0.055f, 0.2f, 0.035f),
                    Vector3.up * 0.15f, 0.18f, contact.y, false, random.Next());
                star.spin = Vector3.zero;
            }
        }

        public void EmitDetachedPart(Mesh mesh, Material material, Matrix4x4 pose, Vector3 velocityWS, float groundY,
            uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            HLStoneRandom random = new HLStoneRandom(seed);
            // A copy belongs to the effects owner, so releasing the enemy's cache lease cannot invalidate it.
            Mesh copy = Instantiate(mesh);
            copy.name = "HLDetachedStone";
            Vector3 direction = new Vector3(random.Range(-1f, 1f), 0f, random.Range(-1f, 1f)).normalized;
            Material partMaterial = material != null ? material : _stoneMaterial;
            Vector3 velocity = velocityWS + direction * random.Range(0.6f, 1.2f) + Vector3.up * 0.2f;
            Spawn(copy, partMaterial, pose.GetColumn(3), pose.rotation, pose.lossyScale, velocity, 0.25f, groundY,
                false, seed, copy, true);
        }

        public void CollapseOnce(HLStoneEnemyVisual visual, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }
            if (visual == null || !visual.TryBeginCollapse())
            {
                return;
            }

            HLStoneRandom random = new HLStoneRandom(seed);
            _surviving.Clear();
            for (int i = 0; i < visual.parts.Count; i++)
            {
                if (visual.parts[i].transform.gameObject.activeSelf)
                {
                    _surviving.Add(visual.parts[i]);
                }
            }

            int count = _surviving.Count == 0 ? 0 : 12;
            for (int i = 0; i < count; i++)
            {
                HLStoneAssembly.Part part = _surviving[i % _surviving.Count];
                float angle = i * Mathf.PI * 2f / count;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * random.Range(1.2f, 1.8f);
                Vector3[] vertices = part.lease.data.vertices;
                int vertex = (int)(random.Next() % (uint)vertices.Length);
                Vector3 position = part.transform.TransformPoint(vertices[vertex]);
                Material material = i % 4 == 0 ? _coralMaterial : _stoneMaterial;
                Vector3 scale = Vector3.one * random.Range(0.06f, 0.16f);
                Vector3 velocity = visual.planarVelocity + outward + Vector3.up * random.Range(0.7f, 1.5f);
                Spawn(_meshes.pyramid, material, position, part.transform.rotation, scale, velocity, 0.8f, visual.groundY,
                    true, random.Next());
            }
            EmitDust(visual.transform.position + Vector3.up * 0.15f, seed);
            visual.HideParts();
        }

        public static Vector3 PositionAt(Vector3 start, Vector3 velocity, float age, float ground, bool bounce)
        {
            float gravity = 8f;
            Vector3 position = start + velocity * age + Vector3.down * (0.5f * gravity * age * age);
            if (!bounce)
            {
                return position;
            }

            float height = Mathf.Max(0f, start.y - ground);
            float hit = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2f * gravity * height)) / gravity;
            if (age >= hit)
            {
                float t = age - hit;
                float up = (gravity * hit - velocity.y) * 0.3f;
                position.y = Mathf.Max(ground, ground + up * t - 0.5f * gravity * t * t);
            }
            return position;
        }

        public void Advance(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Fragment fragment = _active[i];
                fragment.age += Mathf.Max(0f, deltaTime);
                Transform fragmentTransform = fragment.gameObject.transform;
                if (fragment.isDust)
                {
                    fragmentTransform.position = fragment.start + fragment.velocity * fragment.age;
                    float fade = Mathf.Clamp01(1f - fragment.age / fragment.life);
                    fragmentTransform.localScale = fragment.scale * (1f + fragment.age * 0.8f);
                    fragment.block.SetColor(baseColorId, new Color(0.48f, 0.49f, 0.51f, fade * 0.55f));
                    fragment.renderer.SetPropertyBlock(fragment.block);
                }
                else
                {
                    fragmentTransform.position = PositionAt(fragment.start, fragment.velocity, fragment.age,
                        fragment.ground, fragment.bounce);
                }
                fragmentTransform.rotation = fragment.rotation * Quaternion.Euler(fragment.spin * fragment.age);
                if (fragment.age < fragment.life)
                {
                    continue;
                }

                bool split = fragment.split;
                Vector3 position = fragmentTransform.position;
                float ground = fragment.ground;
                uint seed = fragment.seed;
                Release(fragment);
                if (split)
                {
                    SpawnSplit(position, ground, seed);
                }
            }
        }

        void SpawnSplit(Vector3 position, float ground, uint seed)
        {
            HLStoneRandom random = new HLStoneRandom(seed);
            for (int j = 0; j < 3; j++)
            {
                Vector3 scale = Vector3.one * random.Range(0.04f, 0.07f);
                Vector3 velocity = new Vector3(random.Range(-0.3f, 0.3f), 0.3f, random.Range(-0.3f, 0.3f));
                Spawn(_meshes.pyramid, _stoneMaterial, position, Quaternion.identity, scale, velocity, 0.25f, ground, true,
                    random.Next());
            }
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        void OnDisable()
        {
            while (_active.Count > 0)
            {
                Release(_active[_active.Count - 1]);
            }
            foreach (Fragment fragment in _pool)
            {
                if (fragment.gameObject != null)
                {
                    fragment.gameObject.SetActive(false);
                }
            }
        }

        void OnDestroy()
        {
            OnDisable();
            _shards.Clear();
            _pool.Clear();
            if (_dustLease != null)
            {
                _dustLease.Dispose();
                _dustLease = null;
            }
            _stoneMeshes.Clear();
        }
    }
}
