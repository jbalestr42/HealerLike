using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Zones
{
    // A body's solid meshes as capsules: taken once per rebuild, measured every frame as they move. Only meshes
    // whose own transform places them, so a chain drawn as one mesh in its parent's space never joins: its bounds
    // would span its whole length along a world axis.
    public class BodyMeshes
    {
        readonly List<MeshFilter> _meshes = new List<MeshFilter>();

        public int count { get { return _meshes.Count; } }

        // The rig's own parts and roots
        public void Refresh(CreatureRig rig)
        {
            _meshes.Clear();
            if (rig != null)
            {
                rig.CollectBodyMeshes(_meshes);
            }
        }

        public void Refresh(IEnumerable<MeshFilter> meshes)
        {
            _meshes.Clear();
            if (meshes != null)
            {
                _meshes.AddRange(meshes);
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
