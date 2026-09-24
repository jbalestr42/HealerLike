using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // What makes a derived unit behave like a stone, on the parts the CreatureBuilder beside it built:
    // it chips where it is hit, sheds a limb when hurt, collapses on death and throws a shard for every shot
    public class StoneBody : MonoBehaviour, IEntityView, IDeliverySource
    {
        struct ImpactRecord
        {
            public StoneImpact impact;
            public int frame;
        }

        class Delivery
        {
            public Transform shard;
            public Transform projectile;
            public StoneMeshCache.Lease lease;
        }

        // Seed salts, one per kind of emission so two kinds never share a random stream
        static readonly uint hitSalt = 100;
        static readonly uint shedSalt = 201;
        static readonly uint collapseSalt = 301;
        static readonly uint shardMeshSalt = 701;
        static readonly uint contactSalt = 801;
        static readonly StoneSettings shardShape = StonePresets.Shape(0.15f, 1.7f, 0.65f, 0.18f, 0);

        [SerializeField] StoneGroundDisc _groundShadow;
        [SerializeField] float _shedHealthFraction = 0.5f;
        [SerializeField] LookPalette _palette;

        CreatureBuilder _builder;
        CreatureRig _rig;
        StoneEffects _effects;
        Entity _entity;
        ResourceAttribute _health;
        StoneMeshData[] _partMeshes = new StoneMeshData[0];
        uint _seed;
        uint _hitIndex;
        int _completedFrames;
        bool _isBound;

        readonly Dictionary<int, Delivery> _deliveries = new Dictionary<int, Delivery>();
        readonly List<int> _endedDeliveries = new List<int>();
        readonly StoneHealthState _state = new StoneHealthState();
        readonly StoneMotionSampler _sampler = new StoneMotionSampler();
        readonly Dictionary<ResourceModifier, ImpactRecord> _impacts = new Dictionary<ResourceModifier, ImpactRecord>();
        readonly List<ResourceModifier> _expired = new List<ResourceModifier>();

        public int liveDeliveryCount { get { return _deliveries.Count; } }

        public int pendingImpactCount { get { return _impacts.Count; } }

        bool _isCollapsed;
        public bool isCollapsed { get { return _isCollapsed; } }

        // The recipe part the stone lost when it was hurt, -1 while it is whole
        int _shedPart = -1;
        public int shedPart { get { return _shedPart; } }

        Vector3 _planarVelocity;
        public Vector3 planarVelocity { get { return _planarVelocity; } }

        public float groundY { get { return transform.position.y; } }

        public IReadOnlyList<Transform> parts
        {
            get
            {
                if (_rig == null)
                {
                    return System.Array.Empty<Transform>();
                }
                return _rig.partTransforms;
            }
        }

        public void Init(Entity entity, RenderManager manager)
        {
            StoneEffects effects = null;
            if (manager != null)
            {
                effects = manager.stoneEffects;
            }
            Init(entity, effects);
        }

        public void Init(Entity owner, StoneEffects effects)
        {
            _entity = owner;
            ResourceAttribute health = null;
            if (owner != null)
            {
                health = owner.health;
            }
            Init(health, (uint)transform.GetEntityId().GetHashCode(), effects);
        }

        // Taking the resource directly lets tests run without Entity.Init or the game managers
        public void Init(ResourceAttribute resource, uint visualSeed, StoneEffects effects)
        {
            Unsubscribe();
            _health = resource;
            _seed = visualSeed;
            _effects = effects;
            _builder = GetComponent<CreatureBuilder>();
            _rig = null;
            _partMeshes = new StoneMeshData[0];
            _state.Reset(_shedHealthFraction);
            _shedPart = -1;
            _isCollapsed = false;
            _hitIndex = 0;
            _completedFrames = 0;
            _sampler.Reset();
            _planarVelocity = Vector3.zero;
            FindRig();
            Subscribe();
        }

        // The builder makes its rig at Init and again when the ground frame changes,
        // so the rig is looked up every frame
        void FindRig()
        {
            CreatureRig rig = _builder != null ? _builder.rig : null;
            if (rig == _rig)
            {
                return;
            }

            _rig = rig;
            IReadOnlyList<Transform> partTransforms = parts;
            _partMeshes = new StoneMeshData[partTransforms.Count];
            for (int i = 0; i < partTransforms.Count; i++)
            {
                Mesh mesh = partTransforms[i].GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null)
                {
                    _partMeshes[i] = new StoneMeshData(mesh.vertices, mesh.normals, mesh.triangles, mesh.bounds);
                }
            }

            if (_isCollapsed)
            {
                HideParts();
                return;
            }

            // A body set up again on the same rig stands whole, a rebuilt rig keeps the limb it lost
            for (int i = 0; i < partTransforms.Count; i++)
            {
                partTransforms[i].gameObject.SetActive(i != _shedPart);
            }

            if (_groundShadow != null && partTransforms.Count > 0)
            {
                _groundShadow.Init(LocalBounds(partTransforms), StageKeyLight.KeyDirection);
                _groundShadow.Show(isActiveAndEnabled);
            }
        }

        // The parts' box in this view's space, which the ground shadow stretches away from the key light
        Bounds LocalBounds(IReadOnlyList<Transform> partTransforms)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool isEmpty = true;
            foreach (Transform part in partTransforms)
            {
                Bounds world = part.GetComponent<Renderer>().bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3(CornerSign(corner, 1), CornerSign(corner, 2), CornerSign(corner, 4));
                    Vector3 point = transform.InverseTransformPoint(world.center + Vector3.Scale(world.extents, sign));
                    if (isEmpty)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        isEmpty = false;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
            return bounds;
        }

        // -1 or 1 along one axis of a box corner, the axis picked by its bit
        static float CornerSign(int corner, int bit)
        {
            if ((corner & bit) == 0)
            {
                return -1f;
            }
            return 1f;
        }

        public void RecordImpact(ResourceModifier modifier, StoneImpact impact)
        {
            if (modifier == null || !_isBound || !isActiveAndEnabled)
            {
                return;
            }

            _impacts[modifier] = new ImpactRecord { impact = impact, frame = _completedFrames };
            if (_effects != null)
            {
                _effects.RecordImpact(impact.point, StoneSeed.ForPart(_seed, ++_hitIndex + hitSalt));
            }
        }

        public StoneImpact EstimateImpact(Vector3 query)
        {
            Vector3 point = transform.position + Vector3.up * 0.5f;
            Vector3 normal = Vector3.up;
            float best = float.PositiveInfinity;
            IReadOnlyList<Transform> partTransforms = parts;
            for (int i = 0; i < partTransforms.Count && i < _partMeshes.Length; i++)
            {
                if (!partTransforms[i].gameObject.activeSelf)
                {
                    continue;
                }

                if (StoneImpactLocator.TryClosestPoint(_partMeshes[i], partTransforms[i].localToWorldMatrix, query,
                    out Vector3 partPoint, out Vector3 partNormal))
                {
                    float distance = (partPoint - query).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        point = partPoint;
                        normal = partNormal;
                    }
                }
            }
            return new StoneImpact(point, normal);
        }

        void OnConsumersProcessed(GameObject owner, ResourceModifier modifier, float delta, bool critical)
        {
            bool isRecorded = modifier != null && _impacts.ContainsKey(modifier);
            StoneImpact impact = isRecorded ? _impacts[modifier].impact : default;
            if (modifier != null)
            {
                _impacts.Remove(modifier);
            }

            _state.RecordProcessedDelta(delta);
            if (!(delta < 0f) || _isCollapsed)
            {
                return;
            }

            if (!isRecorded)
            {
                Vector3 query = transform.position + Vector3.up * 2f;
                if (modifier != null && modifier.source != null)
                {
                    query = modifier.source.transform.position;
                }
                impact = EstimateImpact(query);
                if (_effects != null)
                {
                    _effects.RecordImpact(impact.point, StoneSeed.ForPart(_seed, ++_hitIndex + hitSalt));
                }
            }

            if (_effects != null)
            {
                _effects.EmitHit(impact, critical, StoneSeed.ForPart(_seed, ++_hitIndex + hitSalt));
            }
        }

        void OnHealthChanged(ResourceAttribute resource)
        {
            // The callback comes after the clamp, and before the entity is destroyed
            if (resource.Value <= 0f)
            {
                Collapse(null);
            }
        }

        public void CompleteHealthBatch()
        {
            if (_health == null)
            {
                return;
            }

            StoneHealthAction action = _state.CompleteBatch(_health.Value, _health.Max);
            if (action == StoneHealthAction.Collapse)
            {
                Collapse(null);
            }
            else if (action == StoneHealthAction.ShedPart)
            {
                ShedPart();
            }
        }

        // One limb or accessory, picked by the seed, falls and splits
        void ShedPart()
        {
            if (_rig == null || _rig.recipe == null)
            {
                return;
            }

            IReadOnlyList<Transform> partTransforms = _rig.partTransforms;
            List<int> candidates = new List<int>();
            for (int i = 0; i < partTransforms.Count; i++)
            {
                PartRole role = _rig.recipe.parts[i].role;
                if ((role == PartRole.Limb || role == PartRole.Accessory) && partTransforms[i].gameObject.activeSelf)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            _shedPart = candidates[(int)(_seed % (uint)candidates.Count)];
            Transform part = partTransforms[_shedPart];
            if (_effects != null)
            {
                Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
                Material material = part.GetComponent<Renderer>().sharedMaterial;
                _effects.EmitDetachedPart(mesh, material, part.localToWorldMatrix, planarVelocity, groundY,
                    StoneSeed.ForPart(_seed, shedSalt));
            }
            part.gameObject.SetActive(false);
        }

        // Every part breaks into debris at once; effects handed by the death bridge replace missing ones
        public void Collapse(StoneEffects effects)
        {
            if (effects != null)
            {
                _effects = effects;
            }

            if (_isCollapsed)
            {
                return;
            }

            _isCollapsed = true;
            _state.TryBeginCollapse();
            ClearDeliveries();
            if (_effects != null)
            {
                _effects.CollapseParts(parts, planarVelocity, groundY, StoneSeed.ForPart(_seed, collapseSalt));
            }
            HideParts();
        }

        void HideParts()
        {
            if (_groundShadow != null)
            {
                _groundShadow.Show(false);
            }

            foreach (Transform part in parts)
            {
                part.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            FindRig();
            FollowDeliveries();
            if (!_isBound)
            {
                return;
            }

            CompleteHealthBatch();
            Vector3 position = _entity != null ? _entity.transform.position : transform.position;
            _planarVelocity = _sampler.Sample(position, Time.deltaTime, _entity != null && _entity.isDraggable);
            if (_groundShadow != null && !_isCollapsed)
            {
                _groundShadow.Refresh();
            }

            _completedFrames++;
            _expired.Clear();
            foreach (KeyValuePair<ResourceModifier, ImpactRecord> pair in _impacts)
            {
                if (_completedFrames - pair.Value.frame >= 2)
                {
                    _expired.Add(pair.Key);
                }
            }

            foreach (ResourceModifier key in _expired)
            {
                _impacts.Remove(key);
            }
        }

        // The highest head throws, or the highest part of a stone that has no head
        Transform ThrowingPart()
        {
            if (_rig == null)
            {
                return null;
            }

            IReadOnlyList<Transform> partTransforms = _rig.partTransforms;
            Transform best = null;
            bool isBestHead = false;
            for (int i = 0; i < partTransforms.Count; i++)
            {
                Transform part = partTransforms[i];
                if (!part.gameObject.activeInHierarchy)
                {
                    continue;
                }

                bool isHead = _rig.recipe.parts[i].role == PartRole.Head;
                bool isHigher = best == null || part.position.y > best.position.y;
                if ((isHead && !isBestHead) || (isHead == isBestHead && isHigher))
                {
                    best = part;
                    isBestHead = isHead;
                }
            }
            return best;
        }

        #region IDeliverySource

        // The armless stone rig refuses every shot, so the stone claims them all
        public bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            if (!isActiveAndEnabled || _isCollapsed || projectile == null || _effects == null
                || _deliveries.ContainsKey(token))
            {
                return false;
            }

            Transform thrower = ThrowingPart();
            if (thrower == null)
            {
                return false;
            }

            uint shardSeed = StoneSeed.ForPart(_seed, shardMeshSalt);
            StoneMeshCache.Lease lease = _effects.stoneMeshes.Acquire(shardSeed, shardShape);
            if (lease == null)
            {
                return false;
            }

            Transform shard = _effects.TakeShard(lease.mesh, ShardColour());
            if (shard == null)
            {
                lease.Dispose();
                return false;
            }

            shard.position = thrower.GetComponent<Renderer>().bounds.center;
            shard.rotation = Quaternion.identity;
            Delivery delivery = new Delivery();
            delivery.shard = shard;
            delivery.projectile = projectile;
            delivery.lease = lease;
            _deliveries.Add(token, delivery);
            return true;
        }

        public void UpdateDelivery(int token, Vector3 projectilePosition)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            Vector3 travel = projectilePosition - delivery.shard.position;
            if (travel.sqrMagnitude > 0.00000001f)
            {
                delivery.shard.rotation = Quaternion.FromToRotation(Vector3.up, travel.normalized);
            }
            delivery.shard.position = projectilePosition;
        }

        public void ContactDelivery(int token, Vector3 contactPosition, GameObject target)
        {
            if (!_deliveries.ContainsKey(token))
            {
                return;
            }

            if (_effects != null)
            {
                _effects.EmitThrownContact(contactPosition, StoneSeed.ForPart(_seed, ++_hitIndex + contactSalt));
            }
            EndDelivery(token);
        }

        public void EndDelivery(int token)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            _deliveries.Remove(token);
            if (_effects != null)
            {
                _effects.ReturnShard(delivery.shard);
            }
            delivery.lease.Dispose();
        }

        #endregion

        void FollowDeliveries()
        {
            _endedDeliveries.Clear();
            foreach (KeyValuePair<int, Delivery> pair in _deliveries)
            {
                Transform projectile = pair.Value.projectile;
                if (projectile == null || !projectile.gameObject.activeInHierarchy)
                {
                    _endedDeliveries.Add(pair.Key);
                }
                else
                {
                    UpdateDelivery(pair.Key, projectile.position);
                }
            }

            foreach (int token in _endedDeliveries)
            {
                EndDelivery(token);
            }
        }

        // A thrown shard is a piece of the stone's body
        Color ShardColour()
        {
            if (_palette == null)
            {
                Debug.LogError("[StoneBody] No palette.");
                return Color.magenta;
            }
            return _palette.Colour(ColourRole.Body, EffectFamily.Damage, LookSide.Stone);
        }

        void ClearDeliveries()
        {
            _endedDeliveries.Clear();
            _endedDeliveries.AddRange(_deliveries.Keys);
            foreach (int token in _endedDeliveries)
            {
                EndDelivery(token);
            }
        }

        void Subscribe()
        {
            if (_isBound || _health == null || !isActiveAndEnabled)
            {
                return;
            }

            _health.OnAllConsumerProcessed.AddListener(OnConsumersProcessed);
            _health.OnValueChanged.AddListener(OnHealthChanged);
            _isBound = true;
        }

        void Unsubscribe()
        {
            if (_isBound && _health != null)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnConsumersProcessed);
                _health.OnValueChanged.RemoveListener(OnHealthChanged);
            }

            ClearDeliveries();
            _isBound = false;
            _impacts.Clear();
            _sampler.Reset();
            _state.CompleteBatch(float.PositiveInfinity, 1f);
        }

        void OnEnable()
        {
            if (_groundShadow != null && _rig != null)
            {
                _groundShadow.Show(!_isCollapsed);
            }
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            if (_groundShadow != null)
            {
                _groundShadow.Show(false);
            }
        }

        void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
