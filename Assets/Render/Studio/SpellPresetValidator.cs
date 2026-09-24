using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio
{
    // What a spell preset still needs before it previews as authored. It never edits the preset:
    // Compose bounds malformed numbers on its own, these lines only tell the author.
    public static class SpellPresetValidator
    {
        public static string[] Validate(SpellStudioPreset preset)
        {
            List<string> warnings = new List<string>();
            bool isHandlerMode = preset.mode == SpellStudioMode.GameplayHandler;

            if (isHandlerMode && preset.sourceHandler != null && !preset.usesGameplayOverride
                && !IsDerivable(preset.sourceHandler))
            {
                warnings.Add("The gameplay handler contains missing buff data. "
                    + "Complete its modifier or consumer settings before deriving an effect.");
            }

            if (!preset.overrideColour && (preset.vocabulary == null || preset.vocabulary.palette == null))
            {
                warnings.Add("Choose a vocabulary with a palette, or enable an authored colour override.");
            }

            if (isHandlerMode && preset.sourceHandler == null)
            {
                warnings.Add("Choose a gameplay buff handler to derive its renderer grammar.");
            }

            if (!Enum.IsDefined(typeof(SpellStudioMode), preset.mode)
                || !Enum.IsDefined(typeof(AttributeGroup), preset.attributeGroup))
            {
                warnings.Add("An unknown grammar mode or group will use its default.");
            }

            ElementEntry source = preset.GetSourceEntry();
            if (source == null && !(isHandlerMode && preset.sourceHandler == null))
            {
                if (preset.overrideEntry)
                {
                    warnings.Add("The authored entry is missing.");
                }
                else
                {
                    warnings.Add("Choose a vocabulary containing the selected element, or enable an authored entry.");
                }
            }

            CheckContext(preset, warnings);
            if (source != null)
            {
                CheckEntry(source, warnings);
            }
            return warnings.ToArray();
        }

        // Whether EffectDerivation can read every buff of the handler without meeting a buff that lacks its data
        public static bool IsDerivable(ABuffHandlerFactory handler)
        {
            BuffHandlerFactory factory = handler as BuffHandlerFactory;
            if (handler == null || (factory != null && factory.data == null) || handler.buffFactoryList == null)
            {
                return true;
            }

            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (!HasData(buff))
                {
                    return false;
                }
            }
            return true;
        }

        // The buffs EffectDerivation opens: the modifiers it reads a value from and the ones that apply a consumer
        static bool HasData(ABuffFactory buff)
        {
            if (buff is FlatModifierFactory flat)
            {
                return flat.data != null;
            }

            if (buff is UpgradeModifierFactory upgrade)
            {
                return upgrade.data != null;
            }

            if (buff is SlowModifierFactory slow)
            {
                return slow.data != null;
            }

            if (buff is TimeModifierFactory time)
            {
                return time.data != null;
            }

            if (buff is CurrentWaveModifierFactory wave)
            {
                return wave.data != null;
            }

            if (buff is HPBasedModifierFactory hpBased)
            {
                return hpBased.data != null;
            }

            if (buff is ApplyConsumerBuffFactory applyConsumer)
            {
                return applyConsumer.data != null;
            }

            if (buff is DamageAllEntityOnEntityDieBuffFactory damageAll)
            {
                return damageAll.data != null;
            }

            if (buff is HealAllEntitiesOnRoundEndBuffFactory healAll)
            {
                return healAll.data != null;
            }

            if (buff is ManaOnRoundEndBuffFactory mana)
            {
                return mana.data != null;
            }
            return true;
        }

        static void CheckContext(SpellStudioPreset preset, List<string> warnings)
        {
            bool isElementKnown = Enum.IsDefined(typeof(EffectElement), preset.element)
                && Enum.IsDefined(typeof(EffectFamily), preset.family);
            bool isContextKnown = Enum.IsDefined(typeof(EffectTempo), preset.tempo)
                && Enum.IsDefined(typeof(Entity.EntityType), preset.side);
            if (!isElementKnown || !isContextKnown)
            {
                warnings.Add("An unknown enum value will use its default in the preview.");
            }

            bool isScaleSafe = preset.scale == preset.safeScale && preset.stacks == preset.safeStacks;
            bool isTimingSafe = SpellPresetBounds.InRange(preset.durationSeconds, 0.01f, 120f);
            bool isAmountSafe = SpellPresetBounds.InRange(preset.charges, 0f, SpellStudioPreset.MaxParts)
                && SpellPresetBounds.InRange(preset.amount, -1f, 1f);
            if (!isScaleSafe || !isTimingSafe || !isAmountSafe)
            {
                warnings.Add("Numeric values outside the supported ranges will be bounded in the preview.");
            }

            if (preset.overrideColour && !SpellPresetBounds.IsValidColour(preset.colour))
            {
                warnings.Add("The colour contains invalid or out-of-range channels; the preview will bound them.");
            }
        }

        static void CheckEntry(ElementEntry source, List<string> warnings)
        {
            if (source.parts == null || source.parts.Length == 0)
            {
                warnings.Add("This entry has no shape parts and will be invisible.");
            }

            if (!SpellPresetBounds.InRange(source.cycleSeconds, 0.01f, 120f))
            {
                warnings.Add("Cycle duration must be between 0.01 and 120 seconds.");
            }

            if (!Enum.IsDefined(typeof(EffectMotionKind), source.motion)
                || !Enum.IsDefined(typeof(EffectSocket), source.socket)
                || !Enum.IsDefined(typeof(EffectCount), source.count))
            {
                warnings.Add("An unknown entry enum value will use its default in the preview.");
            }

            if (HasInvalidParts(source.parts) || HasInvalidParts(source.stackBeads)
                || HasInvalidParts(source.criticalRings) || HasInvalidParts(source.sideRim))
            {
                warnings.Add("Part data needs repair: use finite transforms, positive sizes, valid types and no more "
                    + "than 256 parts per layer. Preview values are bounded.");
            }

            int shapes = 0;
            if (source.parts != null)
            {
                foreach (LookPart part in source.parts)
                {
                    if (part.role != PartRole.Stem)
                    {
                        shapes++;
                    }
                }
            }

            if (source.minCount < 0 || source.minCount > shapes)
            {
                warnings.Add("Minimum count must fit the available shape parts.");
            }
        }

        static bool HasInvalidParts(LookPart[] parts)
        {
            if (parts == null || parts.Length > SpellStudioPreset.MaxParts)
            {
                return true;
            }

            foreach (LookPart part in parts)
            {
                bool isPlaced = SpellPresetBounds.InRange(part.position, -50f, 50f)
                    && SpellPresetBounds.InRange(part.euler, -3600f, 3600f);
                bool isSized = SpellPresetBounds.InRange(part.size, 0.001f, 20f)
                    && SpellPresetBounds.InRange(part.glow, 0f, 10f);
                bool isShapeKnown = Enum.IsDefined(typeof(Primitive), part.primitive)
                    && Enum.IsDefined(typeof(PartRole), part.role);
                bool isToneKnown = Enum.IsDefined(typeof(ColourRole), part.colour)
                    && Enum.IsDefined(typeof(CountBand), part.minCount);
                if (!isPlaced || !isSized || !isShapeKnown || !isToneKnown)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
