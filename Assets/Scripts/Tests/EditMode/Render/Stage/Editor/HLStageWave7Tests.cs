using System.IO;
using HealerLike.Render.Environment;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    // Built-state checks for the wave-7 beauty wiring; they read the assets HLStageBuilder.Build wrote.
    public class HLStageWave7Tests
    {
        [Test] public void EveryProjectileGustsTheEnvironmentAndOnlyLightningDrawsContactThreads()
        {
            int lightning=0;
            foreach(var path in Directory.GetFiles(HLStageBuilder.Root+"Prefabs/Projectiles","*.prefab"))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab.GetComponent<HLStageLaunchGust>(),Is.Not.Null,path);
                bool expected=HLStageBuilder.IsLightning(path);
                Assert.That(prefab.GetComponent<HLChainContactVisual>()!=null,Is.EqualTo(expected),path);
                if(expected) lightning++;
            }
            Assert.That(lightning,Is.EqualTo(2));
        }
        [Test] public void IsLightningNamesTheTwoLightningVariants()
        {
            Assert.That(HLStageBuilder.IsLightning("A/HLChainLightning.prefab"),Is.True);
            Assert.That(HLStageBuilder.IsLightning("A/HLChannelingLightning.prefab"),Is.True);
            Assert.That(HLStageBuilder.IsLightning("A/HLCurveBullet.prefab"),Is.False);
        }
        [Test] public void EveryStageModelTramplesAroundItsRoot()
        {
            int models=0;
            foreach(var path in Directory.GetFiles(HLStageBuilder.Root+"Prefabs/Models","*.prefab"))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var trample=prefab.GetComponent<HLTrampleZone>();
                Assert.That(trample,Is.Not.Null,path);
                Assert.That(trample.Radius,Is.EqualTo(HLStageBeautyWiring.TrampleRadius(HLStageBuilder.CreatureFootprint(prefab.transform))).Within(1e-5f),path);
                models++;
            }
            Assert.That(models,Is.GreaterThan(0));
        }
        [Test] public void StageGroundMaterialDrawsTheGridAndTheSharedAssetDoesNot()
        {
            var stage=AssetDatabase.LoadAssetAtPath<Material>(HLStageBuilder.StageGroundPath);
            Assert.That(stage,Is.Not.Null);
            Assert.That(stage.GetFloat("_HLGroundGrid"),Is.EqualTo(1));
            var shared=AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Environment/HLLook_Ground.mat");
            Assert.That(shared.GetFloat("_HLGroundGrid"),Is.Zero);
        }
        [Test] public void StageSceneCarriesTheWiringGustAndHealerBind()
        {
            string yaml=File.ReadAllText(HLStageBuilder.ScenePath);
            Assert.That(yaml,Does.Match(@"gust: \{fileID: [1-9]"));
            Assert.That(yaml,Does.Match(@"stoneGridEntry: \{fileID: [1-9]"));
            Assert.That(yaml,Does.Match(@"healerView: \{fileID: [1-9]"));
            Assert.That(yaml,Does.Match(@"viewCamera: \{fileID: [1-9]"));
        }
    }
}
