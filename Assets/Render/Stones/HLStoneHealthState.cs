using UnityEngine;

namespace HealerLike.Render.Stones
{
    public enum HLStoneHealthAction
    {
        None,
        ShedPart,
        Collapse
    }

    public class HLStoneHealthState
    {
        float _threshold = 0.5f;
        bool _hadDamage;
        bool _isShed;
        bool _isCollapsed;

        public void Reset(float thresholdFraction)
        {
            if (!float.IsFinite(thresholdFraction) || thresholdFraction <= 0f || thresholdFraction >= 1f)
            {
                Debug.LogError($"[HLStoneHealthState] Shed threshold {thresholdFraction} is outside (0, 1), clamping it.");
                thresholdFraction = float.IsNaN(thresholdFraction) ? 0.5f : Mathf.Clamp(thresholdFraction, 0.01f, 0.99f);
            }

            _threshold = thresholdFraction;
            _hadDamage = false;
            _isShed = false;
            _isCollapsed = false;
        }

        public void RecordProcessedDelta(float delta)
        {
            if (delta < 0f)
            {
                _hadDamage = true;
            }
        }

        public HLStoneHealthAction CompleteBatch(float health, float maxHealth)
        {
            bool hasDamage = _hadDamage;
            _hadDamage = false;
            if (_isCollapsed)
            {
                return HLStoneHealthAction.None;
            }

            if (health <= 0f)
            {
                TryBeginCollapse();
                return HLStoneHealthAction.Collapse;
            }

            if (hasDamage && !_isShed && maxHealth > 0f && health <= maxHealth * _threshold)
            {
                _isShed = true;
                return HLStoneHealthAction.ShedPart;
            }
            return HLStoneHealthAction.None;
        }

        public bool TryBeginCollapse()
        {
            if (_isCollapsed)
            {
                return false;
            }

            _isCollapsed = true;
            _hadDamage = false;
            return true;
        }
    }
}
