using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Creatures
{
    [DefaultExecutionOrder(200)]
    public class HLCreatureBuilder : MonoBehaviour, IVisualBehaviour, IHLHealVisualSink, IHLDeliverySource
    {
        [FormerlySerializedAs("recipe")]
        [SerializeField] HLCreatureRecipe _recipe;
        [FormerlySerializedAs("material")]
        [SerializeField] Material _material;
        [FormerlySerializedAs("cellSize")]
        [SerializeField] float _cellSize = 1f;

        Entity _entity;
        readonly List<ASkill> _skills = new List<ASkill>();
        readonly Dictionary<ASkill, Func<float>> _cooldowns = new Dictionary<ASkill, Func<float>>();
        readonly List<ASkill> _removedSkills = new List<ASkill>();
        ResourceAttribute _health;
        HLResourceOutcomeObserver _outcomeObserver;
        GameObject _registeredSource;
        HLRenderRegistry _registeredRegistry;
        HLRenderRegistry _injectedRegistry;
        bool _hasInjection;
        bool _hasConfiguredPlane;
        Vector3 _groundOrigin;
        Vector3 _groundNormal = Vector3.up;

        public HLCreatureRig rig { get; private set; }

        public HLCreatureRecipe recipe { get { return _recipe; } }

        public int cooldownSkillCount { get { return _cooldowns.Count; } }

        void OnEnable()
        {
            if (_entity)
            {
                EnsureRig();
                Attach();
            }
        }

        void OnDisable()
        {
            Detach();
            if (rig != null)
            {
                rig.CancelAll();
                rig.SetVisible(false);
            }
        }

        void OnDestroy()
        {
            Detach();
            if (rig != null)
            {
                rig.Dispose();
            }

            rig = null;
        }

        public void Init(Entity owner)
        {
            Detach();
            if (_entity != owner && rig != null)
            {
                rig.CancelAll();
            }

            _entity = owner;
            RefreshSkills();
            if (_entity)
            {
                _outcomeObserver = HLResourceOutcomeObserver.Ensure(_entity, _injectedRegistry, _hasInjection);
            }

            if (!_entity)
            {
                if (rig != null)
                {
                    rig.SetVisible(false);
                }

                return;
            }

            EnsureRig();
            if (isActiveAndEnabled)
            {
                Attach();
            }
        }

        void LateUpdate()
        {
            if (!_entity)
            {
                return;
            }

            Attach();
            RefreshSkills();
            TargetProvider provider = _entity.targetProvider;
            if (!provider)
            {
                provider = _entity.GetComponent<TargetProvider>();
            }

            List<GameObject> targets = provider ? provider.GetTargets() : null;
            Vector3? target = null;
            if (targets != null && targets.Count > 0 && targets[0])
            {
                target = targets[0].transform.position;
            }

            float readiness = 0f;
            foreach (KeyValuePair<ASkill, Func<float>> pair in _cooldowns)
            {
                if (!pair.Key || !pair.Key.isEnabled)
                {
                    continue;
                }

                float remaining = pair.Value();
                if (HLChainSolver.Finite(remaining))
                {
                    readiness = Mathf.Max(readiness, 1f - Mathf.Clamp01(remaining));
                }
            }

            if (rig != null)
            {
                rig.SetStatusTint(HLBodyTintState.Read(_entity.gameObject));
                float healthFraction = _health && _health.Max > 0 ? _health.Value / _health.Max : 1f;
                rig.SetReadout(target, healthFraction, readiness, readiness);
                rig.Tick(Time.time, Time.deltaTime, Frame());
            }
        }

        public void SetRecipe(HLCreatureRecipe value, Material sharedMaterial)
        {
            if (_recipe == value && _material == sharedMaterial)
            {
                return;
            }

            if (rig != null)
            {
                rig.Dispose();
            }

            rig = null;
            _recipe = value;
            _material = sharedMaterial;
        }

        public void Configure(HLRenderRegistry registry, float size, Vector3 origin, Vector3 normal)
        {
            if (!HLChainSolver.Finite(size) || size <= 0f || !HLChainSolver.Finite(origin)
                || !HLChainSolver.Finite(normal) || normal.sqrMagnitude < 0.00000001f)
            {
                throw new ArgumentException("Invalid ground frame.");
            }

            Unregister();
            _injectedRegistry = registry;
            _hasInjection = true;
            if (_cellSize != size)
            {
                if (rig != null)
                {
                    rig.Dispose();
                }

                rig = null;
            }

            _cellSize = size;
            _groundOrigin = origin;
            _groundNormal = normal.normalized;
            _hasConfiguredPlane = true;
            if (_entity)
            {
                EnsureRig();
                Attach();
            }
        }

        public static Vector3 TargetPosition(GameObject target)
        {
            if (!target)
            {
                return Vector3.zero;
            }

            Entity entity = target.GetComponent<Entity>();
            if (entity && entity.targetPoint)
            {
                return entity.targetPoint.transform.position;
            }

            SkillTargetPointTag tag = target.GetComponentInChildren<SkillTargetPointTag>();
            return tag ? tag.transform.position : target.transform.position;
        }

        void RefreshSkills()
        {
            if (!_entity)
            {
                _cooldowns.Clear();
                return;
            }

            _entity.GetComponents(_skills);
            _removedSkills.Clear();
            foreach (ASkill skill in _cooldowns.Keys)
            {
                if (!skill || !_skills.Contains(skill))
                {
                    _removedSkills.Add(skill);
                }
            }

            foreach (ASkill skill in _removedSkills)
            {
                _cooldowns.Remove(skill);
            }

            foreach (ASkill skill in _skills)
            {
                if (_cooldowns.ContainsKey(skill))
                {
                    continue;
                }

                // The runtime exposes a generic cooldown base, with no non-generic interface
                for (Type type = skill.GetType(); type != null; type = type.BaseType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ACooldownSkill<>))
                    {
                        MethodInfo getter = type.GetProperty("cooldownProgress").GetGetMethod();
                        Delegate progress = Delegate.CreateDelegate(typeof(Func<float>), skill, getter);
                        _cooldowns.Add(skill, (Func<float>)progress);
                        break;
                    }
                }
            }
        }

        void EnsureRig()
        {
            if (rig == null && _recipe && _material)
            {
                rig = HLCreatureRig.Build(_recipe, transform, _material, _cellSize);
            }

            if (rig != null)
            {
                rig.SetVisible(isActiveAndEnabled);
                rig.Tick(Time.time, 0f, Frame());
            }
        }

        HLFootFrame Frame()
        {
            Vector3 origin = transform.position;
            if (_hasConfiguredPlane)
            {
                origin -= _groundNormal * Vector3.Dot(origin - _groundOrigin, _groundNormal);
            }

            return new HLFootFrame(origin, _hasConfiguredPlane ? _groundNormal : transform.up, _cellSize);
        }

        void Attach()
        {
            if (!_entity || !isActiveAndEnabled)
            {
                return;
            }

            if (!_outcomeObserver)
            {
                _outcomeObserver = HLResourceOutcomeObserver.Ensure(_entity, _injectedRegistry, _hasInjection);
            }

            if (_health != _entity.health)
            {
                if (_health)
                {
                    _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
                }

                _health = _entity.health;
                if (_health)
                {
                    _health.OnAllConsumerProcessed.AddListener(OnHealthProcessed);
                }
            }

            if (_outcomeObserver)
            {
                _outcomeObserver.enabled = true;
            }

            SyncRegistry();
            if (rig != null)
            {
                rig.SetVisible(true);
            }
        }

        void SyncRegistry()
        {
            HLRenderRegistry registry = _hasInjection ? _injectedRegistry : HLRenderRegistry.current;
            if (_registeredRegistry == registry)
            {
                return;
            }

            Unregister();
            _registeredRegistry = registry;
            _registeredSource = _entity ? _entity.gameObject : null;
            if (_registeredRegistry != null)
            {
                _registeredRegistry.Register(_registeredSource, this);
            }
        }

        void Unregister()
        {
            if (_registeredRegistry != null)
            {
                _registeredRegistry.Unregister(_registeredSource, this);
            }

            _registeredRegistry = null;
            _registeredSource = null;
        }

        void Detach()
        {
            if (_health)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            }

            _health = null;
            if (_outcomeObserver)
            {
                _outcomeObserver.enabled = false;
            }

            _outcomeObserver = null;
            Unregister();
        }

        void OnHealthProcessed(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (!isActiveAndEnabled || value == 0f || !HLChainSolver.Finite(value))
            {
                return;
            }

            if (value < 0f && rig != null)
            {
                rig.Hit();
            }
        }

        #region IHLHealVisualSink

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (isActiveAndEnabled && target && value > 0f && rig != null)
            {
                rig.HealContact(TargetPosition(target));
            }
        }

        #endregion

        #region IHLDeliverySource

        public bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 end)
        {
            return isActiveAndEnabled && rig != null && rig.BeginDelivery(token, style, projectile, end);
        }

        public void UpdateDelivery(int token, Vector3 position)
        {
            if (rig != null)
            {
                rig.UpdateDelivery(token, position);
            }
        }

        public void ContactDelivery(int token, Vector3 position, GameObject target)
        {
            if (rig != null)
            {
                rig.ContactDelivery(token, position, target);
            }
        }

        public void EndDelivery(int token)
        {
            if (rig != null)
            {
                rig.EndDelivery(token);
            }
        }

        #endregion
    }
}
