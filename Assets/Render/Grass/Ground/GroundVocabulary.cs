using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The grass's named answers, one tunable effect per thing the game shows in it. Producers play these through
    // Ground; the zones the tuft compute reads (a heal, a range preview) move the grass through ForZone.
    [CreateAssetMenu(menuName = "HealerLike/Render/Ground Vocabulary")]
    public class GroundVocabulary : ScriptableObject
    {
        [Header("Zones the tufts also read")]
        [Tooltip("A heal: the grass leans away, spins, turns lush and glows as the disc blooms")]
        public GroundEffect heal = Heal();
        [Tooltip("A selected ally's range: the grass leans gently away")]
        public GroundEffect range = Range();

        [Header("Bodies and deliveries")]
        [Tooltip("A shapeless obstacle standing on the grass, laying it flat and outward")]
        public GroundEffect obstacle = Obstacle();
        [Tooltip("A shot in flight: the grass parts along its trail")]
        public GroundEffect launch = Launch();
        [Tooltip("A hit: a ring blasts the grass out of the target")]
        public GroundEffect hit = Ring(170f);
        [Tooltip("A creature landing: a ring out of each root foot")]
        public GroundEffect footRing = Ring(170f);
        [Tooltip("A creature landing: a ring round its whole footprint")]
        public GroundEffect bodyRing = Ring(170f);

        [Header("States")]
        [Tooltip("Round a rocky enemy: quiet grey ash")]
        public GroundEffect ash = Mark(0.35f, 0.22f, 1f, 0f, 0f, 0f);
        [Tooltip("Round a hurt ally: dead grass, as deep as the strength it is shown with")]
        public GroundEffect wilt = Mark(0.5f, 0.15f, 0f, -1f, 0f, 0f);
        [Tooltip("A cell a boost blesses: lush, softly lit grass")]
        public GroundEffect boost = Mark(0.6f, 0.12f, 0f, 0.6f, 0.3f, 0f);
        [Tooltip("Round a poisoned creature: sickly blight")]
        public GroundEffect blight = Mark(0.5f, 0.2f, 0f, 0f, 0f, 1f);
        [Tooltip("Round a slowed creature: frost")]
        public GroundEffect frost = Mark(0.4f, 0.25f, 0f, 0f, -1f, 0f);
        [Tooltip("A lightning bolt: a jagged ash line from contact to contact")]
        public GroundEffect scorch = Scorch();

        [Header("Warnings")]
        [Tooltip("An enemy's area about to land: the grass in it shivers")]
        public GroundEffect warning = Warning();

        // The effect a zone of the tuft compute also plays in the grass, or none
        public GroundEffect ForZone(ZoneKind kind)
        {
            switch (kind)
            {
                case ZoneKind.Heal:
                    return heal;
                case ZoneKind.Range:
                    return range;
                default:
                    return null;
            }
        }

        // An in-memory vocabulary at the defaults, for tests and for a stage built without the asset
        public static GroundVocabulary CreateDefault()
        {
            GroundVocabulary vocabulary = CreateInstance<GroundVocabulary>();
            vocabulary.hideFlags = HideFlags.DontSave;
            return vocabulary;
        }

        static GroundEffect Heal()
        {
            return new GroundEffect
            {
                grow = 0.3f, fadeIn = 0.12f, edge = 0.3f, hold = 0.5f, kick = 30f, kickTurn = Mathf.PI * 0.5f,
                vitality = 1f, light = 1f
            };
        }

        static GroundEffect Range()
        {
            return new GroundEffect { fadeIn = 0.12f, edge = 0.25f, hold = 0.2f };
        }

        static GroundEffect Obstacle()
        {
            return new GroundEffect { fadeIn = 0.12f, edge = 0.15f, wobble = 0.24f, hold = 0.35f, flatten = 1f };
        }

        static GroundEffect Launch()
        {
            return new GroundEffect { shape = GroundShape.Line, width = 0.45f, kick = 110f };
        }

        static GroundEffect Ring(float kick)
        {
            return new GroundEffect
            {
                shape = GroundShape.Ring, grow = 0.35f, release = 0.1f, lifetime = 0.6f, band = 0.15f,
                minBand = 0.2f, kick = kick
            };
        }

        static GroundEffect Mark(float edge, float wobble, float ash, float vitality, float light, float blight)
        {
            return new GroundEffect
            {
                edge = edge, wobble = wobble, ash = ash, vitality = vitality, light = light, blight = blight
            };
        }

        static GroundEffect Scorch()
        {
            return new GroundEffect
            {
                shape = GroundShape.Line, lifetime = 0.6f, width = 0.12f, swing = 0.15f, turns = 1.8f, ash = 1f
            };
        }

        static GroundEffect Warning()
        {
            return new GroundEffect { edge = 0.25f, kick = 320f, shiver = 5f };
        }
    }
}
