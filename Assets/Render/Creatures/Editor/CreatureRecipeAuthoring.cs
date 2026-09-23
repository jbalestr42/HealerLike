using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Saves a recipe asset from a part list, with the display scale, joints, roots and arms every creature shares
    public static class CreatureRecipeAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        public static readonly Color body = new Color(0.50f, 0.79f, 0.25f);
        public static readonly Color stem = new Color(0.18f, 0.49f, 0.31f);
        public static readonly Color bud = new Color(0.78f, 0.95f, 0.29f);

        public static CreaturePart Part(string id, Primitive primitive, Vector3 position, Vector3 dimensions, Color colour,
            Vector3 euler = default, int parent = 0, float glow = 0f)
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
                torusTubeRatio = 0.2f,
                glow = glow
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
            float displayHeight = name == "Healer" ? 2.05f : 1.7f;
            Vector3 displayScale = new Vector3(1.45f, displayHeight, 1.45f);
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
                    Vector3.one * (stemPart.dimensions.x * 1.5f), bud, parent: i));
            }

            recipe.parts = parts.ToArray();
            recipe.roots.count = roots;
            recipe.roots.segments = 3;
            recipe.idle.seed = seed;
            // Roots lie along the ground from the body's base, reaching as far as the derived units
            recipe.roots.thickness = LookComposer.RootThickness;
            recipe.roots.footRadius = LookComposer.PinnedReach * LookComposer.BodyUnit;
            recipe.roots.hipHeight = LookComposer.RootHip;
            recipe.roots.kneeHeight = LookComposer.RootKnee;
            if (name == "Healer")
            {
                // The healer's rosette reaches 2.1 body units, from the foot of its stem
                recipe.roots.footRadius = LookComposer.LongReach * LookComposer.BodyUnit;
                recipe.roots.kneeHeight = 0.07f;
                recipe.roots.thickness = 0.049f;
            }

            recipe.idle.swayFrequency = 0.25f;
            recipe.targetLocal = Vector3.up * 0.8f;
            recipe.sourceLocal = new Vector3[armCount];
            recipe.arms = new ArmDefinition[armCount];
            for (int j = 0; j < armCount; j++)
            {
                recipe.sourceLocal[j] = new Vector3(j % 2 == 0 ? -0.26f : 0.26f, 1.1f, 0f);
                Vector3[] rest = new Vector3[49];
                // Four compact coils: 48 x 0.5 = 24 cells covers the current 16 x 16 board.
                // Larger boards and targets outside the board still clamp without changing gameplay.
                for (int i = 0; i < 48; i++)
                {
                    float angle = i * Mathf.PI * 2f / 12f;
                    rest[i + 1] = rest[i] + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.015f).normalized * 0.5f;
                }

                recipe.arms[j] = new ArmDefinition
                {
                    bodyPart = 0,
                    rootLocal = recipe.sourceLocal[j] - parts[0].localPosition,
                    sourceSocketIndex = j,
                    segmentCount = 48,
                    segmentLength = 0.5f,
                    radius = 0.045f,
                    restJoints = rest,
                    bendPole = Vector3.up,
                    colour = stem
                };
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
