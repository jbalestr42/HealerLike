using UnityEngine;

namespace HealerLike.Render.Zones
{
    /// <summary>
    /// Cosmetic resolved-heal pulses, not persistent healing fields. Attach to each healer's model:
    /// IVisualBehaviour.Init registers this sink for that source with HLRenderRegistry.NotifyHeal.
    /// Pulses stay at the target's position at resolution, fade over 0.45 scaled seconds and expire
    /// independently of the source. Set CellSize from the stage grid (default one world unit).
    /// </summary>
    public sealed class HLHealPulse : MonoBehaviour, IVisualBehaviour, IHLHealVisualSink
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

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (!isActiveAndEnabled || target == null || !(value > 0) || float.IsInfinity(value)) return;
            HLZoneRegistry.Current?.AddPulse(HLZoneKind.Heal, target.transform.position, 0.6f * _cellSize, 1, 0.45f);
        }
    }
}
