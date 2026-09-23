using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Zones
{
    // Obstacle footprint in world units, reads the root transform and never the gameplay occupancy
    public class HLTrampleZone : MonoBehaviour
    {
        [FormerlySerializedAs("Radius")]
        [Min(0)]
        public float radius = 0.65f;

        [FormerlySerializedAs("Strength")]
        [Range(0, 1)]
        public float strength = 1;

        HLZoneRegistry _zones;
        HLZoneRegistry _owner;
        int _handle;
        bool _isInitialized = false;

        public void Init(HLZoneRegistry zones)
        {
            Clear();
            _zones = zones;
            _isInitialized = true;
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            HLZoneRegistry zones = GetZones();
            if (_owner != zones)
            {
                Clear();
            }

            if (!isActiveAndEnabled)
            {
                Clear();
                return;
            }

            _owner = zones;
            if (!_owner)
            {
                return;
            }

            if (!_owner.Contains(_handle))
            {
                _handle = _owner.Add(HLZoneKind.Trample, transform.position, radius, strength);
            }
            else
            {
                _owner.RefreshZone(_handle, HLZoneKind.Trample, transform.position, radius, strength);
            }
        }

        void Clear()
        {
            if (_owner)
            {
                _owner.Remove(_handle);
            }

            _owner = null;
            _handle = 0;
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }

        // Falls back to the static registry until the RenderManager calls Init, removed in D2
        HLZoneRegistry GetZones()
        {
            return _isInitialized ? _zones : HLZoneRegistry.current;
        }
    }
}
