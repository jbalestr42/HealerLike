using HealerLike.Render.Stones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace HealerLike.Render.Stage
{
    public class StageKeyLightTests
    {
        [Test] public void AimPointsTheLightAlongTheStoneShadowDirection()
        {
            var rotation=StageKeyLight.Aim(StageKeyLight.StoneKeyDirection);
            Assert.That(Vector3.Angle(-(rotation*Vector3.forward),StageKeyLight.StoneKeyDirection),Is.LessThan(.01f));
            // Upper left: the light sits at -x and above the ground.
            Assert.That(StageKeyLight.StoneKeyDirection.x,Is.LessThan(0)); Assert.That(StageKeyLight.StoneKeyDirection.y,Is.GreaterThan(0));
        }
        [Test] public void RealShadowsNeedPipelineShadowsLightShadowsAndDistance()
        {
            var go=new GameObject("KeyLightTest"); var light=go.AddComponent<Light>(); light.type=LightType.Directional; light.shadows=LightShadows.Soft;
            var pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                pipeline.shadowDistance=70;
                bool on=pipeline.supportsMainLightShadows && pipeline.mainLightRenderingMode==LightRenderingMode.PerPixel;
                Assert.That(StageKeyLight.RendersRealShadows(pipeline,light,50),Is.EqualTo(on));
                Assert.That(StageKeyLight.RendersRealShadows(pipeline,light,80),Is.False,"shadow distance short of the board");
                light.shadows=LightShadows.None; Assert.That(StageKeyLight.RendersRealShadows(pipeline,light,50),Is.False);
                light.shadows=LightShadows.Soft; light.type=LightType.Point; Assert.That(StageKeyLight.RendersRealShadows(pipeline,light,50),Is.False);
                Assert.That(StageKeyLight.RendersRealShadows(null,light,50),Is.False);
                Assert.That(StageKeyLight.RendersRealShadows(pipeline,null,50),Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(pipeline); }
        }
        [Test] public void CheapEllipsesTurnOffWithRealShadowsAndBackOnWithout()
        {
            var go=new GameObject("ClumpTest"); go.SetActive(false);
            try
            {
                var clump=go.AddComponent<StoneTerrainClump>();
                Assert.That(clump.groundShadowEnabled,Is.True);
                Assert.That(StageKeyLight.ApplyCheapShadows(true,null,new[]{clump}),Is.EqualTo(1));
                Assert.That(clump.groundShadowEnabled,Is.False);
                Assert.That(StageKeyLight.ApplyCheapShadows(true,null,new[]{clump}),Is.Zero,"already off");
                Assert.That(StageKeyLight.ApplyCheapShadows(false,null,new[]{clump}),Is.Zero);
                Assert.That(clump.groundShadowEnabled,Is.True);
                Assert.DoesNotThrow(()=>StageKeyLight.ApplyCheapShadows(true,new StoneEnemyVisual[]{null},null));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
