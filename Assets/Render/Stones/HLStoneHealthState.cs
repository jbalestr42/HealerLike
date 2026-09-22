using System;
namespace HealerLike.Render.Stones
{
    public enum HLStoneHealthAction { None, ShedPart, Collapse }
    public sealed class HLStoneHealthState
    {
        float threshold = .5f;
        bool hadDamage, shed, collapsed;
        public void Reset(float thresholdFraction)
        {
            if (!float.IsFinite(thresholdFraction) || thresholdFraction <= 0 || thresholdFraction >= 1)
                throw new ArgumentOutOfRangeException(nameof(thresholdFraction));
            threshold=thresholdFraction; hadDamage=shed=collapsed=false;
        }
        public void RecordProcessedDelta(float delta) { if (delta < 0) hadDamage=true; }
        public HLStoneHealthAction CompleteBatch(float health, float maxHealth)
        {
            bool damage=hadDamage; hadDamage=false;
            if (collapsed) return HLStoneHealthAction.None;
            if (health <= 0) { TryBeginCollapse(); return HLStoneHealthAction.Collapse; }
            if (damage && !shed && maxHealth > 0 && health <= maxHealth * threshold)
            { shed=true; return HLStoneHealthAction.ShedPart; }
            return HLStoneHealthAction.None;
        }
        public bool TryBeginCollapse()
        {
            if (collapsed) return false; collapsed=true; hadDamage=false; return true;
        }
    }
}
