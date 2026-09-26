using UnityEngine;
using HealerLike.Render.Environment;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Zones
{
    // One shot in flight: the grass parts along the ground under the projectile, in a trail behind it that follows
    // the projectile itself, harder the lower it flies, and settles once it lands. A launch also gusts the
    // scatter plants near its path. Every shot the projectile starts, reused projectiles included.
    public class LaunchWave : AProjectileBehaviour
    {
        public static readonly float GustStrength = 0.65f;
        public static readonly float GustSeconds = 0.8f;
        // In world units: the trail's longest reach behind the projectile, and the heights over the ground at
        // which it parts the grass fully and not at all
        public static readonly float TrailLength = 1.2f;
        public static readonly float LowFlight = 0.3f;
        public static readonly float HighFlight = 1.8f;

        readonly ZoneHandle _shot = new ZoneHandle();
        ZoneRegistry _zones;
        EnvironmentGust _gust;
        Vector3 _launch;
        float _ground;
        bool _isFlying;

        // The RenderManager calls it before Projectile.Init runs the behaviours
        public void Init(ZoneRegistry zones, EnvironmentGust gust)
        {
            _zones = zones;
            _gust = gust;
            _shot.Init(zones);
        }

        public override void Init(GameObject source)
        {
            _isFlying = false;
            _shot.Clear();
            if (!isActiveAndEnabled || source == null || projectile == null || projectile.target == null)
            {
                return;
            }

            Vector3 sourcePosition = source.transform.position;
            CharacterView screenSource = CharacterView.ScreenSource(source);
            if (screenSource)
            {
                screenSource.TryGetCastPoint(out sourcePosition);
            }

            _launch = transform.position;
            _ground = source.transform.position.y;
            _isFlying = _zones != null;
            if (_gust != null)
            {
                _gust.Gust(sourcePosition, projectile.target.transform.position, GustStrength, GustSeconds);
            }
        }

        void Update()
        {
            Follow();
        }

        // Moves the trail to where the projectile is now
        public void Follow()
        {
            if (!_isFlying)
            {
                return;
            }

            Vector3 position = transform.position;
            Vector3 travelled = position - _launch;
            travelled.y = 0f;
            float length = Mathf.Min(travelled.magnitude, TrailLength);
            float strength = Strength(position.y - _ground);
            if (length < 0.01f || strength <= 0f)
            {
                _shot.Clear();
                return;
            }

            Vector3 head = new Vector3(position.x, _ground, position.z);
            _shot.Refresh(ZoneKind.Launch, head, length, strength, travelled);
        }

        // Full near the ground, none past HighFlight
        public static float Strength(float height)
        {
            if (!float.IsFinite(height))
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01((height - LowFlight) / (HighFlight - LowFlight));
        }

        void OnDisable()
        {
            _isFlying = false;
            _shot.Clear();
        }

        void OnDestroy()
        {
            _shot.Clear();
        }
    }
}
