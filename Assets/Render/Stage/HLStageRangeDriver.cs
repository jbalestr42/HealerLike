using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    /// <summary>
    /// Drives HLRangePreview through its explicit state API (Zones/README.md). Gameplay selection is
    /// private, so the stage cannot mirror it; Featured shows the first live preview as a cosmetic
    /// look-stage choice, Pointer hands previews back to their own approximate pointer observation.
    /// Runs before the zone owner publishes (-1000).
    /// </summary>
    [DefaultExecutionOrder(-1500)]
    public sealed class HLStageRangeDriver : MonoBehaviour
    {
        public enum PreviewMode { Pointer, Featured, Hidden }
        [SerializeField] PreviewMode mode = PreviewMode.Featured;
        readonly List<HLRangePreview> _previews = new List<HLRangePreview>();
        public PreviewMode Mode { get => mode; set => mode = value; }
        public HLRangePreview Featured { get; private set; }

        /// <summary>Index of the first enabled preview in an active hierarchy, or -1.</summary>
        public static int SelectFeatured(IReadOnlyList<HLRangePreview> previews)
        {
            if (previews == null) return -1;
            for (int i = 0; i < previews.Count; i++)
                if (previews[i] && previews[i].enabled && previews[i].gameObject.activeInHierarchy) return i;
            return -1;
        }

        /// <summary>Applies the mode to the given previews: explicit state for all but Pointer.</summary>
        public void Apply(IReadOnlyList<HLRangePreview> previews)
        {
            int featured = mode == PreviewMode.Featured ? SelectFeatured(previews) : -1;
            Featured = featured >= 0 ? previews[featured] : null;
            for (int i = 0; i < previews.Count; i++)
            {
                var preview = previews[i];
                if (!preview) continue;
                preview.ObservePointer = mode == PreviewMode.Pointer;
                if (mode != PreviewMode.Pointer) preview.SetPreviewState(i == featured, false);
            }
        }

        void Update()
        {
            _previews.Clear();
            _previews.AddRange(FindObjectsByType<HLRangePreview>(FindObjectsSortMode.InstanceID));
            Apply(_previews);
        }

        void OnDisable()
        {
            foreach (var preview in _previews) if (preview) preview.SetPreviewState(false, false);
            _previews.Clear(); Featured = null;
        }
    }
}
