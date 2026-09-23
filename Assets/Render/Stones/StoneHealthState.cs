using UnityEngine;

namespace HealerLike.Render.Stones
{
    public enum StoneHealthAction
    {
        None,
        ShedPart,
        Collapse
    }

    public class StoneHealthState
    {
        float _threshold = 0.5f;
        bool _hadDamage;
        bool _isShed;
        bool _isCollapsed;

        public void Reset(float thresholdFraction)
        {
            if (!float.IsFinite(thresholdFraction) || thresholdFraction <= 0f || thresholdFraction >= 1f)
            {
                Debug.LogError($"[StoneHealthState] Shed threshold {thresholdFraction} is outside (0, 1), clamping it.");
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

        public StoneHealthAction CompleteBatch(float health, float maxHealth)
        {
            bool hasDamage = _hadDamage;
            _hadDamage = false;
            if (_isCollapsed)
            {
                return StoneHealthAction.None;
            }

            if (health <= 0f)
            {
                TryBeginCollapse();
                return StoneHealthAction.Collapse;
            }

            if (hasDamage && !_isShed && maxHealth > 0f && health <= maxHealth * _threshold)
            {
                _isShed = true;
                return StoneHealthAction.ShedPart;
            }
            return StoneHealthAction.None;
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
