using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Zones
{
    // Goes on each healer view, Init registers it for that healer's resolved heals
    public class HealPulse : MonoBehaviour, IEntityView, IHealVisualSink
    {
        // A heal pulse reaches this far, in cells
        static readonly float pulseCells = 0.6f;

        [SerializeField] float _cellSize = 1f;
        GameObject _source;
        RenderRegistry _registry;
        ZoneRegistry _zones;
        bool _isRegistered = false;

        public float cellSize { get { return _cellSize; } set { _cellSize = value; } }

        public void Init(Entity entity, RenderManager manager)
        {
            Init(entity.gameObject, manager.registry, manager.zones);
        }

        public void Init(GameObject source, RenderRegistry registry, ZoneRegistry zones)
        {
            Unsubscribe();
            _source = source;
            _registry = registry;
            _zones = zones;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        void OnEnable()
        {
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

        public int Pulse(Transform target)
        {
            if (_zones == null)
            {
                return 0;
            }

            return _zones.AddHealPulse(target, pulseCells * _cellSize);
        }

        #region IHealVisualSink

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (!isActiveAndEnabled || target == null || !RenderMath.IsPositive(value))
            {
                return;
            }

            Pulse(target.transform);
        }

        #endregion

        void Subscribe()
        {
            if (_isRegistered || _source == null || _registry == null)
            {
                return;
            }

            _registry.Register(_source, this);
            _isRegistered = true;
        }

        void Unsubscribe()
        {
            if (_isRegistered && _registry != null)
            {
                _registry.Unregister(_source, this);
            }

            _isRegistered = false;
        }
    }
}
