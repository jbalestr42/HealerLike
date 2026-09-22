using System;
using System.Collections;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    [DefaultExecutionOrder(-2000), DisallowMultipleComponent]
    public sealed class HLRenderBootstrap : MonoBehaviour
    {
        [SerializeField] Behaviour lookController;
        [SerializeField] Behaviour zoneRegistry;
        [SerializeField] Behaviour zoneBridge;
        [SerializeField] Behaviour grassField;
        [SerializeField] MonoBehaviour spellVisualSink;
        [SerializeField] GridManager grid;
        [SerializeField] Transform ground;
        [SerializeField] Component stoneGridEntry;
        [SerializeField] int stoneSeed = 1707;
        HLRenderRegistry registry;
        bool owns;
        public HLRenderRegistry Registry => registry;

        // Explicit injection keeps ownership testable without a scene or gameplay singleton.
        public void Configure(IHLSpellVisualSink sink, Behaviour look, Behaviour zones)
        {
            if (owns) throw new InvalidOperationException("Disable bootstrap before rewiring.");
            registry = new HLRenderRegistry { SpellSink = sink, ZoneOwner = zones as IHLZoneOwner };
            spellVisualSink = sink as MonoBehaviour;
            lookController = look;
            zoneRegistry = zones;
        }
        void OnEnable()
        {
            if (owns) return;
            if (HLRenderRegistry.Current != null)
            {
                Debug.LogWarning("HL stage registry already has an owner.", this);
                return;
            }
            if (registry == null) registry = new HLRenderRegistry { SpellSink = spellVisualSink as IHLSpellVisualSink, ZoneOwner = zoneRegistry as IHLZoneOwner };
            HLRenderRegistry.Current = registry;
            owns = true;
            if (zoneRegistry) zoneRegistry.enabled = true;
            // Explicit stage adapter; the sink would otherwise reach the same owner through the registry.
            if (spellVisualSink is HLSpellVisualSink sink && zoneRegistry is HLZoneRegistry zones)
                sink.AreaPulse = (center, radius, kind, strength) => zones.AddPulse(kind, center, radius, strength, HLSpellVisualSink.PulseSeconds);
            if (lookController) lookController.enabled = true;
            if (zoneBridge) zoneBridge.enabled = true;
            if (grassField) grassField.enabled = true;
        }
        IEnumerator Start()
        {
            if (!owns || !stoneGridEntry || !grid || !ground) yield break;
            // PlayerBehaviour creates cells in Start. Never generate them a second time.
            while (grid.cells == null || grid.cells.Length != grid.width * grid.height) yield return null;
            var generate = stoneGridEntry.GetType().GetMethod("Generate", new[] { typeof(GridManager), typeof(Transform), typeof(int) });
            if (generate == null) throw new InvalidOperationException("HL stone entry must expose Generate(grid, ground, seed).");
            generate.Invoke(stoneGridEntry, new object[] { grid, ground, stoneSeed });
        }
        void OnDisable()
        {
            if (!owns) return;
            if (spellVisualSink is HLSpellVisualSink sink) sink.AreaPulse = null;
            if (zoneBridge) zoneBridge.enabled = false;
            if (grassField) grassField.enabled = false;
            if (lookController) lookController.enabled = false;
            if (zoneRegistry) zoneRegistry.enabled = false;
            if (ReferenceEquals(HLRenderRegistry.Current, registry)) HLRenderRegistry.Current = null;
            owns = false;
        }
    }
}
