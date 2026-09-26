using UnityEngine;

namespace HealerLike.Render.Zones
{
    // One zone a view keeps in the registry: the first Refresh adds it, the next ones move it
    public class ZoneHandle
    {
        ZoneRegistry _zones;
        int _handle;

        public void Init(ZoneRegistry zones)
        {
            Clear();
            _zones = zones;
        }

        public void Refresh(ZoneKind kind, Vector3 position, float radius, float strength)
        {
            if (_zones == null)
            {
                return;
            }

            if (!_zones.Contains(_handle))
            {
                _handle = _zones.Add(kind, position, radius, strength);
            }
            else
            {
                _zones.UpdateZone(_handle, kind, position, radius, strength);
            }
        }

        // The same, carrying an XZ heading
        public void Refresh(ZoneKind kind, Vector3 position, float radius, float strength, Vector3 direction)
        {
            Refresh(kind, position, radius, strength);
            if (_zones != null)
            {
                _zones.SetDirection(_handle, direction);
            }
        }

        public void Clear()
        {
            if (_zones != null)
            {
                _zones.Remove(_handle);
            }
            _handle = 0;
        }
    }
}
