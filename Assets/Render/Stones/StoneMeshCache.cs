using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Stone meshes shared by seed and shape, owned by whoever creates the cache
    public class StoneMeshCache
    {
        struct Key : IEquatable<Key>
        {
            uint _seed;
            StoneSettings _settings;

            public Key(uint seed, StoneSettings settings)
            {
                _seed = seed;
                _settings = settings;
            }

            public bool Equals(Key other)
            {
                return _seed == other._seed && _settings.Equals(other._settings);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            // Only for the lookup, never for geometry or visual seeding
            public override int GetHashCode()
            {
                return (int)_seed * 397 ^ _settings.GetHashCode() ^ StoneMesh.GeneratorVersion;
            }
        }

        class Entry
        {
            public Mesh mesh;
            public StoneMeshData data;
            public int references;
        }

        public class Lease : IDisposable
        {
            StoneMeshCache _cache;
            uint _seed;
            StoneSettings _settings;

            Mesh _mesh;
            public Mesh mesh { get { return _mesh; } }

            StoneMeshData _data;
            public StoneMeshData data { get { return _data; } }

            public Lease(StoneMeshCache cache, uint seed, StoneSettings settings, Mesh mesh, StoneMeshData data)
            {
                _cache = cache;
                _seed = seed;
                _settings = settings;
                _mesh = mesh;
                _data = data;
            }

            public void Dispose()
            {
                if (_cache == null)
                {
                    return;
                }

                _cache.Release(new Key(_seed, _settings));
                _cache = null;
            }
        }

        readonly Dictionary<Key, Entry> _entries = new Dictionary<Key, Entry>();

        public int count { get { return _entries.Count; } }

        public Lease Acquire(uint seed, StoneSettings settings)
        {
            Key key = new Key(seed, settings);
            if (!_entries.TryGetValue(key, out Entry entry))
            {
                StoneMeshData meshData;
                if (!StoneMesh.TryGenerate(seed, settings, out meshData))
                {
                    Debug.LogError($"[StoneMeshCache] No stone mesh for seed {seed}, the settings are out of range.");
                    return null;
                }

                entry = new Entry();
                entry.data = meshData;
                entry.mesh = StoneMesh.CreateMesh(meshData);
                _entries.Add(key, entry);
            }
            entry.references++;
            return new Lease(this, seed, settings, entry.mesh, entry.data);
        }

        void Release(Key key)
        {
            if (!_entries.TryGetValue(key, out Entry entry))
            {
                return;
            }

            entry.references--;
            if (entry.references > 0)
            {
                return;
            }

            RenderObjects.Release(entry.mesh);
            _entries.Remove(key);
        }

        public void Clear()
        {
            foreach (Entry entry in _entries.Values)
            {
                RenderObjects.Release(entry.mesh);
            }
            _entries.Clear();
        }
    }
}
