using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{

    public enum HLResourceKind : byte
    {
        Health,
        Mana
    }

    public enum HLClockKind : byte
    {
        Simulation,
        Realtime
    }

    public interface IHLSpellVisualSink
    {
        void ShowImpact(GameObject source, GameObject target, HLResourceKind resource, float preClampAmount, bool isCritical);

        void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks, float elapsedSeconds, float durationSeconds, HLClockKind clock);

        void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory);

        void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength);
    }

    public interface IHLHealVisualSink
    {
        void OnHealResolved(GameObject target, float value, bool critical);
    }
}
