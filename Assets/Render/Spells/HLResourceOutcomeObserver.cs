using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLResourceOutcomeObserver : MonoBehaviour, IVisualBehaviour
    {
        public static event Action<GameObject, GameObject, HLResourceKind, float, bool> Outcome;

        ResourceAttribute _health;
        ResourceAttribute _mana;
        HLRenderRegistry _injected;
        bool _hasInjection;

        public static HLResourceOutcomeObserver Ensure(
            Entity entity,
            HLRenderRegistry registry = null,
            bool inject = false
        )
        {
            if (!entity)
            {
                return null;
            }
            HLResourceOutcomeObserver observer = entity.GetComponent<HLResourceOutcomeObserver>();
            if (!observer)
            {
                observer = entity.gameObject.AddComponent<HLResourceOutcomeObserver>();
            }
            observer.Bind(entity.health, null, registry, inject);
            return observer;
        }

        public void Init(Entity entity)
        {
            if (entity)
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

        public void Bind(
            ResourceAttribute healthResource,
            ResourceAttribute manaResource,
            HLRenderRegistry registry = null,
            bool inject = false
        )
        {
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
            if (_health)
            {
                _health.OnAllConsumerProcessed.AddListener(OnHealth);
            }
            if (_mana && _mana != _health)
            {
                _mana.OnAllConsumerProcessed.AddListener(OnMana);
            }
        }

        void Unsubscribe()
        {
            if (_health)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnHealth);
            }
            if (_mana && _mana != _health)
            {
                _mana.OnAllConsumerProcessed.RemoveListener(OnMana);
            }
        }

        void OnHealth(GameObject owner, ResourceModifier modifier, float amount, bool critical)
        {
            Publish(owner, modifier, HLResourceKind.Health, amount, critical);
        }

        void OnMana(GameObject owner, ResourceModifier modifier, float amount, bool critical)
        {
            Publish(owner, modifier, HLResourceKind.Mana, amount, critical);
        }

        void Publish(GameObject owner, ResourceModifier modifier, HLResourceKind kind, float amount, bool critical)
        {
            if (!isActiveAndEnabled || !owner || amount == 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return;
            }
            HLRenderRegistry registry = _hasInjection ? _injected : HLRenderRegistry.Current;
            GameObject source = modifier != null ? modifier.source : null;
            registry?.SpellSink?.ShowImpact(source, owner, kind, amount, critical);
            if (kind == HLResourceKind.Health && amount > 0f)
            {
                registry?.NotifyHeal(source, owner, amount, critical);
            }
            Outcome?.Invoke(source, owner, kind, amount, critical);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvents()
        {
            Outcome = null;
        }
    }
}
