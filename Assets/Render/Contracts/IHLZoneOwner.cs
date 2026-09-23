using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{
    public interface IHLZoneOwner
    {
        // Returns 0 when the pulse is rejected
        int AddPulse(HLZoneKind kind, Vector3 center, float radius, float strength, float seconds);
    }
}
