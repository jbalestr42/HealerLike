using System.Collections.Generic;
using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // One owner per rig/root assembly. Baked assets are never stored or destroyed here.
    public class ShapeMeshCache : IDisposable
    {
        readonly Dictionary<(ShapeProfile, int), Mesh> _meshes = new Dictionary<(ShapeProfile, int), Mesh>();
        public int count
        {
            get { return _meshes.Count; }
        }

        public Mesh Get(ShapeProfile shape, int variant = 0)
        {
            if (!shape.isProcedural || !shape.IsValid())
            {
                return null;
            }

            // Growth has no seeded geometry; reuse the mesh even when the recipe carries a stone variant.
            bool mineral =
                shape.kind == ShapeKind.Block
                || shape.kind == ShapeKind.Shard
                || (shape.kind == ShapeKind.Ring && shape.faceted);
            if (!mineral)
            {
                variant = 0;
            }

            (ShapeProfile shape, int variant) key = (shape, variant);
            if (!_meshes.TryGetValue(key, out Mesh mesh))
            {
                mesh = ProceduralShapeMeshes.Create(shape, variant);
                _meshes.Add(key, mesh);
            }

            return mesh;
        }

        public void Dispose()
        {
            foreach (Mesh mesh in _meshes.Values)
            {
                RenderObjects.Release(mesh);
            }

            _meshes.Clear();
        }
    }
}
