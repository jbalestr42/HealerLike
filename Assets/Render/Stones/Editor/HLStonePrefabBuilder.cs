#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public static class HLStonePrefabBuilder
    {
        const string Root="Assets/Render/Stones/";
        public const int BlockCountDivisor=3;
        [MenuItem("HealerLike/Build stone prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"Prefabs");
            Material material=null;
            if(AssetDatabase.IsValidFolder("Assets/Render/Look"))
            {
                foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Render/Look"}))
                { var candidate=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)); if(candidate.name.Contains("Stone")) {material=candidate;break;} }
            }
            if(material==null)
            {
                string path=Root+"HLPlaceholderStone.mat";
                material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
                material.SetColor("_BaseColor",new Color32(74,84,104,255)); material.SetFloat("_Smoothness",0); material.SetFloat("_Metallic",0);
                material.enableInstancing=true; EditorUtility.SetDirty(material);
            }
            BuildModel("Assets/Models/Kawaii Slime/Prefabs/Slime_01_Viking.prefab","HLStoneSoldierModel",HLStonePreset.Boulder,material);
            BuildModel("Assets/Models/Kawaii Slime/Prefabs/Slime_03 Leaf.prefab","HLStoneCairnModel",HLStonePreset.Cairn,material);
            BuildBlock(material); BuildGrid();
            foreach(string path in Directory.GetFiles("Assets/Prefabs/Projectiles","*.prefab"))
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(source.GetComponent<Projectile>()==null) continue;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);
                try {
                    instance.name="HLStone"+source.name;
                    if(instance.GetComponent<HLStoneProjectileImpactBridge>()==null) instance.AddComponent<HLStoneProjectileImpactBridge>();
                    PrefabUtility.SaveAsPrefabAsset(instance,Root+"Prefabs/"+instance.name+".prefab");
                } finally { Object.DestroyImmediate(instance); }
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        static void BuildModel(string sourcePath,string name,HLStonePreset preset,Material material)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
            try {
                go.name=name;
                // Remove inherited render hierarchy, retain the authored HUD as a sibling of BodyPivot.
                foreach(Transform child in go.transform.Cast<Transform>().ToArray())
                    if(child.GetComponentInChildren<EntityHUD>(true)==null) Object.DestroyImmediate(child.gameObject);
                foreach(var c in go.GetComponents<Component>()) if(!(c is Transform) && !(c is EntityModel)) Object.DestroyImmediate(c);
                if(go.GetComponent<EntityModel>()==null) go.AddComponent<EntityModel>();
                var body=new GameObject("BodyPivot"); body.transform.SetParent(go.transform,false); body.AddComponent<LookAtTarget>();
                var source=new GameObject("SkillSource"); source.transform.SetParent(body.transform,false); source.transform.localPosition=new Vector3(0,.55f,.2f); source.AddComponent<SkillSource>();
                var target=new GameObject("SkillTargetPoint"); target.transform.SetParent(body.transform,false); target.transform.localPosition=new Vector3(0,.45f,0); target.AddComponent<SkillTargetPointTag>();
                var visual=go.AddComponent<HLStoneEnemyVisual>(); var so=new SerializedObject(visual);
                so.FindProperty("bodyPivot").objectReferenceValue=body.transform; so.FindProperty("preset").enumValueIndex=(int)preset;
                so.FindProperty("stoneMaterial").objectReferenceValue=material; so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+name+".prefab");
            } finally { Object.DestroyImmediate(go); }
        }
        static void BuildBlock(Material material)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab"));
            try {
                go.name="HLStoneBlock";
                foreach(var c in go.GetComponentsInChildren<MeshRenderer>()) Object.DestroyImmediate(c);
                foreach(var c in go.GetComponentsInChildren<MeshFilter>()) Object.DestroyImmediate(c);
                var child=new GameObject("HLStoneClump"); child.layer=go.layer; child.transform.SetParent(go.transform,false);
                var clump=child.AddComponent<HLStoneTerrainClump>(); var so=new SerializedObject(clump);
                so.FindProperty("stoneMaterial").objectReferenceValue=material; so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/HLStoneBlock.prefab");
            } finally { Object.DestroyImmediate(go); }
        }
        static void BuildGrid()
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid.prefab"));
            try {
                go.name="HLStoneGrid"; var generator=go.GetComponent<GridGenerator>(); var generatorSO=new SerializedObject(generator);
                var list=generatorSO.FindProperty("_gridGenerators");
                // The source's first slot has an unresolved script GUID. Preserve its serialized base
                // spawn settings in this variant, with an explicit basic-system fallback only here.
                if(list.arraySize>0 && list.GetArrayElementAtIndex(0).objectReferenceValue==null)
                {
                    var yaml=File.ReadAllText("Assets/Prefabs/Grid.prefab");
                    var block=System.Text.RegularExpressions.Regex.Match(yaml,@"--- !u!114 &4796522870559552952[\s\S]*?(?=\n---|$)").Value;
                    if(string.IsNullOrEmpty(block)) throw new System.InvalidOperationException("Source grid missing-slot recipe changed; review it before rebuilding");
                    var basic=go.AddComponent<GridGeneratorSystem>(); var basicSO=new SerializedObject(basic);
                    foreach(string field in new[]{"_min","_max"}) basicSO.FindProperty(field).intValue=int.Parse(System.Text.RegularExpressions.Regex.Match(block,field+@": (\d+)").Groups[1].Value);
                    basicSO.FindProperty("_isWalkable").boolValue=System.Text.RegularExpressions.Regex.Match(block,@"_isWalkable: (\d+)").Groups[1].Value=="1";
                    string guid=System.Text.RegularExpressions.Regex.Match(block,@"_prefab: \{fileID: \d+, guid: ([0-9a-f]+)").Groups[1].Value;
                    var prop=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if(prop==null)
                    {
                        // This source prefab GUID is unresolved too. An empty prop preserves the
                        // base spawn selection without inventing an obstacle on a walkable cell.
                        var placeholder=new GameObject("HLStoneGridPlaceholder");
                        try { prop=PrefabUtility.SaveAsPrefabAsset(placeholder,Root+"Prefabs/HLStoneGridPlaceholder.prefab"); }
                        finally { Object.DestroyImmediate(placeholder); }
                    }
                    basicSO.FindProperty("_prefab").objectReferenceValue=prop;
                    basicSO.ApplyModifiedPropertiesWithoutUndo(); list.GetArrayElementAtIndex(0).objectReferenceValue=basic;
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                }
                var systems=new System.Collections.Generic.List<HLStoneBlockGridSystem>();
                for(int i=0;i<list.arraySize;i++)
                {
                    var old=list.GetArrayElementAtIndex(i).objectReferenceValue as BlockGridSystem; if(old==null) continue;
                    var oldSO=new SerializedObject(old); var replacement=go.AddComponent<HLStoneBlockGridSystem>(); var replacementSO=new SerializedObject(replacement);
                    foreach(string property in new[]{"_minSize","_maxSize"}) replacementSO.FindProperty(property).intValue=oldSO.FindProperty(property).intValue;
                    // Wave 4: a third of Julien's block counts, so the stones stop hiding the allies. Sizes and order are unchanged.
                    foreach(string property in new[]{"_min","_max"}) replacementSO.FindProperty(property).intValue=Mathf.RoundToInt(oldSO.FindProperty(property).intValue/(float)BlockCountDivisor);
                    replacementSO.FindProperty("_isWalkable").boolValue=old.isWalkable;
                    replacementSO.FindProperty("_prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/HLStoneBlock.prefab");
                    replacementSO.ApplyModifiedPropertiesWithoutUndo(); list.GetArrayElementAtIndex(i).objectReferenceValue=replacement; systems.Add(replacement);
                    Object.DestroyImmediate(old);
                }
                var fence=go.AddComponent<HLStoneGenerationFence>(); list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=fence;
                generatorSO.ApplyModifiedPropertiesWithoutUndo();
                var entry=go.AddComponent<HLStoneGridEntry>(); var entrySO=new SerializedObject(entry);
                entrySO.FindProperty("generator").objectReferenceValue=generator; entrySO.FindProperty("completionFence").objectReferenceValue=fence;
                var array=entrySO.FindProperty("systems"); array.arraySize=systems.Count;
                for(int i=0;i<systems.Count;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=systems[i];
                entrySO.ApplyModifiedPropertiesWithoutUndo();
                var saved=PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/HLStoneGrid.prefab");
                if(saved==null) throw new System.InvalidOperationException("Stone grid prefab was not saved");
            } finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
