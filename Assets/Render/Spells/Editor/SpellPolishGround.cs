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
                grow = .65f, release = .65f, lifetime = 1.5f, kick = 130f, ash = .8f,
                vitality = -.5f }, 2f);
            Set(vocabulary, EffectKey.Rise, new GroundEffect { grow = .45f, fadeIn = .08f,
                lifetime = 2.8f, hold = .4f, kick = 45f, vitality = .85f, light = 1f }, 1.8f);
            Set(vocabulary, EffectKey.Stalks, new GroundEffect { grow = .5f, fadeIn = .25f,
                lifetime = 3f, hold = .4f, kick = 30f, holdTurn = .5f, vitality = 1f, light = .6f }, 1.75f);
            Set(vocabulary, EffectKey.Drips, new GroundEffect { grow = .35f, fadeIn = .2f,
                lifetime = 3f, kick = 80f, shiver = 2.5f, blight = 1f, vitality = -.6f }, 1.55f);
            Set(vocabulary, EffectKey.Orbit, new GroundEffect { grow = .45f, fadeIn = .15f,
                lifetime = 2.8f, hold = .3f, holdTurn = Mathf.PI * .5f, kick = 55f,
                kickTurn = Mathf.PI * .5f, light = .65f, vitality = .35f }, 1.75f);
            Set(vocabulary, EffectKey.Plates, new GroundEffect { grow = .4f, fadeIn = .1f,
                lifetime = 2.5f, hold = .45f, flatten = .4f, light = .7f, vitality = .25f }, 1.65f);
            Set(vocabulary, EffectKey.Bud, new GroundEffect { grow = .65f, fadeIn = .18f,
                lifetime = 2.8f, hold = .4f, holdTurn = -.5f, kick = 35f,
                vitality = .6f, light = .85f }, 1.8f);
            Set(vocabulary, EffectKey.Press, new GroundEffect { grow = .25f, fadeIn = .1f,
                lifetime = 2.6f, kick = 70f, flatten = .6f, vitality = -.5f, light = -.9f }, 1.65f);
            Set(vocabulary, EffectKey.Crack, new GroundEffect { grow = .4f, fadeIn = .15f,
                lifetime = 2.8f, kick = 100f, kickTurn = -.5f, shiver = 1.5f,
                vitality = -.8f, light = -.7f, ash = .35f }, 1.7f);
            Set(vocabulary, EffectKey.ManaUp, new GroundEffect { grow = .4f, fadeIn = .1f,
                lifetime = 2.5f, kick = 35f, kickTurn = 1.2f, light = .8f }, 1.45f);
            Set(vocabulary, EffectKey.ManaDown, new GroundEffect { grow = .4f, fadeIn = .1f,
                lifetime = 2.5f, kick = 35f, kickTurn = -1.2f, light = -.8f }, 1.45f);
            Set(vocabulary, EffectKey.Beam, new GroundEffect { shape = GroundShape.Line,
                fadeIn = .08f, lifetime = 2.4f, width = .45f, kick = 70f, light = .7f }, 1f);
            Set(vocabulary, EffectKey.Ring, new GroundEffect { grow = .65f, fadeIn = .3f,
                lifetime = 3.2f, hold = .35f, kick = 30f, flatten = .4f, vitality = 1f, light = .7f }, 1f);
            Set(vocabulary, EffectKey.Litter, new GroundEffect { grow = .55f, fadeIn = .3f,
                lifetime = 3.2f, hold = .45f, shiver = 1.7f, kick = 80f, flatten = .4f,
                vitality = -.8f, blight = .9f, light = -.6f }, 1f);
        }

        static void Set(EffectVocabulary vocabulary, EffectKey element, GroundEffect ground, float radius)
        {
            ground.edge = .42f;
            ground.wobble = .12f;
            ElementEntry entry = vocabulary.entries[element];
            entry.ground = ground;
            entry.groundRadius = radius;
            entry.groundStrength = .85f;
        }
    }
}
