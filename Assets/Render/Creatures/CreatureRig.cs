using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Deliveries;

namespace HealerLike.Render.Creatures
{
    public class CreatureRig : IDisposable, IDeliverySource
    {
        public static readonly int MaxArms = 8;
        // Tips draw a wider outline, so a coral tip on a green body separates by an edge and not only by hue
        public static readonly float TipOutlineWidth = 1.5f;
        // How dark a tip gets on a dying unit, its hue kept
        public static readonly float WiltedTipValue = 0.45f;
        // How far a crown turns each second
        public static readonly float CrownSpinDegrees = 18f;
        // A hit pulses every part this much larger, and a charged head or tip swells this much more
        static readonly float hitSwell = 0.06f;
        static readonly float chargeSwell = 0.24f;
        // A dying unit leans this many degrees and sinks this many cells
        static readonly float wiltLean = 32f;
        static readonly float wiltSink = 0.08f;
        // A root thins to this share of its thickness at the foot, its joints are this many radii wide
        static readonly float rootTaper = 0.65f;
        static readonly float rootJointWidth = 2.8f;
        static readonly int outlineWidthId = Shader.PropertyToID("_HLOutlineWidthMultiplier");

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
        readonly MaterialPropertyBlock _tipBlock = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _ochreBlock = new MaterialPropertyBlock();
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
        bool[] _hasOchreFaces;
        Color[] _colours;
        IdleDefinition _idle;
        int _nextToken;
        bool _isDisposed;
        float _crownPulse;
        float _hitPulse;
        Vector3? _aimTarget;
        float _budPower;
        Vector3 _previousOrigin;
        bool _isPlaced;

        float _charge;
        public float charge { get { return _charge; } }

        float _healthFraction = 1f;
        public float healthFraction { get { return _healthFraction; } }

        Quaternion _aim = Quaternion.identity;
        public Quaternion aim { get { return _aim; } }

        Transform[] _budAnchors;
        public Transform[] budAnchors { get { return _budAnchors; } }

        public Transform root { get { return _root; } }

        public CreatureRecipe recipe { get { return _recipe; } }

        public float cellSize { get { return _cellSize; } }

        // One per recipe part, the transform carrying that part's mesh
        public IReadOnlyList<Transform> partTransforms
        {
            get
            {
                if (_geometry == null)
                {
                    return Array.Empty<Transform>();
                }
                return _geometry;
            }
        }

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

        public bool Init(CreatureRecipe data, Transform parent, Material material, PrimitiveMeshes meshes,
            float cellSize = 1f)
        {
            return Init(data, parent, material, material, meshes, cellSize);
        }

        // A recipe that fails validation logs and leaves the view empty. Body parts draw with the body material,
        // which shades a plant's body with its own threshold and tint, every other part with the shared one.
        public bool Init(CreatureRecipe data, Transform parent, Material material, Material bodyMaterial,
            PrimitiveMeshes meshes, float cellSize)
        {
            if (!CreatureValidator.TryValidate(data, out string error))
            {
                Debug.LogError($"[CreatureRig] {error}");
                return false;
            }

            if (!parent || !material || !meshes || !RenderMath.IsPositive(cellSize))
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
            if (bodyMaterial == null)
            {
                bodyMaterial = material;
            }
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
            _hasOchreFaces = new bool[data.parts.Length];
            _colours = new Color[data.parts.Length];
            _idle = data.idle;
            _idle.seed ^= parent.GetEntityId().GetHashCode();
            for (int i = 0; i < data.parts.Length; i++)
            {
                CreaturePart part = data.parts[i];
                // The accent stays the palette's own colour, only the body varies from unit to unit
                _colours[i] = part.role == PartRole.Tip ? part.colour : ColourJitter.Vary(part.colour, _idle.seed);
                _pivots[i] = new GameObject(part.id).transform;
                Transform pivotParent = _sway;
                if (part.parent >= 0)
                {
                    pivotParent = _pivots[part.parent];
                }
                _pivots[i].SetParent(pivotParent, false);
                _pivots[i].localPosition = part.localPosition * cellSize;
                _pivots[i].localRotation = Quaternion.Euler(part.localEuler);
                Mesh mesh = meshes.GetMesh(part.primitive, part.variant);
                Material partMaterial = material;
                if (part.role == PartRole.Body)
                {
                    partMaterial = bodyMaterial;
                }
                _geometry[i] = PrimitiveMeshes.Geometry("Geometry", _pivots[i], mesh, partMaterial, _colours[i],
                    part.glow);
                _geometry[i].localScale = part.dimensions * cellSize;
                _bodyRenderers[i] = _geometry[i].GetComponent<Renderer>();
                _hasOchreFaces[i] = mesh && mesh.subMeshCount > 1;
                if (_hasOchreFaces[i])
                {
                    _bodyRenderers[i].sharedMaterials = new Material[] { partMaterial, partMaterial };
                }

                Paint(i, _colours[i], part.glow);
            }

            List<Transform> buds = new List<Transform>();
            for (int i = 0; i < data.parts.Length; i++)
            {
                if (data.parts[i].role == PartRole.Tip)
                {
                    buds.Add(_pivots[i]);
                }
            }

            _budAnchors = buds.ToArray();
            int segments = data.roots.segments;
            _roots = new Transform[data.roots.count * segments];
            _rootJoints = new Transform[data.roots.count * (segments - 1)];
            Color rootColour = ColourJitter.Vary(data.roots.colour, _idle.seed);
            for (int i = 0; i < _roots.Length; i++)
            {
                _roots[i] = PrimitiveMeshes.Geometry("Root", _root, meshes.cylinder, material, rootColour);
            }

            // Lighter knuckles fill the bends between segments
            for (int i = 0; i < _rootJoints.Length; i++)
            {
                _rootJoints[i] = PrimitiveMeshes.Geometry("RootJoint", _root, meshes.sphere, material, rootColour,
                    0.35f);
            }

            for (int i = 0; i < data.arms.Length; i++)
            {
                CreateArm(i, i);
            }

            return true;
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
            Vector3? branchFrom = null;
            if (isBranch)
            {
                branchFrom = delivery.contact;
            }
            Contact(delivery.lease, position, branchFrom);
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
            _arms[slot].isDeliveryProfile = kind == GestureKind.Heal;
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
                _arms[slot].isDeliveryProfile = true;
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
                    _arms[i].End(_tokens[i]);
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
            Vector3 direction = new Vector3(Mathf.Sin(time * 0.3f) * 0.4f, 0f, 1f);
            if (_aimTarget.HasValue)
            {
                direction = _root.InverseTransformDirection(_aimTarget.Value - frame.origin);
            }
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f)
            {
                _aim = Quaternion.Slerp(_aim, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-dt * 7f));
            }

            _hitPulse = Mathf.Max(0f, _hitPulse - dt * 5f);
            IdlePose idlePose = IdleMotion.Evaluate(_idle, time);
            float shake = Mathf.Sin(_hitPulse * 24f) * _hitPulse * 9f;
            Quaternion wilt = Quaternion.Euler((1f - _healthFraction) * wiltLean, 0f, shake);
            _sway.localRotation = _aim * idlePose.sway * wilt;
            _sway.localPosition = Vector3.down * ((1f - _healthFraction) * wiltSink * _cellSize);
            _crownPulse = Mathf.Max(0f, _crownPulse - dt / 0.2f);
            float light = Mathf.Max(_budPower, _charge) * _healthFraction;
            for (int i = 0; i < _geometry.Length; i++)
            {
                CreaturePart part = _recipe.parts[i];
                bool isSwelling = part.role == PartRole.Head || part.role == PartRole.Tip;
                float swell = 1f + _crownPulse * hitSwell;
                if (isSwelling)
                {
                    swell += _charge * chargeSwell;
                }
                _geometry[i].localScale = Vector3.Scale(part.dimensions, idlePose.bodyScale) * _cellSize * swell;
                if (part.role == PartRole.Crown)
                {
                    Quaternion spin = Quaternion.AngleAxis(time * CrownSpinDegrees, Vector3.up);
                    _pivots[i].localRotation = Quaternion.Euler(part.localEuler) * spin;
                }

                if (part.role == PartRole.Tip)
                {
                    // The accent keeps its hue: charge brightens it, the wilt darkens it
                    float value = Mathf.Lerp(WiltedTipValue, 1f, _healthFraction);
                    Color tip = _colours[i];
                    Paint(i, new Color(tip.r * value, tip.g * value, tip.b * value, tip.a), part.glow * light);
                    continue;
                }

                Color wiltColour = _recipe.wiltColour;
                wiltColour.a = _colours[i].a;
                Paint(i, Color.Lerp(wiltColour, _colours[i], _healthFraction), part.glow * light);
            }

            RootDefinition roots = _recipe.roots;
            for (int i = 0; i < roots.count; i++)
            {
                float angle = i * Mathf.PI * 2f / roots.count;
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
                    float taper = Mathf.Lerp(1f, rootTaper, (float)k / Mathf.Max(1, roots.segments - 1));
                    float radius = roots.thickness * taper * _cellSize;
                    PrimitiveMeshes.Segment(_roots[i * roots.segments + k], start, end, radius);
                    if (k > 0)
                    {
                        Transform joint = _rootJoints[i * (roots.segments - 1) + k - 1];
                        joint.position = start;
                        joint.localScale = Vector3.one * (radius * rootJointWidth / _root.lossyScale.x);
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
                Vector3 shoulder = _pivots[definition.bodyPart].TransformPoint(definition.rootLocal * _cellSize);
                if (_branchRoots[i].HasValue)
                {
                    shoulder = _branchRoots[i].Value;
                }
                _arms[i].Tick(deltaTime, shoulder, _root.rotation * _sway.localRotation);
                if (_arms[i].isAvailable)
                {
                    _branchRoots[i] = null;
                    _tokens[i] = 0;
                    _arms[i].SetVisible(i < _recipe.arms.Length);
                }
            }
        }

        // The body, neck, head and foot as they stand now, read from the recipe's roles and the live transforms
        public bool TryGetAnchors(out EffectAnchors anchors)
        {
            anchors = new EffectAnchors();
            if (!_root || _geometry == null || _geometry.Length == 0)
            {
                return false;
            }

            int body = -1;
            int head = -1;
            for (int i = 0; i < _geometry.Length; i++)
            {
                PartRole role = _recipe.parts[i].role;
                if (role == PartRole.Body && body < 0)
                {
                    body = i;
                }

                bool isHead = role == PartRole.Head || role == PartRole.Tip;
                if (isHead && (head < 0 || _geometry[i].position.y > _geometry[head].position.y))
                {
                    head = i;
                }
            }

            Vector3 neck = _recipe.neckLocal;
            if (neck == Vector3.zero && _recipe.sourceLocal.Length > 0)
            {
                foreach (Vector3 source in _recipe.sourceLocal)
                {
                    neck += source;
                }
                neck /= _recipe.sourceLocal.Length;
            }

            Bounds bodyBounds = _bodyRenderers[Mathf.Max(0, body)].bounds;
            anchors.foot = _root.position;
            anchors.bodyCentre = bodyBounds.center;
            anchors.bodyRadius = Mathf.Max(bodyBounds.extents.x, bodyBounds.extents.z);
            anchors.neck = _pivots[0].TransformPoint((neck - _recipe.parts[0].localPosition) * _cellSize);
            if (head < 0)
            {
                anchors.headCentre = anchors.neck;
                anchors.headRadius = 0f;
                anchors.castPoint = anchors.neck;
                return true;
            }

            Bounds headBounds = _bodyRenderers[head].bounds;
            anchors.headCentre = headBounds.center;
            anchors.headRadius = Mathf.Max(headBounds.extents.x, Mathf.Max(headBounds.extents.y, headBounds.extents.z));
            // The tip of the top head
            anchors.castPoint = headBounds.center + Vector3.up * headBounds.extents.y;
            return true;
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
            RenderObjects.Release(_root.gameObject);
        }

        // A tip carries its wider outline, a stone's ochre faces take the recipe's ochre at the same brightness
        void Paint(int index, Color colour, float glow)
        {
            Color lit = PrimitiveMeshes.Brighten(colour, glow);
            bool isTip = _recipe.parts[index].role == PartRole.Tip;
            MaterialPropertyBlock block = isTip ? _tipBlock : _colourBlock;
            block.SetColor(RenderObjects.BaseColorId, lit);
            if (isTip)
            {
                block.SetFloat(outlineWidthId, TipOutlineWidth);
            }

            Renderer renderer = _bodyRenderers[index];
            if (!_hasOchreFaces[index])
            {
                renderer.SetPropertyBlock(block);
                return;
            }

            renderer.SetPropertyBlock(block, 0);
            float wilt = _healthFraction;
            Color ochre = Color.Lerp(_recipe.wiltColour, _recipe.stoneOchre, wilt);
            _ochreBlock.SetColor(RenderObjects.BaseColorId, PrimitiveMeshes.Brighten(ochre, glow));
            renderer.SetPropertyBlock(_ochreBlock, 1);
        }

        void CreateArm(int slot, int definitionIndex)
        {
            _definitions[slot] = definitionIndex;
            ArmDefinition definition = _recipe.arms[definitionIndex];
            definition.colour = ColourJitter.Vary(definition.colour, _idle.seed);
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

            // The tip first reaches for the shot itself, or where it is headed when there is none yet
            Vector3 goal = intendedEnd;
            if (projectile)
            {
                goal = projectile.position;
            }
            int leaseToken = Begin(GestureKind.Attack, goal);
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
                    _arms[i].isDeliveryProfile = true;
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
