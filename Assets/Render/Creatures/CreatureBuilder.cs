using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    public class CreatureBuilder : MonoBehaviour, IEntityView, IHealVisualSink,
        IDeliverySource, IEffectAnchors
    {
        [SerializeField] CreatureRecipe _recipe;
        [SerializeField] Material _material;
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] float _cellSize = 1f;

        Entity _entity;
        readonly List<ASkill> _skills = new List<ASkill>();
        readonly Dictionary<ASkill, Func<float>> _cooldowns = new Dictionary<ASkill, Func<float>>();
        readonly List<ASkill> _removedSkills = new List<ASkill>();
        ResourceAttribute _health;
        ResourceOutcomeObserver _outcomeObserver;
        GameObject _registeredSource;
        RenderRegistry _registeredRegistry;
        RenderRegistry _registry;
        bool _hasConfiguredPlane;
        Vector3 _groundOrigin;
        Vector3 _groundNormal = Vector3.up;

        CreatureRig _rig;
        public CreatureRig rig { get { return _rig; } }

        public CreatureRecipe recipe { get { return _recipe; } }

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

            _rig = null;
        }

        public void Init(Entity owner, RenderManager manager)
        {
            if (!_meshes && manager)
            {
                _meshes = manager.meshes;
            }

            _registry = manager ? manager.registry : null;

            // A view without an authored recipe draws the one derived from the entity's data
            if (!_recipe && owner && manager && manager.creatureLooks)
            {
                _recipe = manager.creatureLooks.GetRecipe(owner.data, owner.entityType);
            }

            Init(owner);
        }

        // Taking the entity alone lets tests run without the manager, Configure then hands the registry
        public void Init(Entity owner)
        {
            Detach();
            if (_entity != owner && rig != null)
            {
                rig.CancelAll();
            }

            _entity = owner;
            RefreshSkills();
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
                if (float.IsFinite(remaining))
                {
                    readiness = Mathf.Max(readiness, 1f - Mathf.Clamp01(remaining));
                }
            }

            if (rig != null)
            {
                float healthFraction = _health && _health.Max > 0 ? _health.Value / _health.Max : 1f;
                rig.SetReadout(target, healthFraction, readiness, readiness);
                rig.Tick(Time.time, Time.deltaTime, Frame());
            }
        }

        public void SetRecipe(CreatureRecipe value, Material sharedMaterial, PrimitiveMeshes meshes)
        {
            if (_recipe == value && _material == sharedMaterial && _meshes == meshes)
            {
                return;
            }

            if (rig != null)
            {
                rig.Dispose();
            }

            _rig = null;
            _recipe = value;
            _material = sharedMaterial;
            _meshes = meshes;
        }

        public void Configure(RenderRegistry registry, float size, Vector3 origin, Vector3 normal)
        {
            bool isOriginFinite = float.IsFinite(origin.x) && float.IsFinite(origin.y) && float.IsFinite(origin.z);
            bool isNormalFinite = float.IsFinite(normal.x) && float.IsFinite(normal.y) && float.IsFinite(normal.z);
            if (!float.IsFinite(size) || size <= 0f || !isOriginFinite || !isNormalFinite
                || normal.sqrMagnitude < 0.00000001f)
            {
                Debug.LogError("[CreatureBuilder] Invalid ground frame.");
                return;
            }

            Unregister();
            _registry = registry;
            if (_cellSize != size)
            {
                if (rig != null)
                {
                    rig.Dispose();
                }

                _rig = null;
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

                Func<float> progress = CreateCooldownReader(skill);
                if (progress != null)
                {
                    _cooldowns.Add(skill, progress);
                }
            }
        }

        // Reflection until ACooldownSkill exposes cooldownProgress through a non-generic ICooldownSkill
        static Func<float> CreateCooldownReader(ASkill skill)
        {
            for (Type type = skill.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ACooldownSkill<>))
                {
                    MethodInfo getter = type.GetProperty("cooldownProgress").GetGetMethod();
                    return (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), skill, getter);
                }
            }

            return null;
        }

        void EnsureRig()
        {
            if (rig == null && _recipe && _material)
            {
                CreatureRig created = new CreatureRig();
                if (created.Init(_recipe, transform, _material, _meshes, _cellSize))
                {
                    _rig = created;
                }
            }

            if (rig != null)
            {
                rig.SetVisible(isActiveAndEnabled);
                rig.Tick(Time.time, 0f, Frame());
            }
        }

        FootFrame Frame()
        {
            Vector3 origin = transform.position;
            Vector3 normal = transform.up;
            if (_hasConfiguredPlane)
            {
                origin -= _groundNormal * Vector3.Dot(origin - _groundOrigin, _groundNormal);
                normal = _groundNormal;
            }

            return new FootFrame(origin, normal, _cellSize);
        }

        void Attach()
        {
            if (!_entity || !isActiveAndEnabled)
            {
                return;
            }

            if (!_outcomeObserver)
            {
                _outcomeObserver = ResourceOutcomeObserver.Ensure(_entity.gameObject);
            }

            ISpellVisualSink spellSink = null;
            if (_registry != null)
            {
                spellSink = _registry.spellSink;
            }
            _outcomeObserver.Bind(_entity.health, null, spellSink, _registry);

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
            if (_registeredRegistry == _registry)
            {
                return;
            }

            Unregister();
            _registeredRegistry = _registry;
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
            if (!isActiveAndEnabled || value == 0f || !float.IsFinite(value))
            {
                return;
            }

            if (value < 0f && rig != null)
            {
                rig.Hit();
            }
        }

        #region IHealVisualSink

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (isActiveAndEnabled && target && value > 0f && rig != null)
            {
                rig.HealContact(TargetPosition(target));
            }
        }

        #endregion

        #region IDeliverySource

        public bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 end)
        {
            return isActiveAndEnabled && rig != null && rig.BeginDelivery(token, style, projectile, end);
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

        #region IEffectAnchors

        public bool TryGetAnchors(out EffectAnchors anchors)
        {
            if (rig == null)
            {
                anchors = new EffectAnchors();
                return false;
            }
            return rig.TryGetAnchors(out anchors);
        }

        #endregion
    }
}
