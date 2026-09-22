using System;
using System.Collections.Generic;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public static class HLStoneMeshCache
    {
        readonly struct Key : IEquatable<Key>
        {
            readonly uint seed; readonly HLStoneSettings settings;
            public Key(uint seed, HLStoneSettings settings) { this.seed=seed; this.settings=settings; }
            public bool Equals(Key other) => seed == other.seed && settings.Equals(other.settings);
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            // Hashing here is only for cache lookup, never geometry or visual seeding.
            public override int GetHashCode() => unchecked((int)seed * 397 ^ settings.GetHashCode() ^ HLStoneMesh.GeneratorVersion);
        }
        sealed class Entry { public Mesh Mesh; public HLStoneMeshData Data; public int References; }
        static readonly Dictionary<Key,Entry> entries = new Dictionary<Key,Entry>();
        public sealed class Lease : IDisposable
        {
            readonly Key key; bool disposed;
            public Mesh Mesh { get; }
            public HLStoneMeshData Data { get; }
            internal Lease(uint seed, HLStoneSettings settings)
            {
                key=new Key(seed,settings);
                if (!entries.TryGetValue(key,out var entry))
                {
                    var data=HLStoneMesh.Generate(seed,settings);
                    entry=new Entry { Data=data, Mesh=HLStoneMesh.CreateMesh(data) }; entries.Add(key,entry);
                }
                entry.References++; Mesh=entry.Mesh; Data=entry.Data;
            }
            public void Dispose()
            {
                if (disposed) return; disposed=true;
                if (--entries[key].References != 0) return;
                DestroyOwned(entries[key].Mesh); entries.Remove(key);
            }
        }
        public static Lease Acquire(uint seed, in HLStoneSettings settings) => new Lease(seed,settings);
        public static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
