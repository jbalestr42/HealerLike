using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Camera heading follows immediately; only the unit's small target reaction is damped.
    public class CreatureFacing
    {
        Quaternion _aim = Quaternion.identity;
        Vector3? _presentationForward;
        Quaternion _presentationBasis = Quaternion.identity;
        float _presentationTurn;

        public void SetForward(Vector3? forward)
        {
            if (_presentationForward.HasValue != forward.HasValue)
            {
                _presentationTurn = 0f;
            }

            _presentationForward = forward;
        }

        public Quaternion Evaluate(float time, float deltaTime, Vector3 origin, Transform root, Vector3? aimTarget)
        {
            float damping = 1f - Mathf.Exp(-deltaTime * 7f);
            if (_presentationForward.HasValue)
            {
                Vector3 forward = _presentationForward.Value;
                if (RenderMath.IsFinite(forward))
                {
                    forward = root.InverseTransformDirection(forward);
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.000001f)
                    {
                        _presentationBasis = Quaternion.LookRotation(forward);
                    }
                }

                // Follow camera heading immediately, including the initial paused frame. Only the small
                // reaction to a target is damped, so a moving camera cannot leave the full silhouette edge-on.
                float turn = Mathf.Sin(time * 0.3f) * 4f;
                if (aimTarget.HasValue)
                {
                    Vector3 target = root.InverseTransformDirection(aimTarget.Value - origin);
                    target.y = 0f;
                    turn =
                        target.sqrMagnitude > 0.000001f
                            ? Vector3.Dot(target.normalized, _presentationBasis * Vector3.right) * 18f
                            : 0f;
                }

                // Lateral response is continuous even when the target crosses directly behind the creature.
                _presentationTurn = Mathf.Lerp(_presentationTurn, turn, damping);
                _aim = _presentationBasis * Quaternion.AngleAxis(_presentationTurn, Vector3.up);
                return _aim;
            }

            Vector3 direction = new Vector3(Mathf.Sin(time * 0.3f) * 0.4f, 0f, 1f);
            if (aimTarget.HasValue)
            {
                direction = root.InverseTransformDirection(aimTarget.Value - origin);
            }

            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f)
            {
                _aim = Quaternion.Slerp(_aim, Quaternion.LookRotation(direction), damping);
            }

            return _aim;
        }
    }
}
