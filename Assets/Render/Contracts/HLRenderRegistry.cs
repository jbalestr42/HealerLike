using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render
{
    /// <summary>
    /// The render layer's wiring point: a plain class, deliberately never a Singleton&lt;T&gt;, so it
    /// creates no DontDestroyOnLoad object and stays constructible in a test. The stage bootstrap
    /// assigns <see cref="Current"/> on enable and clears it on disable; every caller goes through
    /// a null check (HLRenderRegistry.Current?.Something), and each member here is itself null-safe,
    /// so a scene without the render layer behaves as if nothing were listening.
    /// </summary>
    public class HLRenderRegistry
    {
        /// <summary>
        /// The registry the stage bootstrap published, or null when the render layer is absent.
        /// Owned by the bootstrap: nothing else assigns it during play.
        /// </summary>
        public static HLRenderRegistry Current { get; set; }

        /// <summary>The registered spell visual sink, or null. Setting null silences spell visuals.</summary>
        public IHLSpellVisualSink SpellSink { get; set; }

        /// <summary>
        /// Contract v2: the cosmetic zone owner, or null. Setting null silences area pulses routed
        /// through the registry. The stage bootstrap assigns it alongside <see cref="SpellSink"/>.
        /// </summary>
        public IHLZoneOwner ZoneOwner { get; set; }

        readonly Dictionary<GameObject, List<IHLHealVisualSink>> _healSinks =
            new Dictionary<GameObject, List<IHLHealVisualSink>>();

        /// <summary>
        /// Register a heal observer for the healer <paramref name="source"/>. Null arguments are
        /// ignored, and registering the same sink twice for the same source is a no-op, so a
        /// component that re-inits does not double its visuals.
        /// </summary>
        public void Register(GameObject source, IHLHealVisualSink sink)
        {
            if (source == null || sink == null) return;

            if (!_healSinks.TryGetValue(source, out List<IHLHealVisualSink> sinks))
            {
                sinks = new List<IHLHealVisualSink>(1);
                _healSinks.Add(source, sinks);
            }

            if (!sinks.Contains(sink)) sinks.Add(sink);
        }

        /// <summary>
        /// Drop a heal observer. Unknown source/sink pairs and null arguments are ignored; the
        /// source entry disappears once its last sink leaves.
        /// </summary>
        public void Unregister(GameObject source, IHLHealVisualSink sink)
        {
            if (source == null || sink == null) return;
            if (!_healSinks.TryGetValue(source, out List<IHLHealVisualSink> sinks)) return;

            sinks.Remove(sink);
            if (sinks.Count == 0) _healSinks.Remove(source);
        }

        /// <summary>
        /// Fan a resolved heal out to the observers registered for <paramref name="source"/>.
        /// A source with no observers is a no-op. Sinks are visited newest first, and one sink
        /// throwing is logged and does not stop the others - a broken cosmetic must not swallow
        /// the rest of the feedback.
        /// </summary>
        public void NotifyHeal(GameObject source, GameObject target, float value, bool critical)
        {
            if (source == null) return;
            if (!_healSinks.TryGetValue(source, out List<IHLHealVisualSink> sinks)) return;

            for (int i = sinks.Count - 1; i >= 0; i--)
            {
                if (i >= sinks.Count) continue; // a sink unregistered others while we notified
                IHLHealVisualSink sink = sinks[i];
                try
                {
                    sink.OnHealResolved(target, value, critical);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
