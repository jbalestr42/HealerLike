using System.IO;
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
            EditorUtility.SetDirty(table);
            var sinkGo=new GameObject("HLSpellVisualSink");var sink=sinkGo.AddComponent<HLSpellVisualSink>();sink.styles=table;sink.material=material;
            PrefabUtility.SaveAsPrefabAsset(sinkGo,Root+"Prefabs/HLSpellVisualSink.prefab");Object.DestroyImmediate(sinkGo);
            AssetDatabase.SaveAssets();
        }
        static void SaveMesh(Mesh mesh,string name)
        {
            string path=Root+"Data/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old) { if(old != mesh) EditorUtility.CopySerialized(mesh,old); } else AssetDatabase.CreateAsset(mesh,path);
        }
    }
}
