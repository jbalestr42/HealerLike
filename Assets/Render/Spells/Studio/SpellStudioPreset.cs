using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Spells.Studio
{
    /// <summary>A portable authoring asset. Composition never edits the source vocabulary or the authored parts.</summary>
    [CreateAssetMenu(menuName = "Custom/Data/Render/Spell Studio Preset", fileName = "SpellPreset")]
    public sealed class SpellStudioPreset : ScriptableObject
    {
        public string displayName = "Untitled spell";
        [TextArea] public string description;
        public EffectVocabulary vocabulary;
        public EffectElement element = EffectElement.Burst;
        public EffectFamily family = EffectFamily.Damage;
        public EffectTempo tempo = EffectTempo.Once;
        [Min(0.01f)] public float periodSeconds = 1f;
        [Min(1)] public int stacks = 1;
        [Min(0f)] public float charges = 1f;
        [Tooltip("Fraction of maximum resource; 0.5 reaches the vocabulary's maximum amount count.")]
        public float amount = 0.25f;
        [Min(0.01f)] public float durationSeconds = 4f;
        public bool critical;
        public Entity.EntityType side = Entity.EntityType.Player;
        [Range(0.05f, 10f)] public float scale = 1f;
        public bool overrideEntry;
        public ElementEntry entry = new ElementEntry();
        public bool overrideColour;
        [ColorUsage(true, true)] public Color colour = Color.white;

        public float SafeScale => Bounded(scale, 1f, 0.05f, 10f);
        public int SafeStacks => Mathf.Clamp(stacks, 1, MaxParts);
        public Entity.EntityType SafeSide => Defined(side, Entity.EntityType.Player);
        public float PreviewDuration
        {
            get
            {
                if (Defined(tempo, EffectTempo.Once) != EffectTempo.Once)
                    return Bounded(durationSeconds, 4f, 0.01f, 120f);
                return Bounded(SourceEntry()?.cycleSeconds ?? 0.6f, 0.6f, 0.01f, 120f);
            }
        }

        public const int MaxParts = 256;

        /// <summary>Uses the runtime composer’s count and colour rules with a sanitized, private entry. Missing sources return null without logging.</summary>
        public EffectRecipe Compose()
        {
            ElementEntry source = SourceEntry();
            if (source == null) return null;

            ElementEntry safeEntry = SanitizedEntry(source);
            EffectElement safeElement = Defined(element, EffectElement.Burst);
            EffectFamily safeFamily = Defined(family, EffectFamily.Damage);
            EffectTempo safeTempo = Defined(tempo, EffectTempo.Once);
            LookPalette palette = vocabulary != null ? vocabulary.palette : null;
            // Mirror the runtime composer's recipe assembly, sharing its count and colour decisions.
            // No temporary Unity objects are needed while the timeline is being scrubbed.
            return new EffectRecipe
            {
                element = safeElement,
                entry = safeEntry,
                motion = safeEntry.motion,
                socket = safeEntry.socket,
                family = safeFamily,
                tempo = safeTempo,
                cycleSeconds = safeTempo == EffectTempo.PerPeriod
                    ? Bounded(periodSeconds, 1f, 0.01f, 120f) : safeEntry.cycleSeconds,
                palette = palette,
                colour = SafeColour(overrideColour ? colour : EffectComposer.Colour(palette, safeElement, safeFamily)),
                count = EffectComposer.Count(safeEntry, SafeStacks, Bounded(charges, 0f, 0f, MaxParts),
                    Bounded(amount, 0f, -1f, 1f))
            };
        }

        /// <summary>Copies the selected vocabulary element for editing without changing the shared asset.</summary>
        public bool CaptureEntry()
        {
            if (vocabulary == null || vocabulary.elements == null ||
                !vocabulary.elements.TryGetValue(element, out ElementEntry source) || source == null)
                return false;
            entry = CloneEntry(source);
            overrideEntry = true;
            return true;
        }

        public static ElementEntry CloneEntry(ElementEntry source)
        {
            if (source == null) return null;
            return new ElementEntry
            {
                parts = Copy(source.parts), stackBeads = Copy(source.stackBeads),
                criticalRings = Copy(source.criticalRings), sideRim = Copy(source.sideRim),
                motion = source.motion, socket = source.socket, count = source.count,
                minCount = source.minCount, cycleSeconds = source.cycleSeconds
            };
        }

        /// <summary>Nonmutating diagnostics. Compose uses bounded defaults for malformed numeric input.</summary>
        public string[] Validate()
        {
            var warnings = new List<string>();
            ElementEntry source = SourceEntry();
            if (source == null)
                warnings.Add(overrideEntry ? "The authored entry is missing." : "Choose a vocabulary containing the selected element, or enable an authored entry.");
            if (!Enum.IsDefined(typeof(EffectElement), element) || !Enum.IsDefined(typeof(EffectFamily), family) ||
                !Enum.IsDefined(typeof(EffectTempo), tempo) || !Enum.IsDefined(typeof(Entity.EntityType), side))
                warnings.Add("An unknown enum value will use its default in the preview.");
            if (scale != SafeScale || stacks != SafeStacks || !InRange(periodSeconds, 0.01f, 120f) ||
                !InRange(durationSeconds, 0.01f, 120f) || !InRange(charges, 0f, MaxParts) || !InRange(amount, -1f, 1f))
                warnings.Add("Numeric values outside the supported ranges will be bounded in the preview.");
            if (overrideColour && !ValidColour(colour))
                warnings.Add("The colour contains invalid or out-of-range channels; the preview will bound them.");
            if (source != null)
            {
                if (source.parts == null || source.parts.Length == 0)
                    warnings.Add("This entry has no shape parts and will be invisible.");
                if (!InRange(source.cycleSeconds, 0.01f, 120f))
                    warnings.Add("Cycle duration must be between 0.01 and 120 seconds.");
                if (!Enum.IsDefined(typeof(EffectMotion), source.motion) || !Enum.IsDefined(typeof(EffectSocket), source.socket) ||
                    !Enum.IsDefined(typeof(EffectCount), source.count))
                    warnings.Add("An unknown entry enum value will use its default in the preview.");
                if (InvalidParts(source.parts) || InvalidParts(source.stackBeads) ||
                    InvalidParts(source.criticalRings) || InvalidParts(source.sideRim))
                    warnings.Add("Part data needs repair: use finite transforms, positive sizes, valid types and no more than 256 parts per layer. Preview values are bounded.");
                int shapes = 0;
                if (source.parts != null)
                    foreach (LookPart part in source.parts) if (part.role != PartRole.Stem) shapes++;
                if (source.minCount < 0 || source.minCount > shapes)
                    warnings.Add("Minimum count must fit the available shape parts.");
            }
            return warnings.ToArray();
        }

        ElementEntry SourceEntry()
        {
            if (overrideEntry) return entry;
            if (vocabulary == null || vocabulary.elements == null) return null;
            vocabulary.elements.TryGetValue(Defined(element, EffectElement.Burst), out ElementEntry source);
            return source;
        }

        static ElementEntry SanitizedEntry(ElementEntry source)
        {
            ElementEntry result = new ElementEntry();
            result.parts = SafeParts(source.parts);
            result.stackBeads = SafeParts(source.stackBeads);
            result.criticalRings = SafeParts(source.criticalRings);
            result.sideRim = SafeParts(source.sideRim);
            result.motion = Defined(source.motion, EffectMotion.Burst);
            result.socket = Defined(source.socket, EffectSocket.Body);
            result.count = Defined(source.count, EffectCount.Fixed);
            result.minCount = Mathf.Clamp(source.minCount, 0, EffectComposer.Shapes(result));
            result.cycleSeconds = Bounded(source.cycleSeconds, 0.6f, 0.01f, 120f);
            return result;
        }

        static LookPart[] Copy(LookPart[] source) => source == null ? Array.Empty<LookPart>() : (LookPart[])source.Clone();

        static LookPart[] SafeParts(LookPart[] source)
        {
            if (source == null) return Array.Empty<LookPart>();
            var result = new LookPart[Mathf.Min(source.Length, MaxParts)];
            for (int i = 0; i < result.Length; i++)
            {
                LookPart part = source[i];
                part.id = string.IsNullOrEmpty(part.id) ? "Part " + (i + 1) : part.id;
                part.primitive = Defined(part.primitive, default(Primitive));
                part.role = Defined(part.role, PartRole.Body);
                part.colour = Defined(part.colour, ColourRole.Accent);
                part.minCount = Defined(part.minCount, default(CountBand));
                part.position = SafeVector(part.position, 0f, -50f, 50f);
                part.euler = SafeVector(part.euler, 0f, -3600f, 3600f);
                part.size = SafeVector(part.size, 0.1f, 0.001f, 20f);
                part.glow = Bounded(part.glow, 0f, 0f, 10f);
                result[i] = part;
            }
            return result;
        }

        static bool InvalidParts(LookPart[] parts)
        {
            if (parts == null || parts.Length > MaxParts) return true;
            foreach (LookPart part in parts)
                if (!InRange(part.position, -50f, 50f) || !InRange(part.euler, -3600f, 3600f) ||
                    !InRange(part.size, 0.001f, 20f) || !InRange(part.glow, 0f, 10f) ||
                    !Enum.IsDefined(typeof(Primitive), part.primitive) || !Enum.IsDefined(typeof(PartRole), part.role) ||
                    !Enum.IsDefined(typeof(ColourRole), part.colour) || !Enum.IsDefined(typeof(CountBand), part.minCount)) return true;
            return false;
        }

        static T Defined<T>(T value, T fallback) where T : struct => Enum.IsDefined(typeof(T), value) ? value : fallback;
        static float Bounded(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
        static Vector3 SafeVector(Vector3 value, float fallback, float min, float max) => new Vector3(
            Bounded(value.x, fallback, min, max), Bounded(value.y, fallback, min, max), Bounded(value.z, fallback, min, max));
        static bool InRange(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
        static bool InRange(Vector3 value, float min, float max) => InRange(value.x, min, max) && InRange(value.y, min, max) && InRange(value.z, min, max);
        static bool ValidColour(Color value) => InRange(value.r, 0f, 16f) && InRange(value.g, 0f, 16f) && InRange(value.b, 0f, 16f) && InRange(value.a, 0f, 1f);
        static Color SafeColour(Color value) => new Color(Bounded(value.r, 1f, 0f, 16f), Bounded(value.g, 1f, 0f, 16f), Bounded(value.b, 1f, 0f, 16f), Bounded(value.a, 1f, 0f, 1f));
    }
}
