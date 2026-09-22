using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class HLCreatureBuilder : MonoBehaviour, IVisualBehaviour, IHLHealVisualSink
    {
        [SerializeField] HLCreatureRecipe recipe;
        [SerializeField] Material material;
        [SerializeField] float cellSize = 1;
        Entity entity;
        ResourceAttribute health;
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
            entity = owner;
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
            if (health != entity.health)
            {
                if (health) health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
                health = entity.health;
                if (health) health.OnAllConsumerProcessed.AddListener(OnHealthProcessed);
            }
            SyncRegistry(); Rig?.SetVisible(true);
        }
        void SyncRegistry()
        {
            var registry = hasInjection ? injectedRegistry : HLRenderRegistry.Current;
            if (registeredRegistry == registry) return;
            Unregister(); registeredRegistry = registry;
            registeredRegistry?.Register(entity ? entity.gameObject : null, this);
        }
        void Unregister() { registeredRegistry?.Unregister(entity ? entity.gameObject : null, this); registeredRegistry = null; }
        void Detach()
        {
            if (health) health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            health = null; Unregister();
        }
        void OnHealthProcessed(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (!isActiveAndEnabled || value == 0 || !HLChainSolver.Finite(value)) return;
            SyncRegistry();
            GameObject source = modifier?.source;
            var registry = hasInjection ? injectedRegistry : HLRenderRegistry.Current;
            registry?.SpellSink?.ShowImpact(source, owner, HLResourceKind.Health, value, critical);
            if (value > 0)
            {
                Rig?.EmitHeal(TargetPosition(owner), Time.time);
                registry?.NotifyHeal(source, owner, value, critical);
            }
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
            Attach(); Rig?.Tick(Time.time, Time.deltaTime, Frame());
        }
        void OnEnable() { if (entity) { EnsureRig(); Attach(); } }
        void OnDisable() { Detach(); Rig?.CancelAll(); Rig?.SetVisible(false); }
        void OnDestroy() { Detach(); Rig?.Dispose(); Rig = null; }
    }
}
