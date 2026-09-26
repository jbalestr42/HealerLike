using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // The meshes under a body's root as capsules: taken once per rebuild, measured every frame as they move
    public class BodyMeshes
    {
        readonly List<MeshFilter> _meshes = new List<MeshFilter>();

        public int count { get { return _meshes.Count; } }

        public void Refresh(Transform root)
        {
            _meshes.Clear();
            if (root)
            {
                root.GetComponentsInChildren(false, _meshes);
            }
        }

        public void Clear()
        {
            _meshes.Clear();
        }

        // Up to limit capsules from start on, skipping meshes whose lowest point is above ceiling
        public int Append(BodyCapsule[] into, int start, float ceiling, int limit)
        {
            if (into == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _meshes.Count && count < limit && start + count < into.Length; i++)
            {
                MeshFilter filter = _meshes[i];
                if (!filter || !filter.gameObject.activeInHierarchy || filter.sharedMesh == null)
                {
                    continue;
                }

                if (!BodyCapsule.TryFromBounds(filter.transform.localToWorldMatrix, filter.sharedMesh.bounds,
                                               out BodyCapsule capsule) || capsule.bottom > ceiling)
                {
                    continue;
                }

                into[start + count] = capsule;
                count++;
            }

            return count;
        }
    }
}
