using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Saves a recipe asset from a part list, with the display scale, joints, roots and arms every creature shares
    public static class CreatureRecipeAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly string vocabularyPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";

        // The healer is a plant whose buds, crown and collars take the heal accent
        public static Color Colour(LookVocabulary vocabulary, ColourRole role)
        {
            return vocabulary.Colour(role, EffectFamily.Heal, LookSide.Plant);
        }

        public static LookVocabulary LoadVocabulary()
        {
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(vocabularyPath);
            if (vocabulary == null)
            {
                Debug.LogError($"[CreatureRecipeAuthoring] Missing {vocabularyPath}.");
            }

            return vocabulary;
        }

        public static CreaturePart Part(
            string id,
            Primitive primitive,
            Vector3 position,
            Vector3 dimensions,
            Color colour,
            Vector3 euler = default,
            int parent = 0,
            float glow = 0f,
            PartRole role = PartRole.Body
        )
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
                role = role,
                isSource = role == PartRole.Tip || role == PartRole.Crown,
            };
        }

        public static CreatureRecipe SaveRecipe(
            string name,
            List<CreaturePart> parts,
            int roots,
            int armCount,
            int seed,
            LookVocabulary vocabulary
        )
        {
            if (!HasInputs(parts, roots, armCount, vocabulary))
            {
                Debug.LogError("[CreatureRecipeAuthoring] Invalid parts, roots, arm count or vocabulary.");
                return null;
            }

            string path = root + "Data/" + name + ".asset";
            CreatureRecipe existing = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
            CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
            recipe.name = existing != null ? existing.name : name;
            try
            {
                parts = new List<CreaturePart>(parts);
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

                    parts.Add(
                        Part(
                            "Joint" + i,
                            Primitive.Sphere,
                            Vector3.up * (stemPart.dimensions.y * 0.38f),
                            Vector3.one * (stemPart.dimensions.x * 1.5f),
                            Colour(vocabulary, ColourRole.Accent),
                            parent: i,
                            role: PartRole.Stem
                        )
                    );
                }

                recipe.parts = parts.ToArray();
                recipe.idle.seed = seed;
                recipe.wiltColour = Colour(vocabulary, ColourRole.Wilt);
                recipe.stoneOchre = Colour(vocabulary, ColourRole.Ochre);
                // The authored body's sphere is the body unit; its rosette reaches the long band from the stem foot.
                recipe.roots = LookComposer.Roots(
                    vocabulary.roots[ReachBand.Long].reach,
                    vocabulary.bodyUnit,
                    vocabulary
                );
                recipe.roots.count = roots;
                recipe.idle.swayFrequency = 0.25f;
                recipe.sourceLocal = new Vector3[armCount];
                recipe.arms = new ArmDefinition[armCount];
                for (int j = 0; j < armCount; j++)
                {
                    // The arms leave the neck on either side, the first on the left
                    float side = 0.26f;
                    if (j % 2 == 0)
                    {
                        side = -0.26f;
                    }

                    recipe.sourceLocal[j] = new Vector3(side, 1.1f, 0f);
                    recipe.arms[j] = LianaShape.Arm(
                        recipe.sourceLocal[j] - parts[0].localPosition,
                        Colour(vocabulary, ColourRole.Stem),
                        Color.clear
                    );
                }

                if (!CreatureValidator.TryValidate(recipe, out string error))
                {
                    Debug.LogError($"[CreatureRecipeAuthoring] {name}: {error}");
                    return null;
                }

                if (existing != null)
                {
                    EditorUtility.CopySerialized(recipe, existing);
                    EditorUtility.SetDirty(existing);
                    return existing;
                }

                AssetDatabase.CreateAsset(recipe, path);
                EditorUtility.SetDirty(recipe);
                return recipe;
            }
            finally
            {
                if (!AssetDatabase.Contains(recipe))
                {
                    Object.DestroyImmediate(recipe);
                }
            }
        }

        static bool HasInputs(List<CreaturePart> parts, int roots, int arms, LookVocabulary vocabulary)
        {
            if (
                parts == null
                || parts.Count == 0
                || parts.Count > CreatureValidator.MaxParts
                || arms < 0
                || arms > ArmPool.MaxArms
                || (roots != 0 && (roots < 4 || roots > 14))
                || vocabulary == null
                || vocabulary.palette == null
                || vocabulary.roots == null
            )
            {
                return false;
            }

            if (!vocabulary.roots.TryGetValue(ReachBand.Long, out LookVocabulary.RootEntry longest) || longest == null)
            {
                return false;
            }

            return !vocabulary.roots.TryGetValue(ReachBand.Mid, out LookVocabulary.RootEntry middle) || middle != null;
        }
    }
}
