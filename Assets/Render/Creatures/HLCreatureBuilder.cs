using UnityEngine;
using HealerLike.Render.Spells;
using System.Collections.Generic;
using System;

namespace HealerLike.Render.Creatures
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class HLCreatureBuilder : MonoBehaviour, IVisualBehaviour, IHLHealVisualSink, IHLDeliverySource
    {
        [SerializeField] HLCreatureRecipe recipe;
        [SerializeField] Material material;
        [SerializeField] float cellSize = 1;
        Entity entity;
        readonly List<ASkill> skills = new List<ASkill>();
        readonly Dictionary<ASkill, Func<float>> cooldowns = new Dictionary<ASkill, Func<float>>();
        readonly List<ASkill> removedSkills = new List<ASkill>();
        public int CooldownSkillCount => cooldowns.Count;
        void RefreshSkills()
        {
            if (!entity) { cooldowns.Clear(); return; }
            entity.GetComponents(skills);
            removedSkills.Clear();
            foreach (var skill in cooldowns.Keys) if (!skill || !skills.Contains(skill)) removedSkills.Add(skill);
            foreach (var skill in removedSkills) cooldowns.Remove(skill);
            foreach (var skill in skills)
            {
                if (cooldowns.ContainsKey(skill)) continue;
                // The runtime exposes a generic cooldown base, with no non-generic interface.
                for (Type type = skill.GetType(); type != null; type = type.BaseType)
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ACooldownSkill<>))
                    {
                        var getter = type.GetProperty("cooldownProgress").GetGetMethod();
                        cooldowns.Add(skill, (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), skill, getter));
                        break;
                    }
            }
        }
        public bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 end) =>
            isActiveAndEnabled && Rig != null && Rig.BeginDelivery(token, style, projectile, end);
        public void UpdateDelivery(int token, Vector3 position) => Rig?.UpdateDelivery(token, position);
        public void ContactDelivery(int token, Vector3 position, GameObject target) => Rig?.ContactDelivery(token, position, target);
        public void EndDelivery(int token) => Rig?.EndDelivery(token);
        ResourceAttribute health;
        HLResourceOutcomeObserver outcomeObserver;
        GameObject registeredSource;
        HLRenderRegistry registeredRegistry;
        HLRenderRegistry injectedRegistry;
        bool hasInjection, configuredPlane;
        Vector3 groundOrigin, groundNormal = Vector3.up;
        public HLCreatureRig Rig { get; private set; }
        public HLCreatureRecipe Recipe => recipe;
        public void SetRecipe(HLCreatureRecipe value, Material sharedMaterial)
        { if (recipe == value && material == sharedMaterial) return; Rig?.Dispose(); Rig = null; recipe = value; material = sharedMaterial; }
        public void Configure(HLRenderRegistry registry, float size, Vector3 origin, Vector3 normal)
        {
            if (!HLChainSolver.Finite(size) || size <= 0 || !HLChainSolver.Finite(origin) || !HLChainSolver.Finite(normal) || normal.sqrMagnitude < 1e-8f)
                throw new System.ArgumentException("Invalid ground frame.");
            Unregister(); injectedRegistry = registry; hasInjection = true;
            if (cellSize != size) { Rig?.Dispose(); Rig = null; }
            cellSize = size; groundOrigin = origin; groundNormal = normal.normalized; configuredPlane = true;
            if (entity) { EnsureRig(); Attach(); }
        }
        public void Init(Entity owner)
        {
            Detach();
            if (entity != owner) Rig?.CancelAll();
            entity = owner; RefreshSkills();
            if (entity) outcomeObserver = HLResourceOutcomeObserver.Ensure(entity, injectedRegistry, hasInjection);
            if (!entity) { Rig?.SetVisible(false); return; }
            EnsureRig();
            if (isActiveAndEnabled) Attach();
        }
        void EnsureRig()
        {
            if (Rig == null && recipe && material) Rig = HLCreatureRig.Build(recipe, transform, material, cellSize);
            if (Rig != null) { Rig.SetVisible(isActiveAndEnabled); Rig.Tick(Time.time, 0, Frame()); }
        }
        HLFootFrame Frame()
        {
            Vector3 origin = transform.position;
            if (configuredPlane) origin -= groundNormal * Vector3.Dot(origin - groundOrigin, groundNormal);
            return new HLFootFrame(origin, configuredPlane ? groundNormal : transform.up, cellSize);
        }
        void Attach()
        {
            if (!entity || !isActiveAndEnabled) return;
            if (!outcomeObserver) outcomeObserver = HLResourceOutcomeObserver.Ensure(entity, injectedRegistry, hasInjection);
            if (health != entity.health)
            {
                if (health) health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
                health = entity.health;
                if (health) health.OnAllConsumerProcessed.AddListener(OnHealthProcessed);
            }
            if (outcomeObserver) outcomeObserver.enabled = true;
            SyncRegistry(); Rig?.SetVisible(true);
        }
        void SyncRegistry()
        {
            var registry = hasInjection ? injectedRegistry : HLRenderRegistry.current;
            if (registeredRegistry == registry) return;
            Unregister(); registeredRegistry = registry;
            registeredSource = entity ? entity.gameObject : null;
            registeredRegistry?.Register(registeredSource, this);
        }
        void Unregister() { registeredRegistry?.Unregister(registeredSource, this); registeredRegistry = null; registeredSource = null; }
        void Detach()
        {
            if (health) health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            health = null;
            if (outcomeObserver) outcomeObserver.enabled = false;
            outcomeObserver = null;
            Unregister();
        }
        void OnHealthProcessed(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (!isActiveAndEnabled || value == 0 || !HLChainSolver.Finite(value)) return;
            if (value < 0) Rig?.Hit();

        }
        public void OnHealResolved(GameObject target, float value, bool critical)
        { if (isActiveAndEnabled && target && value > 0) Rig?.HealContact(TargetPosition(target)); }
        public static Vector3 TargetPosition(GameObject target)
        {
            if (!target) return Vector3.zero;
            var e = target.GetComponent<Entity>();
            if (e && e.targetPoint) return e.targetPoint.transform.position;
            var tag = target.GetComponentInChildren<SkillTargetPointTag>();
            return tag ? tag.transform.position : target.transform.position;
        }
        void LateUpdate()
        {
            if (!entity) return;
            Attach(); RefreshSkills();
            var provider = entity.targetProvider ? entity.targetProvider : entity.GetComponent<TargetProvider>();
            var targets = provider ? provider.GetTargets() : null;
            Vector3? target = targets != null && targets.Count > 0 && targets[0] ? targets[0].transform.position : (Vector3?)null;
            float readiness = 0;
            foreach (var pair in cooldowns)
            {
                if (!pair.Key || !pair.Key.isEnabled) continue;
                float remaining = pair.Value();
                if (HLChainSolver.Finite(remaining)) readiness = Mathf.Max(readiness, 1 - Mathf.Clamp01(remaining));
            }
            Rig?.SetStatusTint(HLBodyTintState.Read(entity.gameObject));
            Rig?.SetReadout(target, health && health.Max > 0 ? health.Value / health.Max : 1, readiness, readiness);
            Rig?.Tick(Time.time, Time.deltaTime, Frame());
        }
        void OnEnable() { if (entity) { EnsureRig(); Attach(); } }
        void OnDisable() { Detach(); Rig?.CancelAll(); Rig?.SetVisible(false); }
        void OnDestroy() { Detach(); Rig?.Dispose(); Rig = null; }
    }
}
