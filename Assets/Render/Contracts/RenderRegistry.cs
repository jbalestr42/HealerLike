using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render
{
    // Plain class so a test can build one, the RenderManager owns one and hands it to the views through Init
    public class RenderRegistry
    {
        readonly Dictionary<GameObject, List<IHealVisualSink>> _healSinks =
            new Dictionary<GameObject, List<IHealVisualSink>>();

        public ISpellVisualSink spellSink { get; set; }

        public IZoneOwner zoneOwner { get; set; }

        public void Init(ISpellVisualSink spellSink, IZoneOwner zoneOwner)
        {
            this.spellSink = spellSink;
            this.zoneOwner = zoneOwner;
        }

        public void Register(GameObject source, IHealVisualSink sink)
        {
            if (source == null || sink == null)
            {
                return;
            }

            if (!_healSinks.TryGetValue(source, out List<IHealVisualSink> sinks))
            {
                sinks = new List<IHealVisualSink>(1);
                _healSinks.Add(source, sinks);
            }

            if (!sinks.Contains(sink))
            {
                sinks.Add(sink);
            }
        }

        public void Unregister(GameObject source, IHealVisualSink sink)
        {
            // A destroyed source still has to find its entry, so no Unity null check here
            if (ReferenceEquals(source, null) || sink == null)
            {
                return;
            }

            if (!_healSinks.TryGetValue(source, out List<IHealVisualSink> sinks))
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

            if (!_healSinks.TryGetValue(source, out List<IHealVisualSink> sinks))
            {
                return;
            }

            // Copy first so a sink can unregister itself or notify again while we loop
            IHealVisualSink[] snapshot = sinks.ToArray();
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                snapshot[i].OnHealResolved(target, value, critical);
            }
        }
    }
}
