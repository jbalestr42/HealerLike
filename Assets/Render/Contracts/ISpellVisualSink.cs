using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{
    public enum ResourceKind
    {
        Health,
        Mana
    }

    public enum ClockKind
    {
        Simulation,
        Realtime
    }

    // Everything here is cosmetic: a sink never calls gameplay back
    public interface ISpellVisualSink
    {
        void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float preClampAmount,
                        bool isCritical);

        void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
                       float elapsedSeconds, float durationSeconds, ClockKind clock);

        void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory);

        void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength);
    }
}
