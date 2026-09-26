using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Stage;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;

namespace HealerLike.Render.Zones
{
    // What an obstacle or a creature presses into the grass. A creature with a rig is a body: every mesh of its
    // parts and roots near the ground becomes a capsule the grass parts around, so feet, roots and low bodies
    // leave their own shapes. Anything else keeps a footprint disc from its root transform, never from the
    // gameplay occupancy. When a creature lands, spawned, dropped or moved into place, each root foot throws the
    // grass out in a ring and the body throws one round itself.
    public class TrampleZone : MonoBehaviour, IEntityView, IZoneBody
    {
        // The ring clears the root crown by this margin
        public static readonly float Margin = 0.15f;
        // Meshes whose lowest point sits higher than this above the view's root never reach the grass
        public static readonly float BodyReach = 1.5f;
        // The most capsules one body sends in a frame, and of those the most one liana sends
        public static readonly int MaxCapsules = 128;
        public static readonly int MaxArmCapsules = 12;
        // Of those, this many are kept for the lianas out at once, whatever the body's own mesh count
        public static readonly int ArmReserve = 36;
        // A liana brushes the grass as if this much thicker than its tube
        public static readonly float ArmReach = 1.5f;
        // In cells: a jump longer than this in one frame is a move into place, not a step
        public static readonly float LandingJump = 0.5f;
        // The landing's rings, radii in cells: one out of each root foot, one round the whole footprint
        public static readonly float FootRingRadius = 0.9f;
        public static readonly float FootRingStrength = 1f;
        public static readonly float BodyRingScale = 1.8f;
        public static readonly float BodyRingStrength = 1f;

        public float radius = 0.65f;
        public float strength = 1f;

        readonly ZoneHandle _zone = new ZoneHandle();
        readonly BodyMeshes _meshes = new BodyMeshes();
        readonly List<Vector3> _feet = new List<Vector3>();
        Vector3[] _joints = new Vector3[32];
        ZoneRegistry _zones;
        Collider _hold;
        Vector3 _lastPosition;
        bool _isHeld;
        bool _hasLanded;
        ARigHost _host;
        CreatureRig _footprintRig;
        int _footprintRevision;
        bool _isBody;

        public bool isBody { get { return _isBody; } }

        // How many times this creature has landed, spawn included
        int _landings;
        public int landings { get { return _landings; } }

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
            _hold = EntityHold.Find(entity);
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
            _meshes.Refresh(_footprintRig);
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

            UpdateLanding();

            // A body presses through its capsules, the disc is only for shapeless obstacles
            if (_isBody || _isHeld)
            {
                _zone.Clear();
                return;
            }

            _zone.Refresh(ZoneKind.Trample, transform.position, radius, strength);
        }

        // Held while the player drags it; lands when let go, when it first stands, and when it jumps into place
        void UpdateLanding()
        {
            Vector3 position = transform.position;
            bool isHeld = EntityHold.IsHeld(_hold);
            Vector3 jump = position - _lastPosition;
            jump.y = 0f;
            bool hasJumped = _hasLanded && jump.magnitude > LandingJump * StageCalibration.CellSize;
            bool isLetGo = _isHeld && !isHeld;
            _isHeld = isHeld;
            _lastPosition = position;
            if (!isHeld && (!_hasLanded || isLetGo || hasJumped))
            {
                _hasLanded = true;
                Land();
            }
        }

        void Land()
        {
            if (_zones == null)
            {
                return;
            }

            _landings++;
            Landing(_zones, _footprintRig, transform.position, radius, _feet);
        }

        // A ring out of each root foot of the rig, if it has roots, and one round a footprint of this radius
        public static void Landing(ZoneRegistry zones, CreatureRig rig, Vector3 position, float footprint,
                                   List<Vector3> feet)
        {
            float cell = rig != null ? rig.cellSize : StageCalibration.CellSize;
            feet.Clear();
            if (rig != null && rig.recipe != null && rig.root)
            {
                Feet(rig.root, rig.recipe.roots, cell, feet);
            }

            foreach (Vector3 foot in feet)
            {
                zones.AddShock(foot, FootRingRadius * cell, FootRingStrength);
            }

            zones.AddShock(position, footprint * BodyRingScale, BodyRingStrength);
        }

        // Where each root's foot rests, as RootChain places it: out along its heading at the foot radius
        public static void Feet(Transform root, RootDefinition roots, float cellSize, List<Vector3> into)
        {
            for (int i = 0; i < roots.count; i++)
            {
                float angle = i * Mathf.PI * 2f / roots.count;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                into.Add(root.TransformPoint(radial * roots.footRadius * cellSize));
            }
        }

        // The capsules of the body's parts and roots low enough to touch the grass, as they stand this frame. A held
        // creature still brushes the grass it is dragged over, leaving a trail behind it.
        public int AppendCapsules(BodyCapsule[] into, int start)
        {
            if (into == null || !_isBody || !isActiveAndEnabled || strength <= 0f)
            {
                return 0;
            }

            float ceiling = transform.position.y + BodyReach;
            int count = _meshes.Append(into, start, ceiling, MaxCapsules - ArmReserve);
            return count + AppendArms(into, start + count, ceiling, MaxCapsules - count);
        }

        // Every liana out of its rest parts the grass where it sweeps low, along its own curve
        int AppendArms(BodyCapsule[] into, int start, float ceiling, int room)
        {
            if (_host == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _host.armCount && count < room; i++)
            {
                LianaArm arm = _host.GetArm(i);
                if (arm == null || arm.phase == GesturePhase.Rest)
                {
                    continue;
                }

                int joints = arm.segmentCount + 1;
                if (_joints.Length < joints)
                {
                    _joints = new Vector3[joints];
                }

                for (int j = 0; j < joints; j++)
                {
                    _joints[j] = arm.Joint(j);
                }

                count += ChainCapsules.Append(_joints, joints, arm.radius * ArmReach, ceiling, into, start + count,
                                              Mathf.Min(MaxArmCapsules, room - count));
            }

            return count;
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
