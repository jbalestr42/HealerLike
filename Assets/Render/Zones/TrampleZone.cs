using UnityEngine;
using HealerLike.Render.Stage;
using HealerLike.Render.Creatures;

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
        ARigHost _host;
        CreatureRig _footprintRig;
        int _footprintRevision;

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
            _host = GetComponent<ARigHost>();
            RefreshFootprint();
            Init(zones);
        }

        // Only the root crown and the primary body determine the clearing. Elevated heads and arms never
        // enlarge it. Rigs compensate their parent scale, so their authored reach is in world-sized cells.
        public static float CreatureFootprint(Transform root, CreatureRig rig = null)
        {
            if (rig == null || rig.recipe == null)
            {
                return StageCalibration.CellSize * 0.5f
                    * Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z));
            }

            RootDefinition roots = rig.recipe.roots;
            float extent = roots.count > 0 ? (roots.footRadius + roots.thickness) * rig.cellSize : 0f;
            for (int i = 0; i < rig.recipe.parts.Length; i++)
            {
                CreaturePart part = rig.recipe.parts[i];
                if (part.role != PartRole.Body || part.parent >= 0) continue;
                Renderer renderer = rig.partTransforms[i].GetComponent<Renderer>();
                if (!renderer) continue;
                Bounds bounds = renderer.bounds;
                Vector3 offset = bounds.center - root.position;
                extent = Mathf.Max(extent, Mathf.Abs(offset.x) + bounds.extents.x,
                    Mathf.Abs(offset.z) + bounds.extents.z);
            }
            return Mathf.Max(extent, StageCalibration.CellSize * 0.25f);
        }

        void RefreshFootprint()
        {
            _footprintRig = _host ? _host.rig : null;
            _footprintRevision = _footprintRig != null ? _footprintRig.revision : 0;
            radius = TrampleRadius(CreatureFootprint(transform, _footprintRig));
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

            if (_host && (_host.rig != _footprintRig
                || (_footprintRig != null && _footprintRig.revision != _footprintRevision)))
            {
                RefreshFootprint();
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
