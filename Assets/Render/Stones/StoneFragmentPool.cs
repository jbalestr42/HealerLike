using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // One pool owns all fragment objects. A borrowed shard has a revocable lease before its slot can be reused.
    public class StoneFragmentPool
    {
        public class Fragment
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

        public class ShardLease : IDisposable
        {
            StoneFragmentPool _owner;
            Fragment _fragment;
            internal readonly Transform key;

            public Transform shard
            {
                get { return _fragment != null && _fragment.gameObject ? _fragment.gameObject.transform : null; }
            }

            public ShardLease(StoneFragmentPool owner, Fragment fragment)
            {
                _owner = owner;
                _fragment = fragment;
                key = fragment.gameObject.transform;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    _owner.ReturnShard(this);
                }
            }

            internal Fragment Revoke()
            {
                Fragment fragment = _fragment;
                _fragment = null;
                _owner = null;
                return fragment;
            }
        }

        readonly Stack<Fragment> _available = new Stack<Fragment>();
        readonly Dictionary<Transform, ShardLease> _shards = new Dictionary<Transform, ShardLease>();
        readonly GameObject _prefab;
        readonly Transform _parent;

        public StoneFragmentPool(GameObject prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public Fragment Take()
        {
            if (_available.Count > 0)
            {
                return _available.Pop();
            }

            GameObject fragmentGo = UnityEngine.Object.Instantiate(_prefab, _parent, false);
            Fragment fragment = new Fragment();
            fragment.gameObject = fragmentGo;
            fragment.filter = fragmentGo.GetComponent<MeshFilter>();
            fragment.renderer = fragmentGo.GetComponent<MeshRenderer>();
            return fragment;
        }

        public void Return(Fragment fragment)
        {
            if (fragment == null || fragment.gameObject == null)
            {
                return;
            }

            fragment.gameObject.SetActive(false);
            fragment.filter.sharedMesh = null;
            _available.Push(fragment);
        }

        public ShardLease Borrow(Mesh mesh, Material material, Color colour)
        {
            Fragment fragment = Take();
            fragment.filter.sharedMesh = mesh;
            fragment.renderer.sharedMaterial = material;
            fragment.block.Clear();
            fragment.block.SetColor(RenderObjects.BaseColorId, colour);
            fragment.renderer.SetPropertyBlock(fragment.block);
            fragment.gameObject.transform.localScale = Vector3.one;
            fragment.gameObject.SetActive(true);
            ShardLease lease = new ShardLease(this, fragment);
            _shards.Add(lease.shard, lease);
            return lease;
        }

        public void ReturnShard(Transform shard)
        {
            if (!ReferenceEquals(shard, null) && _shards.TryGetValue(shard, out ShardLease lease))
            {
                ReturnShard(lease);
            }
        }

        void ReturnShard(ShardLease lease)
        {
            Fragment fragment = lease.Revoke();
            if (fragment == null)
            {
                return;
            }
            _shards.Remove(lease.key);
            Return(fragment);
        }

        public void RevokeShards()
        {
            foreach (ShardLease lease in _shards.Values)
            {
                Return(lease.Revoke());
            }
            _shards.Clear();
        }

        public void Clear()
        {
            RevokeShards();
            _available.Clear();
        }
    }
}
