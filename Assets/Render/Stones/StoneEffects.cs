using System.Collections.Generic;
using HealerLike.Render.Creatures;
using Fragment = HealerLike.Render.Stones.StoneFragmentPool.Fragment;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // The one owner of stone debris, dust and thrown shards, and of the stone meshes shared by every stone:
    // the fragment pool, their ballistic flight and the shard lender. StoneEmitters holds the recipes
    public class StoneEffects : MonoBehaviour
    {
        public static readonly int MaxLiveFragments = 256;

        // Fragments the dust and collapse recipes of StoneEmitters spawn
        public static readonly int DustPuffs = 5;
        public static readonly int CollapseDebris = 12;

        static readonly float gravity = 8f;
        static readonly Vector3 fragmentSpin = new Vector3(70f, 120f, 45f);
        static readonly uint dustMeshSeed = 123;

        [SerializeField] Material _stoneMaterial;
        [SerializeField] Material _coralMaterial;
        [SerializeField] Material _dustMaterial;
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] GameObject _fragmentPrefab;

        readonly List<Fragment> _active = new List<Fragment>(MaxLiveFragments);
        StoneFragmentPool _pool;
        StoneMeshCache.Lease _dustLease;

        readonly StoneMeshCache _stoneMeshes = new StoneMeshCache();
        public StoneMeshCache stoneMeshes { get { return _stoneMeshes; } }

        public Material stoneMaterial { get { return _stoneMaterial; } }

        public Material coralMaterial { get { return _coralMaterial; } }

        // The baked pyramid every chip and debris piece is drawn with
        public Mesh debrisMesh { get { return _meshes.pyramid; } }

        public int liveCount { get { return _active.Count; } }

        // Whether a recipe can spawn now: the effects are live and wired
        public bool CanEmit()
        {
            return isActiveAndEnabled && HasAssets();
        }

        bool HasAssets()
        {
            if (_fragmentPrefab == null || _meshes == null || _stoneMaterial == null || _coralMaterial == null
                || _dustMaterial == null)
            {
                Debug.LogError("[StoneEffects] Wire the fragment prefab, meshes and materials on the effects prefab.");
                return false;
            }

            if (_pool == null)
            {
                _pool = new StoneFragmentPool(_fragmentPrefab, transform);
            }
            if (_dustLease == null)
            {
                _dustLease = _stoneMeshes.Acquire(dustMeshSeed, StonePresets.Shape(1f, 1f, 1f, 0f, 1));
            }
            return true;
        }

        public void Spawn(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale,
            Vector3 velocity, float life, float ground, bool bounce, uint seed, Mesh owned = null, bool split = false)
        {
            SpawnFragment(mesh, material, position, rotation, scale, velocity, life, ground, bounce, seed, owned,
                split);
        }

        // A puff rises straight up and fades, it does not spin
        public void SpawnDust(Vector3 position, Vector3 scale, Vector3 velocity, float life, uint seed)
        {
            Fragment fragment = SpawnFragment(_dustLease.mesh, _dustMaterial, position, Quaternion.identity, scale,
                velocity, life, position.y, false, seed, null, false);
            fragment.isDust = true;
            fragment.spin = Vector3.zero;
        }

        Fragment SpawnFragment(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale,
            Vector3 velocity, float life, float ground, bool bounce, uint seed, Mesh owned, bool split)
        {
            while (_active.Count >= MaxLiveFragments)
            {
                Release(_active[0]);
            }

            Fragment fragment = _pool.Take();

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
            _pool.Return(fragment);
        }

        // A thrown delivery keeps this lease; disabling the owner revokes it before reusing the fragment.
        public StoneFragmentPool.ShardLease BorrowShard(Mesh mesh, Color colour)
        {
            if (!isActiveAndEnabled || !HasAssets())
            {
                return null;
            }
            return _pool.Borrow(mesh, _stoneMaterial, colour);
        }

        public Transform TakeShard(Mesh mesh, Color colour)
        {
            StoneFragmentPool.ShardLease lease = BorrowShard(mesh, colour);
            return lease != null ? lease.shard : null;
        }

        public void ReturnShard(Transform shard)
        {
            if (_pool != null)
            {
                _pool.ReturnShard(shard);
            }
        }

        static Vector3 PositionAt(Vector3 start, Vector3 velocity, float age, float ground, bool bounce)
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
                    Color dust = _dustMaterial.GetColor(RenderObjects.BaseColorId);
                    dust.a *= fade;
                    fragment.block.SetColor(RenderObjects.BaseColorId, dust);
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
                    StoneEmitters.Split(this, position, ground, seed);
                }
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
            if (_pool != null)
            {
                _pool.RevokeShards();
            }
        }

        void OnDestroy()
        {
            OnDisable();
            if (_pool != null)
            {
                _pool.Clear();
            }
            if (_dustLease != null)
            {
                _dustLease.Dispose();
                _dustLease = null;
            }
            _stoneMeshes.Clear();
        }
    }
}
