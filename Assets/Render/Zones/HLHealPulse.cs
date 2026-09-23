using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Goes on each healer view, Init registers it for that healer's resolved heals
    public class HLHealPulse : MonoBehaviour, IVisualBehaviour, IEntityView, IHLHealVisualSink
    {
        [SerializeField] float _cellSize = 1f;
        GameObject _source;
        HLRenderRegistry _injectedRegistry;
        HLRenderRegistry _registry;
        HLZoneRegistry _zones;
        bool _isInitialized = false;

        public float cellSize { get { return _cellSize; } set { _cellSize = value; } }

        public void Init(GameObject source, HLRenderRegistry registry, HLZoneRegistry zones)
        {
            Unsubscribe();
            _source = source;
            _injectedRegistry = registry;
            _zones = zones;
            _isInitialized = true;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity.gameObject, manager.registry, manager.zones);
        }

        // Called by EntityModel.Init on the staged model copies, removed in D2
        public void Init(Entity entity)
        {
            Initialize(entity != null ? entity.gameObject : null);
        }

        // Called by HLRenderBootstrap for the healer, removed in D2
        public void Initialize(GameObject source)
        {
            Unsubscribe();
            _source = source;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        void OnEnable()
        {
            Subscribe();
        }

        void Update()
        {
            // Follows the static registry until the RenderManager calls Init, removed in D2
            if (!_isInitialized && _registry != HLRenderRegistry.current)
            {
                Unsubscribe();
                Subscribe();
            }
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        public int Pulse(Transform target)
        {
            // Falls back to the static registry until the RenderManager calls Init, removed in D2
            HLZoneRegistry zones = _isInitialized ? _zones : HLZoneRegistry.current;
            if (zones == null)
            {
                return 0;
            }

            return zones.AddHealPulse(target, 0.6f * _cellSize);
        }

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (!isActiveAndEnabled || target == null || !(value > 0f) || float.IsInfinity(value))
            {
                return;
            }

            Pulse(target.transform);
        }

        void Subscribe()
        {
            if (_source == null)
            {
                return;
            }

            // Falls back to the static registry until the RenderManager calls Init, removed in D2
            _registry = _isInitialized ? _injectedRegistry : HLRenderRegistry.current;
            if (_registry != null)
            {
                _registry.Register(_source, this);
            }
        }

        void Unsubscribe()
        {
            if (_registry != null)
            {
                _registry.Unregister(_source, this);
            }

            _registry = null;
        }
    }
}
