using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The authored values of each studio sample, by its index in SpellStudioSamples
    public static class SpellSampleSettings
    {
        static readonly string opposingHandlerPath =
            "Assets/Data/CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory.asset";

        // The six samples with their own element, colour and cast context
        public static void Authored(SpellStudioPreset preset, int index)
        {
            if (index == 0)
            {
                Set(preset, EffectElement.Rise, EffectFamily.Heal, EffectTempo.Once, new Color(0.54f, 1f, 0.26f));
                preset.amount = 0.5f;
                preset.critical = true;
                preset.description = "A full-strength critical heal. Lime spheres rise from the feet; scrub the first "
                    + "cycle to inspect the launch and critical ring. Amount controls the visible shape count.";
            }
            else if (index == 1)
            {
                Set(preset, EffectElement.Drips, EffectFamily.Rot, EffectTempo.PerPeriod, new Color(0.62f, 0.3f, 0.8f));
                preset.periodSeconds = 1.2f;
                preset.durationSeconds = 6f;
                preset.stacks = 3;
                preset.side = Entity.EntityType.Computer;
                preset.description = "Three stacks of violet rot, ticking every 1.2 seconds for five cycles. The first "
                    + "tick starts after one period. Watch the drops swell, fall and reset.";
            }
            else if (index == 2)
            {
                Color gold = new Color(1f, 0.77f, 0.27f);
                Set(preset, EffectElement.Plates, EffectFamily.Boon, EffectTempo.ForDuration, gold);
                preset.durationSeconds = 5f;
                preset.charges = 3f;
                preset.description = "A warm gold, three-charge ward. Plates close around the body and remain held. "
                    + "Lower Charges to study how the vocabulary reveals each plate.";
            }
            else if (index == 3)
            {
                Color blue = new Color(0.35f, 0.77f, 1f);
                Set(preset, EffectElement.Beam, EffectFamily.Damage, EffectTempo.ForDuration, blue);
                preset.durationSeconds = 4f;
                preset.description = "A cool blue connection with travelling beads. The link socket spans the preview "
                    + "endpoints; orbit the camera to inspect the curve. Shape edits remain local to this preset.";
            }
            else if (index == 4)
            {
                Set(preset, EffectElement.ManaUp, EffectFamily.Heal, EffectTempo.Once, new Color(0.42f, 0.66f, 1f));
                preset.amount = 0.4f;
                preset.description = "A sky-blue mana gain. An isolated impact example for comparing the mana "
                    + "silhouette with Verdant Bloom; loop the preview to inspect the upward motion.";
            }
            else if (index == 5)
            {
                Set(preset, EffectElement.Burst, EffectFamily.Damage, EffectTempo.Once, new Color(1f, 0.36f, 0.12f));
                preset.amount = 0.5f;
                preset.critical = true;
                preset.side = Entity.EntityType.Computer;
                preset.scale = 1.2f;
                preset.description = "An orange critical impact with a slightly enlarged silhouette. Scrub near the "
                    + "opening frames to inspect the burst expansion and compare it with the unscaled vocabulary.";
            }
        }

        // The three samples that take their look from the grammar or from a gameplay handler
        public static void Linked(SpellStudioPreset preset, int index)
        {
            preset.mode = SpellStudioMode.GrammarChannels;
            preset.overrideColour = false;
            preset.spellLooks = AssetDatabase.LoadAssetAtPath<SpellLooks>(SpellStudioSamples.SpellLooksPath);
            preset.durationSeconds = 6f;
            if (index == 6)
            {
                preset.family = EffectFamily.Renew;
                preset.attributeGroup = AttributeGroup.Defence;
                preset.tempo = EffectTempo.PerPeriod;
                preset.periodSeconds = 1.25f;
                preset.description = "Renderer grammar: a periodic renewal derives Stalks from the Renew family. "
                    + "Change family or group to explore the shared vocabulary; the shape stays linked to it.";
            }
            else if (index == 7)
            {
                preset.family = EffectFamily.Boon;
                preset.attributeGroup = AttributeGroup.Defence;
                preset.tempo = EffectTempo.ForDuration;
                preset.charges = 3f;
                preset.description = "Renderer grammar: a held defence boon derives Plates. Switch the attribute "
                    + "group to Prevention to produce Bud, or Offence to produce Orbit.";
            }
            else
            {
                preset.mode = SpellStudioMode.GameplayHandler;
                preset.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(opposingHandlerPath);
                preset.isSameSide = false;
                preset.side = Entity.EntityType.Computer;
                preset.description = "Actual gameplay handler: the damage modifier and its duration derive the "
                    + "opposing debuff. Native SpellLooks rows win when enabled, exactly as in SpellVisualSink.";
            }
        }

        static void Set(SpellStudioPreset preset, EffectElement element, EffectFamily family, EffectTempo tempo,
            Color colour)
        {
            preset.element = element;
            preset.family = family;
            preset.tempo = tempo;
            preset.colour = colour;
        }
    }
}
