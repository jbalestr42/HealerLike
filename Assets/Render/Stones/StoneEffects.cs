using System.Collections.Generic;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // The one owner of stone debris, dust and thrown shards, and of the stone meshes shared by every stone
    public class StoneEffects : MonoBehaviour
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

        // Fragments each emitter spawns
        public static readonly int DustPuffs = 5;
        public static readonly int HitSparks = 6;
        public static readonly int CriticalHitSparks = 9;
        public static readonly int HitChips = 3;
        public static readonly int CriticalHitChips = 5;
        public static readonly int CollapseDebris = 12;
        public static readonly int SplitPieces = 3;
        public static readonly int StarRays = 5;
        public static readonly int MinThrownChips = 3;

        // A thrown contact draws from MinThrownChips to MinThrownChips + thrownChipSpread - 1 chips
        static readonly uint thrownChipSpread = 3;
        static readonly float gravity = 8f;
        static readonly Vector3 fragmentSpin = new Vector3(70f, 120f, 45f);
        static readonly uint dustMeshSeed = 123;

        [SerializeField] Material _stoneMaterial;
        [SerializeField] Material _coralMaterial;
        [SerializeField] Material _dustMaterial;
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] GameObject _fragmentPrefab;

        readonly List<Fragment> _active = new List<Fragment>(MaxLiveFragments);
        readonly Stack<Fragment> _pool = new Stack<Fragment>(MaxLiveFragments);
        readonly Dictionary<Transform, Fragment> _shards = new Dictionary<Transform, Fragment>();
        readonly List<Transform> _standing = new List<Transform>(8);
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
        }

        bool HasAssets()
        {
            if (_fragmentPrefab == null || _meshes == null || _stoneMaterial == null || _coralMaterial == null
                || _dustMaterial == null)
            {
                Debug.LogError("[StoneEffects] Wire the fragment prefab, meshes and materials on the effects prefab.");
                return false;
            }

            if (_dustLease == null)
            {
                _dustLease = _stoneMeshes.Acquire(dustMeshSeed, StonePresets.Shape(1f, 1f, 1f, 0f, 1));
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
            fragment.spin = fragmentSpin;
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
                RenderObjects.Release(fragment.ownedMesh);
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
            fragment.block.SetVector(RenderObjects.BaseColorId, colour.linear);
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

        static Vector3 Direction(ref StoneRandom random, Vector3 normal)
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

            StoneRandom random = new StoneRandom(seed);
            for (int i = 0; i < DustPuffs; i++)
            {
                Vector3 scale = Vector3.one * random.Range(0.09f, 0.17f);
                float x = random.Range(-0.24f, 0.24f);
                float y = random.Range(0.25f, 0.5f);
                float z = random.Range(-0.24f, 0.24f);
                Vector3 velocity = new Vector3(x, y, z);
                Fragment fragment = Spawn(_dustLease.mesh, _dustMaterial, position, Quaternion.identity, scale,
                    velocity, 0.65f, position.y, false, random.Next());
                fragment.isDust = true;
                fragment.spin = Vector3.zero;
            }
        }

        public void EmitHit(StoneImpact impact, bool critical, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            int sparks = critical ? CriticalHitSparks : HitSparks;
            int shards = critical ? CriticalHitChips : HitChips;
            Vector3 normal = impact.normal.sqrMagnitude > 0f ? impact.normal.normalized : Vector3.up;
            for (int i = 0; i < sparks + shards; i++)
            {
                bool isSpark = i < sparks;
                float size = random.Range(0.025f, 0.07f);
                Material material = _stoneMaterial;
                if (isSpark || i % 3 == 0)
                {
                    material = _coralMaterial;
                }
                Vector3 scale = isSpark ? new Vector3(size * 0.25f, size, size * 0.25f) : Vector3.one * size;
                // Keep the random draws in order: direction, speed, then life
                Vector3 velocity = Direction(ref random, normal) * random.Range(0.6f, 1.4f);
                float life = isSpark ? random.Range(0.12f, 0.22f) : random.Range(0.35f, 0.55f);
                Vector3 position = impact.point + normal * 0.005f;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
                Spawn(_meshes.pyramid, material, position, rotation, scale, velocity, life, impact.point.y - 1f, false,
                    random.Next());
            }
        }

        public void EmitThrownContact(Vector3 contact, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            int count = MinThrownChips + (int)(random.Next() % thrownChipSpread);
            for (int i = 0; i < count; i++)
            {
                Quaternion rotation = Quaternion.Euler(random.Range(0f, 180f), random.Range(0f, 360f), 0f);
                Vector3 scale = Vector3.one * random.Range(0.055f, 0.11f);
                Vector3 velocity = Direction(ref random, Vector3.up) * random.Range(0.6f, 1.3f);
                Spawn(_meshes.pyramid, _stoneMaterial, contact, rotation, scale, velocity, 0.45f, contact.y, false,
                    random.Next());
            }

            // Five coral rays share a center: one star silhouette at the resolved contact
            for (int i = 0; i < StarRays; i++)
            {
                Vector3 ray = Quaternion.AngleAxis(i * 360f / StarRays, Vector3.forward) * Vector3.up;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, ray);
                Vector3 rayScale = new Vector3(0.055f, 0.2f, 0.035f);
                Fragment star = Spawn(_meshes.pyramid, _coralMaterial, contact, rotation, rayScale, Vector3.up * 0.15f,
                    0.18f, contact.y, false, random.Next());
                star.spin = Vector3.zero;
            }
        }

        public void EmitDetachedPart(Mesh mesh, Material material, Matrix4x4 pose, Vector3 stoneVelocity, float groundY,
            uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            // A copy belongs to the effects owner, so releasing the enemy's cache lease cannot invalidate it
            Mesh copy = Instantiate(mesh);
            copy.name = "DetachedStone";
            Vector3 direction = new Vector3(random.Range(-1f, 1f), 0f, random.Range(-1f, 1f)).normalized;
            Material partMaterial = material != null ? material : _stoneMaterial;
            Vector3 velocity = stoneVelocity + direction * random.Range(0.6f, 1.2f) + Vector3.up * 0.2f;
            Spawn(copy, partMaterial, pose.GetColumn(3), pose.rotation, pose.lossyScale, velocity, 0.25f, groundY,
                false, seed, copy, true);
        }

        // Breaks the parts still standing into debris and dust, the caller hides them
        public void CollapseParts(IReadOnlyList<Transform> parts, Vector3 stoneVelocity, float groundY, uint seed)
        {
            if (!isActiveAndEnabled || !HasAssets() || parts == null)
            {
                return;
            }

            _standing.Clear();
            Vector3 centre = Vector3.zero;
            foreach (Transform part in parts)
            {
                if (part != null && part.gameObject.activeSelf)
                {
                    _standing.Add(part);
                    centre += part.position;
                }
            }

            if (_standing.Count == 0)
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            int count = CollapseDebris;
            for (int i = 0; i < count; i++)
            {
                Transform part = _standing[i % _standing.Count];
                float angle = i * Mathf.PI * 2f / count;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * random.Range(1.2f, 1.8f);
                Bounds bounds = part.GetComponent<Renderer>().bounds;
                float offsetX = random.Range(-0.8f, 0.8f);
                float offsetY = random.Range(-0.8f, 0.8f);
                float offsetZ = random.Range(-0.8f, 0.8f);
                Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);
                Vector3 position = bounds.center + Vector3.Scale(bounds.extents, offset);
                Material material = i % 4 == 0 ? _coralMaterial : _stoneMaterial;
                Vector3 scale = Vector3.one * random.Range(0.06f, 0.16f);
                Vector3 velocity = stoneVelocity + outward + Vector3.up * random.Range(0.7f, 1.5f);
                Spawn(_meshes.pyramid, material, position, part.rotation, scale, velocity, 0.8f, groundY, true,
                    random.Next());
            }

            centre /= _standing.Count;
            EmitDust(new Vector3(centre.x, groundY + 0.15f, centre.z), seed);
        }

        public static Vector3 PositionAt(Vector3 start, Vector3 velocity, float age, float ground, bool bounce)
        {
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
                    fragment.block.SetColor(RenderObjects.BaseColorId, new Color(0.48f, 0.49f, 0.51f, fade * 0.55f));
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
            StoneRandom random = new StoneRandom(seed);
            for (int j = 0; j < SplitPieces; j++)
            {
                Vector3 scale = Vector3.one * random.Range(0.04f, 0.07f);
                Vector3 velocity = new Vector3(random.Range(-0.3f, 0.3f), 0.3f, random.Range(-0.3f, 0.3f));
                Spawn(_meshes.pyramid, _stoneMaterial, position, Quaternion.identity, scale, velocity, 0.25f, ground,
                    true, random.Next());
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
