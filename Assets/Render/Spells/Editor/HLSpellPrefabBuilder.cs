using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Spells
{
    public static class HLSpellPrefabBuilder
    {
        static readonly string root = "Assets/Render/Spells/";

        // Batch verification of actual player defines, without building or changing a scene.
        public static void VerifyPlayerCompilation()
        {
            ScriptCompilationSettings settings = new ScriptCompilationSettings
            {
                target = BuildTarget.StandaloneOSX,
                group = BuildTargetGroup.Standalone
            };
            string output = Path.Combine(Path.GetTempPath(), "hl-f2-player-scripts");
            Directory.CreateDirectory(output);
            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, output);
            try
            {
                if (
                    result.assemblies == null
                    || !result.assemblies.Any(p => Path.GetFileName(p) == "HealerLike.Render.dll")
                )
                {
                    throw new InvalidOperationException("HL player compilation did not produce the render assembly.");
                }
                if (result.assemblies.Any(p => Path.GetFileName(p) == "HealerLike.Render.Spells.Editor.dll"))
                {
                    throw new InvalidOperationException("HL spell authoring was included in the player.");
                }
                Debug.Log("HL StandaloneOSX player script compilation passed, spell authoring excluded.");
            }
            finally
            {
                result.typeDB?.Dispose();
            }
        }

        [MenuItem("HealerLike/Render/Build Spell Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(root + "Prefabs");
            Directory.CreateDirectory(root + "Data");
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            if (!material)
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(root + "Data/HLSpellPlaceholder.mat");
                if (!material)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        name = "HLSpellPlaceholder",
                        enableInstancing = true
                    };
                    AssetDatabase.CreateAsset(material, root + "Data/HLSpellPlaceholder.mat");
                }
            }
            SaveMesh(HLSpellPrimitives.torus, "HLTorus");
            SaveMesh(HLSpellPrimitives.cone, "HLCone");
            SaveMesh(HLSpellPrimitives.star, "HLStar");
            SaveMesh(HLSpellPrimitives.boulder, "HLBoulder");
            string[] names =
            {
                "HLStatus_Buff",
                "HLStatus_Shield",
                "HLFx_HealSpheres",
                "HLFx_Impact",
                "HLFx_ChainBeam",
                "HLFx_HostileLitter",
                "HLFx_HealRing",
                "HLFx_PoisonDrips"
            };
            HLSpellEffectKind[] kinds =
            {
                HLSpellEffectKind.Buff,
                HLSpellEffectKind.Shield,
                HLSpellEffectKind.Heal,
                HLSpellEffectKind.Impact,
                HLSpellEffectKind.Chain,
                HLSpellEffectKind.Litter,
                HLSpellEffectKind.Area,
                HLSpellEffectKind.Drip
            };
            GameObject[] prefabs = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                GameObject go = new GameObject(names[i]);
                HLSpellEffect effect = go.AddComponent<HLSpellEffect>();
                effect.kind = kinds[i];
                if (effect.kind == HLSpellEffectKind.Litter)
                {
                    effect.lifetime = HLSpellVisualSink.PulseSeconds;
                }
                effect.material = material;
                HLSpellPrimitives.Build(effect);
                PersistBeautyMeshes(go);
                foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == HLSpellPrimitives.torus)
                    {
                        filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLTorus.asset");
                    }
                    else if (filter.sharedMesh == HLSpellPrimitives.cone)
                    {
                        filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLCone.asset");
                    }
                }
                prefabs[i] = PrefabUtility.SaveAsPrefabAsset(go, root + "Prefabs/" + names[i] + ".prefab");
                effect.ReleaseResources();
                Object.DestroyImmediate(go);
            }
            HLSpellStyleTable table = AssetDatabase.LoadAssetAtPath<HLSpellStyleTable>(
                root + "Data/HLSpellStyles.asset"
            );
            if (!table)
            {
                table = ScriptableObject.CreateInstance<HLSpellStyleTable>();
                AssetDatabase.CreateAsset(table, root + "Data/HLSpellStyles.asset");
            }
            table.buff = prefabs[0];
            table.shield = prefabs[1];
            table.heal = prefabs[2];
            table.impact = prefabs[3];
            table.chain = prefabs[4];
            if (table.entries.Count == 0)
            {
                foreach (HLSign sign in new[] { HLSign.Positive, HLSign.Negative })
                {
                    table.entries.Add(
                        new HLSpellStyleTable.HLEntry
                        {
                            signature = new HLSpellSignature
                            {
                                operation = HLOperation.Resource,
                                sign = sign,
                                hasAttribute = true,
                                attribute = AttributeType.HealthMax,
                                topology = HLTopology.Single,
                                duration = HLDurationShape.Instant,
                                tempo = HLTempo.Immediate
                            },
                            prefab = sign == HLSign.Positive ? prefabs[2] : prefabs[3]
                        }
                    );
                }
            }
            AuthorSignatures(table, material);
            AuthorDeliveries();
            EditorUtility.SetDirty(table);
            GameObject sinkGo = new GameObject("HLSpellVisualSink");
            HLSpellVisualSink sink = sinkGo.AddComponent<HLSpellVisualSink>();
            sink.styles = table;
            sink.material = material;
            PrefabUtility.SaveAsPrefabAsset(sinkGo, root + "Prefabs/HLSpellVisualSink.prefab");
            Object.DestroyImmediate(sinkGo);
            AssetDatabase.SaveAssets();
        }

        public static SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> Inventory()
        {
            SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> rows =
                new SortedDictionary<string, (HLSpellSignature, SortedSet<string>)>(StringComparer.Ordinal);
            HLSpellGrammar grammar = new HLSpellGrammar();
            foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets/Data" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset", StringComparison.Ordinal))
                {
                    continue;
                }
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                Walk(asset, path, grammar, rows, new HashSet<object>(), 0);
                if (asset is ABuffHandlerFactory)
                {
                    Collect(
                        grammar.DescribeData(asset, new HLGrammarContext { topology = HLTopology.Self }),
                        path + " (self)",
                        rows
                    );
                }
            }
            return rows;
        }

        static void Walk(
            object value,
            string path,
            HLSpellGrammar grammar,
            SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> rows,
            HashSet<object> seen,
            int depth
        )
        {
            if (value == null || depth > 16 || value is string || value.GetType().IsValueType || !seen.Add(value))
            {
                return;
            }
            HLVisualRecipe recipe = grammar.DescribeData(value);
            if (recipe.signature.operation != HLOperation.Unknown)
            {
                Collect(recipe, path, rows);
                return;
            }
            if (value is IEnumerable list)
            {
                foreach (object child in list)
                {
                    Walk(child, path, grammar, rows, seen, depth + 1);
                }
                return;
            }
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Walk(field.GetValue(value), path, grammar, rows, seen, depth + 1);
            }
        }

        static void Collect(
            HLVisualRecipe recipe,
            string path,
            SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> rows
        )
        {
            if (recipe.signature.operation != HLOperation.Unknown)
            {
                string key = recipe.signature.ToString();
                if (!rows.TryGetValue(key, out (HLSpellSignature signature, SortedSet<string> paths) row))
                {
                    row = (recipe.signature, new SortedSet<string>(StringComparer.Ordinal));
                }
                row.paths.Add(path + (recipe.isValid ? "" : " [diagnostic: " + recipe.diagnostic + "]"));
                rows[key] = row;
            }
            foreach (HLVisualRecipe child in recipe.children)
            {
                Collect(child, path, rows);
            }
        }

        static void AuthorSignatures(HLSpellStyleTable table, Material material)
        {
            table.entries.Clear();
            StringBuilder report = new StringBuilder(
                "# Instantiated signatures\n\n"
                + "Generated by HLSpellPrefabBuilder.Inventory using HLSpellGrammar.DescribeData on Assets/Data. "
                + "Includes composition nodes, standalone factories and self handler contexts; "
                + "diagnostic rows describe "
                + "authored intent, never permission to emit. Runtime outcomes lose provenance at the frozen seam.\n\n"
                + "| Signature | Prefab | Beauty code | Asset paths |\n|---|---|---|---|\n"
            );
            foreach (KeyValuePair<string, (HLSpellSignature signature, SortedSet<string> paths)> row in Inventory())
            {
                HLSpellSignature signature = row.Value.signature;
                string name = "HLSignature_" + row.Key.Replace('/', '_');
                GameObject go = new GameObject(name);
                HLSpellEffect effect = go.AddComponent<HLSpellEffect>();
                effect.kind = HLSpellPrimitives.Kind(signature);
                effect.material = material;
                effect.hasAuthoredSignature = true;
                effect.authoredSignature = signature;
                HLSpellPrimitives.Build(effect);
                HLSpellPrimitives.AddMarker(effect, signature);
                PersistBeautyMeshes(go);
                foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == HLSpellPrimitives.torus)
                    {
                        filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLTorus.asset");
                    }
                    if (filter.sharedMesh == HLSpellPrimitives.cone)
                    {
                        filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLCone.asset");
                    }
                }
                string path = root + "Prefabs/" + name + ".prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                effect.ReleaseResources();
                Object.DestroyImmediate(go);
                table.entries.Add(new HLSpellStyleTable.HLEntry { signature = signature, prefab = prefab });
                report
                    .Append("| ")
                    .Append(row.Key)
                    .Append(" | ")
                    .Append(name)
                    .Append(" | ")
                    .Append(BeautyCode(HLSpellPrimitives.Kind(signature)))
                    .Append(" | ")
                    .Append(string.Join("<br>", row.Value.paths))
                    .Append(" |\n");
            }
            // Resolved resource outcomes only carry a sign, not the original topology, tempo or expression.
            foreach (AttributeType resource in new[] { AttributeType.HealthMax, AttributeType.ManaMax })
            {
                foreach (HLSign sign in new[] { HLSign.Positive, HLSign.Negative })
                {
                    HLSpellSignature signature = new HLSpellSignature
                    {
                        operation = HLOperation.Resource,
                        hasAttribute = true,
                        attribute = resource,
                        sign = sign,
                        topology = HLTopology.Single,
                        duration = HLDurationShape.Instant,
                        tempo = HLTempo.Immediate
                    };
                    if (table.entries.Any(x => x.signature.Equals(signature)))
                    {
                        continue;
                    }
                    GameObject go = new GameObject("HLResolved_" + resource + sign);
                    HLSpellEffect effect = go.AddComponent<HLSpellEffect>();
                    effect.kind = HLSpellPrimitives.Kind(signature);
                    effect.material = material;
                    HLSpellPrimitives.Build(effect);
                    PersistBeautyMeshes(go);
                    foreach (MeshFilter f in go.GetComponentsInChildren<MeshFilter>())
                    {
                        if (f.sharedMesh == HLSpellPrimitives.torus)
                        {
                            f.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLTorus.asset");
                        }
                        else if (f.sharedMesh == HLSpellPrimitives.cone)
                        {
                            f.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLCone.asset");
                        }
                    }
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, root + "Prefabs/" + go.name + ".prefab");
                    effect.ReleaseResources();
                    Object.DestroyImmediate(go);
                    table.entries.Add(new HLSpellStyleTable.HLEntry { signature = signature, prefab = prefab });
                }
            }
            File.WriteAllText(root + "SIGNATURES.md", report.ToString());
        }

        static void AuthorDeliveries()
        {
            string path = root + "Data/HLDeliveryStyles.asset";
            HLDeliveryStyles styles = AssetDatabase.LoadAssetAtPath<HLDeliveryStyles>(path);
            if (!styles)
            {
                styles = ScriptableObject.CreateInstance<HLDeliveryStyles>();
                AssetDatabase.CreateAsset(styles, path);
            }
            styles.entries.Clear();
            foreach (
                string name in new[]
                {
                    "BulletSpeed",
                    "ChainLightning",
                    "ChannelingLightning",
                    "CurveBullet",
                    "CurveBullet2",
                    "CurveSphereBullet",
                    "LaserBullet",
                    "StraightLaserBullet",
                    "SwarmBullet"
                }
            )
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Projectiles/" + name + ".prefab"
                );
                bool curve =
                    go.GetComponent<CurvedHomingProjectileBehaviour>()
                    || go.GetComponent<ArcHomingProjectileBehaviour>();
                bool bounce = go.GetComponent<BounceProjectileBehaviour>();
                bool chain = go.GetComponent<ChainLightningProjectile>();
                // Laser/swarm are authored presentation variants, with component evidence retained.
                string evidence = string.Join(
                    ", ",
                    go.GetComponents<Component>().Where(x => x).Select(x => x.GetType().Name)
                );
                styles.entries.Add(
                    new HLDeliveryStyles.HLEntry
                    {
                        prefabName = name,
                        style = HLDeliveryStyles.Classify(
                            curve,
                            name.Contains("Laser"),
                            name == "SwarmBullet",
                            bounce,
                            chain
                        ),
                        evidence = evidence
                    }
                );
            }
            EditorUtility.SetDirty(styles);
        }

        static string BeautyCode(HLSpellEffectKind kind)
        {
            switch (kind)
            {
                case HLSpellEffectKind.Heal:
                    return "Lime spheres / thin stalks / grow-pop";
                case HLSpellEffectKind.Impact:
                    return "Coral flat star / four gravity shards";
                case HLSpellEffectKind.Shield:
                    return "Six overlapping leaf plates / close-open";
                case HLSpellEffectKind.Drip:
                    return "Coral drops / observed tick period";
                case HLSpellEffectKind.Mana:
                    return "Hollow gold resource beads";
                default:
                    return "Three tilted tori / orbit / soft pulse";
            }
        }

        static void PersistBeautyMeshes(GameObject go)
        {
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == HLSpellPrimitives.star)
                {
                    filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLStar.asset");
                }
                else if (filter.sharedMesh == HLSpellPrimitives.boulder)
                {
                    filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "Data/HLBoulder.asset");
                }
            }
        }

        static void SaveMesh(Mesh mesh, string name)
        {
            string path = root + "Data/" + name + ".asset";
            Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old)
            {
                if (old != mesh)
                {
                    EditorUtility.CopySerialized(mesh, old);
                }
            }
            else
            {
                AssetDatabase.CreateAsset(Object.Instantiate(mesh), path);
            }
        }
    }
}
