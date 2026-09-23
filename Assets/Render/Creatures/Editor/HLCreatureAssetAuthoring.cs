using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public static class HLCreatureAssetAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly Color body = new Color(0.50f, 0.79f, 0.25f);
        static readonly Color stem = new Color(0.18f, 0.49f, 0.31f);
        static readonly Color bud = new Color(0.78f, 0.95f, 0.29f);

        [MenuItem("HL/Creatures/Author Presentation Assets")]
        public static void Author()
        {
            Directory.CreateDirectory(root + "Data");
            Directory.CreateDirectory(root + "Prefabs");
            Material material = null;
            if (AssetDatabase.IsValidFolder("Assets/Render/Look"))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new string[] { "Assets/Render/Look" }))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (material)
                    {
                        break;
                    }
                }
            }

            if (!material)
            {
                string path = root + "Data/HLPlaceholder.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    material = new Material(shader) { name = "HLPlaceholder", enableInstancing = true };
                    material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_Smoothness", 0.2f);
                    AssetDatabase.CreateAsset(material, path);
                }
            }

            HLCreatureRecipe healer = SaveRecipe("HLHealer", Healer(), 6, 2, 17);
            HLCreatureRecipe fern = SaveRecipe("HLSpiralFern", Fern(), 7, 2, 31);
            HLCreatureRecipe arch = SaveRecipe("HLHangingArch", Arch(), 6, 4, 57);
            HLCreatureRecipe rosette = SaveRecipe("HLBladeRosette", Rosette(), 6, 2, 103);
            HLCreatureRecipe stack = SaveRecipe("HLSphereStack", Stack(), 8, 1, 89);
            Model("HLNormal", "Assets/Models/Jomon.prefab", fern, material);
            Model("HLTest", "Assets/Models/Jomon.prefab", stack, material);
            Model("HLSwarm", "Assets/Models/Jomon.prefab", arch, material);
            Model("HLFastShoot", "Assets/Models/OwlZun.prefab", fern, material);
            Model("HLTripleShoot", "Assets/Models/OwlZun.prefab", arch, material);
            Model("HLMultiShot", "Assets/Models/LakshmiTower.prefab", arch, material);
            Model("HLRandomShoot", "Assets/Models/LakshmiTower.prefab", fern, material);
            Model("HLChainLightning", "Assets/Models/LightningTower.prefab", stack, material);
            Model("HLChanneling", "Assets/Models/SlowTowerModel.prefab", stack, material);
            Model("HLSoldier", "Assets/Models/Kawaii Slime/Prefabs/Slime_01_Viking.prefab", stack, material);
            Model("HLHitArmorBuffer", "Assets/Models/Kawaii Slime/Prefabs/Slime_03 Leaf.prefab", rosette, material);
            CharacterView(healer, material);
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab"))
            {
                ProjectileView(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HL creature recipes and presentation prefab variants authored.");
        }

        // Recipe-only refresh keeps existing prefab presentation and delivery overrides intact
        public static void AuthorBeautyRecipes()
        {
            SaveRecipe("HLHealer", Healer(), 6, 2, 17);
            SaveRecipe("HLSpiralFern", Fern(), 7, 2, 31);
            SaveRecipe("HLHangingArch", Arch(), 6, 4, 57);
            SaveRecipe("HLBladeRosette", Rosette(), 6, 2, 103);
            SaveRecipe("HLSphereStack", Stack(), 8, 1, 89);
            AssetDatabase.SaveAssets();
        }

        static HLPart Part(string id, HLPrimitive primitive, Vector3 position, Vector3 dimensions, Color colour,
            Vector3 euler = default, int parent = 0, float glow = 0f)
        {
            return new HLPart
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

        static List<HLPart> Base()
        {
            return new List<HLPart>
            {
                Part("HLStem", HLPrimitive.Capsule, new Vector3(0f, 0.25f, 0f), new Vector3(0.11f, 0.5f, 0.11f), stem,
                    parent: -1)
            };
        }

        static List<HLPart> Healer()
        {
            List<HLPart> parts = Base();
            parts.Add(Part("HLBulb", HLPrimitive.Cone, new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.62f, 0.45f),
                body, new Vector3(0f, 0f, 180f)));
            parts.Add(Part("HLHip", HLPrimitive.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.38f, 0.30f, 0.38f),
                body));
            parts.Add(Part("HLCrown", HLPrimitive.Torus, new Vector3(0f, 0.86f, 0f), new Vector3(0.60f, 0.3f, 0.48f),
                bud, glow: 0.4f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 0.24f, 0.69f, Mathf.Sin(angle) * 0.24f);
                Vector3 euler = new Vector3(0f, -angle * Mathf.Rad2Deg, -25f);
                Vector3 size = new Vector3(0.13f, 0.23f, 0.13f);
                parts.Add(Part("HLBud" + i, HLPrimitive.Sphere, position, size, bud, euler, glow: 1.6f));
            }

            return parts;
        }

        static List<HLPart> Rosette()
        {
            List<HLPart> parts = new List<HLPart>
            {
                Part("HLRosette", HLPrimitive.Sphere, Vector3.up * 0.15f, new Vector3(0.4f, 0.3f, 0.4f), body,
                    parent: -1)
            };
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 position = new Vector3(0.26f * Mathf.Cos(angle), 0.28f, 0.26f * Mathf.Sin(angle));
                Vector3 facing = new Vector3(Mathf.Cos(angle), 0.65f, Mathf.Sin(angle));
                Vector3 euler = Quaternion.FromToRotation(Vector3.up, facing).eulerAngles;
                Vector3 size = new Vector3(0.2f, 0.72f, 0.1f);
                parts.Add(Part("HLBud" + i, HLPrimitive.Cone, position, size, body, euler, glow: 0.7f));
            }

            return parts;
        }

        static List<HLPart> Fern()
        {
            List<HLPart> parts = Base();
            Vector3 previous = Vector3.zero;
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 0.53f;
                float radius = 0.31f * (1f - i * 0.055f);
                float height = 0.13f + i * 0.075f;
                Vector3 point = new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius * 0.35f);
                Vector3 delta = point - previous;
                Vector3 stemSize = new Vector3(0.065f, delta.magnitude + 0.035f, 0.065f);
                Vector3 stemEuler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
                Vector3 middle = (point + previous) * 0.5f;
                parts.Add(Part("HLFernStem" + i, HLPrimitive.Capsule, middle, stemSize, stem, stemEuler));
                Vector3 frondSize = new Vector3(0.28f * (1f - i * 0.04f), 0.07f, 0.15f);
                Vector3 frondEuler = new Vector3(0f, i * 29f, i % 2 == 0 ? 32f : -32f);
                Color frondColour = i % 2 == 0 ? body : bud;
                parts.Add(Part("HLFrond" + i, HLPrimitive.Sphere, point, frondSize, frondColour, frondEuler,
                    glow: i > 6 ? 0.45f : 0f));
                previous = point;
            }

            return parts;
        }

        static List<HLPart> Arch()
        {
            List<HLPart> parts = Base();
            parts[0] = Part("HLArchFoot", HLPrimitive.Capsule, new Vector3(-0.29f, 0.2f, 0f),
                new Vector3(0.10f, 0.4f, 0.1f), stem, parent: -1);
            Vector3 previous = new Vector3(0f, 0.12f, 0f);
            for (int i = 1; i <= 8; i++)
            {
                float angle = Mathf.PI - i * Mathf.PI / 8f;
                Vector3 point = new Vector3(0.29f + Mathf.Cos(angle) * 0.29f, 0.12f + Mathf.Sin(angle) * 0.75f, 0f);
                Vector3 delta = point - previous;
                Vector3 size = new Vector3(0.09f, delta.magnitude + 0.04f, 0.09f);
                Vector3 euler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
                parts.Add(Part("HLArch" + i, HLPrimitive.Capsule, (point + previous) * 0.5f, size, body, euler));
                previous = point;
            }

            for (int i = 0; i < 3; i++)
            {
                float x = 0.13f + i * 0.16f;
                float y = i == 1 ? 0.58f : 0.41f;
                parts.Add(Part("HLPodStem" + i, HLPrimitive.CylinderSegment, new Vector3(x, y + 0.12f, 0f),
                    new Vector3(0.028f, 0.25f, 0.028f), stem));
                parts.Add(Part("HLPod" + i, HLPrimitive.Sphere, new Vector3(x, y - 0.06f, 0f),
                    new Vector3(0.16f, 0.28f, 0.18f), bud, glow: 0.6f));
            }

            return parts;
        }

        static List<HLPart> Stack()
        {
            List<HLPart> parts = new List<HLPart>
            {
                Part("HLConicalRoot", HLPrimitive.Cone, new Vector3(0f, 0.2f, 0f), new Vector3(0.30f, 0.4f, 0.30f),
                    stem, parent: -1)
            };
            parts.Add(Part("HLBottomSphere", HLPrimitive.Sphere, new Vector3(0f, 0.34f, 0f), Vector3.one * 0.43f,
                body));
            parts.Add(Part("HLMiddleSphere", HLPrimitive.Sphere, new Vector3(0.045f, 0.69f, 0f), Vector3.one * 0.32f,
                bud));
            parts.Add(Part("HLTopSphere", HLPrimitive.Sphere, new Vector3(-0.02f, 0.95f, 0f), Vector3.one * 0.22f,
                body, glow: 0.7f));
            parts.Add(Part("HLRing", HLPrimitive.Torus, new Vector3(0f, 0.56f, 0f), new Vector3(0.48f, 0.26f, 0.48f),
                stem));
            return parts;
        }

        static HLCreatureRecipe SaveRecipe(string name, List<HLPart> parts, int roots, int armCount, int seed)
        {
            string path = root + "Data/" + name + ".asset";
            HLCreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            if (!recipe)
            {
                recipe = ScriptableObject.CreateInstance<HLCreatureRecipe>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            // Geometry-only readability: root footprint and gameplay sockets stay unchanged
            float displayHeight = name == "HLHealer" ? 2.05f : 1.7f;
            Vector3 displayScale = new Vector3(1.45f, displayHeight, 1.45f);
            for (int i = 0; i < parts.Count; i++)
            {
                HLPart part = parts[i];
                part.localPosition = Vector3.Scale(part.localPosition, displayScale);
                part.dimensions = Vector3.Scale(part.dimensions, displayScale);
                parts[i] = part;
            }

            // A rounded collar makes capsule endpoints readable without moving their pivots
            int stemCount = parts.Count;
            for (int i = 0; i < stemCount; i++)
            {
                HLPart stemPart = parts[i];
                if (stemPart.primitive != HLPrimitive.Capsule)
                {
                    continue;
                }

                parts.Add(Part("HLJoint" + i, HLPrimitive.Sphere, Vector3.up * (stemPart.dimensions.y * 0.38f),
                    Vector3.one * (stemPart.dimensions.x * 1.5f), bud, parent: i));
            }

            recipe.parts = parts.ToArray();
            recipe.roots.count = roots;
            recipe.idle.seed = seed;
            recipe.roots.thickness = 0.042f;
            recipe.roots.footRadius = 0.41f;
            recipe.roots.hipHeight = 0.26f;
            recipe.roots.kneeHeight = 0.14f;
            if (name == "HLHealer")
            {
                // The body grew independently of the crown. Join the inverted bowl
                // to a visible hip above the grass without changing the legal cell footprint.
                recipe.roots.hipHeight = 0.60f;
                recipe.roots.kneeHeight = 0.32f;
                recipe.roots.thickness = 0.049f;
            }

            recipe.idle.swayFrequency = 0.25f;
            recipe.targetLocal = Vector3.up * 0.8f;
            recipe.sourceLocal = new Vector3[armCount];
            recipe.arms = new HLArmDefinition[armCount];
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

                recipe.arms[j] = new HLArmDefinition
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

            if (!HLCreatureValidator.TryValidate(recipe, out string error))
            {
                throw new InvalidOperationException(error);
            }

            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        static void Model(string name, string original, HLCreatureRecipe recipe, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(original);
            if (!source)
            {
                throw new InvalidOperationException("Missing model: " + original);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                instance.name = name;
                // Keep transforms, EntityModel, SkillSource subclasses, target tags and colliders
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    UnityEngine.Object.DestroyImmediate(renderer);
                }

                foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
                {
                    UnityEngine.Object.DestroyImmediate(filter);
                }

                foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
                {
                    UnityEngine.Object.DestroyImmediate(animator);
                }

                foreach (Animation animation in instance.GetComponentsInChildren<Animation>(true))
                {
                    UnityEngine.Object.DestroyImmediate(animation);
                }

                foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is IVisualBehaviour)
                    {
                        UnityEngine.Object.DestroyImmediate(behaviour);
                    }
                }

                HLCreatureBuilder builder = instance.AddComponent<HLCreatureBuilder>();
                builder.SetRecipe(recipe, material);
                PrefabUtility.SaveAsPrefabAsset(instance, root + "Prefabs/" + name + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static void CharacterView(HLCreatureRecipe recipe, Material material)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character.prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.name = "HLHealerCharacter";
                Transform anchor = new GameObject("HLHealerAnchor").transform;
                anchor.SetParent(instance.transform, false);
                HLCharacterView view = anchor.gameObject.AddComponent<HLCharacterView>();
                SerializedObject data = new SerializedObject(view);
                data.FindProperty("_character").objectReferenceValue = instance.GetComponent<Character>();
                data.FindProperty("_recipe").objectReferenceValue = recipe;
                data.FindProperty("_visualAnchor").objectReferenceValue = anchor;
                data.FindProperty("_material").objectReferenceValue = material;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, root + "Prefabs/HLHealerCharacter.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static void ProjectileView(string original)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(original);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.name = "HLProjectile" + Path.GetFileNameWithoutExtension(original);
                HLProjectileVisualObserver observer = instance.AddComponent<HLProjectileVisualObserver>();
                SerializedObject data = new SerializedObject(observer);
                bool isChain = instance.GetComponent<ChainLightningProjectile>() != null;
                bool isChanneling = original.Contains("Channeling");
                HLGestureKind presentation = isChanneling ? HLGestureKind.Channel : HLGestureKind.Attack;
                data.FindProperty("_preserveContactPath").boolValue = isChain;
                data.FindProperty("_presentation").enumValueIndex = (int)presentation;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, root + "Prefabs/" + instance.name + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
