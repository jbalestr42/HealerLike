using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Spells
{
    // One-off: moves the spell parts onto the baked meshes and fills SpellLooks from the signature mapping
    public static class SpellLooksSeeder
    {
        static readonly string spells = "Assets/Render/Spells/";
        static readonly string prefabs = "Assets/Render/Spells/Prefabs/";
        static readonly Color gold = new Color32(242, 194, 48, 255);
        static readonly Color lime = new Color32(198, 242, 74, 255);
        static readonly Color coral = new Color32(242, 96, 122, 255);
        static readonly Color slate = new Color32(58, 66, 87, 255);
        static readonly Color leaf = new Color32(151, 203, 99, 255);

        public static void Seed()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            HLPrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<HLPrimitiveMeshes>(
                "Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
            Dictionary<Mesh, Mesh> swaps = new Dictionary<Mesh, Mesh>();
            swaps[AssetDatabase.LoadAssetAtPath<Mesh>(spells + "Data/HLTorus.asset")] = meshes.thinTorus;
            swaps[AssetDatabase.LoadAssetAtPath<Mesh>(spells + "Data/HLStar.asset")] = meshes.star;
            swaps[AssetDatabase.LoadAssetAtPath<Mesh>(spells + "Data/HLBoulder.asset")] = meshes.boulder;
            Mesh oldCone = AssetDatabase.LoadAssetAtPath<Mesh>(spells + "Data/HLCone.asset");

            string[] names =
            {
                "HLStatus_Buff", "HLStatus_Shield", "HLFx_HealSpheres", "HLFx_Impact", "HLFx_ChainBeam",
                "HLFx_HostileLitter", "HLFx_HealRing", "HLFx_PoisonDrips", "HLResolved_ManaMaxPositive",
                "HLResolved_ManaMaxNegative"
            };
            foreach (string name in names)
            {
                RebuildPart(prefabs + name + ".prefab", material, meshes, swaps, oldCone);
            }

            SpellLooks looks = AssetDatabase.LoadAssetAtPath<SpellLooks>(spells + "Data/SpellLooks.asset");
            if (looks == null)
            {
                looks = ScriptableObject.CreateInstance<SpellLooks>();
                AssetDatabase.CreateAsset(looks, spells + "Data/SpellLooks.asset");
            }

            looks.boon = Look("HLStatus_Buff", gold);
            looks.bane = Look("HLStatus_Buff", coral);
            looks.heal = Look("HLFx_HealSpheres", lime);
            looks.impact = Look("HLFx_Impact", coral);
            looks.manaGain = Look("HLResolved_ManaMaxPositive", gold);
            looks.manaLoss = Look("HLResolved_ManaMaxNegative", gold);
            looks.chain = Look("HLFx_ChainBeam", gold);
            looks.shield = Look("HLStatus_Shield", leaf);
            looks.area = Look("HLFx_HealRing", Color.white);
            looks.hostileArea = Look("HLFx_HostileLitter", slate);
            SeedBuffs(looks);
            SeedProjectiles(looks);
            EditorUtility.SetDirty(looks);

            GameObject sinkRoot = PrefabUtility.LoadPrefabContents(prefabs + "HLSpellVisualSink.prefab");
            SerializedObject sink = new SerializedObject(sinkRoot.GetComponent<HLSpellVisualSink>());
            sink.FindProperty("_looks").objectReferenceValue = looks;
            sink.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(sinkRoot, prefabs + "HLSpellVisualSink.prefab");
            PrefabUtility.UnloadPrefabContents(sinkRoot);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpellLooksSeeder] {looks.buffs.Count} buff rows, {looks.projectiles.Count} projectile rows");
        }

        static void RebuildPart(string path, Material material, HLPrimitiveMeshes meshes, Dictionary<Mesh, Mesh> swaps,
                                Mesh oldCone)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null && swaps.ContainsKey(filter.sharedMesh))
                {
                    filter.sharedMesh = swaps[filter.sharedMesh];
                }
                else if (filter.sharedMesh != null && filter.sharedMesh == oldCone)
                {
                    // The spell cone had radius one and its base at the pivot, the baked one is half as wide and centred
                    Transform part = filter.transform;
                    Vector3 scale = part.localScale;
                    part.localPosition += part.localRotation * new Vector3(0f, 0.5f * scale.y, 0f);
                    part.localScale = new Vector3(scale.x * 2f, scale.y, scale.z * 2f);
                    filter.sharedMesh = meshes.cone;
                }
            }

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                string childName = root.transform.GetChild(i).name;
                if (childName == "SideRim" || childName == "StackBead" || childName == "CriticalRing")
                {
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }
            }

            Transform rim = Part(root, "SideRim", meshes.thinTorus, material, Vector3.down * 0.18f,
                                 new Vector3(0.72f, 0.2f, 0.72f), Quaternion.identity);
            Transform[] beads = new Transform[8];
            for (int i = 0; i < beads.Length; i++)
            {
                GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(bead.GetComponent<Collider>());
                bead.name = "StackBead";
                bead.GetComponent<Renderer>().sharedMaterial = material;
                bead.transform.SetParent(root.transform, false);
                bead.transform.localPosition = new Vector3((i - 3.5f) * 0.055f, 0.48f, 0f);
                bead.transform.localScale = Vector3.one * 0.035f;
                bead.SetActive(false);
                beads[i] = bead.transform;
            }

            Transform[] rings =
            {
                Part(root, "CriticalRing", meshes.thinTorus, material, Vector3.zero, Vector3.one * 0.7f,
                     Quaternion.Euler(90f, 0f, 0f)),
                Part(root, "CriticalRing", meshes.thinTorus, material, Vector3.zero, Vector3.one * 0.85f,
                     Quaternion.Euler(90f, 0f, 0f))
            };

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }

            SerializedObject effect = new SerializedObject(root.GetComponent<HLSpellEffect>());
            effect.FindProperty("_sideRim").objectReferenceValue = rim.GetComponent<Renderer>();
            SerializedProperty beadList = effect.FindProperty("_stackBeads");
            beadList.arraySize = beads.Length;
            for (int i = 0; i < beads.Length; i++)
            {
                beadList.GetArrayElementAtIndex(i).objectReferenceValue = beads[i];
            }

            SerializedProperty ringList = effect.FindProperty("_criticalRings");
            ringList.arraySize = rings.Length;
            for (int i = 0; i < rings.Length; i++)
            {
                ringList.GetArrayElementAtIndex(i).objectReferenceValue = rings[i].GetComponent<Renderer>();
            }
            effect.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static Transform Part(GameObject root, string name, Mesh mesh, Material material, Vector3 position,
                              Vector3 scale, Quaternion rotation)
        {
            GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = rotation;
            part.SetActive(false);
            return part.transform;
        }

        static SpellLook Look(string prefab, Color tint)
        {
            SpellLook look = new SpellLook();
            look.effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs + prefab + ".prefab")
                .GetComponent<HLSpellEffect>();
            look.tint = tint;
            return look;
        }

        static void SeedBuffs(SpellLooks looks)
        {
            looks.buffs.Clear();
            HLSpellGrammar grammar = new HLSpellGrammar();
            foreach (string guid in AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ABuffHandlerFactory factory = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);
                HLVisualRecipe recipe = grammar.Describe(factory, null, null);
                List<HLVisualRecipe> leaves = new List<HLVisualRecipe>();
                CollectLeaves(recipe, leaves);
                if (leaves.Count == 0)
                {
                    Debug.Log($"[SpellLooksSeeder] {path}: no row, the grammar had no look for it");
                    continue;
                }

                HLSpellSignature signature = leaves[0].signature;
                HLSpellEffectKind kind = Kind(signature);
                SpellLook look = Look(Prefab(kind, signature), SignatureColor(kind, signature));
                if (signature.hasAttribute && signature.attribute == AttributeType.Speed)
                {
                    // The status sits 0.35 below the anchor in a root scaled by 1.35
                    look.offset = Vector3.down * 0.35f * 1.35f;
                }
                looks.buffs[factory] = look;
                Debug.Log($"[SpellLooksSeeder] {path}: {kind} from {signature}, {leaves.Count} atoms");
            }
        }

        static void SeedProjectiles(SpellLooks looks)
        {
            looks.projectiles.Clear();
            HLDeliveryStyles styles = AssetDatabase.LoadAssetAtPath<HLDeliveryStyles>(spells + "Data/HLDeliveryStyles.asset");
            foreach (HLDeliveryStyles.HLEntry entry in styles.entries)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Projectiles/" + entry.prefabName + ".prefab");
                ProjectileLook look = new ProjectileLook();
                look.style = entry.style;
                GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Render/Creatures/Prefabs/HLProjectile" + entry.prefabName + ".prefab");
                HLProjectileVisualObserver observer = variant != null
                    ? variant.GetComponent<HLProjectileVisualObserver>()
                    : null;
                if (observer != null)
                {
                    SerializedObject serialized = new SerializedObject(observer);
                    look.presentation = (HLGestureKind)serialized.FindProperty("_presentation").intValue;
                    look.preserveContactPath = serialized.FindProperty("_preserveContactPath").boolValue;
                }
                looks.projectiles[prefab] = look;
                Debug.Log($"[SpellLooksSeeder] {entry.prefabName}: {look.style} {look.presentation} {look.preserveContactPath}");
            }
        }

        // Leaves the grammar could describe, a broken sibling no longer hides them
        static void CollectLeaves(HLVisualRecipe recipe, List<HLVisualRecipe> leaves)
        {
            if (recipe.children.Count == 0)
            {
                if (recipe.diagnostic == null && recipe.signature.operation != HLOperation.Unknown)
                {
                    leaves.Add(recipe);
                }
                else
                {
                    Debug.Log($"[SpellLooksSeeder] skipped atom {recipe.signature}: {recipe.diagnostic}");
                }
                return;
            }

            foreach (HLVisualRecipe child in recipe.children)
            {
                CollectLeaves(child, leaves);
            }
        }

        static HLSpellEffectKind Kind(HLSpellSignature signature)
        {
            bool isArmor = signature.attribute == AttributeType.HitArmor
                || signature.attribute == AttributeType.PercentArmor
                || signature.attribute == AttributeType.FlatArmor;
            if (signature.operation == HLOperation.Prevention || (signature.operation == HLOperation.Attribute && isArmor))
            {
                return HLSpellEffectKind.Shield;
            }

            if (signature.operation == HLOperation.Resource)
            {
                if (signature.tempo == HLTempo.HandlerTick && signature.sign != HLSign.Positive)
                {
                    return HLSpellEffectKind.Drip;
                }

                if (signature.attribute == AttributeType.ManaMax)
                {
                    return HLSpellEffectKind.Mana;
                }
                return signature.sign == HLSign.Positive ? HLSpellEffectKind.Heal : HLSpellEffectKind.Impact;
            }
            return HLSpellEffectKind.Buff;
        }

        static string Prefab(HLSpellEffectKind kind, HLSpellSignature signature)
        {
            switch (kind)
            {
                case HLSpellEffectKind.Shield:
                    return "HLStatus_Shield";
                case HLSpellEffectKind.Drip:
                    return "HLFx_PoisonDrips";
                case HLSpellEffectKind.Mana:
                    return signature.sign == HLSign.Positive ? "HLResolved_ManaMaxPositive" : "HLResolved_ManaMaxNegative";
                case HLSpellEffectKind.Heal:
                    return "HLFx_HealSpheres";
                case HLSpellEffectKind.Impact:
                    return "HLFx_Impact";
                default:
                    return "HLStatus_Buff";
            }
        }

        static Color SignatureColor(HLSpellEffectKind kind, HLSpellSignature signature)
        {
            if (kind == HLSpellEffectKind.Shield)
            {
                return leaf;
            }

            if (signature.sign == HLSign.Negative)
            {
                return coral;
            }

            if (signature.operation == HLOperation.Resource && signature.sign == HLSign.Positive
                && signature.attribute == AttributeType.HealthMax)
            {
                return lime;
            }
            return gold;
        }
    }
}
