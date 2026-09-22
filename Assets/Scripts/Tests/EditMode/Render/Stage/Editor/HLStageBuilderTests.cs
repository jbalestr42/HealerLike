using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace HealerLike.Render.Stage
{
    public class HLStageBuilderTests
    {
        [Test] public void EveryQualityRendererHasOneActiveOutlineFeature()
        {
            var guids=AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset",new[]{"Assets/Settings"});
            Assert.That(guids.Length,Is.EqualTo(6));
            foreach(var guid in guids)
            {
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var list=new SerializedObject(pipeline).FindProperty("m_RendererDataList");
                for(int i=0;i<list.arraySize;i++)
                {
                    var renderer=list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                    Assert.That(renderer,Is.Not.Null);
                    Assert.That(renderer.rendererFeatures.Count(f=>f && f.name.StartsWith("HLOutlines") && f.isActive),Is.EqualTo(1),renderer.name);
                }
            }
        }
        [Test] public void CopiedDataDoesNotReferToSourceDataAndVariantRootReferencesResolve()
        {
            var originalGuids=Directory.GetFiles("Assets/Data","*.asset",SearchOption.AllDirectories).Select(AssetDatabase.AssetPathToGUID).ToArray();
            foreach(var path in Directory.GetFiles(HLStageBuilder.Root+"Data","*.asset",SearchOption.AllDirectories))
            {
                string yaml=File.ReadAllText(path);
                foreach(var guid in originalGuids) Assert.That(yaml,Does.Not.Contain("guid: "+guid),path);
                foreach(Match match in Regex.Matches(yaml,@"fileID: (-?\d+), guid: ([a-f0-9]{32})"))
                {
                    var assetPath=AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);
                    if(!assetPath.StartsWith(HLStageBuilder.Root+"Prefabs/Projectiles/") && !assetPath.StartsWith(HLStageBuilder.Root+"Prefabs/Area/")) continue;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    Assert.That(prefab,Is.Not.Null,assetPath);
                    var objects=prefab.GetComponentsInChildren<Transform>(true).SelectMany(t=>new Object[]{t.gameObject}.Concat(t.GetComponents<Component>().Cast<Object>()));
                    var ids=objects.Where(o=>o).Select(o=> { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string _,out long id); return id; }).ToArray();
                    Assert.That(ids,Does.Contain(long.Parse(match.Groups[1].Value)),path+" -> "+assetPath);
                }
            }
        }
        [Test] public void EveryEntityHasARenderOwnedModel()
        {
            var guids=AssetDatabase.FindAssets("t:EntityData",new[]{HLStageBuilder.Root+"Data"});
            Assert.That(guids.Length,Is.GreaterThanOrEqualTo(11));
            foreach(var guid in guids)
            {
                var data=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var model=new SerializedObject(data).FindProperty("model").objectReferenceValue;
                Assert.That(model,Is.Not.Null,data.name);
                Assert.That(AssetDatabase.GetAssetPath(model),Does.StartWith("Assets/Render/"),data.name);
            }
        }
    }
}
