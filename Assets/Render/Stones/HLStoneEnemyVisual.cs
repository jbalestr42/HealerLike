using System;
using System.Collections.Generic;
using System.Reflection;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    public class HLStoneEnemyVisual : MonoBehaviour, IEntityView, IHLDeliverySource
    {
        struct ImpactRecord
        {
            public HLStoneImpact impact;
            public int frame;
        }

        class Delivery
        {
            public Transform shard;
            public Transform projectile;
            public StoneMeshCache.Lease lease;
        }

        [FormerlySerializedAs("bodyPivot")]
        [SerializeField] Transform _bodyPivot;
        [SerializeField] Transform _presentation;
        [SerializeField] StoneGroundDisc _groundShadow;
        [FormerlySerializedAs("preset")]
        [SerializeField] HLStonePreset _preset;
        [FormerlySerializedAs("profile")]
        [SerializeField] HLStoneAssemblyProfile _profile;
        [FormerlySerializedAs("stoneMaterial")]
        [SerializeField] Material _stoneMaterial;
        [FormerlySerializedAs("seed")]
        [SerializeField] uint _seed = 1;
        [FormerlySerializedAs("effects")]
        [SerializeField] HLStoneEffects _effects;
        [FormerlySerializedAs("groundShadowEnabled")]
        [SerializeField] bool _groundShadowEnabled = true;
        [FormerlySerializedAs("directionToKeyLight")]
        [SerializeField] Vector3 _directionToKeyLight = new Vector3(-1f, 2f, -1f);

        LookAtTarget _bodyLookAtTarget;
        HLStoneLife _life;
        TargetProvider _targets;
        Entity _entity;
        ResourceAttribute _health;
        IHLStoneMotionSource _motion;
        StoneMeshCache _ownMeshes;
        float _settleAge;
        Vector3 _tricklePoint;
        bool _isBound;
        bool _isCollapsed;
        int _completedFrames;
        uint _hitIndex;

        readonly List<ASkill> _skills = new List<ASkill>();
        readonly Dictionary<ASkill, Func<float>> _cooldownReaders = new Dictionary<ASkill, Func<float>>();
        readonly Dictionary<int, Delivery> _deliveries = new Dictionary<int, Delivery>();
        readonly List<int> _endedDeliveries = new List<int>();
        readonly HLStoneAssembly _assembly = new HLStoneAssembly();
        readonly HLStoneHealthState _state = new HLStoneHealthState();
        readonly HLStoneMotionSampler _sampler = new HLStoneMotionSampler();
        readonly Dictionary<ResourceModifier, ImpactRecord> _impacts = new Dictionary<ResourceModifier, ImpactRecord>();
        readonly List<ResourceModifier> _expired = new List<ResourceModifier>();

        public int liveDeliveryCount { get { return _deliveries.Count; } }

        public bool groundShadowEnabled
        {
            get
            {
                return _groundShadowEnabled;
            }
            set
            {
                _groundShadowEnabled = value;
                if (_groundShadow != null)
                {
                    _groundShadow.Show(value && !_isCollapsed && isActiveAndEnabled);
                }
            }
        }

        public IReadOnlyList<HLStoneAssembly.Part> parts { get { return _assembly.parts; } }

        public Vector3 planarVelocity { get; private set; }

        public float groundY { get { return transform.position.y; } }

        public int pendingImpactCount { get { return _impacts.Count; } }

        // The view is added after EntityModel.Init, so its body turn is set up here
        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity, manager.stoneEffects);
            if (_bodyLookAtTarget != null)
            {
                _bodyLookAtTarget.Init(entity);
            }
        }

        public void Init(Entity owner, HLStoneEffects effects)
        {
            _entity = owner;
            _targets = owner.GetComponent<TargetProvider>();
            Init(owner.health, _seed, effects);
            foreach (MonoBehaviour component in owner.GetComponents<MonoBehaviour>())
            {
                if (component is IHLStoneMotionSource source)
                {
                    _motion = source;
                    break;
                }
            }
        }

        // Taking the resource directly lets tests run without Entity.Init or the game managers
        public void Init(ResourceAttribute resource, uint visualSeed, HLStoneEffects effects)
        {
            Unbind();
            _health = resource;
            _seed = visualSeed;
            _effects = effects;
            if (_bodyPivot == null || _presentation == null)
            {
                Debug.LogError("[HLStoneEnemyVisual] The model prefab needs its BodyPivot and presentation children.");
                return;
            }

            _bodyLookAtTarget = _bodyPivot.GetComponent<LookAtTarget>();
            _presentation.localRotation = Quaternion.identity;
            _settleAge = 0f;

            if (_effects != null)
            {
                _assembly.Init(_effects.stoneMeshes);
            }
            else
            {
                if (_ownMeshes == null)
                {
                    _ownMeshes = new StoneMeshCache();
                }
                _assembly.Init(_ownMeshes);
            }
            _assembly.BuildEnemy(_presentation, _seed, _preset, _stoneMaterial, _profile);
            if (_assembly.parts.Count == 0)
            {
                return;
            }

            Vector3[] vertices = _assembly.parts[0].lease.data.vertices;
            _tricklePoint = vertices[0];
            for (int i = 1; i < vertices.Length; i++)
            {
                if (vertices[i].x > _tricklePoint.x)
                {
                    _tricklePoint = vertices[i];
                }
            }

            if (_groundShadow != null)
            {
                _groundShadow.Init(_assembly.localBounds, _directionToKeyLight);
                _groundShadow.Show(_groundShadowEnabled && isActiveAndEnabled);
            }
            _state.Reset(_profile != null ? _profile.shedHealthFraction : 0.5f);
            _isCollapsed = false;
            _hitIndex = 0;
            _completedFrames = 0;

            if (_life == null)
            {
                _life = gameObject.AddComponent<HLStoneLife>();
            }
            _life.enabled = true;
            Transform cairnTop = null;
            if (_preset == HLStonePreset.Cairn)
            {
                cairnTop = _assembly.parts[_assembly.parts.Count - 1].transform;
            }
            _life.Init(_effects, null, _seed, _assembly.localBounds.extents.magnitude, false, cairnTop);

            _motion = null;
            _sampler.Reset();
            planarVelocity = Vector3.zero;
            Bind();
        }

        public void RecordImpact(ResourceModifier modifier, HLStoneImpact impact)
        {
            if (modifier == null || !_isBound || !isActiveAndEnabled)
            {
                return;
            }

            _impacts[modifier] = new ImpactRecord { impact = impact, frame = _completedFrames };
            if (_effects != null)
            {
                _effects.RecordImpact(impact.pointWS, HLStoneSeed.ForPart(_seed, ++_hitIndex + 100));
            }
        }

        public HLStoneImpact EstimateImpact(Vector3 queryWS, Vector3 incomingVelocityWS)
        {
            Vector3 point = transform.position + Vector3.up * 0.5f;
            Vector3 normal = Vector3.up;
            float best = float.PositiveInfinity;
            foreach (HLStoneAssembly.Part part in _assembly.parts)
            {
                if (!part.transform.gameObject.activeSelf)
                {
                    continue;
                }

                if (HLStoneImpactLocator.TryClosestPoint(part.lease.data, part.transform.localToWorldMatrix, queryWS,
                    out Vector3 partPoint, out Vector3 partNormal))
                {
                    float distance = (partPoint - queryWS).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        point = partPoint;
                        normal = partNormal;
                    }
                }
            }
            return new HLStoneImpact(point, normal, incomingVelocityWS, true);
        }

        void OnConsumersProcessed(GameObject owner, ResourceModifier modifier, float delta, bool critical)
        {
            bool isRecorded = modifier != null && _impacts.TryGetValue(modifier, out _);
            HLStoneImpact impact = isRecorded ? _impacts[modifier].impact : default;
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
                impact = EstimateImpact(query, Vector3.zero);
                if (_effects != null)
                {
                    _effects.RecordImpact(impact.pointWS, HLStoneSeed.ForPart(_seed, ++_hitIndex + 100));
                }
            }
            if (_effects != null)
            {
                _effects.EmitHit(impact, critical, HLStoneSeed.ForPart(_seed, ++_hitIndex + 100));
            }
        }

        void OnHealthChanged(ResourceAttribute resource)
        {
            // The callback is after clamp. Synchronous fallback survives Entity's earlier destruction callback.
            if (resource.Value <= 0f)
            {
                Collapse();
            }
        }

        public void CompleteHealthBatch()
        {
            if (_health == null)
            {
                return;
            }

            Color tint = Color.white;
            if (_entity)
            {
                tint = HLBodyTintState.Read(_entity.gameObject);
            }
            _assembly.ApplyFracture(_health.Max > 0f ? _health.Value / _health.Max : 1f, _seed, tint);

            HLStoneHealthAction action = _state.CompleteBatch(_health.Value, _health.Max);
            if (action == HLStoneHealthAction.Collapse)
            {
                Collapse();
            }
            else if (action == HLStoneHealthAction.ShedPart)
            {
                ShedPart();
            }
        }

        void ShedPart()
        {
            int index = 2;
            if (_profile != null)
            {
                index = _profile.detachablePartIndex;
            }
            else if (_preset == HLStonePreset.Monolith)
            {
                index = -1;
            }
            if (index < 0 || index >= _assembly.parts.Count)
            {
                return;
            }

            HLStoneAssembly.Part part = _assembly.parts[index];
            Material material = part.renderer.sharedMaterial;
            Matrix4x4 pose = part.transform.localToWorldMatrix;
            uint seed = HLStoneSeed.ForPart(_seed, 201);
            if (_effects != null)
            {
                _effects.EmitDetachedPart(part.lease.mesh, material, pose, planarVelocity, groundY, seed);
            }
            part.transform.gameObject.SetActive(false);
        }

        public void Collapse(HLStoneEffects owner = null)
        {
            if (owner != null)
            {
                _effects = owner;
            }

            if (_effects != null)
            {
                _effects.CollapseOnce(this, HLStoneSeed.ForPart(_seed, 301));
            }
            else if (TryBeginCollapse())
            {
                HideParts();
            }
        }

        public bool TryBeginCollapse()
        {
            if (_isCollapsed)
            {
                return false;
            }

            _isCollapsed = true;
            _state.TryBeginCollapse();
            return true;
        }

        public void HideParts()
        {
            if (_groundShadow != null)
            {
                _groundShadow.Show(false);
            }
            foreach (HLStoneAssembly.Part part in _assembly.parts)
            {
                part.transform.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            FollowDeliveries();
            if (!_isBound)
            {
                return;
            }

            CompleteHealthBatch();
            if (_life != null && _health != null && !_isCollapsed)
            {
                float fraction = _health.Max > 0f ? _health.Value / _health.Max : 1f;
                _life.PollHealth(fraction, Time.deltaTime, _assembly.parts[0].transform.TransformPoint(_tricklePoint));
            }

            Vector3 aim = Vector3.zero;
            List<GameObject> currentTargets = _targets != null ? _targets.GetTargets() : null;
            if (currentTargets != null && currentTargets.Count > 0 && currentTargets[0] != null)
            {
                aim = currentTargets[0].transform.position - transform.position;
            }
            float remaining = ReadCooldown();
            AdvancePresentation(aim, remaining, Time.deltaTime);

            Vector3 position = _entity != null ? _entity.transform.position : transform.position;
            planarVelocity = _sampler.Sample(position, Time.deltaTime, _entity != null && _entity.isDraggable);
            Quaternion facing = Quaternion.identity;
            Vector3 velocity = Vector3.zero;
            bool hasExplicitFacing = _motion != null && _motion.TrySample(out velocity, out facing);
            if (hasExplicitFacing && (_entity == null || !_entity.isDraggable))
            {
                velocity.y = 0f;
                planarVelocity = velocity.magnitude < 0.02f ? Vector3.zero : velocity;
            }
            if (planarVelocity != Vector3.zero && _bodyLookAtTarget == null)
            {
                if (hasExplicitFacing)
                {
                    _bodyPivot.rotation = Quaternion.Euler(0f, facing.eulerAngles.y, 0f);
                }
                else
                {
                    _bodyPivot.rotation = Quaternion.LookRotation(planarVelocity, Vector3.up);
                }
            }

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

        // Only public cooldownProgress on an ACooldownSkill<T> is polled. Components are
        // discovered after Init because Entity creates skills after it initializes its model.
        float ReadCooldown()
        {
            if (_entity == null || _entity.isDraggable)
            {
                return float.NaN;
            }

            _entity.GetComponents(_skills);
            float remaining = float.NaN;
            foreach (ASkill skill in _skills)
            {
                if (!skill.isEnabled)
                {
                    continue;
                }

                if (!_cooldownReaders.TryGetValue(skill, out Func<float> reader))
                {
                    reader = CreateCooldownReader(skill);
                    _cooldownReaders[skill] = reader;
                }
                if (reader == null)
                {
                    continue;
                }

                float value = reader();
                if (!float.IsFinite(value))
                {
                    continue;
                }

                if (float.IsNaN(remaining))
                {
                    remaining = Mathf.Clamp01(value);
                }
                else
                {
                    remaining = Mathf.Min(remaining, Mathf.Clamp01(value));
                }
            }
            return remaining;
        }

        static Func<float> CreateCooldownReader(ASkill skill)
        {
            for (Type parent = skill.GetType(); parent != null; parent = parent.BaseType)
            {
                if (parent.IsGenericType && parent.GetGenericTypeDefinition() == typeof(ACooldownSkill<>))
                {
                    PropertyInfo property = parent.GetProperty("cooldownProgress");
                    if (property == null)
                    {
                        return null;
                    }
                    return (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), skill, property.GetMethod);
                }
            }
            return null;
        }

        public void AdvancePresentation(Vector3 targetDirection, float remaining, float deltaTime)
        {
            if (_presentation == null || _isCollapsed)
            {
                return;
            }

            float dt = float.IsFinite(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;
            Quaternion desired = Quaternion.identity;
            if (_preset != HLStonePreset.Boulder)
            {
                // A single rigid settle, triggered by visual Init (spawn), never an idle loop.
                _settleAge += dt;
                float angle = 1.5f * Mathf.Sin(_settleAge * 5f) * Mathf.Exp(-_settleAge * 2f);
                desired = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            else
            {
                targetDirection.y = 0f;
                if (targetDirection.sqrMagnitude > 0.000001f)
                {
                    float anticipation = float.IsFinite(remaining) ? 1f - Mathf.Clamp01(remaining / 0.3f) : 0f;
                    Vector3 side = Vector3.Cross(Vector3.up, targetDirection.normalized);
                    Vector3 axis = _bodyPivot.InverseTransformDirection(side);
                    desired = Quaternion.AngleAxis(7f - 21f * anticipation, axis);
                }
            }
            float blend = 1f - Mathf.Exp(-8f * dt);
            _presentation.localRotation = Quaternion.Slerp(_presentation.localRotation, desired, blend);
        }

        #region IHLDeliverySource

        public bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            if (!isActiveAndEnabled || _isCollapsed || projectile == null || _presentation == null || _effects == null
                || _deliveries.ContainsKey(token))
            {
                return false;
            }
            if (style != HLDeliveryStyle.Thrown && style != HLDeliveryStyle.Direct && style != HLDeliveryStyle.Rigid)
            {
                return false;
            }

            HLStoneSettings shardShape = HLStonePresets.Shape(0.15f, 1.7f, 0.65f, 0.18f, 0);
            StoneMeshCache.Lease lease = _effects.stoneMeshes.Acquire(HLStoneSeed.ForPart(_seed, 701), shardShape);
            if (lease == null)
            {
                return false;
            }

            Transform shard = _effects.TakeShard(lease.mesh, HLStoneAssembly.Palette[1]);
            if (shard == null)
            {
                lease.Dispose();
                return false;
            }

            shard.position = _presentation.TransformPoint(_assembly.localBounds.center);
            shard.rotation = Quaternion.identity;
            Delivery delivery = new Delivery();
            delivery.shard = shard;
            delivery.projectile = projectile;
            delivery.lease = lease;
            _deliveries.Add(token, delivery);

            if (_preset == HLStonePreset.Boulder)
            {
                Vector3 direction = intendedEnd - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.000001f)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, direction.normalized);
                    Vector3 axis = _bodyPivot.InverseTransformDirection(side);
                    _presentation.localRotation = Quaternion.AngleAxis(18f, axis);
                }
            }
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
                _effects.EmitThrownContact(contactPosition, HLStoneSeed.ForPart(_seed, ++_hitIndex + 801));
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

        void ClearDeliveries()
        {
            _endedDeliveries.Clear();
            _endedDeliveries.AddRange(_deliveries.Keys);
            foreach (int token in _endedDeliveries)
            {
                EndDelivery(token);
            }
        }

        void Bind()
        {
            if (_isBound || _health == null || !isActiveAndEnabled)
            {
                return;
            }

            _health.OnAllConsumerProcessed.AddListener(OnConsumersProcessed);
            _health.OnValueChanged.AddListener(OnHealthChanged);
            _isBound = true;
        }

        void Unbind()
        {
            if (_isBound && _health != null)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnConsumersProcessed);
                _health.OnValueChanged.RemoveListener(OnHealthChanged);
            }
            if (_life != null)
            {
                _life.enabled = false;
            }
            _cooldownReaders.Clear();
            ClearDeliveries();
            _isBound = false;
            _impacts.Clear();
            _sampler.Reset();
            _state.CompleteBatch(float.PositiveInfinity, 1f);
        }

        void SetVisible(bool value)
        {
            if (_presentation != null)
            {
                _presentation.gameObject.SetActive(value);
            }
            if (_groundShadow != null)
            {
                _groundShadow.Show(value && _groundShadowEnabled && !_isCollapsed);
            }
        }

        void OnEnable()
        {
            SetVisible(true);
            if (_life != null)
            {
                _life.enabled = true;
            }
            Bind();
        }

        void OnDisable()
        {
            Unbind();
            SetVisible(false);
        }

        void OnDestroy()
        {
            Unbind();
            _assembly.Dispose();
            if (_ownMeshes != null)
            {
                _ownMeshes.Clear();
            }
        }
    }
}
