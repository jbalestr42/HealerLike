using System.IO;
using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Look;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace HealerLike.Render.Stage
{
    // Built-state checks for the wave-6 stage pass; they read the assets HLStageBuilder.Build wrote.
    public class HLStageWave6Tests
    {
        static string[] Projectiles => Directory.GetFiles(HLStageBuilder.Root+"Prefabs/Projectiles","*.prefab");
        [Test] public void ProjectileVariantsCopyTheCreatureObserverAndTakeTheirDeliveryStyle()
        {
            var styles=AssetDatabase.LoadAssetAtPath<HLDeliveryStyles>(HLStageBuilder.DeliveryStylesPath);
            Assert.That(styles,Is.Not.Null);
            Assert.That(Projectiles.Length,Is.EqualTo(9));
            foreach(var path in Projectiles)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var observer=prefab.GetComponent<HLProjectileVisualObserver>();
                Assert.That(observer,Is.Not.Null,path);
                Assert.That(observer.DeliveryStyle,Is.EqualTo(styles.For(prefab)),path);
                var creature=AssetDatabase.LoadAssetAtPath<GameObject>(HLStageBuilder.CreatureProjectiles+prefab.name.Substring(2)+".prefab");
                Assert.That(creature,Is.Not.Null,path);
                var expected=new SerializedObject(creature.GetComponent<HLProjectileVisualObserver>());
                var actual=new SerializedObject(observer);
                foreach(var field in new[]{"preserveContactPath","presentation"})
                    Assert.That(actual.FindProperty(field).intValue,Is.EqualTo(expected.FindProperty(field).intValue),path+" "+field);
                Assert.That(prefab.GetComponent<HLLaunchWave>(),Is.Not.Null,path);
            }
            Assert.That(styles.ForName("HLChainLightning"),Is.EqualTo(HLDeliveryStyle.ChainSync));
        }
        [Test] public void LegacyChainVisualNeverRunsItsSocketLoopButKeepsTheComponent()
        {
            int chains=0;
            foreach(var path in Projectiles)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var chain=prefab.GetComponent<ChainLightningProjectile>(); if(!chain) continue;
                chains++;
                var so=new SerializedObject(chain);
                Assert.That(so.FindProperty("_effectMode").intValue,Is.EqualTo((int)ChainLightningProjectile.EffectMode.FixedDuration),path);
                Assert.That(so.FindProperty("_effectDuration").floatValue,Is.LessThan(0),path);
                Assert.That(prefab.GetComponentsInChildren<LineRenderer>(true).All(l=>!l.enabled),path);
            }
            Assert.That(chains,Is.EqualTo(2));
        }
        [Test] public void LegacyChainVisualOffIgnoresOtherProjectiles()
        {
            var go=new GameObject("HLNotAChain");
            try { Assert.That(HLStageBuilder.LegacyChainVisualOff(go),Is.False); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void EveryTierRendersMainShadowsToSeventyAndDrawsGrassSafeEdges()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset",new[]{"Assets/Settings"}))
            {
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(pipeline.supportsMainLightShadows,Is.True,pipeline.name);
                Assert.That(pipeline.mainLightRenderingMode,Is.EqualTo(LightRenderingMode.PerPixel),pipeline.name);
                Assert.That(pipeline.shadowDistance,Is.EqualTo(HLStageBuilder.ShadowDistance),pipeline.name);
                Assert.That(pipeline.shadowCascadeCount,Is.EqualTo(2),pipeline.name);
                var list=new SerializedObject(pipeline).FindProperty("m_RendererDataList");
                for(int i=0;i<list.arraySize;i++)
                {
                    var data=(ScriptableRendererData)list.GetArrayElementAtIndex(i).objectReferenceValue;
                    var outlines=data.rendererFeatures.OfType<HLOutlines>().Single();
                    Assert.That(outlines.depthNormalEdges && outlines.useNormalEdgeMask,Is.True,data.name);
                    Assert.That(outlines.normalAngleDegrees,Is.EqualTo(55)); Assert.That(outlines.depthThresholdWorld,Is.EqualTo(1));
                }
            }
        }
        [Test] public void ModelsCarryTheirReadoutsAndTheBufferIsAPlant()
        {
            int allies=0, buffers=0;
            foreach(var guid in AssetDatabase.FindAssets("t:EntityData",new[]{HLStageBuilder.Root+"Data"}))
            {
                var data=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var so=new SerializedObject(data);
                var model=so.FindProperty("model").objectReferenceValue as GameObject;
                Assert.That(model,Is.Not.Null,data.name);
                Assert.That(model.GetComponent<HLHealPulse>(),Is.Not.Null,data.name);
                if(model.GetComponent<HLRangePreview>()) allies++;
                if(model.GetComponent<HLBruiseZone>()) Assert.That(HLStageBuilder.Bruises(HLStageBuilder.AuthoredRange(so)),Is.True,data.name+" bruises the whole board");
                if(data.name.Contains("HitArmor"))
                {
                    buffers++;
                    var source=PrefabUtility.GetCorrespondingObjectFromSource(model);
                    Assert.That(AssetDatabase.GetAssetPath(source),Is.EqualTo("Assets/Render/Creatures/Prefabs/HLHitArmorBuffer.prefab"),data.name);
                }
            }
            Assert.That(allies,Is.GreaterThan(0)); Assert.That(buffers,Is.GreaterThan(0));
        }
        [Test] public void BruiseOnlyForRangesShorterThanTheBoard()
        {
            Assert.That(HLStageBuilder.Bruises(3),Is.True);
            Assert.That(HLStageBuilder.Bruises(100),Is.False,"Soldier");
            Assert.That(HLStageBuilder.Bruises(HLStageBuilder.BruiseMaxRange),Is.False);
            Assert.That(HLStageBuilder.Bruises(0),Is.False);
        }
        [Test] public void StagePreservesGameplayGridGeneratorAndWiresTheHealerPulse()
        {
            string yaml=File.ReadAllText(HLStageBuilder.ScenePath);
            Assert.That(yaml,Does.Not.Contain("HLStoneGeneration"));
            Assert.That(yaml,Does.Not.Contain("stoneGridEntry:"));
            const string generator=@"_gridGenerator: \{fileID: -?\d+\}";
            var original=System.Text.RegularExpressions.Regex.Match(File.ReadAllText("Assets/Scenes/Main.unity"),generator);
            var render=System.Text.RegularExpressions.Regex.Match(yaml,generator);
            Assert.That(original.Success && render.Success,Is.True,"Player grid generator reference must exist");
            Assert.That(render.Value,Is.EqualTo(original.Value),"Render copy must retain gameplay generator ownership");
            Assert.That(yaml,Does.Match(@"healPulse: \{fileID: [1-9]"));
            Assert.That(yaml,Does.Match(@"healSource: \{fileID: [1-9]"));
        }
        [Test] public void AreaVariantsKeepGameplayAndGrassPulseWithoutLegacyFilledEffects()
        {
            var paths=Directory.GetFiles(HLStageBuilder.Root+"Prefabs/Area","*.prefab");
            Assert.That(paths.Length,Is.GreaterThan(0));
            foreach(var path in paths) {
                var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(go.GetComponent<AreaOfEffect>().enabled,Is.True,path);
                Assert.That(go.GetComponent<HLAreaPulse>().enabled,Is.True,path);
                Assert.That(go.GetComponent<HLLegacyAreaVisualMask>(),Is.Not.Null,path);
                Assert.That(((Behaviour)go.GetComponent("DestroyOnDone")).enabled,Is.True,path);
                foreach(var effect in go.GetComponentsInChildren<Behaviour>(true))
                    if(effect.GetType().FullName=="UnityEngine.VFX.VisualEffect") Assert.That(effect.enabled,Is.True,path);
            }
        }
    }
}
