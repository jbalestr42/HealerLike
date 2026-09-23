using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{
    public interface IZoneOwner
    {
        // Returns 0 when the pulse is rejected
        int AddPulse(ZoneKind kind, Vector3 center, float radius, float strength, float seconds);
    }
}
