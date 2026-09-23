using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class HLStoneMeshCache
    {
        readonly struct Key : IEquatable<Key>
        {
            readonly uint _seed;
            readonly HLStoneSettings _settings;

            public Key(uint seed, HLStoneSettings settings)
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

            // Hashing here is only for cache lookup, never geometry or visual seeding.
            public override int GetHashCode()
            {
                return unchecked((int)_seed * 397 ^ _settings.GetHashCode() ^ HLStoneMesh.GeneratorVersion);
            }
        }

        class Entry
        {
            public Mesh mesh;
            public HLStoneMeshData data;
            public int references;
        }

        public class Lease : IDisposable
        {
            readonly Key _key;
            bool _isDisposed;

            public Mesh mesh { get; }

            public HLStoneMeshData data { get; }

            public Lease(uint seed, HLStoneSettings settings)
            {
                _key = new Key(seed, settings);
                if (!_entries.TryGetValue(_key, out Entry entry))
                {
                    HLStoneMeshData meshData = HLStoneMesh.Generate(seed, settings);
                    entry = new Entry { data = meshData, mesh = HLStoneMesh.CreateMesh(meshData) };
                    _entries.Add(_key, entry);
                }
                entry.references++;
                mesh = entry.mesh;
                data = entry.data;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                if (--_entries[_key].references != 0)
                {
                    return;
                }

                DestroyOwned(_entries[_key].mesh);
                _entries.Remove(_key);
            }
        }

        static readonly Dictionary<Key, Entry> _entries = new Dictionary<Key, Entry>();

        public static Lease Acquire(uint seed, in HLStoneSettings settings)
        {
            return new Lease(seed, settings);
        }

        public static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
