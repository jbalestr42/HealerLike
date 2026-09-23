using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render
{
    // Plain class so a test can build one, the RenderManager owns one and hands it to the views through Init
    public class HLRenderRegistry
    {
        readonly Dictionary<GameObject, List<IHLHealVisualSink>> _healSinks =
            new Dictionary<GameObject, List<IHLHealVisualSink>>();

        public IHLSpellVisualSink spellSink { get; set; }

        public IHLZoneOwner zoneOwner { get; set; }

        public void Init(IHLSpellVisualSink spellSink, IHLZoneOwner zoneOwner)
        {
            this.spellSink = spellSink;
            this.zoneOwner = zoneOwner;
        }

        public void Register(GameObject source, IHLHealVisualSink sink)
        {
            if (source == null || sink == null)
            {
                return;
            }

            if (!_healSinks.TryGetValue(source, out List<IHLHealVisualSink> sinks))
            {
                sinks = new List<IHLHealVisualSink>(1);
                _healSinks.Add(source, sinks);
            }

            if (!sinks.Contains(sink))
            {
                sinks.Add(sink);
            }
        }

        public void Unregister(GameObject source, IHLHealVisualSink sink)
        {
            // A destroyed source still has to find its entry, so no Unity null check here
            if (ReferenceEquals(source, null) || sink == null)
            {
                return;
            }

            if (!_healSinks.TryGetValue(source, out List<IHLHealVisualSink> sinks))
            {
                return;
            }

            sinks.Remove(sink);
            if (sinks.Count == 0)
            {
                _healSinks.Remove(source);
            }
        }

        // Every health change a source caused, negative for damage; the heal sinks draw the positive ones
        public void NotifyHeal(GameObject source, GameObject target, float value, bool critical)
        {
            if (source == null)
            {
                return;
            }

            if (!_healSinks.TryGetValue(source, out List<IHLHealVisualSink> sinks))
            {
                return;
            }

            // Copy first so a sink can unregister itself or notify again while we loop
            IHLHealVisualSink[] snapshot = sinks.ToArray();
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                snapshot[i].OnHealResolved(target, value, critical);
            }
        }
    }
}
