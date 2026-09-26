using UnityEngine;

namespace HealerLike.Render.Grass
{
    // A held effect a producer keeps showing where its source is: shown each frame it is placed, hidden when
    // not, for as long as the producer holds it. Its age counts while shown, for the effect's easing and shiver.
    public class GroundHandle
    {
        readonly Ground _ground;

        GroundEffect _effect;
        public GroundEffect effect { get { return _effect; } set { _effect = value; } }

        internal Vector2 from;
        internal Vector2 to;
        internal float radius;
        internal float strength;
        internal float age;

        bool _isShown;
        public bool isShown { get { return _isShown; } }

        bool _isReleased;
        public bool isReleased { get { return _isReleased; } }

        internal GroundHandle(Ground ground, GroundEffect effect)
        {
            _ground = ground;
            _effect = effect;
        }

        // Round a point, at this radius and strength
        public void Show(Vector3 position, float radius, float strength)
        {
            Place(new Vector2(position.x, position.z), new Vector2(position.x, position.z), radius, strength);
        }

        // Along a segment on the ground, at this strength
        public void ShowLine(Vector3 start, Vector3 end, float strength)
        {
            Place(new Vector2(start.x, start.z), new Vector2(end.x, end.z), 0f, strength);
        }

        void Place(Vector2 start, Vector2 end, float size, float power)
        {
            bool isValid = !_isReleased && _effect != null && float.IsFinite(start.x) && float.IsFinite(start.y)
                           && float.IsFinite(end.x) && float.IsFinite(end.y) && float.IsFinite(size)
                           && RenderMath.IsPositive(power);
            if (!isValid)
            {
                Hide();
                return;
            }

            if (!_isShown)
            {
                age = 0f;
            }

            from = start;
            to = end;
            radius = size;
            strength = power;
            _isShown = true;
        }

        // Gone until shown again; showing again eases it in from the start
        public void Hide()
        {
            _isShown = false;
        }

        // Done for good; the ground forgets it
        public void Release()
        {
            _isShown = false;
            if (!_isReleased)
            {
                _isReleased = true;
                _ground.Forget(this);
            }
        }
    }
}
