using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Spells.Editor
{
    // Forces account for Ground's spring stiffness: about 100 acceleration units hold one radian of lean.
    // Slow state channels persist beyond the mesh flash, then the shared landscape simulation restores them.
    public static class SpellPolishGround
    {
        public static void Apply(EffectVocabulary vocabulary)
        {
            Set(vocabulary, EffectKey.Burst, new GroundEffect { shape = GroundShape.Ring,
                grow = 0.65f, release = 0.65f, lifetime = 1.5f, kick = 130f, ash = 0.8f,
                vitality = -0.5f }, 2f);
            Set(vocabulary, EffectKey.Rise, new GroundEffect { grow = 0.45f, fadeIn = 0.08f,
                lifetime = 2.8f, hold = 0.4f, kick = 45f, vitality = 0.85f, light = 1f }, 1.8f);
            Set(vocabulary, EffectKey.Stalks, new GroundEffect { grow = 0.5f, fadeIn = 0.25f,
                lifetime = 3f, hold = 0.4f, kick = 30f, holdTurn = 0.5f, vitality = 1f, light = 0.6f }, 1.75f);
            Set(vocabulary, EffectKey.Drips, new GroundEffect { grow = 0.35f, fadeIn = 0.2f,
                lifetime = 3f, kick = 80f, shiver = 2.5f, blight = 1f, vitality = -0.6f }, 1.55f);
            Set(vocabulary, EffectKey.Orbit, new GroundEffect { grow = 0.45f, fadeIn = 0.15f,
                lifetime = 2.8f, hold = 0.3f, holdTurn = Mathf.PI * 0.5f, kick = 55f,
                kickTurn = Mathf.PI * 0.5f, light = 0.65f, vitality = 0.35f }, 1.75f);
            Set(vocabulary, EffectKey.Plates, new GroundEffect { grow = 0.4f, fadeIn = 0.1f,
                lifetime = 2.5f, hold = 0.45f, flatten = 0.4f, light = 0.7f, vitality = 0.25f }, 1.65f);
            Set(vocabulary, EffectKey.Bud, new GroundEffect { grow = 0.65f, fadeIn = 0.18f,
                lifetime = 2.8f, hold = 0.4f, holdTurn = -0.5f, kick = 35f,
                vitality = 0.6f, light = 0.85f }, 1.8f);
            Set(vocabulary, EffectKey.Press, new GroundEffect { grow = 0.25f, fadeIn = 0.1f,
                lifetime = 2.6f, kick = 70f, flatten = 0.6f, vitality = -0.5f, light = -0.9f }, 1.65f);
            Set(vocabulary, EffectKey.Crack, new GroundEffect { grow = 0.4f, fadeIn = 0.15f,
                lifetime = 2.8f, kick = 100f, kickTurn = -0.5f, shiver = 1.5f,
                vitality = -0.8f, light = -0.7f, ash = 0.35f }, 1.7f);
            Set(vocabulary, EffectKey.ManaUp, new GroundEffect { grow = 0.4f, fadeIn = 0.1f,
                lifetime = 2.5f, kick = 35f, kickTurn = 1.2f, light = 0.8f }, 1.45f);
            Set(vocabulary, EffectKey.ManaDown, new GroundEffect { grow = 0.4f, fadeIn = 0.1f,
                lifetime = 2.5f, kick = 35f, kickTurn = -1.2f, light = -0.8f }, 1.45f);
            Set(vocabulary, EffectKey.Beam, new GroundEffect { shape = GroundShape.Line,
                fadeIn = 0.08f, lifetime = 2.4f, width = 0.45f, kick = 70f, light = 0.7f }, 1f);
            Set(vocabulary, EffectKey.Ring, new GroundEffect { grow = 0.65f, fadeIn = 0.3f,
                lifetime = 3.2f, hold = 0.35f, kick = 30f, flatten = 0.4f, vitality = 1f, light = 0.7f }, 1f);
            Set(vocabulary, EffectKey.Litter, new GroundEffect { grow = 0.55f, fadeIn = 0.3f,
                lifetime = 3.2f, hold = 0.45f, shiver = 1.7f, kick = 80f, flatten = 0.4f,
                vitality = -0.8f, blight = 0.9f, light = -0.6f }, 1f);
        }

        static void Set(EffectVocabulary vocabulary, EffectKey element, GroundEffect ground, float radius)
        {
            ground.edge = 0.42f;
            ground.wobble = 0.12f;
            ElementEntry entry = vocabulary.entries[element];
            entry.ground = ground;
            entry.groundRadius = radius;
            entry.groundStrength = 0.85f;
        }
    }
}
