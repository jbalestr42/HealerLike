using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    /// <summary>One subscriber per resource owner. Outcomes retain explicit resource identity.</summary>
    [DisallowMultipleComponent]
    public sealed class HLResourceOutcomeObserver : MonoBehaviour, IVisualBehaviour
    {
        public static event Action<GameObject, GameObject, HLResourceKind, float, bool> Outcome;
        ResourceAttribute health, mana;
        HLRenderRegistry injected;
        bool hasInjection;
        public static HLResourceOutcomeObserver Ensure(Entity entity, HLRenderRegistry registry = null, bool inject = false)
        {
            if (!entity) return null;
            var observer = entity.GetComponent<HLResourceOutcomeObserver>();
            if (!observer) observer = entity.gameObject.AddComponent<HLResourceOutcomeObserver>();
            observer.Bind(entity.health, null, registry, inject);
            return observer;
        }
        public void Init(Entity entity) { if (entity) Bind(entity.health, null); }
        public void Bind(ResourceAttribute healthResource, ResourceAttribute manaResource, HLRenderRegistry registry = null, bool inject = false)
        {
            injected = registry; hasInjection = inject;
            if (health == healthResource && mana == manaResource) return;
            Unsubscribe(); health = healthResource; mana = manaResource;
            if (isActiveAndEnabled) Subscribe();
        }
        void Subscribe()
        {
            if (health) health.OnAllConsumerProcessed.AddListener(Health);
            if (mana && mana != health) mana.OnAllConsumerProcessed.AddListener(Mana);
        }
        void Unsubscribe()
        {
            if (health) health.OnAllConsumerProcessed.RemoveListener(Health);
            if (mana && mana != health) mana.OnAllConsumerProcessed.RemoveListener(Mana);
        }
        void Health(GameObject owner, ResourceModifier modifier, float amount, bool critical) => Publish(owner, modifier, HLResourceKind.Health, amount, critical);
        void Mana(GameObject owner, ResourceModifier modifier, float amount, bool critical) => Publish(owner, modifier, HLResourceKind.Mana, amount, critical);
        void Publish(GameObject owner, ResourceModifier modifier, HLResourceKind kind, float amount, bool critical)
        {
            if (!isActiveAndEnabled || !owner || amount == 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            var registry = hasInjection ? injected : HLRenderRegistry.Current;
            var source = modifier?.source;
            registry?.SpellSink?.ShowImpact(source, owner, kind, amount, critical);
            if (kind == HLResourceKind.Health && amount > 0) registry?.NotifyHeal(source, owner, amount, critical);
            Outcome?.Invoke(source, owner, kind, amount, critical);
        }
        void OnEnable() { Unsubscribe(); Subscribe(); }
        void OnDisable() => Unsubscribe();
        void OnDestroy() => Unsubscribe();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvents() => Outcome = null;
    }
}
