using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Creatures
{
    /// <summary>Presentation at an authored anchor. Character.Init and Entity.Init are never invoked.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class HLCharacterView : MonoBehaviour, IHLHealVisualSink, IHLDeliverySource
    {
        [SerializeField] Character character;
        [SerializeField] HLCreatureRecipe recipe;
        [SerializeField] Transform visualAnchor;
        [SerializeField] Material material;
        [SerializeField] float cellSize = 1;
        HLRenderRegistry injectedRegistry, registeredRegistry;
        bool injected;
        GameObject registeredSource;
        readonly HashSet<ResourceAttribute> observed = new HashSet<ResourceAttribute>();
        readonly Dictionary<(GameObject, float, bool), int> resolvedHeals = new Dictionary<(GameObject, float, bool), int>();
        int resolvedFrame = -1;
        readonly List<GameObject> sceneRoots = new List<GameObject>(128);
        readonly List<ResourceAttribute> resources = new List<ResourceAttribute>(128);
        public IReadOnlyList<Transform> BudAnchors => Rig?.BudAnchors ?? System.Array.Empty<Transform>();
        public Transform Bud0 => BudAnchors.Count > 0 ? BudAnchors[0] : null;
        public Transform Bud1 => BudAnchors.Count > 1 ? BudAnchors[1] : null;
        public Transform Bud2 => BudAnchors.Count > 2 ? BudAnchors[2] : null;
        public int CastGestureCount { get; private set; }
        public bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 end) =>
            isActiveAndEnabled && Rig != null && Rig.BeginDelivery(token, style, projectile, end);
        public void UpdateDelivery(int token, Vector3 position) => Rig?.UpdateDelivery(token, position);
        public void ContactDelivery(int token, Vector3 position, GameObject target) => Rig?.ContactDelivery(token, position, target);
        public void EndDelivery(int token) => Rig?.EndDelivery(token);
        void ObserveResources()
        {
            if (!character || !isActiveAndEnabled) return;
            observed.RemoveWhere(r => !r);
            bool ownerSceneVisited = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                ObserveScene(scene);
                if (scene == character.gameObject.scene) ownerSceneVisited = true;
            }
            // The persistent scene is absent from SceneManager's ordinary scene list.
            if (!ownerSceneVisited) ObserveScene(character.gameObject.scene);
        }
        void ObserveScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (sceneRoots.Capacity <= scene.rootCount) sceneRoots.Capacity = scene.rootCount + 32;
            scene.GetRootGameObjects(sceneRoots);
            foreach (var sceneRoot in sceneRoots)
            {
                if (!sceneRoot.activeInHierarchy) continue;
                sceneRoot.GetComponentsInChildren(false, resources);
                foreach (var resource in resources)
                    if (observed.Add(resource)) resource.OnAllConsumerProcessed.AddListener(OnResourceProcessed);
            }
        }
        void OnResourceProcessed(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (!character || modifier?.source != character.gameObject || !HLChainSolver.Finite(value)) return;
            if (value > 0) ResolvedHeal(owner, value, critical, false);
            else Cast(owner);
        }
        void Cast(GameObject target)
        {
            if (!isActiveAndEnabled || !target || Rig == null) return;
            CastGestureCount++; Rig.HealContact(HLCreatureBuilder.TargetPosition(target));
        }
        void Update() => ObserveResources();
        void StopObserving()
        {
            foreach (var resource in observed) if (resource) resource.OnAllConsumerProcessed.RemoveListener(OnResourceProcessed);
            observed.Clear(); resolvedHeals.Clear();
        }
        public HLCreatureRig Rig { get; private set; }
        public void Bind(Character owner, HLCreatureRecipe data, Transform anchor, Material sharedMaterial, HLRenderRegistry registry, float size = 1)
        {
            StopObserving(); Unregister();
            bool changed = recipe != data || visualAnchor != anchor || material != sharedMaterial || cellSize != size;
            if (changed) { Rig?.Dispose(); Rig = null; }
            character = owner; recipe = data; visualAnchor = anchor; material = sharedMaterial; cellSize = size;
            injectedRegistry = registry; injected = true;
            BuildAndRegister(); ObserveResources();
        }
        void BuildAndRegister()
        {
            if (!character || !recipe || !visualAnchor || !material) return;
            if (Rig == null) Rig = HLCreatureRig.Build(recipe, visualAnchor, material, cellSize);
            Rig.SetVisible(isActiveAndEnabled);
            if (!isActiveAndEnabled) return;
            var registry = injected ? injectedRegistry : HLRenderRegistry.Current;
            if (registeredRegistry != registry) { Unregister(); registeredRegistry = registry; registeredSource = character.gameObject; registeredRegistry?.Register(registeredSource, this); }
        }
        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (value <= 0 || !target || !HLChainSolver.Finite(value)) return;
            ResolvedHeal(target, value, critical, true);
        }
        void ResolvedHeal(GameObject target, float value, bool critical, bool registry)
        {
            // Pair duplicate reports one-for-one, retaining multiple outcomes in one frame.
            if (resolvedFrame != Time.frameCount) { resolvedFrame = Time.frameCount; resolvedHeals.Clear(); }
            var key = (target, value, critical);
            resolvedHeals.TryGetValue(key, out int balance);
            if (registry ? balance >= 0 : balance <= 0) Cast(target);
            resolvedHeals[key] = balance + (registry ? 1 : -1);
        }
        void LateUpdate()
        {
            BuildAndRegister();
            if (Rig != null && character)
                Rig.SetReadout(null, 1, 0, character.mana && character.mana.Max > 0 ? character.mana.Value / character.mana.Max : 0);
            if (Rig != null && visualAnchor) Rig.Tick(Time.time, Time.deltaTime, new HLFootFrame(visualAnchor.position, visualAnchor.up, cellSize));
        }
        void Unregister() { registeredRegistry?.Unregister(registeredSource, this); registeredRegistry = null; registeredSource = null; }
        void OnEnable() { BuildAndRegister(); ObserveResources(); }
        void OnDisable() { StopObserving(); Unregister(); Rig?.CancelAll(); Rig?.SetVisible(false); }
        void OnDestroy() { StopObserving(); Unregister(); Rig?.Dispose(); Rig = null; }
    }
}
