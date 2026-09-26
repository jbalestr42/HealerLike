using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Idle sway and gust push of the scatter plants, on the root the scatter builds, so it goes with that root
    public class EnvironmentSway : MonoBehaviour
    {
        // A freshly built plant rocks for this long after spawning, decaying as it goes
        static readonly float settleSeconds = 1.2f;
        static readonly float settleFrequency = 12f;
        static readonly float settleDecay = 5f;
        static readonly float settleDegrees = 3f;
        // Share of the swing that also tips the plant forward, and the gust push per degree of sway
        static readonly float pitchShare = 0.35f;
        static readonly float gustGain = 2f;
        // Idle cycles per second
        static readonly Vector2 frequencyRange = new Vector2(0.2f, 0.4f);

        struct Motion
        {
            public Transform pivot;
            public Transform anchor;
            public Quaternion rest;
            public float frequency;
            public float phase;
            public float amplitude;
            public float uncurl;
        }

        readonly List<Motion> _motions = new List<Motion>();
        Camera _camera;
        EnvironmentGust _gust;
        float _farDistance;
        double _builtAt;

        // Plants past the far distance from the camera stand still; without a camera every plant moves
        public void Init(Camera camera, EnvironmentGust gust, float farDistance, double builtAt)
        {
            _camera = camera;
            _gust = gust;
            _farDistance = Mathf.Max(1f, farDistance);
            _builtAt = builtAt;
        }

        void Update()
        {
            Animate(Time.timeAsDouble);
        }

        // The pivot turns; the anchor is where its distance to the camera is measured from
        public void Add(Transform pivot, Transform anchor, uint seed, float degrees, float uncurl)
        {
            SeededRandom random = new SeededRandom(seed);
            _motions.Add(new Motion
            {
                pivot = pivot,
                anchor = anchor,
                rest = pivot.localRotation,
                frequency = random.Range(frequencyRange.x, frequencyRange.y),
                phase = random.Range(0f, Mathf.PI * 2f),
                amplitude = degrees,
                uncurl = uncurl
            });
        }

        // Sampled from the absolute clock, so a plant coming back into range needs no catch-up on missed frames
        public void Animate(double time)
        {
            Vector3 cameraPosition = Vector3.zero;
            float distance = float.MaxValue;
            if (_camera)
            {
                cameraPosition = _camera.transform.position;
                distance = _farDistance;
            }

            float age = Mathf.Max(0f, (float)(time - _builtAt));
            float settle = 0f;
            if (age < settleSeconds)
            {
                settle = Mathf.Sin(age * settleFrequency) * Mathf.Exp(-age * settleDecay) * settleDegrees;
            }

            for (int i = 0; i < _motions.Count; i++)
            {
                Motion motion = _motions[i];
                if (!motion.pivot || (motion.anchor.position - cameraPosition).sqrMagnitude > distance * distance)
                {
                    continue;
                }

                // A launch bends only the plants near its path
                Vector3 wind = _gust ? _gust.Sample(time, motion.anchor.position) : Vector3.zero;
                double cycle = time * motion.frequency * 2 * System.Math.PI % (2 * System.Math.PI);
                float wave = Mathf.Sin((float)cycle + motion.phase);
                Vector3 localWind = motion.pivot.parent.InverseTransformDirection(wind);
                float pushPitch = localWind.z * motion.amplitude * gustGain;
                float pushRoll = -localWind.x * motion.amplitude * gustGain - wind.magnitude * motion.uncurl;
                Quaternion push = Quaternion.Euler(pushPitch, 0f, pushRoll);
                float swing = wave * motion.amplitude;
                Quaternion sway = Quaternion.Euler(swing * pitchShare, 0f, swing + settle);
                motion.pivot.localRotation = push * motion.rest * sway;
            }
        }
    }
}
