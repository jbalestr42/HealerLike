using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render
{
    // Routes each source's health outcomes to the views that draw them. Plain class so a test can build one;
    // the RenderManager owns it and the views reach it as manager.registry
    public class RenderRegistry
    {
        readonly Dictionary<GameObject, List<IHealthVisualSink>> _healthSinks =
            new Dictionary<GameObject, List<IHealthVisualSink>>();

        public void Register(GameObject source, IHealthVisualSink sink)
        {
            if (source == null || sink == null)
            {
                return;
            }

            if (!_healthSinks.TryGetValue(source, out List<IHealthVisualSink> sinks))
            {
                sinks = new List<IHealthVisualSink>(1);
                _healthSinks.Add(source, sinks);
            }

            if (!sinks.Contains(sink))
            {
                sinks.Add(sink);
            }
        }

        public void Unregister(GameObject source, IHealthVisualSink sink)
        {
            // A destroyed source still has to find its entry, so no Unity null check here
            if (ReferenceEquals(source, null) || sink == null)
            {
                return;
            }

            if (!_healthSinks.TryGetValue(source, out List<IHealthVisualSink> sinks))
            {
                return;
            }

            sinks.Remove(sink);
            if (sinks.Count == 0)
            {
                _healthSinks.Remove(source);
            }
        }

        // Every health change a source caused, negative for damage; each sink picks the sign it draws
        public void NotifyHealth(GameObject source, GameObject target, float value, bool critical)
        {
            if (source == null)
            {
                return;
            }

            if (!_healthSinks.TryGetValue(source, out List<IHealthVisualSink> sinks))
            {
                return;
            }

            // Copy first so a sink can unregister itself or notify again while we loop
            IHealthVisualSink[] snapshot = sinks.ToArray();
            foreach (IHealthVisualSink sink in snapshot)
            {
                sink.OnHealthResolved(target, value, critical);
            }
        }
    }
}
