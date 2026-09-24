using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // Turns every processed health or mana change of an entity into an impact on the sink, and every health
    // change into a notice to the registry so the source's view can gesture
    public class ResourceOutcomeObserver : MonoBehaviour, IEntityView
    {
        ResourceAttribute _health;
        ResourceAttribute _mana;
        ISpellVisualSink _sink;
        RenderRegistry _registry;

        public static ResourceOutcomeObserver Ensure(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            ResourceOutcomeObserver observer = owner.GetComponent<ResourceOutcomeObserver>();
            if (observer == null)
            {
                observer = owner.AddComponent<ResourceOutcomeObserver>();
            }
            return observer;
        }

        public void Init(Entity entity, RenderManager manager)
        {
            if (entity == null || manager == null)
            {
                Debug.LogError("[ResourceOutcomeObserver] Init needs an entity and the RenderManager.");
                return;
            }

            Bind(entity.health, null, manager.spellSink, manager.registry);
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

        public void Bind(ResourceAttribute healthResource, ResourceAttribute manaResource, ISpellVisualSink sink,
                         RenderRegistry registry)
        {
            _sink = sink;
            _registry = registry;
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
            Publish(owner, modifier, ResourceKind.Health, amount, isCritical);
        }

        void OnManaProcessed(GameObject owner, ResourceModifier modifier, float amount, bool isCritical)
        {
            Publish(owner, modifier, ResourceKind.Mana, amount, isCritical);
        }

        void Publish(GameObject owner, ResourceModifier modifier, ResourceKind kind, float amount, bool isCritical)
        {
            if (!isActiveAndEnabled || owner == null || amount == 0f || !float.IsFinite(amount))
            {
                return;
            }

            GameObject source = modifier != null ? modifier.source : null;
            if (_sink != null)
            {
                _sink.ShowImpact(source, owner, kind, amount, isCritical);
            }

            if (_registry != null && kind == ResourceKind.Health)
            {
                _registry.NotifyHealth(source, owner, amount, isCritical);
            }
        }
    }
}
