using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Saves a recipe asset from a part list, with the display scale, joints, roots and arms every creature shares
    public static class CreatureRecipeAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly string vocabularyPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";
        public static readonly Color body = new Color(0.50f, 0.79f, 0.25f);
        public static readonly Color stem = new Color(0.18f, 0.49f, 0.31f);
        public static readonly Color bud = new Color(0.78f, 0.95f, 0.29f);

        public static CreaturePart Part(string id, Primitive primitive, Vector3 position, Vector3 dimensions, Color colour,
            Vector3 euler = default, int parent = 0, float glow = 0f, PartRole role = PartRole.Body)
        {
            return new CreaturePart
            {
                id = id,
                parent = parent,
                primitive = primitive,
                localPosition = position,
                dimensions = dimensions,
                colour = colour,
                localEuler = euler,
                glow = glow,
                role = role
            };
        }

        public static CreatureRecipe SaveRecipe(string name, List<CreaturePart> parts, int roots, int armCount, int seed)
        {
            string path = root + "Data/" + name + ".asset";
            CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
            if (!recipe)
            {
                recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            // Geometry-only readability: root footprint and gameplay sockets stay unchanged
            Vector3 displayScale = new Vector3(1.45f, 2.05f, 1.45f);
            for (int i = 0; i < parts.Count; i++)
            {
                CreaturePart part = parts[i];
                part.localPosition = Vector3.Scale(part.localPosition, displayScale);
                part.dimensions = Vector3.Scale(part.dimensions, displayScale);
                parts[i] = part;
            }

            // A rounded collar makes capsule endpoints readable without moving their pivots
            int stemCount = parts.Count;
            for (int i = 0; i < stemCount; i++)
            {
                CreaturePart stemPart = parts[i];
                if (stemPart.primitive != Primitive.Capsule)
                {
                    continue;
                }

                parts.Add(Part("Joint" + i, Primitive.Sphere, Vector3.up * (stemPart.dimensions.y * 0.38f),
                    Vector3.one * (stemPart.dimensions.x * 1.5f), bud, parent: i, role: PartRole.Stem));
            }

            recipe.parts = parts.ToArray();
            recipe.idle.seed = seed;
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(vocabularyPath);
            if (vocabulary == null)
            {
                Debug.LogError($"[CreatureRecipeAuthoring] Missing {vocabularyPath}.");
                return null;
            }

            // An authored creature's body sphere is the body unit, and its rosette reaches the long band from the foot
            // of its stem, the same roots a plant grows at that reach
            recipe.roots = LookComposer.Roots(vocabulary.roots[ReachBand.Long].reach, vocabulary.bodyUnit, vocabulary);
            recipe.roots.count = roots;

            recipe.idle.swayFrequency = 0.25f;
            recipe.sourceLocal = new Vector3[armCount];
            recipe.arms = new ArmDefinition[armCount];
            for (int j = 0; j < armCount; j++)
            {
                recipe.sourceLocal[j] = new Vector3(j % 2 == 0 ? -0.26f : 0.26f, 1.1f, 0f);
                recipe.arms[j] = LookComposer.Arm(recipe.sourceLocal[j] - parts[0].localPosition, stem, Color.clear);
            }

            if (!CreatureValidator.TryValidate(recipe, out string error))
            {
                Debug.LogError($"[CreatureRecipeAuthoring] {name}: {error}");
                return null;
            }

            EditorUtility.SetDirty(recipe);
            return recipe;
        }
    }
}
