#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    /// <summary>Explicit, repeatable authoring command. Writes only this track's asset folder.</summary>
    public static class HLCreatureAssetAuthoring
    {
        const string Root = "Assets/Render/Creatures/";
        static readonly Color Body = new Color(.50f, .79f, .25f), Stem = new Color(.18f, .49f, .31f), Bud = new Color(.78f, .95f, .29f);
        [MenuItem("HL/Creatures/Author Presentation Assets")]
        public static void Author()
        {
            Directory.CreateDirectory(Root + "Data"); Directory.CreateDirectory(Root + "Prefabs");
            Material material = null;
            if (AssetDatabase.IsValidFolder("Assets/Render/Look"))
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Render/Look" }))
                { material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)); if (material) break; }
            if (!material)
            {
                string path = Root + "Data/HLPlaceholder.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "HLPlaceholder", enableInstancing = true };
                    material.SetColor("_BaseColor", Color.white); material.SetFloat("_Smoothness", .2f);
                    AssetDatabase.CreateAsset(material, path);
                }
            }
            var healer = SaveRecipe("HLHealer", Healer(), 6, 2, 17);
            var fern = SaveRecipe("HLSpiralFern", Fern(), 4, 2, 31);
            var arch = SaveRecipe("HLHangingArch", Arch(), 6, 4, 57);
            var rosette = SaveRecipe("HLBladeRosette", Rosette(), 6, 2, 103);
            var stack = SaveRecipe("HLSphereStack", Stack(), 4, 1, 89);
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
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab")) ProjectileView(path);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("HL creature recipes and presentation prefab variants authored.");
        }
        static HLPart Part(string id, HLPrimitive primitive, Vector3 position, Vector3 dimensions, Color colour, Vector3 euler = default, int parent = 0, float glow = 0)
            => new HLPart { id = id, parent = parent, primitive = primitive, localPosition = position, dimensions = dimensions, colour = colour, localEuler = euler, torusTubeRatio = .2f, glow = glow };
        static List<HLPart> Base() => new List<HLPart> { Part("HLStem", HLPrimitive.Capsule, new Vector3(0, .25f, 0), new Vector3(.11f, .5f, .11f), Stem, parent: -1) };
        static List<HLPart> Healer()
        {
            var p = Base();
            p.Add(Part("HLBulb", HLPrimitive.Sphere, new Vector3(0, .48f, 0), new Vector3(.5f, .62f, .45f), Body));
            p.Add(Part("HLCrown", HLPrimitive.Torus, new Vector3(0, .81f, 0), new Vector3(.42f, .3f, .42f), Bud, glow: .4f));
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2 / 3;
                p.Add(Part("HLBud" + i, HLPrimitive.Sphere, new Vector3(Mathf.Cos(a) * .24f, .69f, Mathf.Sin(a) * .24f), new Vector3(.13f, .23f, .13f), Bud, new Vector3(0, -a * Mathf.Rad2Deg, -25), glow: 1.6f));
            }
            return p;
        }
        static List<HLPart> Rosette()
        {
            var p = new List<HLPart> { Part("HLRosette", HLPrimitive.Sphere, Vector3.up * .15f, new Vector3(.4f, .3f, .4f), Body, parent: -1) };
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3;
                p.Add(Part("HLBud" + i, HLPrimitive.Cone, new Vector3(.26f * Mathf.Cos(angle), .28f, .26f * Mathf.Sin(angle)),
                    new Vector3(.2f, .72f, .1f), Body, new Vector3(0, i * 60, -55), glow: .7f));
            }
            return p;
        }
        static List<HLPart> Fern()
        {
            var p = Base(); Vector3 previous = Vector3.zero;
            for (int i = 0; i < 10; i++)
            {
                float a = i * .53f;
                float radius = .31f * (1 - i * .055f);
                Vector3 point = new Vector3(Mathf.Sin(a) * radius, .13f + i * .075f, Mathf.Cos(a) * radius * .35f);
                Vector3 delta = point - previous;
                p.Add(Part("HLFernStem" + i, HLPrimitive.Capsule, (point + previous) * .5f, new Vector3(.065f, delta.magnitude + .035f, .065f), Stem,
                    Quaternion.FromToRotation(Vector3.up, delta).eulerAngles));
                p.Add(Part("HLFrond" + i, HLPrimitive.Sphere, point, new Vector3(.28f * (1 - i * .04f), .07f, .15f), i % 2 == 0 ? Body : Bud,
                    new Vector3(0, i * 29, i % 2 == 0 ? 32 : -32)));
                previous = point;
            }
            return p;
        }
        static List<HLPart> Arch()
        {
            var p = Base(); p[0] = Part("HLArchFoot", HLPrimitive.Capsule, new Vector3(-.29f, .2f, 0), new Vector3(.10f, .4f, .1f), Stem, parent: -1);
            Vector3 previous = new Vector3(0, .12f, 0);
            for (int i = 1; i <= 8; i++)
            {
                float a = Mathf.PI - i * Mathf.PI / 8;
                Vector3 point = new Vector3(.29f + Mathf.Cos(a) * .29f, .12f + Mathf.Sin(a) * .75f, 0);
                Vector3 d = point - previous;
                p.Add(Part("HLArch" + i, HLPrimitive.Capsule, (point + previous) * .5f, new Vector3(.09f, d.magnitude + .04f, .09f), Body,
                    Quaternion.FromToRotation(Vector3.up, d).eulerAngles)); previous = point;
            }
            for (int i = 0; i < 3; i++)
            {
                float x = .13f + i * .16f, y = i == 1 ? .58f : .41f;
                p.Add(Part("HLPodStem" + i, HLPrimitive.CylinderSegment, new Vector3(x, y + .12f, 0), new Vector3(.028f, .25f, .028f), Stem));
                p.Add(Part("HLPod" + i, HLPrimitive.Sphere, new Vector3(x, y - .06f, 0), new Vector3(.16f, .28f, .18f), Bud));
            }
            return p;
        }
        static List<HLPart> Stack()
        {
            var p = new List<HLPart> { Part("HLConicalRoot", HLPrimitive.Cone, new Vector3(0, .2f, 0), new Vector3(.5f, .4f, .5f), Stem, parent: -1) };
            p.Add(Part("HLBottomSphere", HLPrimitive.Sphere, new Vector3(0, .34f, 0), Vector3.one * .43f, Body));
            p.Add(Part("HLMiddleSphere", HLPrimitive.Sphere, new Vector3(.045f, .69f, 0), Vector3.one * .32f, Bud));
            p.Add(Part("HLTopSphere", HLPrimitive.Sphere, new Vector3(-.02f, .95f, 0), Vector3.one * .22f, Body));
            p.Add(Part("HLRing", HLPrimitive.Torus, new Vector3(0, .56f, 0), new Vector3(.48f, .26f, .48f), Stem));
            return p;
        }
        static HLCreatureRecipe SaveRecipe(string name, List<HLPart> parts, int roots, int armCount, int seed)
        {
            string path = Root + "Data/" + name + ".asset";
            var recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            if (!recipe) { recipe = ScriptableObject.CreateInstance<HLCreatureRecipe>(); AssetDatabase.CreateAsset(recipe, path); }
            recipe.parts = parts.ToArray(); recipe.roots.count = roots; recipe.idle.seed = seed;
            recipe.targetLocal = Vector3.up * .8f; recipe.sourceLocal = new Vector3[armCount]; recipe.arms = new HLArmDefinition[armCount];
            for (int j = 0; j < armCount; j++)
            {
                recipe.sourceLocal[j] = new Vector3(j % 2 == 0 ? -.19f : .19f, .75f, 0);
                var rest = new Vector3[121];
                // Ten compact coils: 120 x .2 = 24 cells covers the current 16 x 16 board.
                // Larger boards/out-of-board targets still clamp without changing gameplay.
                for (int i = 0; i < 120; i++)
                {
                    float angle = i * Mathf.PI * 2 / 12;
                    rest[i + 1] = rest[i] + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), .015f).normalized * .2f;
                }
                recipe.arms[j] = new HLArmDefinition { bodyPart = 0, rootLocal = recipe.sourceLocal[j] - parts[0].localPosition,
                    sourceSocketIndex = j, segmentCount = 120, segmentLength = .2f, radius = .018f, restJoints = rest,
                    bendPole = Vector3.up, colour = Stem };
            }
            if (!HLCreatureValidator.TryValidate(recipe, out string error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(recipe); return recipe;
        }
        static void Model(string name, string original, HLCreatureRecipe recipe, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(original);
            if (!source) throw new InvalidOperationException("Missing model: " + original);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                instance.name = name;
                // Preserve transforms, EntityModel, SkillSource subclasses, target tags and colliders.
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) UnityEngine.Object.DestroyImmediate(renderer);
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true)) UnityEngine.Object.DestroyImmediate(filter);
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
                foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) UnityEngine.Object.DestroyImmediate(animation);
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour is IVisualBehaviour) UnityEngine.Object.DestroyImmediate(behaviour);
                var builder = instance.AddComponent<HLCreatureBuilder>(); builder.SetRecipe(recipe, material);
                PrefabUtility.SaveAsPrefabAsset(instance, Root + "Prefabs/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        static void CharacterView(HLCreatureRecipe recipe, Material material)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character.prefab"));
            try
            {
                instance.name = "HLHealerCharacter";
                var anchor = new GameObject("HLHealerAnchor").transform; anchor.SetParent(instance.transform, false);
                var view = anchor.gameObject.AddComponent<HLCharacterView>();
                var data = new SerializedObject(view);
                data.FindProperty("character").objectReferenceValue = instance.GetComponent<Character>();
                data.FindProperty("recipe").objectReferenceValue = recipe;
                data.FindProperty("visualAnchor").objectReferenceValue = anchor;
                data.FindProperty("material").objectReferenceValue = material;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, Root + "Prefabs/HLHealerCharacter.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        static void ProjectileView(string original)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(original));
            try
            {
                instance.name = "HLProjectile" + Path.GetFileNameWithoutExtension(original);
                var observer = instance.AddComponent<HLProjectileVisualObserver>();
                var data = new SerializedObject(observer);
                data.FindProperty("preserveContactPath").boolValue = instance.GetComponent<ChainLightningProjectile>() != null;
                data.FindProperty("presentation").enumValueIndex = original.Contains("Channeling") ? (int)HLGestureKind.Channel : (int)HLGestureKind.Attack;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, Root + "Prefabs/" + instance.name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
#endif
