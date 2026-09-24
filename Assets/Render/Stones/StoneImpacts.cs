using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Where a stone was hit: the contact a projectile reported before its damage resolved,
    // or else a point on the standing parts estimated from the attacker
    public class StoneImpacts
    {
        struct ImpactRecord
        {
            public StoneImpact impact;
            public int frame;
        }

        // Seed salt of every hit, so hits never share a random stream with the body's other emissions
        static readonly uint hitSalt = 100;
        // A reported contact whose damage has not resolved after this many frames is dropped
        static readonly int recordFrames = 2;

        readonly Dictionary<ResourceModifier, ImpactRecord> _impacts = new Dictionary<ResourceModifier, ImpactRecord>();
        readonly List<ResourceModifier> _expired = new List<ResourceModifier>();
        Transform _owner;
        StoneEffects _effects;
        IReadOnlyList<Transform> _parts = System.Array.Empty<Transform>();
        StoneMeshData[] _partMeshes = new StoneMeshData[0];
        uint _seed;
        uint _hitIndex;
        int _completedFrames;

        public void Init(Transform owner, StoneEffects effects, uint seed)
        {
            _impacts.Clear();
            _owner = owner;
            _effects = effects;
            _seed = seed;
            _hitIndex = 0;
            _completedFrames = 0;
            ReadParts(System.Array.Empty<Transform>());
        }

        // The meshes an estimate searches, read again whenever the rig is rebuilt
        public void ReadParts(IReadOnlyList<Transform> parts)
        {
            _parts = parts;
            _partMeshes = new StoneMeshData[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                Mesh mesh = parts[i].GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null)
                {
                    _partMeshes[i] = new StoneMeshData(mesh.vertices, mesh.normals, mesh.triangles, mesh.bounds);
                }
            }
        }

        public void Record(ResourceModifier modifier, StoneImpact impact)
        {
            _impacts[modifier] = new ImpactRecord { impact = impact, frame = _completedFrames };
            EmitDust(impact.point);
        }

        public StoneImpact Estimate(Vector3 query)
        {
            Vector3 point = _owner.position + Vector3.up * 0.5f;
            Vector3 normal = Vector3.up;
            float best = float.PositiveInfinity;
            for (int i = 0; i < _parts.Count && i < _partMeshes.Length; i++)
            {
                if (!_parts[i].gameObject.activeSelf)
                {
                    continue;
                }

                if (StoneImpactLocator.TryClosestPoint(_partMeshes[i], _parts[i].localToWorldMatrix, query,
                    out Vector3 partPoint, out Vector3 partNormal))
                {
                    float distance = (partPoint - query).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        point = partPoint;
                        normal = partNormal;
                    }
                }
            }
            return new StoneImpact(point, normal);
        }

        // A resolved modifier gives back its reported contact; a hit nobody reported is estimated from its source
        public void Resolve(ResourceModifier modifier, bool isHit, bool critical)
        {
            ImpactRecord record = default;
            bool isRecorded = false;
            if (modifier != null)
            {
                isRecorded = _impacts.TryGetValue(modifier, out record);
                _impacts.Remove(modifier);
            }

            if (!isHit)
            {
                return;
            }

            StoneImpact impact = record.impact;
            if (!isRecorded)
            {
                Vector3 query = _owner.position + Vector3.up * 2f;
                if (modifier != null && modifier.source != null)
                {
                    query = modifier.source.transform.position;
                }
                impact = Estimate(query);
                EmitDust(impact.point);
            }

            if (_effects != null)
            {
                StoneEmitters.Hit(_effects, impact, critical, NextSeed());
            }
        }

        // Drops the reported contacts whose damage never resolved
        public void CompleteFrame()
        {
            _completedFrames++;
            _expired.Clear();
            foreach (KeyValuePair<ResourceModifier, ImpactRecord> pair in _impacts)
            {
                if (_completedFrames - pair.Value.frame >= recordFrames)
                {
                    _expired.Add(pair.Key);
                }
            }

            foreach (ResourceModifier key in _expired)
            {
                _impacts.Remove(key);
            }
        }

        public void Clear()
        {
            _impacts.Clear();
        }

        void EmitDust(Vector3 point)
        {
            if (_effects != null)
            {
                StoneEmitters.Dust(_effects, point, NextSeed());
            }
        }

        uint NextSeed()
        {
            _hitIndex++;
            return StoneSeed.ForPart(_seed, _hitIndex + hitSalt);
        }
    }
}
