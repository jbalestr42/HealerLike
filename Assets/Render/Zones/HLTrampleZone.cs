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

        HLZoneRegistry _owner;
        int _handle;

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_owner != HLZoneRegistry.current)
            {
                Clear();
            }

            if (!isActiveAndEnabled)
            {
                Clear();
                return;
            }

            _owner = HLZoneRegistry.current;
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
    }
}
