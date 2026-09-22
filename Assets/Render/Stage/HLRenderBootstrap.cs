using System;
using System.Collections;
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
            registry = new HLRenderRegistry { SpellSink = sink };
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
            if (registry == null) registry = new HLRenderRegistry { SpellSink = spellVisualSink as IHLSpellVisualSink };
            HLRenderRegistry.Current = registry;
            owns = true;
            if (zoneRegistry) zoneRegistry.enabled = true;
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
            if (zoneBridge) zoneBridge.enabled = false;
            if (grassField) grassField.enabled = false;
            if (lookController) lookController.enabled = false;
            if (zoneRegistry) zoneRegistry.enabled = false;
            if (ReferenceEquals(HLRenderRegistry.Current, registry)) HLRenderRegistry.Current = null;
            owns = false;
        }
    }
}
