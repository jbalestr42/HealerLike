using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Stage;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Zones
{
    // What an obstacle or a creature presses into the grass. A creature with a rig is a body: every mesh of its
    // parts and roots near the ground becomes a capsule the grass parts around, so feet, roots and low bodies
    // leave their own shapes, and its lianas brush the grass where they sweep. Anything else holds the ground's
    // obstacle effect round its root transform, never the gameplay occupancy. When a creature lands, spawned,
    // dropped or moved into place, each root foot plays the foot ring and the body the body ring.
    public class TrampleZone : MonoBehaviour, IEntityView, IGroundBody
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
        // The landing's rings: out of each root foot this many cells wide, round the body this share wider than
        // its footprint
        public static readonly float FootRingRadius = 0.9f;
        public static readonly float BodyRingScale = 1.8f;

        public float radius = 0.65f;
        public float strength = 1f;

        readonly BodyMeshes _meshes = new BodyMeshes();
        readonly List<Vector3> _feet = new List<Vector3>();
        Vector3[] _joints = new Vector3[32];
        Ground _ground;
        GroundHandle _obstacle;
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

        public void Init(Ground ground)
        {
            if (_ground != ground)
            {
                ReleaseGround();
                _ground = ground;
                _obstacle = ground != null ? ground.Hold(ground.vocabulary.obstacle) : null;
            }

            SyncBody();
        }

        public void Init(Entity entity, RenderManager manager)
        {
            _hold = EntityHold.Find(entity);
            InitFootprint(manager.ground);
        }

        // Creature views size the ring from their own root, obstacles keep the authored radius
        public void InitFootprint(Ground ground)
        {
            _host = GetComponent<ARigHost>();
            RefreshFootprint();
            Init(ground);
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
                _obstacle?.Hide();
                return;
            }

            if (_host && (_host.rig != _footprintRig
                || (_footprintRig != null && _footprintRig.revision != _footprintRevision)))
            {
                RefreshFootprint();
            }

            UpdateLanding();

            // A body presses through its capsules, the disc is only for shapeless obstacles
            if (_obstacle == null)
            {
                return;
            }

            if (_isBody || _isHeld)
            {
                _obstacle.Hide();
                return;
            }

            _obstacle.Show(transform.position, radius, strength);
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
            if (_ground == null)
            {
                return;
            }

            _landings++;
            Landing(_ground, _footprintRig, transform.position, radius, _feet);
        }

        // The foot ring out of each root foot of the rig, if it has roots, and the body ring round a footprint
        // of this radius
        public static void Landing(Ground ground, CreatureRig rig, Vector3 position, float footprint,
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
                ground.Play(ground.vocabulary.footRing, foot, FootRingRadius * cell);
            }

            ground.Play(ground.vocabulary.bodyRing, position, footprint * BodyRingScale);
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
            _obstacle?.Hide();
            _ground?.RemoveBody(this);
        }

        void OnDestroy()
        {
            ReleaseGround();
        }

        void ReleaseGround()
        {
            _obstacle?.Release();
            _obstacle = null;
            _ground?.RemoveBody(this);
        }

        // Registered with the ground exactly while it is an enabled body
        void SyncBody()
        {
            if (_ground == null)
            {
                return;
            }

            if (_isBody && isActiveAndEnabled)
            {
                _ground.AddBody(this);
            }
            else
            {
                _ground.RemoveBody(this);
            }
        }
    }
}
