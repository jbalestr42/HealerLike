using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    /// <summary>
    /// Drives HLRangePreview through its explicit state API (Zones/README.md). Gameplay selection is
    /// private, so the stage cannot mirror it; Featured shows the first live preview as a cosmetic
    /// look-stage choice, Pointer hands previews back to their own approximate pointer observation.
    /// Runs before the zone owner publishes (-1000) and before the previews' own Update.
    /// </summary>
    [DefaultExecutionOrder(-1500)]
    public sealed class HLStageRangeDriver : MonoBehaviour
    {
        public enum PreviewMode { Pointer, Featured, Hidden }
        [SerializeField] PreviewMode mode = PreviewMode.Pointer;
        [Tooltip("Seconds between scene scans for new previews; a destroyed or disabled cached preview forces a rescan.")]
        [SerializeField, Min(0.05f)] float rescanSeconds = .5f;
        readonly List<HLRangePreview> _previews = new List<HLRangePreview>();
        float _nextScan;
        PreviewMode _applied = (PreviewMode)(-1);
        HLRangePreview _appliedFeatured;
        public PreviewMode Mode { get => mode; set => mode = value; }
        public HLRangePreview Featured { get; private set; }
        public IReadOnlyList<HLRangePreview> Cached => _previews;
        public int Scans { get; private set; }

        /// <summary>Index of the first enabled preview in an active hierarchy, or -1.</summary>
        public static int SelectFeatured(IReadOnlyList<HLRangePreview> previews)
        {
            if (previews == null) return -1;
            for (int i = 0; i < previews.Count; i++)
                if (previews[i] && previews[i].enabled && previews[i].gameObject.activeInHierarchy) return i;
            return -1;
        }

        /// <summary>True when the cache holds a destroyed or inactive preview, so the next tick must rescan.</summary>
        public static bool IsStale(IReadOnlyList<HLRangePreview> previews)
        {
            for (int i = 0; i < previews.Count; i++)
                if (!previews[i] || !previews[i].isActiveAndEnabled) return true;
            return false;
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
                preview.observePointer = mode == PreviewMode.Pointer;
                preview.observeHover = mode == PreviewMode.Pointer;
                preview.SetPreviewState(i == featured, false);
            }
            _applied = mode; _appliedFeatured = Featured;
        }

        /// <summary>One tick at the given time: rescans only when due or stale, re-applies only on a change.</summary>
        public void Tick(float now)
        {
            bool rescan = now >= _nextScan || IsStale(_previews);
            if (rescan)
            {
                int before = _previews.Count;
                _previews.Clear();
                foreach(var preview in FindObjectsByType<HLRangePreview>(FindObjectsSortMode.InstanceID)) if(preview.isActiveAndEnabled) _previews.Add(preview);
                _nextScan = now + rescanSeconds; Scans++;
                if (_previews.Count != before) _applied = (PreviewMode)(-1);
            }
            int featured = mode == PreviewMode.Featured ? SelectFeatured(_previews) : -1;
            var next = featured >= 0 ? _previews[featured] : null;
            if (rescan || _applied != mode || _appliedFeatured != next) Apply(_previews);
        }

        void Update() => Tick(Time.unscaledTime);

        void OnDisable()
        {
            foreach (var preview in _previews) if (preview) preview.SetPreviewState(false, false);
            _previews.Clear(); Featured = null; _applied = (PreviewMode)(-1); _appliedFeatured = null; _nextScan = 0;
        }
    }
}
