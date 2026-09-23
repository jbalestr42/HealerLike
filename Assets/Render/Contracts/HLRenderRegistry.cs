using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render
{

    public class HLRenderRegistry
    {

        public static HLRenderRegistry Current { get; set; }

        public IHLSpellVisualSink SpellSink { get; set; }

        public IHLZoneOwner ZoneOwner { get; set; }

        readonly Dictionary<GameObject, List<IHLHealVisualSink>> _healSinks =
            new Dictionary<GameObject, List<IHLHealVisualSink>>();

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

            IHLHealVisualSink[] snapshot = sinks.ToArray();
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                IHLHealVisualSink sink = snapshot[i];
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
