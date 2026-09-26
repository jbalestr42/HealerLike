using UnityEngine;
using HealerLike.Render.Stage;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Zones
{
    // What an obstacle or a creature presses into the grass. A creature with a rig is a body: every mesh of its
    // parts and roots near the ground becomes a capsule the grass parts around, so feet, roots and low bodies
    // leave their own shapes. Anything else keeps a footprint disc from its root transform, never from the
    // gameplay occupancy.
    public class TrampleZone : MonoBehaviour, IEntityView, IZoneBody
    {
        // The ring clears the root crown by this margin
        public static readonly float Margin = 0.15f;
        // Meshes whose lowest point sits higher than this above the view's root never reach the grass
        public static readonly float BodyReach = 1.5f;
        // The most capsules one body sends in a frame
        public static readonly int MaxCapsules = 48;

        public float radius = 0.65f;
        public float strength = 1f;

        readonly ZoneHandle _zone = new ZoneHandle();
        readonly BodyMeshes _meshes = new BodyMeshes();
        ZoneRegistry _zones;
        ARigHost _host;
        CreatureRig _footprintRig;
        int _footprintRevision;
        bool _isBody;

        public bool isBody { get { return _isBody; } }

        public void Init(ZoneRegistry zones)
        {
            _zone.Init(zones);
            if (_zones != null && _zones != zones)
            {
                _zones.RemoveBody(this);
            }

            _zones = zones;
            SyncBody();
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

        // Only the root crown, basal body and mineral feet determine the clearing. Elevated heads and arms never
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
                bool isBase = part.role == PartRole.Body || (roots.count == 0 && part.role == PartRole.Limb);
                if (!isBase) continue;
                Renderer renderer = rig.partTransforms[i].GetComponent<Renderer>();
                if (!renderer) continue;
                Bounds bounds = renderer.bounds;
                Vector3 offset = bounds.center - root.position;
                float x = Mathf.Abs(offset.x) + bounds.extents.x;
                float z = Mathf.Abs(offset.z) + bounds.extents.z;
                extent = Mathf.Max(extent, Mathf.Sqrt(x * x + z * z));
            }
            return Mathf.Max(extent, StageCalibration.CellSize * 0.25f);
        }

        void RefreshFootprint()
        {
            _footprintRig = _host ? _host.rig : null;
            _footprintRevision = _footprintRig != null ? _footprintRig.revision : 0;
            radius = TrampleRadius(CreatureFootprint(transform, _footprintRig));
            _meshes.Refresh(_footprintRig != null ? _footprintRig.root : null);
            _isBody = _meshes.count > 0;
            SyncBody();
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

            // A body presses through its capsules, the disc is only for shapeless obstacles
            if (_isBody)
            {
                _zone.Clear();
                return;
            }

            _zone.Refresh(ZoneKind.Trample, transform.position, radius, strength);
        }

        // The capsules of the meshes low enough to touch the grass, as they stand this frame
        public int AppendCapsules(BodyCapsule[] into, int start)
        {
            if (into == null || !_isBody || !isActiveAndEnabled || strength <= 0f)
            {
                return 0;
            }

            return _meshes.Append(into, start, transform.position.y + BodyReach, MaxCapsules);
        }

        void OnEnable()
        {
            SyncBody();
        }

        void OnDisable()
        {
            _zone.Clear();
            if (_zones != null)
            {
                _zones.RemoveBody(this);
            }
        }

        void OnDestroy()
        {
            _zone.Clear();
            if (_zones != null)
            {
                _zones.RemoveBody(this);
            }
        }

        // Registered with the zones exactly while it is an enabled body
        void SyncBody()
        {
            if (_zones == null)
            {
                return;
            }

            if (_isBody && isActiveAndEnabled)
            {
                _zones.AddBody(this);
            }
            else
            {
                _zones.RemoveBody(this);
            }
        }
    }
}
