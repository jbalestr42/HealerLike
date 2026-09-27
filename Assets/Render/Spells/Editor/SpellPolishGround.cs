using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Spells.Editor
{
    // Authored reactions use the same state channels as the landscape, so they mix with prior spells.
    public static class SpellPolishGround
    {
        public static void Apply(EffectVocabulary vocabulary)
        {
            Set(vocabulary, EffectElement.Burst, new GroundEffect { shape = GroundShape.Ring,
                grow = .6f, release = .45f, lifetime = 1.3f, kick = 8f, ash = .38f,
                vitality = -.15f, flatten = .15f }, 1.7f);
            Set(vocabulary, EffectElement.Rise, new GroundEffect { grow = .45f, fadeIn = .08f,
                lifetime = 1.7f, hold = .18f, kick = 1.5f, vitality = .55f, light = .65f }, 1.4f);
            Set(vocabulary, EffectElement.Stalks, new GroundEffect { grow = .5f, fadeIn = .25f,
                lifetime = 2f, hold = .12f, kick = 1f, holdTurn = .5f, vitality = .7f, light = .3f }, 1.5f);
            Set(vocabulary, EffectElement.Drips, new GroundEffect { grow = .35f, fadeIn = .2f,
                lifetime = 1.8f, kick = 2.5f, shiver = 2.5f, blight = .7f, vitality = -.35f }, 1.25f);
            Set(vocabulary, EffectElement.Orbit, new GroundEffect { grow = .45f, fadeIn = .15f,
                lifetime = 2f, hold = .14f, holdTurn = Mathf.PI * .5f, kick = 2f,
                kickTurn = Mathf.PI * .5f, light = .35f, vitality = .15f }, 1.4f);
            Set(vocabulary, EffectElement.Plates, new GroundEffect { grow = .4f, fadeIn = .1f,
                lifetime = 1.4f, hold = .2f, flatten = .18f, light = .3f, vitality = .1f }, 1.35f);
            Set(vocabulary, EffectElement.Bud, new GroundEffect { grow = .65f, fadeIn = .18f,
                lifetime = 1.7f, hold = .17f, holdTurn = -.5f, kick = 1.3f,
                vitality = .2f, light = .45f }, 1.5f);
            Set(vocabulary, EffectElement.Press, new GroundEffect { grow = .25f, fadeIn = .1f,
                lifetime = 1.7f, kick = 2.8f, flatten = .4f, vitality = -.25f, light = -.45f }, 1.3f);
            Set(vocabulary, EffectElement.Crack, new GroundEffect { grow = .4f, fadeIn = .15f,
                lifetime = 1.8f, kick = 3f, kickTurn = -.5f, shiver = 1.5f,
                vitality = -.4f, light = -.25f, ash = .15f }, 1.4f);
            Set(vocabulary, EffectElement.ManaUp, new GroundEffect { grow = .4f, fadeIn = .1f,
                lifetime = 1.5f, kick = 1.7f, kickTurn = 1.2f, light = .55f }, 1.15f);
            Set(vocabulary, EffectElement.ManaDown, new GroundEffect { grow = .4f, fadeIn = .1f,
                lifetime = 1.5f, kick = 1.7f, kickTurn = -1.2f, light = -.45f }, 1.15f);
            Set(vocabulary, EffectElement.Beam, new GroundEffect { shape = GroundShape.Line,
                fadeIn = .08f, lifetime = 1.4f, width = .35f, kick = 2.5f, light = .3f }, 1f);
            Set(vocabulary, EffectElement.Ring, new GroundEffect { grow = .65f, fadeIn = .3f,
                lifetime = 2.8f, hold = .18f, kick = 1.4f, vitality = .65f, light = .5f }, 1f);
            Set(vocabulary, EffectElement.Litter, new GroundEffect { grow = .55f, fadeIn = .3f,
                lifetime = 2.4f, hold = .25f, shiver = 1.7f, kick = 2f,
                vitality = -.4f, blight = .5f, light = -.3f }, 1f);
        }

        static void Set(EffectVocabulary vocabulary, EffectElement element, GroundEffect ground, float radius)
        {
            ground.edge = .42f;
            ground.wobble = .12f;
            ElementEntry entry = vocabulary.elements[element];
            entry.ground = ground;
            entry.groundRadius = radius;
            entry.groundStrength = .65f;
        }
    }
}
