using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureRig : IDisposable, IDeliverySource
    {
        public static readonly int MaxArms = 8;

        // A projectile the rig follows until its delivery ends
        class Delivery
        {
            public int lease;
            public DeliveryStyle style;
            public Vector3? contact;
            public Transform projectile;
            public bool hasFreshContact;
        }

        readonly LianaArm[] _arms = new LianaArm[MaxArms];
        readonly int[] _tokens = new int[MaxArms];
        readonly int[] _definitions = new int[MaxArms];
        readonly Vector3?[] _branchRoots = new Vector3?[MaxArms];
        readonly MaterialPropertyBlock _colourBlock = new MaterialPropertyBlock();
        readonly Dictionary<int, Delivery> _deliveries = new Dictionary<int, Delivery>();
        CreatureRecipe _recipe;
        Material _material;
        PrimitiveMeshes _meshes;
        float _cellSize;
        Transform _root;
        Transform _sway;
        Transform[] _pivots;
        Transform[] _geometry;
        Transform[] _roots;
        Transform[] _rootJoints;
        Renderer[] _bodyRenderers;
        Color[] _colours;
        IdleDefinition _idle;
        int _nextToken;
        bool _isDisposed;
        float _crownPulse;
        float _hitPulse;
        Vector3? _aimTarget;
        float _budPower;
        Color _statusTint = Color.white;
        Vector3 _previousOrigin;
        bool _isPlaced;

        float _charge;
        public float charge { get { return _charge; } }

        float _healthFraction = 1f;
        public float healthFraction { get { return _healthFraction; } }

        Quaternion _aim = Quaternion.identity;
        public Quaternion aim { get { return _aim; } }

        public Transform[] budAnchors { get; private set; }

        public Transform root { get { return _root; } }

        public int activeArmCount
        {
            get
            {
                int count = 0;
                foreach (LianaArm arm in _arms)
                {
                    if (arm != null && !arm.isAvailable)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // A recipe that fails validation logs and leaves the view empty
        public bool Init(CreatureRecipe data, Transform parent, Material material, PrimitiveMeshes meshes,
            float cellSize = 1f)
        {
            if (!CreatureValidator.TryValidate(data, out string error))
            {
                Debug.LogError($"[CreatureRig] {error}");
                return false;
            }

            if (!parent || !material || !meshes || !float.IsFinite(cellSize) || cellSize <= 0f)
            {
                Debug.LogError("[CreatureRig] Needs a parent, a material, the meshes and a positive cell size.");
                return false;
            }

            Vector3 scale = parent.lossyScale;
            if (scale.x <= 0f || Mathf.Abs(scale.x - scale.y) > 0.0001f || Mathf.Abs(scale.x - scale.z) > 0.0001f)
            {
                Debug.LogError("[CreatureRig] Creature rig ancestors must have positive uniform scale.");
                return false;
            }

            _recipe = data;
            _material = material;
            _meshes = meshes;
            _cellSize = cellSize;
            _root = new GameObject("GeneratedCreature").transform;
            _root.SetParent(parent, false);
            _root.localScale = Vector3.one / parent.lossyScale.x;
            _sway = new GameObject("Sway").transform;
            _sway.SetParent(_root, false);
            _pivots = new Transform[data.parts.Length];
            _geometry = new Transform[data.parts.Length];
            _bodyRenderers = new Renderer[data.parts.Length];
            _colours = new Color[data.parts.Length];
            _idle = data.idle;
            _idle.seed ^= parent.GetEntityId().GetHashCode();
            for (int i = 0; i < data.parts.Length; i++)
            {
                CreaturePart part = data.parts[i];
                _colours[i] = BeautyMotion.Vary(part.colour, _idle.seed);
                _pivots[i] = new GameObject(part.id).transform;
                _pivots[i].SetParent(part.parent < 0 ? _sway : _pivots[part.parent], false);
                _pivots[i].localPosition = part.localPosition * cellSize;
                _pivots[i].localRotation = Quaternion.Euler(part.localEuler);
                _geometry[i] = PrimitiveMeshes.Geometry("Geometry", _pivots[i], meshes.GetMesh(part.primitive),
                    material, _colours[i], part.glow);
                _geometry[i].localScale = part.dimensions * cellSize;
                _bodyRenderers[i] = _geometry[i].GetComponent<Renderer>();
            }

            budAnchors = Array.FindAll(_pivots, pivot => pivot.name.StartsWith("Bud", StringComparison.Ordinal));
            int segments = data.roots.segments;
            _roots = new Transform[data.roots.count * segments];
            _rootJoints = new Transform[data.roots.count * (segments - 1)];
            Color rootColour = BeautyMotion.Vary(data.roots.colour, _idle.seed);
            for (int i = 0; i < _roots.Length; i++)
            {
                _roots[i] = PrimitiveMeshes.Geometry("Root", _root, meshes.cylinder, material, rootColour);
            }

            // Lighter knuckles fill the bends between segments
            for (int i = 0; i < _rootJoints.Length; i++)
            {
                _rootJoints[i] = PrimitiveMeshes.Geometry("RootJoint", _root, meshes.sphere, material, rootColour, 0.35f);
            }

            for (int i = 0; i < data.arms.Length; i++)
            {
                CreateArm(i, i);
            }

            return true;
        }

        public void SetStatusTint(Color tint)
        {
            _statusTint = tint;
        }

        public void SetReadout(Vector3? target, float health, float readiness, float glow)
        {
            _aimTarget = target;
            _healthFraction = float.IsFinite(health) ? Mathf.Clamp01(health) : 1f;
            _charge = float.IsFinite(readiness) ? Mathf.Clamp01(readiness) : 0f;
            _budPower = float.IsFinite(glow) ? Mathf.Clamp01(glow) : 0f;
        }

        public void Hit()
        {
            _hitPulse = 1f;
        }

        public void ContactDeliveryPath(int token, Vector3 position, bool preserve)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            bool isBranch = preserve || delivery.style == DeliveryStyle.ChainSync;
            Contact(delivery.lease, position, isBranch ? delivery.contact : null);
            delivery.contact = position;
            delivery.hasFreshContact = true;
        }

        public int Begin(GestureKind kind, Vector3 goal)
        {
            int slot = FreeSlot();
            if (slot < 0)
            {
                return 0;
            }

            if (++_nextToken == 0)
            {
                ++_nextToken;
            }

            _tokens[slot] = _nextToken;
            _branchRoots[slot] = null;
            _arms[slot].deliveryProfile = kind == GestureKind.Heal;
            _arms[slot].style = kind == GestureKind.Heal ? DeliveryStyle.Arc : DeliveryStyle.Direct;
            _arms[slot].SetVisible(true);
            _arms[slot].Begin(_nextToken, kind, goal);
            return _nextToken;
        }

        public void SetTipGoal(int token, Vector3 goal)
        {
            if (token == 0)
            {
                return;
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] == token && !_branchRoots[i].HasValue && _arms[i] != null)
                {
                    _arms[i].SetTipGoal(token, goal);
                }
            }
        }

        public void Contact(int token, Vector3 goal, Vector3? previousContact = null)
        {
            if (token == 0)
            {
                CoalesceContact(goal);
                return;
            }

            if (previousContact.HasValue)
            {
                int slot = FreeSlot();
                if (slot < 0)
                {
                    CoalesceContact(goal);
                    return;
                }

                _tokens[slot] = token;
                _branchRoots[slot] = previousContact;
                _arms[slot].style = DeliveryStyle.ChainSync;
                _arms[slot].deliveryProfile = true;
                _arms[slot].SetVisible(true);
                _arms[slot].Begin(token, GestureKind.Attack, goal);
                _arms[slot].Contact(token, goal);
            }
            else
            {
                for (int i = 0; i < MaxArms; i++)
                {
                    if (_tokens[i] == token && !_branchRoots[i].HasValue && _arms[i] != null)
                    {
                        _arms[i].Contact(token, goal);
                    }
                }
            }
        }

        public void End(int token)
        {
            if (token == 0)
            {
                return;
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] == token && _arms[i] != null)
                {
                    _arms[i].End(token);
                }
            }
        }

        public void CancelAll()
        {
            _deliveries.Clear();
            for (int i = 0; i < MaxArms; i++)
            {
                if (_arms[i] != null)
                {
                    _arms[i].Cancel(_tokens[i]);
                    _tokens[i] = 0;
                }
            }
        }

        public void HealContact(Vector3 point)
        {
            _crownPulse = 1f;
            int token = Begin(GestureKind.Heal, point);
            Contact(token, point);
        }

        public void Tick(float time, float deltaTime, FootFrame frame)
        {
            if (!_root)
            {
                return;
            }

            if (_isPlaced && Vector3.Distance(frame.origin, _previousOrigin) > _cellSize * 0.75f)
            {
                CancelAll();
            }

            _previousOrigin = frame.origin;
            _isPlaced = true;
            FollowProjectiles();
            _root.SetPositionAndRotation(frame.origin, Quaternion.FromToRotation(Vector3.up, frame.normal));
            _root.localScale = Vector3.one / _root.parent.lossyScale.x;
            float dt = Mathf.Max(0f, deltaTime);
            Vector3 direction = _aimTarget.HasValue
                ? _root.InverseTransformDirection(_aimTarget.Value - frame.origin)
                : new Vector3(Mathf.Sin(time * 0.3f) * 0.4f, 0f, 1f);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f)
            {
                _aim = Quaternion.Slerp(_aim, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-dt * 7f));
            }

            _hitPulse = Mathf.Max(0f, _hitPulse - dt * 5f);
            IdlePose idlePose = IdleMotion.Evaluate(_idle, time);
            float shake = Mathf.Sin(_hitPulse * 24f) * _hitPulse * 9f;
            Quaternion wilt = Quaternion.Euler((1f - _healthFraction) * 32f, 0f, shake);
            _sway.localRotation = _aim * idlePose.sway * wilt;
            _sway.localPosition = Vector3.down * ((1f - _healthFraction) * 0.08f * _cellSize);
            _crownPulse = Mathf.Max(0f, _crownPulse - dt / 0.2f);
            for (int i = 0; i < _geometry.Length; i++)
            {
                CreaturePart part = _recipe.parts[i];
                bool isHead = part.primitive == Primitive.Sphere || part.glow > 0f || part.id == "Bulb";
                float swell = 1f + _crownPulse * 0.06f + (isHead ? _charge * 0.24f : 0f);
                _geometry[i].localScale = Vector3.Scale(part.dimensions, idlePose.bodyScale) * _cellSize * swell;
                if (part.id == "Crown")
                {
                    Quaternion spin = Quaternion.AngleAxis(time * 18f, Vector3.up);
                    _pivots[i].localRotation = Quaternion.Euler(part.localEuler) * spin;
                }

                Color wiltColour = _recipe.wiltColour;
                wiltColour.a = _colours[i].a;
                Color colour = Color.Lerp(wiltColour, _colours[i], _healthFraction);
                float light = Mathf.Max(_budPower, _charge) * _healthFraction;
                if (part.glow > 0f)
                {
                    Color dim = new Color(colour.r * 0.55f, colour.g * 0.55f, colour.b * 0.55f, colour.a);
                    colour = Color.Lerp(dim, new Color(0.78f, 0.95f, 0.29f, colour.a), light);
                }

                colour = _statusTint == Color.white ? colour : Color.Lerp(colour, _statusTint, 0.42f);
                _colourBlock.SetColor("_BaseColor", PrimitiveMeshes.Brighten(colour, part.glow * light));
                _bodyRenderers[i].SetPropertyBlock(_colourBlock);
            }

            RootDefinition roots = _recipe.roots;
            for (int i = 0; i < roots.count; i++)
            {
                float angle = i * Mathf.PI * 2f / roots.count + roots.angularOffset * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 hip = _sway.TransformPoint((radial * 0.08f + Vector3.up * roots.hipHeight) * _cellSize);
                Vector3 kneeLocal = radial * (roots.footRadius * 0.6f) + Vector3.up * roots.kneeHeight;
                Vector3 knee = _root.TransformPoint(kneeLocal * _cellSize);
                Vector3 foot = _root.TransformPoint(radial * roots.footRadius * _cellSize);

                // A curve from hip to foot through the raised knee, cut into equal steps, thinning toward the foot
                Vector3 bend = knee * 2f - (hip + foot) * 0.5f;
                Vector3 start = hip;
                for (int k = 0; k < roots.segments; k++)
                {
                    float t = (k + 1f) / roots.segments;
                    Vector3 end = (1f - t) * (1f - t) * hip + 2f * t * (1f - t) * bend + t * t * foot;
                    float taper = Mathf.Lerp(1f, 0.65f, (float)k / Mathf.Max(1, roots.segments - 1));
                    float radius = roots.thickness * taper * _cellSize;
                    PrimitiveMeshes.Segment(_roots[i * roots.segments + k], start, end, radius);
                    if (k > 0)
                    {
                        Transform joint = _rootJoints[i * (roots.segments - 1) + k - 1];
                        joint.position = start;
                        joint.localScale = Vector3.one * (radius * 2.8f / _root.lossyScale.x);
                    }

                    start = end;
                }
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_arms[i] == null)
                {
                    continue;
                }

                ArmDefinition definition = _recipe.arms[_definitions[i]];
                Vector3 shoulder = _branchRoots[i]
                    ?? _pivots[definition.bodyPart].TransformPoint(definition.rootLocal * _cellSize);
                _arms[i].Tick(deltaTime, shoulder, _root.rotation * _sway.localRotation);
                if (_arms[i].isAvailable)
                {
                    _branchRoots[i] = null;
                    _tokens[i] = 0;
                    _arms[i].SetVisible(i < _recipe.arms.Length);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (LianaArm arm in _arms)
            {
                if (arm != null)
                {
                    arm.Dispose();
                }
            }

            if (!_root)
            {
                return;
            }

            _root.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_root.gameObject);
            }
        }

        void CreateArm(int slot, int definitionIndex)
        {
            _definitions[slot] = definitionIndex;
            ArmDefinition definition = _recipe.arms[definitionIndex];
            definition.colour = BeautyMotion.Vary(definition.colour, _idle.seed);
            _arms[slot] = new LianaArm();
            _arms[slot].Init(definition, _root, _material, _meshes, _cellSize);
            Vector3 shoulder = _pivots[definition.bodyPart].TransformPoint(definition.rootLocal * _cellSize);
            _arms[slot].Tick(0f, shoulder, _root.rotation);
        }

        int FreeSlot()
        {
            if (_recipe.arms.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_arms[i] == null || _arms[i].isAvailable)
                {
                    if (_arms[i] == null)
                    {
                        CreateArm(i, i % _recipe.arms.Length);
                    }

                    return i;
                }
            }

            return -1;
        }

        // The rig reads the projectile itself, so no observer has to update before it
        void FollowProjectiles()
        {
            foreach (Delivery delivery in _deliveries.Values)
            {
                if (delivery.hasFreshContact)
                {
                    delivery.hasFreshContact = false;
                    continue;
                }

                if (delivery.projectile && !(delivery.style == DeliveryStyle.ChainSync && delivery.contact.HasValue))
                {
                    SetTipGoal(delivery.lease, delivery.projectile.position);
                }
            }
        }

        void CoalesceContact(Vector3 goal)
        {
            // Saturated same-position contacts renew an existing visual contact, without sharing
            // its lease token. A dropped observer can never end somebody else's chain.
            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] != 0 && _arms[i] != null && !_arms[i].isAvailable
                    && (_arms[i].goal - goal).sqrMagnitude < 0.000001f)
                {
                    _arms[i].Contact(_arms[i].token, goal);
                    return;
                }
            }
        }

        #region IDeliverySource

        public bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            if (token == 0 || style == DeliveryStyle.Thrown || _deliveries.ContainsKey(token))
            {
                return false;
            }

            if (style == DeliveryStyle.Swarm)
            {
                int count = 0;
                foreach (Delivery item in _deliveries.Values)
                {
                    if (item.style == style)
                    {
                        count++;
                    }
                }

                if (count >= 4)
                {
                    return false;
                }
            }

            int leaseToken = Begin(GestureKind.Attack, projectile ? projectile.position : intendedEnd);
            if (leaseToken == 0)
            {
                return false;
            }

            _deliveries.Add(token, new Delivery { lease = leaseToken, style = style, projectile = projectile });
            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] == leaseToken)
                {
                    _arms[i].style = style;
                    _arms[i].deliveryProfile = true;
                }
            }

            _charge = 0f;
            return true;
        }

        public void UpdateDelivery(int token, Vector3 position)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            if (delivery.style == DeliveryStyle.ChainSync && delivery.contact.HasValue)
            {
                return;
            }

            SetTipGoal(delivery.lease, position);
        }

        public void ContactDelivery(int token, Vector3 position, GameObject target)
        {
            ContactDeliveryPath(token, position, false);
        }

        public void EndDelivery(int token)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            End(delivery.lease);
            _deliveries.Remove(token);
        }

        #endregion
    }
}
