using UnityEngine;

namespace HealerLike.Render.Zones
{
    /// <summary>
    /// Cosmetic one-shot AoE footprint, not an authoritative or persistent damage/heal field.
    /// Stage attaches this to the AoE prefab variant; Start reads radius after caller initialization.
    /// </summary>
    [RequireComponent(typeof(AreaOfEffect))]
    public sealed class HLAreaPulse : MonoBehaviour
    {
        [SerializeField] HLZoneKind _kind = HLZoneKind.Hostile;
        HLZoneRegistry _owner;
        int _handle;
        public HLZoneKind Kind { get => _kind; set => _kind = value; }
        void Start()
        {
            var area = GetComponent<AreaOfEffect>();
            _owner = HLZoneRegistry.Current;
            if (_owner != null)
                _handle = _owner.AddPulse(_kind, area.transform.position, area.radius, 1, 0.8f);
        }
        void OnDisable()
        {
            if (_owner != null) _owner.Remove(_handle);
            _handle = 0;
            _owner = null;
        }
    }
}
