using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Cosmetic footprint of an AoE, the radius is read in Start once the caller has set it
    [RequireComponent(typeof(AreaOfEffect))]
    public class AreaPulse : MonoBehaviour
    {
        [SerializeField] ZoneKind _kind = ZoneKind.Hostile;
        ZoneRegistry _zones;
        ZoneRegistry _owner;
        int _handle;

        public ZoneKind kind { get { return _kind; } set { _kind = value; } }

        public void Init(ZoneRegistry zones)
        {
            _zones = zones;
        }

        void Start()
        {
            AreaOfEffect area = GetComponent<AreaOfEffect>();
            _owner = _zones;
            if (_owner != null)
            {
                _handle = _owner.AddPulse(_kind, area.transform.position, area.radius, 1f, 0.8f);
            }
        }

        void OnDisable()
        {
            if (_owner != null)
            {
                _owner.Remove(_handle);
            }

            _handle = 0;
            _owner = null;
        }
    }
}
