using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{
    /// <summary>Which resource an outcome moved. Mirrors the gameplay AttributeType the sink cares about.</summary>
    public enum HLResourceKind : byte { Health, Mana }

    /// <summary>Which clock a status readout advances on.</summary>
    public enum HLClockKind : byte { Simulation, Realtime }

    /// <summary>
    /// The one seam the render layer exposes to spell outcomes. Everything here is cosmetic:
    /// an implementation never calls gameplay, never moves a projectile and never applies an effect.
    /// Feedback is outcome-based (ResourceAttribute.OnAllConsumerProcessed, Projectile.OnHit,
    /// BuffManager.OnBuffHandlerStarted/Stopped); there is deliberately no cast start, channel or end.
    /// </summary>
    public interface IHLSpellVisualSink
    {
        /// <summary>A resolved resource change on <paramref name="target"/>. The amount is the signed pre-clamp delta.</summary>
        void ShowImpact(GameObject source, GameObject target, HLResourceKind resource, float preClampAmount, bool isCritical);

        /// <summary>
        /// A status is present on <paramref name="target"/>. Keyed by (target, factory): repeated calls
        /// update stacks and timing rather than adding a second visual.
        /// </summary>
        void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks, float elapsedSeconds, float durationSeconds, HLClockKind clock);

        /// <summary>The status keyed by (target, factory) is gone.</summary>
        void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory);

        /// <summary>A transient cosmetic area pulse or preview. Radius is a radius, in world units.</summary>
        void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength);
    }

    /// <summary>
    /// A per-creature observer of resolved heals, registered with <see cref="HLRenderRegistry"/>
    /// by the healer's own visual behaviour. Cosmetic only.
    /// </summary>
    public interface IHLHealVisualSink
    {
        void OnHealResolved(GameObject target, float value, bool critical);
    }
}
