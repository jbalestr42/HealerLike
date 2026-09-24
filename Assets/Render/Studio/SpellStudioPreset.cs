using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio
{
    // Where a preset takes its element from
    // Stored by value in assets: append new members, never reorder or remove
    public enum SpellStudioMode
    {
        AuthoredElement,
        GrammarChannels,
        GameplayHandler
    }

    // A spell the studio authors: the composer's inputs, a cast context and, when asked, its own copy of an element.
    // Composing never edits the vocabulary or the authored parts.
    [CreateAssetMenu(menuName = "Custom/Data/Render/Spell Studio Preset", fileName = "SpellPreset")]
    public class SpellStudioPreset : ScriptableObject
    {
        public static readonly int MaxParts = 256;

        public string displayName = "Untitled spell";
        [TextArea]
        public string description;
        public EffectVocabulary vocabulary;
        public SpellStudioMode mode;
        public AttributeGroup attributeGroup = AttributeGroup.Offence;
        public ABuffHandlerFactory sourceHandler;
        public bool isSameSide = true;
        public SpellLooks spellLooks;
        public bool useGameplayOverrides = true;
        // Looked up for its delivery only, the preview draws the effect element and not the flight
        public GameObject sourceProjectile;
        public EffectElement element = EffectElement.Burst;
        public EffectFamily family = EffectFamily.Damage;
        public EffectTempo tempo = EffectTempo.Once;
        [Min(0f)]
        public float periodSeconds = 1f;
        [Min(1)]
        public int stacks = 1;
        [Min(0f)]
        public float charges = 1f;
        // A share of the maximum resource, 0.5 reaches the vocabulary's largest amount count
        public float amount = 0.25f;
        [Min(0.01f)]
        public float durationSeconds = 4f;
        public bool critical;
        public Entity.EntityType side = Entity.EntityType.Player;
        [Range(0.05f, 10f)]
        public float scale = 1f;
        public bool overrideEntry;
        public ElementEntry entry = new ElementEntry();
        public bool overrideColour;
        [ColorUsage(true, true)]
        public Color colour = Color.white;

        public float safeScale { get { return SpellPresetBounds.Bounded(scale, 1f, 0.05f, 10f); } }

        public int safeStacks { get { return Mathf.Clamp(stacks, 1, MaxParts); } }

        public Entity.EntityType safeSide { get { return SpellPresetBounds.Defined(side, Entity.EntityType.Player); } }

        // One motion cycle for an impact, the authored length for a status
        public float previewDuration
        {
            get
            {
                if (resolvedChannels.tempo != EffectTempo.Once)
                {
                    return SpellPresetBounds.Bounded(durationSeconds, 4f, 0.01f, 120f);
                }

                ElementEntry source = GetSourceEntry();
                float cycle = 0.6f;
                if (source != null)
                {
                    cycle = source.cycleSeconds;
                }
                return SpellPresetBounds.Bounded(cycle, 0.6f, 0.01f, 120f);
            }
        }

        public EffectChannels resolvedChannels
        {
            get
            {
                EffectChannels channels;
                EffectElement resolved;
                TryResolve(out channels, out resolved);
                return channels;
            }
        }

        public EffectElement resolvedElement
        {
            get
            {
                EffectChannels channels;
                EffectElement resolved;
                TryResolve(out channels, out resolved);
                return resolved;
            }
        }

        public bool usesGameplayOverride
        {
            get { return mode == SpellStudioMode.GameplayHandler && GetOverrideRow() != null; }
        }

        // The runtime composer's count and colour rules over a bounded copy of the entry. A missing source gives null.
        public EffectRecipe Compose()
        {
            EffectChannels channels;
            EffectElement resolved;
            if (!TryResolve(out channels, out resolved))
            {
                return null;
            }

            ElementEntry source = GetSourceEntry();
            if (source == null)
            {
                return null;
            }

            LookPalette palette = null;
            if (vocabulary != null)
            {
                palette = vocabulary.palette;
            }

            // The composer needs a palette, an authored colour previews without one
            if (palette == null && !overrideColour)
            {
                return null;
            }

            ElementEntry safeEntry = SpellPresetBounds.SanitizedEntry(source);
            EffectRecipe recipe = new EffectRecipe();
            recipe.element = resolved;
            recipe.entry = safeEntry;
            recipe.motion = safeEntry.motion;
            recipe.socket = safeEntry.socket;
            recipe.family = channels.family;
            recipe.tempo = channels.tempo;

            recipe.cycleSeconds = safeEntry.cycleSeconds;

            // As in EffectComposer, only a finite positive period replaces the entry's cycle
            bool isTicking = channels.tempo == EffectTempo.PerPeriod;
            if (isTicking && float.IsFinite(channels.periodSeconds) && channels.periodSeconds > 0f)
            {
                recipe.cycleSeconds = channels.periodSeconds;
            }

            recipe.palette = palette;
            Color tint = colour;
            if (!overrideColour)
            {
                tint = EffectComposer.Colour(palette, resolved, channels.family);
            }
            recipe.colour = SpellPresetBounds.SafeColour(tint);

            float safeCharges = SpellPresetBounds.Bounded(charges, 0f, 0f, MaxParts);
            float safeAmount = SpellPresetBounds.Bounded(amount, 0f, -1f, 1f);
            recipe.count = EffectComposer.Count(safeEntry, safeStacks, safeCharges, safeAmount);
            return recipe;
        }

        // The channels and element SpellVisualSink would open, a native row winning over the derivation
        public bool TryResolve(out EffectChannels channels, out EffectElement resolved)
        {
            channels = new EffectChannels();
            channels.family = SpellPresetBounds.Defined(family, EffectFamily.Damage);
            channels.group = SpellPresetBounds.Defined(attributeGroup, AttributeGroup.Offence);
            channels.tempo = SpellPresetBounds.Defined(tempo, EffectTempo.Once);
            channels.periodSeconds = periodSeconds;

            resolved = SpellPresetBounds.Defined(element, EffectElement.Burst);
            SpellStudioMode safeMode = SpellPresetBounds.Defined(mode, SpellStudioMode.AuthoredElement);
            if (safeMode == SpellStudioMode.AuthoredElement)
            {
                return true;
            }

            if (safeMode == SpellStudioMode.GameplayHandler)
            {
                if (sourceHandler == null)
                {
                    return false;
                }

                SpellLook row = GetOverrideRow();
                if (row != null)
                {
                    channels.family = SpellPresetBounds.Defined(row.family, EffectFamily.Damage);
                    channels.tempo = SpellPresetBounds.Defined(row.tempo, EffectTempo.Once);
                    channels.periodSeconds = EffectDerivation.Period(sourceHandler);
                    resolved = SpellPresetBounds.Defined(row.element, EffectElement.Burst);
                    return true;
                }

                // A buff added in the inspector can still lack its data, the derivation would read through it
                if (!SpellPresetValidator.IsDerivable(sourceHandler))
                {
                    channels = new EffectChannels();
                    return false;
                }
                channels = EffectDerivation.Channels(sourceHandler, isSameSide);
            }

            resolved = EffectComposer.Element(channels);
            return true;
        }

        public ProjectileLook ResolveProjectile()
        {
            if (spellLooks != null && useGameplayOverrides)
            {
                return spellLooks.GetProjectileLook(sourceProjectile);
            }

            ProjectileLook look = new ProjectileLook();
            look.style = EffectDerivation.Delivery(sourceProjectile);
            return look;
        }

        // Copies the vocabulary's entry for the resolved element into the preset, the shared asset stays as it is
        public bool CaptureEntry()
        {
            if (vocabulary == null || vocabulary.elements == null)
            {
                return false;
            }

            EffectChannels channels;
            EffectElement resolved;
            if (!TryResolve(out channels, out resolved))
            {
                return false;
            }

            if (!vocabulary.elements.ContainsKey(resolved) || vocabulary.elements[resolved] == null)
            {
                return false;
            }

            entry = SpellPresetBounds.CloneEntry(vocabulary.elements[resolved]);
            overrideEntry = true;
            return true;
        }

        // The authored entry when the preset owns one, else the vocabulary's entry for the resolved element
        public ElementEntry GetSourceEntry()
        {
            if (overrideEntry)
            {
                return entry;
            }

            if (vocabulary == null || vocabulary.elements == null)
            {
                return null;
            }

            EffectChannels channels;
            EffectElement resolved;
            if (!TryResolve(out channels, out resolved) || !vocabulary.elements.ContainsKey(resolved))
            {
                return null;
            }
            return vocabulary.elements[resolved];
        }

        // The native row for the handler when the preset lets native rows win, else null
        SpellLook GetOverrideRow()
        {
            if (!useGameplayOverrides || spellLooks == null || spellLooks.buffs == null || sourceHandler == null)
            {
                return null;
            }

            if (!spellLooks.buffs.ContainsKey(sourceHandler))
            {
                return null;
            }
            return spellLooks.buffs[sourceHandler];
        }
    }
}
