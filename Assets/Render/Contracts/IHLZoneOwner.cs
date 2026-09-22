using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{
    /// <summary>
    /// Contract v2 (additive): the cosmetic zone owner as seen from the registry. A pulse fades
    /// linearly over <paramref name="seconds"/> of scaled time and removes itself. Returns a positive
    /// handle, or zero when the input is invalid or the owner is not publishing.
    /// </summary>
    public interface IHLZoneOwner
    {
        int AddPulse(HLZoneKind kind, Vector3 center, float radius, float strength, float seconds);
    }
}
