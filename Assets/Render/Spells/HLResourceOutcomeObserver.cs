using System;
using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // Turns every processed health or mana change of an entity into an impact on the sink
    public class HLResourceOutcomeObserver : MonoBehaviour, IVisualBehaviour, IEntityView
    {
        // removed in D2
        public static event Action<GameObject, GameObject, HLResourceKind, float, bool> Outcome;

        ResourceAttribute _health;
        ResourceAttribute _mana;
        IHLSpellVisualSink _sink;
        HLRenderRegistry _injected;
        bool _hasInjection = false;

        public static HLResourceOutcomeObserver Ensure(Entity entity, HLRenderRegistry registry = null,
                                                       bool inject = false)
        {
            if (entity == null)
            {
                return null;
            }

            HLResourceOutcomeObserver observer = entity.GetComponent<HLResourceOutcomeObserver>();
            if (observer == null)
            {
                observer = entity.gameObject.AddComponent<HLResourceOutcomeObserver>();
            }
            observer.Bind(entity.health, null, registry, inject);
            return observer;
        }

        public void Init(Entity entity, RenderManager manager)
        {
            if (entity == null || manager == null)
            {
                Debug.LogError("[HLResourceOutcomeObserver] Init needs an entity and the RenderManager.");
                return;
            }

            Bind(entity.health, null, manager.registry, true);
            _sink = manager.spellSink;
        }

        // Old path while the stage prefabs still walk IVisualBehaviour, the sink then comes from the registry
        public void Init(Entity entity)
        {
            if (entity != null)
            {
                Bind(entity.health, null);
            }
        }

        void OnEnable()
        {
            Unsubscribe();
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        public void Bind(ResourceAttribute healthResource, ResourceAttribute manaResource,
                         HLRenderRegistry registry = null, bool inject = false)
        {
            _sink = null;
            _injected = registry;
            _hasInjection = inject;
            if (_health == healthResource && _mana == manaResource)
            {
                return;
            }

            Unsubscribe();
            _health = healthResource;
            _mana = manaResource;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        void Subscribe()
        {
            if (_health != null)
            {
                _health.OnAllConsumerProcessed.AddListener(OnHealthProcessed);
            }

            if (_mana != null && _mana != _health)
            {
                _mana.OnAllConsumerProcessed.AddListener(OnManaProcessed);
            }
        }

        void Unsubscribe()
        {
            if (_health != null)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            }

            if (_mana != null && _mana != _health)
            {
                _mana.OnAllConsumerProcessed.RemoveListener(OnManaProcessed);
            }
        }

        void OnHealthProcessed(GameObject owner, ResourceModifier modifier, float amount, bool isCritical)
        {
            Publish(owner, modifier, HLResourceKind.Health, amount, isCritical);
        }

        void OnManaProcessed(GameObject owner, ResourceModifier modifier, float amount, bool isCritical)
        {
            Publish(owner, modifier, HLResourceKind.Mana, amount, isCritical);
        }

        void Publish(GameObject owner, ResourceModifier modifier, HLResourceKind kind, float amount, bool isCritical)
        {
            if (!isActiveAndEnabled || owner == null || amount == 0f || !float.IsFinite(amount))
            {
                return;
            }

            HLRenderRegistry registry = _hasInjection ? _injected : HLRenderRegistry.current;
            IHLSpellVisualSink sink = _sink;
            if (sink == null && registry != null)
            {
                sink = registry.spellSink;
            }

            GameObject source = modifier != null ? modifier.source : null;
            if (sink != null)
            {
                sink.ShowImpact(source, owner, kind, amount, isCritical);
            }

            if (registry != null && kind == HLResourceKind.Health && amount > 0f)
            {
                registry.NotifyHeal(source, owner, amount, isCritical);
            }

            if (Outcome != null)
            {
                Outcome(source, owner, kind, amount, isCritical);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvents()
        {
            Outcome = null;
        }
    }
}
