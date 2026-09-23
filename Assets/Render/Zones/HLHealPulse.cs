using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class HLHealPulse : MonoBehaviour, IVisualBehaviour, IHLHealVisualSink
    {
        [SerializeField, Min(0.001f)] float _cellSize = 1;
        GameObject _source;
        HLRenderRegistry _registry;
        public float CellSize { get => _cellSize; set => _cellSize = value; }

        public void Init(Entity entity) => Initialize(entity != null ? entity.gameObject : null);
        public void Initialize(GameObject source)
        {
            Unsubscribe();
            _source = source;
            if (isActiveAndEnabled) Subscribe();
        }
        void OnEnable() => Subscribe();
        void Update()
        {
            if (_registry != HLRenderRegistry.Current)
            {
                Unsubscribe();
                Subscribe();
            }
        }
        void Subscribe()
        {
            if (_source == null) return;
            _registry = HLRenderRegistry.Current;
            _registry?.Register(_source, this);
        }
        void Unsubscribe()
        {
            _registry?.Unregister(_source, this);
            _registry = null;
        }
        void OnDisable() => Unsubscribe();
        void OnDestroy() => Unsubscribe();

        public int Pulse(Transform target) => HLZoneRegistry.Current?.AddHealPulse(target, 0.6f * _cellSize) ?? 0;

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (!isActiveAndEnabled || target == null || !(value > 0) || float.IsInfinity(value)) return;
            Pulse(target.transform);
        }
    }
}
