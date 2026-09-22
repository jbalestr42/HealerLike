using HealerLike.Render.Stones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace HealerLike.Render.Stage
{
    public class HLStageKeyLightTests
    {
        [Test] public void AimPointsTheLightAlongTheStoneShadowDirection()
        {
            var rotation=HLStageKeyLight.Aim(HLStageKeyLight.StoneKeyDirection);
            Assert.That(Vector3.Angle(-(rotation*Vector3.forward),HLStageKeyLight.StoneKeyDirection),Is.LessThan(.01f));
            // Upper left: the light sits at -x and above the ground.
            Assert.That(HLStageKeyLight.StoneKeyDirection.x,Is.LessThan(0)); Assert.That(HLStageKeyLight.StoneKeyDirection.y,Is.GreaterThan(0));
        }
        [Test] public void RealShadowsNeedPipelineShadowsLightShadowsAndDistance()
        {
            var go=new GameObject("HLKeyLightTest"); var light=go.AddComponent<Light>(); light.type=LightType.Directional; light.shadows=LightShadows.Soft;
            var pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                pipeline.shadowDistance=70;
                bool on=pipeline.supportsMainLightShadows && pipeline.mainLightRenderingMode==LightRenderingMode.PerPixel;
                Assert.That(HLStageKeyLight.RendersRealShadows(pipeline,light,50),Is.EqualTo(on));
                Assert.That(HLStageKeyLight.RendersRealShadows(pipeline,light,80),Is.False,"shadow distance short of the board");
                light.shadows=LightShadows.None; Assert.That(HLStageKeyLight.RendersRealShadows(pipeline,light,50),Is.False);
                light.shadows=LightShadows.Soft; light.type=LightType.Point; Assert.That(HLStageKeyLight.RendersRealShadows(pipeline,light,50),Is.False);
                Assert.That(HLStageKeyLight.RendersRealShadows(null,light,50),Is.False);
                Assert.That(HLStageKeyLight.RendersRealShadows(pipeline,null,50),Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(pipeline); }
        }
        [Test] public void CheapEllipsesTurnOffWithRealShadowsAndBackOnWithout()
        {
            var go=new GameObject("HLClumpTest"); go.SetActive(false);
            try
            {
                var clump=go.AddComponent<HLStoneTerrainClump>();
                Assert.That(clump.GroundShadowEnabled,Is.True);
                Assert.That(HLStageKeyLight.ApplyCheapShadows(true,null,new[]{clump}),Is.EqualTo(1));
                Assert.That(clump.GroundShadowEnabled,Is.False);
                Assert.That(HLStageKeyLight.ApplyCheapShadows(true,null,new[]{clump}),Is.Zero,"already off");
                Assert.That(HLStageKeyLight.ApplyCheapShadows(false,null,new[]{clump}),Is.Zero);
                Assert.That(clump.GroundShadowEnabled,Is.True);
                Assert.DoesNotThrow(()=>HLStageKeyLight.ApplyCheapShadows(true,new HLStoneEnemyVisual[]{null},null));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
