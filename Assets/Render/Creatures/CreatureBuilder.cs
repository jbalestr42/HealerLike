using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // The view of a spawned entity: derives or takes its recipe, reads the entity's skills, target and health
    public class CreatureBuilder : ARigHost, IEntityView
    {
        [SerializeField] CreatureRecipe _recipe;
        [SerializeField] Material _material;
        [SerializeField] Material _bodyMaterial;
        [SerializeField] PrimitiveMeshes _meshes;

        Entity _entity;
        readonly List<ASkill> _skills = new List<ASkill>();
        readonly Dictionary<ASkill, ICooldownSkill> _cooldowns = new Dictionary<ASkill, ICooldownSkill>();
        readonly List<ASkill> _removedSkills = new List<ASkill>();
        ResourceAttribute _health;
        StatusObserver _statusObserver;
        RenderRegistry _registry;
        ISpellVisualSink _spellSink;
        // The game's cell unless Configure hands another ground frame
        float _cellSize = StageCalibration.CellSize;
        bool _hasConfiguredPlane;
        Vector3 _groundOrigin;
        Vector3 _groundNormal = Vector3.up;

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
            HideRig();
        }

        void OnDestroy()
        {
            Detach();
            ReleaseRig();
        }

        public void Init(Entity owner, RenderManager manager)
        {
            if (!_meshes && manager)
            {
                _meshes = manager.meshes;
            }

            _registry = null;
            _spellSink = null;
            if (manager)
            {
                _registry = manager.registry;
                _spellSink = manager.spellSink;
            }

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
            if (_entity != owner)
            {
                CancelGestures();
            }

            _entity = owner;
            RefreshSkills();
            ObserveOutcomes();
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
            foreach (KeyValuePair<ASkill, ICooldownSkill> pair in _cooldowns)
            {
                if (!pair.Key || !pair.Key.isEnabled)
                {
                    continue;
                }

                float remaining = pair.Value.cooldownProgress;
                if (float.IsFinite(remaining))
                {
                    readiness = Mathf.Max(readiness, 1f - Mathf.Clamp01(remaining));
                }
            }

            if (rig != null)
            {
                float healthFraction = _health && _health.Max > 0 ? _health.Value / _health.Max : 1f;
                rig.SetReadout(target, healthFraction, readiness, readiness);
                TickRig(Time.time, Time.deltaTime, Frame());
            }
        }

        public void SetRecipe(CreatureRecipe value, Material sharedMaterial, PrimitiveMeshes meshes)
        {
            if (_recipe == value && _material == sharedMaterial && _meshes == meshes)
            {
                return;
            }

            ReleaseRig();
            _recipe = value;
            _material = sharedMaterial;
            _meshes = meshes;
        }

        public void Configure(RenderRegistry registry, float size, Vector3 origin, Vector3 normal)
        {
            if (!RenderMath.IsPositive(size) || !RenderMath.IsFinite(origin) || !RenderMath.IsFinite(normal)
                || normal.sqrMagnitude < 0.00000001f)
            {
                Debug.LogError("[CreatureBuilder] Invalid ground frame.");
                return;
            }

            Unregister();
            _registry = registry;
            ObserveOutcomes();
            if (_cellSize != size)
            {
                ReleaseRig();
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

        // The view prefab carries the status observer, which wires the outcome observers too
        void ObserveOutcomes()
        {
            if (_statusObserver == null)
            {
                _statusObserver = GetComponent<StatusObserver>();
            }

            if (_statusObserver != null)
            {
                _statusObserver.Init(_entity, _spellSink, _registry);
            }
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
                if (!_cooldowns.ContainsKey(skill) && skill is ICooldownSkill cooldownSkill)
                {
                    _cooldowns.Add(skill, cooldownSkill);
                }
            }
        }

        void EnsureRig()
        {
            if (rig == null && _recipe && _material)
            {
                BuildRig(_recipe, transform, _material, _bodyMaterial, _meshes, _cellSize);
            }

            if (rig != null)
            {
                rig.SetVisible(isActiveAndEnabled);
                TickRig(Time.time, 0f, Frame());
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

            Register(_registry, _entity.gameObject);
            if (rig != null)
            {
                rig.SetVisible(true);
            }
        }

        void Detach()
        {
            if (_health)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            }

            _health = null;
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

        #region IHealthVisualSink

        // Damage reaches this sink too, only a heal draws the contact
        public override void OnHealthResolved(GameObject target, float value, bool critical)
        {
            if (isActiveAndEnabled && target && value > 0f)
            {
                HealContact(target);
            }
        }

        #endregion
    }
}
