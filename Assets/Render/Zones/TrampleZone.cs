using UnityEngine;
using UnityEngine.Serialization;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Obstacle footprint in world units, reads the root transform and never the gameplay occupancy
    public class TrampleZone : MonoBehaviour, IEntityView
    {
        // The game's grid cell is one unit, the ring clears the root crown by this margin
        public static readonly float CellSize = 1f;
        public static readonly float Margin = 0.15f;

        [FormerlySerializedAs("Radius")]
        [Min(0)]
        public float radius = 0.65f;

        [FormerlySerializedAs("Strength")]
        [Range(0, 1)]
        public float strength = 1;

        ZoneRegistry _zones;
        ZoneRegistry _owner;
        int _handle;

        public void Init(ZoneRegistry zones)
        {
            Clear();
            _zones = zones;
        }

        public void Init(Entity entity, RenderManager manager)
        {
            InitFootprint(manager.zones);
        }

        // Creature views size the ring from their own root, obstacles keep the authored radius
        public void InitFootprint(ZoneRegistry zones)
        {
            radius = TrampleRadius(CreatureFootprint(transform));
            Init(zones);
        }

        // The root footprint stays within the cell
        public static float CreatureFootprint(Transform root)
        {
            return CellSize * 0.5f * Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z));
        }

        public static float TrampleRadius(float footprintRadius)
        {
            return Mathf.Max(0f, footprintRadius) + Margin;
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            ZoneRegistry zones = _zones;
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
                _handle = _owner.Add(ZoneKind.Trample, transform.position, radius, strength);
            }
            else
            {
                _owner.RefreshZone(_handle, ZoneKind.Trample, transform.position, radius, strength);
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
