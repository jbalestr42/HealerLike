using System;

namespace HealerLike.Render.Stones
{
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
                throw new ArgumentOutOfRangeException(nameof(thresholdFraction));
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
