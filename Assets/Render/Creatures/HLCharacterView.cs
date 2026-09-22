using UnityEngine;

namespace HealerLike.Render.Creatures
{
    /// <summary>Presentation at an authored anchor. Character.Init and Entity.Init are never invoked.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class HLCharacterView : MonoBehaviour, IHLHealVisualSink
    {
        [SerializeField] Character character;
        [SerializeField] HLCreatureRecipe recipe;
        [SerializeField] Transform visualAnchor;
        [SerializeField] Material material;
        [SerializeField] float cellSize = 1;
        HLRenderRegistry injectedRegistry, registeredRegistry;
        bool injected;
        public HLCreatureRig Rig { get; private set; }
        public void Bind(Character owner, HLCreatureRecipe data, Transform anchor, Material sharedMaterial, HLRenderRegistry registry, float size = 1)
        {
            Unregister();
            bool changed = recipe != data || visualAnchor != anchor || material != sharedMaterial || cellSize != size;
            if (changed) { Rig?.Dispose(); Rig = null; }
            character = owner; recipe = data; visualAnchor = anchor; material = sharedMaterial; cellSize = size;
            injectedRegistry = registry; injected = true;
            BuildAndRegister();
        }
        void BuildAndRegister()
        {
            if (!character || !recipe || !visualAnchor || !material) return;
            if (Rig == null) Rig = HLCreatureRig.Build(recipe, visualAnchor, material, cellSize);
            Rig.SetVisible(isActiveAndEnabled);
            if (!isActiveAndEnabled) return;
            var registry = injected ? injectedRegistry : HLRenderRegistry.Current;
            if (registeredRegistry != registry) { Unregister(); registeredRegistry = registry; registeredRegistry?.Register(character.gameObject, this); }
        }
        public void OnHealResolved(GameObject target, float value, bool critical)
        { if (isActiveAndEnabled && target && value > 0) Rig?.HealContact(HLCreatureBuilder.TargetPosition(target)); }
        void LateUpdate()
        {
            BuildAndRegister();
            if (Rig != null && visualAnchor) Rig.Tick(Time.time, Time.deltaTime, new HLFootFrame(visualAnchor.position, visualAnchor.up, cellSize));
        }
        void Unregister() { registeredRegistry?.Unregister(character ? character.gameObject : null, this); registeredRegistry = null; }
        void OnEnable() => BuildAndRegister();
        void OnDisable() { Unregister(); Rig?.CancelAll(); Rig?.SetVisible(false); }
        void OnDestroy() { Unregister(); Rig?.Dispose(); Rig = null; }
    }
}
