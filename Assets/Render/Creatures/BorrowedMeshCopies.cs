using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // One geometry generation owns its copies; legacy outline code may mutate them without changing assets.
    public class BorrowedMeshCopies : IDisposable
    {
        readonly Dictionary<Mesh, Mesh> _copies = new Dictionary<Mesh, Mesh>();

        public Mesh Get(Mesh source)
        {
            if (!source)
            {
                return null;
            }

            if (!_copies.TryGetValue(source, out Mesh copy))
            {
                copy = UnityEngine.Object.Instantiate(source);
                copy.name = source.name;
                copy.hideFlags = HideFlags.DontSave;
                _copies.Add(source, copy);
            }

            return copy;
        }

        public void Dispose()
        {
            foreach (Mesh copy in _copies.Values)
            {
                RenderObjects.Release(copy);
            }

            _copies.Clear();
        }
    }
}
