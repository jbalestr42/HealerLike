using System;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Creatures
{
    // One identity per cast. A removed source invalidates permanently, even if later recreated.
    public sealed class CastSourceLease : IDisposable
    {
        CreatureRig _rig;
        GameObject _fallback;
        readonly bool _explicit;
        bool _disposed;
        public string sourceId { get; private set; }
        public bool isExplicit => _explicit;

        public CastSourceLease(CreatureRig rig, uint sequence = 0)
        {
            _rig = rig;
            _explicit = rig != null && CreatureSources.HasExplicit(rig);
            sourceId = rig != null ? CreatureSources.Select(rig, sequence) : null;
            if (_rig != null) _rig.recomposed += OnRecompose;
        }

        public static CastSourceLease From(GameObject owner, uint sequence = 0)
        {
            ARigHost host = CreatureSources.Host(owner);
            if (host) host.SyncGeometry();
            CastSourceLease lease = new CastSourceLease(host ? host.rig : null, sequence);
            if (lease._rig == null) lease._fallback = owner;
            return lease;
        }

        public bool TryGet(out Vector3 point)
        {
            point = default;
            if (_disposed) return false;
            if (_explicit)
            {
                if (CreatureSources.Resolve(_rig, sourceId, out point)) return true;
            }
            else if (_rig != null && _rig.root && _rig.root.gameObject.activeInHierarchy
                && _rig.TryGetAnchors(out EffectAnchors anchors))
            {
                point = anchors.castPoint;
                return true;
            }
            else if (_fallback && _fallback.activeInHierarchy)
            {
                point = EffectAnchorReader.Read(_fallback).castPoint;
                return true;
            }
            Dispose();
            return false;
        }

        void OnRecompose()
        {
            if (_explicit && !CreatureSources.Resolve(_rig, sourceId, out _)) Dispose();
        }

        public void Dispose()
        {
            _disposed = true;
            if (_rig != null) _rig.recomposed -= OnRecompose;
            _rig = null;
            _fallback = null;
        }
    }
}
