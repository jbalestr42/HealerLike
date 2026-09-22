using System.IO;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Object = UnityEngine.Object;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public static class HLSpellPrefabBuilder
    {
        const string Root="Assets/Render/Spells/";
        [MenuItem("HealerLike/Render/Build Spell Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"Prefabs");Directory.CreateDirectory(Root+"Data");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            if(!material)
            {
                material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Data/HLSpellPlaceholder.mat");
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="HLSpellPlaceholder",enableInstancing=true};AssetDatabase.CreateAsset(material,Root+"Data/HLSpellPlaceholder.mat");}
            }
            SaveMesh(HLSpellPrimitives.Torus,"HLTorus");SaveMesh(HLSpellPrimitives.Cone,"HLCone");
            string[] names={"HLStatus_Buff","HLStatus_Shield","HLFx_HealSpheres","HLFx_Impact","HLFx_ChainBeam"};
            var prefabs=new GameObject[5];
            for(int i=0;i<names.Length;i++)
            {
                var go=new GameObject(names[i]);var effect=go.AddComponent<HLSpellEffect>();effect.kind=(HLSpellEffectKind)i;effect.material=material;HLSpellPrimitives.Build(effect);
                foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == HLSpellPrimitives.Torus) filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Data/HLTorus.asset");
                    else if (filter.sharedMesh == HLSpellPrimitives.Cone) filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Data/HLCone.asset");
                }
                prefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+names[i]+".prefab");Object.DestroyImmediate(go);
            }
            var table=AssetDatabase.LoadAssetAtPath<HLSpellStyleTable>(Root+"Data/HLSpellStyles.asset");
            if(!table){table=ScriptableObject.CreateInstance<HLSpellStyleTable>();AssetDatabase.CreateAsset(table,Root+"Data/HLSpellStyles.asset");}
            table.buff=prefabs[0];table.shield=prefabs[1];table.heal=prefabs[2];table.impact=prefabs[3];table.chain=prefabs[4];
            if (table.entries.Count == 0)
            {
                foreach (HLSign sign in new[] { HLSign.Positive, HLSign.Negative })
                    table.entries.Add(new HLSpellStyleTable.HLEntry { signature = new HLSpellSignature { operation = HLOperation.Resource, sign = sign, hasAttribute = true, attribute = AttributeType.HealthMax, topology = HLTopology.Single, duration = HLDurationShape.Instant, tempo = HLTempo.Immediate }, prefab = sign == HLSign.Positive ? prefabs[2] : prefabs[3] });
            }
            AuthorSignatures(table, material);
            AuthorDeliveries();
            EditorUtility.SetDirty(table);
            var sinkGo=new GameObject("HLSpellVisualSink");var sink=sinkGo.AddComponent<HLSpellVisualSink>();sink.styles=table;sink.material=material;
            PrefabUtility.SaveAsPrefabAsset(sinkGo,Root+"Prefabs/HLSpellVisualSink.prefab");Object.DestroyImmediate(sinkGo);
            AssetDatabase.SaveAssets();
        }
        public static SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> Inventory()
        {
            var rows = new SortedDictionary<string, (HLSpellSignature, SortedSet<string>)>(StringComparer.Ordinal);
            var grammar = new HLSpellGrammar();
            foreach (var guid in AssetDatabase.FindAssets("", new[]{"Assets/Data"}))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset", StringComparison.Ordinal)) continue;
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                Walk(asset, path, grammar, rows, new HashSet<object>(), 0);
                if (asset is ABuffHandlerFactory)
                    Collect(grammar.DescribeData(asset, new HLGrammarContext{Topology=HLTopology.Self}), path + " (self)", rows);
            }
            return rows;
        }
        static void Walk(object value, string path, HLSpellGrammar grammar,
            SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> rows, HashSet<object> seen, int depth)
        {
            if (value == null || depth > 16 || value is string || value.GetType().IsValueType || !seen.Add(value)) return;
            var recipe = grammar.DescribeData(value);
            if (recipe.Signature.operation != HLOperation.Unknown)
            { Collect(recipe,path,rows); return; }
            if (value is IEnumerable list) { foreach(var child in list) Walk(child,path,grammar,rows,seen,depth+1); return; }
            foreach(var field in value.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
                Walk(field.GetValue(value),path,grammar,rows,seen,depth+1);
        }
        static void Collect(HLVisualRecipe recipe, string path,
            SortedDictionary<string, (HLSpellSignature signature, SortedSet<string> paths)> rows)
        {
            if (recipe.Signature.operation != HLOperation.Unknown)
            {
                var key=recipe.Signature.ToString();
                if(!rows.TryGetValue(key,out var row)) row=(recipe.Signature,new SortedSet<string>(StringComparer.Ordinal));
                row.paths.Add(path + (recipe.IsValid ? "" : " [diagnostic: " + recipe.Diagnostic + "]")); rows[key]=row;
            }
            foreach(var child in recipe.Children) Collect(child,path,rows);
        }
        static void AuthorSignatures(HLSpellStyleTable table, Material material)
        {
            table.entries.Clear();
            var report=new StringBuilder("# Instantiated signatures\n\nGenerated by HLSpellPrefabBuilder.Inventory using HLSpellGrammar.DescribeData on Assets/Data. Includes composition nodes, standalone factories and self handler contexts; diagnostic rows describe authored intent, never permission to emit. Runtime outcomes lose provenance at the frozen seam.\n\n| Signature | Prefab | Asset paths |\n|---|---|---|\n");
            foreach(var row in Inventory())
            {
                var signature=row.Value.signature;
                string name="HLSignature_"+row.Key.Replace('/','_');
                var go=new GameObject(name); var effect=go.AddComponent<HLSpellEffect>();
                effect.kind=HLSpellPrimitives.Kind(signature); effect.material=material; effect.hasAuthoredSignature=true; effect.authoredSignature=signature;
                HLSpellPrimitives.Build(effect); HLSpellPrimitives.AddMarker(effect,signature);
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.sharedMesh==HLSpellPrimitives.Torus) filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Data/HLTorus.asset");
                    if(filter.sharedMesh==HLSpellPrimitives.Cone) filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Data/HLCone.asset");
                }
                string path=Root+"Prefabs/"+name+".prefab";
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,path); Object.DestroyImmediate(go);
                table.entries.Add(new HLSpellStyleTable.HLEntry{signature=signature,prefab=prefab});
                report.Append("| ").Append(row.Key).Append(" | ").Append(name).Append(" | ").Append(string.Join("<br>",row.Value.paths)).Append(" |\n");
            }
            // Signed resolution seam cannot carry original topology/tempo or expression.
            foreach(var resource in new[]{AttributeType.HealthMax,AttributeType.ManaMax})
            foreach(var sign in new[]{HLSign.Positive,HLSign.Negative})
            {
                var signature=new HLSpellSignature{operation=HLOperation.Resource,hasAttribute=true,attribute=resource,sign=sign,topology=HLTopology.Single,duration=HLDurationShape.Instant,tempo=HLTempo.Immediate};
                if(table.entries.Any(x=>x.signature.Equals(signature))) continue;
                var go=new GameObject("HLResolved_"+resource+sign);var effect=go.AddComponent<HLSpellEffect>();effect.kind=HLSpellPrimitives.Kind(signature);effect.material=material;HLSpellPrimitives.Build(effect);
                foreach(var f in go.GetComponentsInChildren<MeshFilter>()) if(f.sharedMesh==HLSpellPrimitives.Torus) f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Data/HLTorus.asset"); else if(f.sharedMesh==HLSpellPrimitives.Cone) f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Data/HLCone.asset");
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+go.name+".prefab");Object.DestroyImmediate(go);
                table.entries.Add(new HLSpellStyleTable.HLEntry{signature=signature,prefab=prefab});
            }
            File.WriteAllText(Root+"SIGNATURES.md",report.ToString());
        }
        static void AuthorDeliveries()
        {
            string path=Root+"Data/HLDeliveryStyles.asset";
            var styles=AssetDatabase.LoadAssetAtPath<HLDeliveryStyles>(path);
            if(!styles){styles=ScriptableObject.CreateInstance<HLDeliveryStyles>();AssetDatabase.CreateAsset(styles,path);}
            styles.entries.Clear();
            foreach(var name in new[]{"BulletSpeed","ChainLightning","ChannelingLightning","CurveBullet","CurveBullet2","CurveSphereBullet","LaserBullet","StraightLaserBullet","SwarmBullet"})
            {
                var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/"+name+".prefab");
                bool curve=go.GetComponent<CurvedHomingProjectileBehaviour>() || go.GetComponent<ArcHomingProjectileBehaviour>();
                bool bounce=go.GetComponent<BounceProjectileBehaviour>();
                bool chain=go.GetComponent<ChainLightningProjectile>();
                // Laser/swarm are authored presentation variants, with component evidence retained.
                var evidence=string.Join(", ",go.GetComponents<Component>().Where(x=>x).Select(x=>x.GetType().Name));
                styles.entries.Add(new HLDeliveryStyles.HLEntry{prefabName=name,style=HLDeliveryStyles.Classify(curve,name.Contains("Laser"),name=="SwarmBullet",bounce,chain),evidence=evidence});
            }
            EditorUtility.SetDirty(styles);
        }
        static void SaveMesh(Mesh mesh,string name)
        {
            string path=Root+"Data/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old) { if(old != mesh) EditorUtility.CopySerialized(mesh,old); } else AssetDatabase.CreateAsset(Object.Instantiate(mesh),path);
        }
    }
}
