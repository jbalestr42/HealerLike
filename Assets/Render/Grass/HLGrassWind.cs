using System;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    [Serializable]
    public class HLGrassWind
    {
        public static readonly float GustSeconds = 0.5f;
        public static readonly float MaxAmplitude = 0.1f;

        public Vector2 direction = new Vector2(1f, 0.35f);
        public float speed = 1.2f;
        public float amplitude = 0.065f;

        float _gustRemaining;
        Vector2 _gustDirection;

        // xy heading, z travel speed, w lean amplitude, as the grass compute reads it
        public Vector4 current
        {
            get
            {
                Vector2 heading = Vector2.right;
                if (_gustRemaining > 0f)
                {
                    heading = _gustDirection;
                }
                else if (direction.sqrMagnitude > 0.00000001f)
                {
                    heading = direction.normalized;
                }

                float lean = Mathf.Clamp(amplitude, 0f, MaxAmplitude);
                if (_gustRemaining > 0f)
                {
                    lean *= 2f;
                }

                return new Vector4(heading.x, heading.y, Mathf.Max(0f, speed), lean);
            }
        }

        // Cosmetic half-second response to a projectile launch
        public void TriggerGust(Vector3 towardTarget)
        {
            Vector2 heading = new Vector2(towardTarget.x, towardTarget.z);
            float lengthSquared = heading.sqrMagnitude;
            if (!float.IsFinite(lengthSquared) || lengthSquared < 0.00000001f)
            {
                return;
            }

            _gustDirection = heading.normalized;
            _gustRemaining = GustSeconds;
        }

        public void Advance(float scaledSeconds)
        {
            if (scaledSeconds >= 0f && !float.IsInfinity(scaledSeconds))
            {
                _gustRemaining = Mathf.Max(0f, _gustRemaining - scaledSeconds);
            }
        }

        public void ClearGust()
        {
            _gustRemaining = 0f;
        }
    }
}
