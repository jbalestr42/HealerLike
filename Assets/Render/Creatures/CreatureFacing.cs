using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Combat aim owns cosmetic yaw. The assigned camera supplies only the target-free rest pose.
    public class CreatureFacing
    {
        Quaternion _aim = Quaternion.identity;
        Vector3? _presentationForward;
        Quaternion _presentationBasis = Quaternion.identity;
        bool _returningToRest;

        public void SetForward(Vector3? forward)
        {
            _presentationForward = forward;
        }

        public Quaternion Evaluate(float time, float deltaTime, Vector3 origin, Transform root, Vector3? aimTarget)
        {
            float dt = float.IsFinite(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;
            float damping = 1f - Mathf.Exp(-dt * 7f);
            if (!float.IsFinite(time)) time = 0f;
            if (_presentationForward.HasValue
                && TryHeading(_presentationForward.Value, root, out Quaternion camera))
            {
                _presentationBasis = camera;
            }

            if (aimTarget.HasValue && RenderMath.IsFinite(origin)
                && TryHeading(aimTarget.Value - origin, root, out Quaternion target))
            {
                _aim = Quaternion.Slerp(_aim, target, damping);
                _returningToRest = true;
                return _aim;
            }

            if (_presentationForward.HasValue)
            {
                Quaternion rest = _presentationBasis * Quaternion.AngleAxis(Mathf.Sin(time * 0.3f) * 4f, Vector3.up);
                // First portrait/rest frame follows the camera immediately. Losing a target returns smoothly.
                _aim = _returningToRest ? Quaternion.Slerp(_aim, rest, damping) : rest;
                if (Quaternion.Angle(_aim, rest) < 0.05f) _returningToRest = false;
            }
            else
            {
                Vector3 rest = new Vector3(Mathf.Sin(time * 0.3f) * 0.4f, 0f, 1f);
                _aim = Quaternion.Slerp(_aim, Quaternion.LookRotation(rest), damping);
            }

            return _aim;
        }

        static bool TryHeading(Vector3 direction, Transform root, out Quaternion heading)
        {
            heading = Quaternion.identity;
            if (!RenderMath.IsFinite(direction)) return false;
            direction = root.InverseTransformDirection(direction);
            direction.y = 0f;
            float length = direction.sqrMagnitude;
            if (!float.IsFinite(length) || length < 0.000001f) return false;
            heading = Quaternion.LookRotation(direction);
            return true;
        }
    }
}
