using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells.Editor
{
    // Batchmode diagnostic. It serializes the values consumed by SpellEffect and SpellGround,
    // without changing any authored assets.
    public static class RecipeSnapshot
    {
        const string VocabularyPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";
        [Serializable] class Snapshot { public string subject; public List<Recipe> recipes = new List<Recipe>(); }
        [Serializable] class Recipe
        {
            public string label, element, motion, socket, family, tempo, colour, entry;
            public int count, minCount, partCount;
            public float scale, cycleSeconds, periodSeconds, groundRadius, groundStrength;
            public EffectPresentation presentation;
            public GroundEffect ground;
            public string[] parts;
            public Recipe[] additions;
        }

        [MenuItem("Tools/Render/Write Recipe Snapshots")]
        public static void Write()
        {
            string folder = global::System.Environment.GetEnvironmentVariable("RENDER_SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(folder)) throw new InvalidOperationException("RENDER_SNAPSHOT_DIR is required.");
            Directory.CreateDirectory(folder);
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(VocabularyPath);
            if (vocabulary == null) throw new InvalidOperationException("Missing " + VocabularyPath);
            foreach (EffectKey key in Enum.GetValues(typeof(EffectKey)))
                WriteOne(folder, "fixture-" + ((int)key).ToString("D2") + "-" + key, new[] {
                    EffectComposer.Compose(vocabulary, key, Family(key), EffectTempo.Once, 0f, 3, 3, .5f) });
            foreach (EffectOperation operation in Enum.GetValues(typeof(EffectOperation)))
                foreach (EffectAspect aspect in Enum.GetValues(typeof(EffectAspect)))
                    foreach (EffectTempo tempo in new[] { EffectTempo.Once, EffectTempo.PerPeriod })
                    {
                        EffectChannels channels = new EffectChannels { operation = operation, aspect = aspect,
                            group = (AttributeGroup)aspect, family = Family(operation), tempo = tempo,
                            periodSeconds = 1.37f, magnitude = EffectMagnitude.Solid };
                        WriteOne(folder, "cell-" + operation + "-" + aspect + "-" + tempo,
                            new[] { EffectComposer.Compose(vocabulary, channels, 3, 3) });
                    }
            foreach (EffectPiece piece in Enum.GetValues(typeof(EffectPiece)))
                WriteOne(folder, "piece-" + piece, new[] { EffectComposer.Compose(vocabulary,
                    (EffectKey)((int)EffectKey.Beam + (int)piece), EffectFamily.Heal, EffectTempo.Once, 0f, 3, 3, .5f) });
            Debug.Log("[RecipeSnapshot] Wrote snapshots to " + folder);
        }

        static void WriteOne(string folder, string name, IEnumerable<EffectRecipe> source)
        {
            Snapshot snapshot = new Snapshot { subject = name };
            foreach (EffectRecipe recipe in source ?? Enumerable.Empty<EffectRecipe>())
                if (recipe != null) snapshot.recipes.Add(Copy(recipe, 0f));
            string json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(Path.Combine(folder, Safe(name) + ".json"), json, Encoding.UTF8);
        }

        static Recipe Copy(EffectRecipe recipe, float period)
        {
            ElementEntry e = recipe.entry;
            LookPart[] parts = e != null && e.parts != null ? e.parts : Array.Empty<LookPart>();
            return new Recipe { label = e != null ? e.label : null, element = recipe.element.ToString(),
                entry = recipe.element.ToString(), motion = recipe.motion.ToString(), socket = recipe.socket.ToString(),
                family = recipe.family.ToString(), tempo = recipe.tempo.ToString(), colour = ColorUtility.ToHtmlStringRGBA(recipe.colour),
                count = recipe.count, minCount = e != null ? e.minCount : 0, scale = recipe.scale,
                cycleSeconds = recipe.cycleSeconds, periodSeconds = period, presentation = recipe.presentation,
                ground = e != null ? e.ground : null, groundRadius = e != null ? e.groundRadius : 0f,
                groundStrength = e != null ? e.groundStrength : 0f, partCount = parts.Length,
                parts = parts.Select(p => p.id).ToArray(),
                additions = (recipe.additions ?? Array.Empty<EffectRecipe>()).Select(a => Copy(a, period)).ToArray() };
        }

        static string Safe(string value) { return value.Replace('/', '_').Replace(' ', '_'); }
        static EffectFamily Family(EffectKey key)
        {
            switch (key) { case EffectKey.Rise: case EffectKey.Ring: case EffectKey.Beam: return EffectFamily.Heal;
                case EffectKey.Stalks: return EffectFamily.Renew; case EffectKey.Drips: return EffectFamily.Rot;
                case EffectKey.Orbit: case EffectKey.Plates: case EffectKey.Bud: return EffectFamily.Boon;
                case EffectKey.Press: case EffectKey.Crack: case EffectKey.Litter: return EffectFamily.Bane;
                default: return EffectFamily.Damage; }
        }
        static EffectFamily Family(EffectOperation operation)
        {
            switch (operation) { case EffectOperation.Heal: return EffectFamily.Heal; case EffectOperation.Boon: return EffectFamily.Boon;
                case EffectOperation.Bane: return EffectFamily.Bane; case EffectOperation.Ward: return EffectFamily.Boon;
                default: return EffectFamily.Damage; }
        }
    }
}
