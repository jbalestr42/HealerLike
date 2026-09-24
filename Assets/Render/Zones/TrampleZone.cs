using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Obstacle footprint in world units, reads the root transform and never the gameplay occupancy
    public class TrampleZone : MonoBehaviour, IEntityView
    {
        // The ring clears the root crown by this margin
        public static readonly float Margin = 0.15f;

        public float radius = 0.65f;
        public float strength = 1f;

        readonly ZoneHandle _zone = new ZoneHandle();

        public void Init(ZoneRegistry zones)
        {
            _zone.Init(zones);
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
            return StageCalibration.CellSize * 0.5f
                * Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z));
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
            if (!isActiveAndEnabled)
            {
                _zone.Clear();
                return;
            }

            _zone.Refresh(ZoneKind.Trample, transform.position, radius, strength);
        }

        void OnDisable()
        {
            _zone.Clear();
        }

        void OnDestroy()
        {
            _zone.Clear();
        }
    }
}
