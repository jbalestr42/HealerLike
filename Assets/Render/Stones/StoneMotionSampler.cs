using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneMotionSampler
    {
        // A step longer than half a cell in one frame is a teleport, not motion
        static readonly float maxStep = 0.5f;

        Vector3 _previous;
        bool _isPrimed;

        public void Reset()
        {
            _isPrimed = false;
        }

        public Vector3 Sample(Vector3 position, float deltaTime, bool dragging)
        {
            Vector3 delta = position - _previous;
            _previous = position;
            delta.y = 0f;
            if (!_isPrimed || dragging || deltaTime <= 0f || delta.magnitude > maxStep)
            {
                _isPrimed = true;
                return Vector3.zero;
            }

            Vector3 velocity = delta / deltaTime;
            if (velocity.magnitude < 0.02f)
            {
                return Vector3.zero;
            }
            return velocity;
        }
    }
}
