using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public static class EffectValidator
    {
        public const int MaxParts = EffectCompositionValidator.MaxParts;

        // Iterative traversal bounds authoring mistakes before any meshes or game objects are allocated.
        // Shared children are legal, but every rendered occurrence consumes the same aggregate budget.
        public static bool TryValidate(EffectRecipe recipe, out string error)
        {
            var pending = new Stack<(EffectRecipe recipe, bool exit)>();
            var ancestors = new HashSet<EffectRecipe>();
            pending.Push((recipe, false));
            long budget = 0;
            while (pending.Count > 0)
            {
                var next = pending.Pop();
                if (next.exit) { ancestors.Remove(next.recipe); continue; }
                EffectRecipe current = next.recipe;
                if (current == null) return Fail("Require a non-null effect recipe and additions.", out error);
                if (!ancestors.Add(current)) return Fail("Effect additions contain a cycle.", out error);
                if (!TryValidateEntry(current.entry, out error)) return false;
                budget += PartCount(current.entry);
                if (budget > MaxParts) return Fail("Require 1..256 effect parts across all additions.", out error);
                if (!TryValidateRecipeData(current, out error)) return false;
                pending.Push((current, true));
                if (current.additions == null) continue;
                if (current.additions.Length > MaxParts)
                    return Fail("Require 1..256 effect parts across all additions.", out error);
                for (int i = current.additions.Length - 1; i >= 0; i--)
                    pending.Push((current.additions[i], false));
            }
            error = null;
            return true;
        }

        public static bool TryValidateEntry(ElementEntry entry, out string error)
        {
            if (entry == null || entry.parts == null || entry.parts.Length == 0)
                return Fail("Require an effect entry with at least one part.", out error);
            if (PartCount(entry) > MaxParts)
                return Fail("Require 1..256 effect parts including beads, rings and rims.", out error);
            if (!Fragment(entry.parts, "parts", out error)
                || !Fragment(entry.stackBeads, "stack beads", out error)
                || !Fragment(entry.criticalRings, "critical rings", out error)
                || !Fragment(entry.sideRim, "side rim", out error)) return false;
            int shapes = 0;
            foreach (LookPart part in entry.parts) if (part.role != PartRole.Stem) shapes++;
            if (!Defined(entry.motion) || !Defined(entry.socket) || !Defined(entry.count))
                return Fail("Require valid entry motion, socket and count values.", out error);
            if (shapes == 0 || entry.minCount < 1 || entry.minCount > shapes)
                return Fail("Require an entry minimum count within its available shape parts.", out error);
            if (!Positive(entry.cycleSeconds)) return Fail("Require a positive finite entry cycle.", out error);
            if (entry.presentation != null && !entry.presentation.IsValid())
                return Fail("Require valid effect presentation values.", out error);
            if (entry.ground != null && (!Positive(entry.groundRadius) || !Range(entry.groundStrength, 0f, 1f)
                || !GroundValid(entry.ground))) return Fail("Require valid ground reaction bounds and timing.", out error);
            error = null;
            return true;
        }

        static bool TryValidateRecipeData(EffectRecipe recipe, out string error)
        {
            if (!Defined(recipe.element) || !Defined(recipe.motion) || !Defined(recipe.socket)
                || !Defined(recipe.family) || !Defined(recipe.tempo))
                return Fail("Require valid recipe element, motion, socket, family and tempo values.", out error);
            if (!EffectCompositionValidator.TryValidateChannels(recipe.channels, out error)) return false;
            int shapes = 0;
            foreach (LookPart part in recipe.entry.parts) if (part.role != PartRole.Stem) shapes++;
            if (recipe.count < 1 || recipe.count > shapes)
                return Fail("Require a recipe count within its available shape parts (1..256).", out error);
            // Zero is a legacy unspecified cycle; the entry still provides a strictly positive fallback.
            if (!Nonnegative(recipe.cycleSeconds) || !Positive(recipe.scale) || !Finite(recipe.colour))
                return Fail("Require finite recipe colour, positive scale and nonnegative cycle.", out error);
            error = null;
            return true;
        }

        static bool Fragment(LookPart[] parts, string label, out string error)
        {
            error = null;
            if (parts == null || parts.Length == 0) return true;
            foreach (LookPart part in parts)
                if (!LookPartValidation.IsValid(part, LookPartBounds.Spell))
                    return Fail("Require valid spell part data in " + label + ".", out error);
            if (!FragmentPlacement.TryValidate(parts, CountBand.Many, out string placement))
                return Fail("Invalid " + label + " fragment: " + placement, out error);
            return true;
        }

        static long PartCount(ElementEntry entry) => (long)entry.parts.Length + (entry.stackBeads?.Length ?? 0)
            + (entry.criticalRings?.Length ?? 0) + (entry.sideRim?.Length ?? 0);

        static bool GroundValid(GroundEffect effect) => Defined(effect.shape)
            && Nonnegative(effect.grow) && Nonnegative(effect.fadeIn) && Nonnegative(effect.lifetime)
            && Nonnegative(effect.release) && Range(effect.edge, 0f, 1f) && Range(effect.wobble, 0f, 1f)
            && Range(effect.band, 0f, 1f) && Nonnegative(effect.minBand) && Positive(effect.width)
            && Nonnegative(effect.swing) && Nonnegative(effect.turns) && float.IsFinite(effect.hold)
            && float.IsFinite(effect.holdTurn) && float.IsFinite(effect.kick) && float.IsFinite(effect.kickTurn)
            && Nonnegative(effect.shiver) && Range(effect.flatten, 0f, 1f) && Range(effect.ash, 0f, 1f)
            && Range(effect.vitality, -1f, 1f) && Range(effect.light, -1f, 1f) && Range(effect.blight, 0f, 1f);

        static bool Finite(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g)
            && float.IsFinite(value.b) && float.IsFinite(value.a);
        static bool Positive(float value) => float.IsFinite(value) && value > 0f;
        static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0f;
        static bool Range(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
        static bool Defined<T>(T value) where T : Enum => Enum.IsDefined(typeof(T), value);
        static bool Fail(string message, out string error) { error = message; return false; }
    }
}
