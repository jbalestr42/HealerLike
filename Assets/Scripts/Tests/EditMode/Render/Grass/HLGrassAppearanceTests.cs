using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.TestTools;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using Object=UnityEngine.Object;
namespace HealerLike.Render.Grass
{
    public class HLGrassAppearanceTests
    {
        [UnityTest] public IEnumerator CaptureCarpetAndFeedbackFixtureOnMetal()
        {
            if(System.Environment.GetEnvironmentVariable("HL_GROUND_CAPTURE")!="1" || SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)
                Assert.Ignore("Opt-in visual fixture: HL_GROUND_CAPTURE=1 with Metal.");
            var owned=new List<Object>();
            var previousPipeline=QualitySettings.renderPipeline;
            var previousSun=RenderSettings.sun; var previousTarget=RenderTexture.active;
            var owners=Object.FindObjectsByType<HLLookController>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();
            foreach(var owner in owners) owner.enabled=false;
            GameObject Make(string name) { var go=new GameObject(name); go.layer=30; owned.Add(go); return go; }
            try
            {
                QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Very High_PipelineAsset.asset");
                var camera=Make("HLGroundFixtureCamera").AddComponent<Camera>();
                camera.cullingMask=1<<30; camera.fieldOfView=44; camera.aspect=1.5f;
                camera.transform.rotation=Quaternion.Euler(48,0,0); camera.transform.position=-camera.transform.forward*12;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.74f,.82f,.83f);
                var light=Make("HLGroundFixtureKey").AddComponent<Light>(); light.type=LightType.Directional;
                light.transform.rotation=Quaternion.Euler(45,-35,0); light.shadows=LightShadows.Soft; light.intensity=1;
                RenderSettings.sun=light;
                var look=Make("HLGroundFixtureLook").AddComponent<HLLookController>();
                var settings=HLLookSettings.Default; settings.FogStart=25;settings.FogEnd=60;
                settings.ShadowTint=new Color32(63,91,148,255);settings.InkStrength=.75f;look.Settings=settings;
                var material=new Material(Shader.Find("HL/Look/Primitive"));owned.Add(material);
                material.SetColor("_BaseColor",((Color)new Color32(78,126,87,255)).linear);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(ground);ground.layer=30;
                ground.transform.localScale=new Vector3(8,.2f,8);ground.transform.position=Vector3.down*.1f;
                ground.GetComponent<Renderer>().sharedMaterial=material;
                var grid=Make("HLGroundFixtureGrid").AddComponent<GridManager>();grid.width=grid.height=8;grid.size=1;grid.cells=new GridCell[64];
                var registry=Make("HLGroundFixtureZones").AddComponent<HLZoneRegistry>();registry.Initialize();
                var field=Make("HLGroundFixtureGrass").AddComponent<HLGrassField>();
                field.Initialize(grid,ground.transform,camera,registry.Buffer,64); field.BladeBudget=16384;
                TestHelpers.InvokePrivate(field,"OnEnable");
                TestHelpers.SetPrivateField(field,"updateGrass",AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/HLGrass.compute"));
                TestHelpers.SetPrivateField(field,"grassShader",Shader.Find("HL/Grass/BladeAndCone"));
                TestHelpers.SetPrivateField(field,"ringShader",AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader"));
                for(int i=0;i<3;i++)
                {
                    var stone=Make("HLFixtureStone"+i);stone.transform.position=new Vector3((i-1)*2.2f,0,1.6f);
                    var clump=stone.AddComponent<HLStoneTerrainClump>();TestHelpers.SetPrivateField(clump,"_stoneMaterial",material);
                    clump.Initialize((uint)(i+3),1.3f);clump.groundShadowEnabled=false;
                    registry.Add(HLZoneKind.Trample,stone.transform.position,.8f,1);
                }
                registry.Add(HLZoneKind.Heal,new Vector3(-1.7f,0,-1.2f),1.3f,1);
                registry.Add(HLZoneKind.Hostile,new Vector3(1.7f,0,-.9f),1.2f,.85f);
                registry.PublishFrame(.32f);field.SetZoneSnapshot(registry.Buffer,registry.Count);
                var target=new RenderTexture(1440,960,24,RenderTextureFormat.ARGB32);owned.Add(target);target.Create();
                var texture=new Texture2D(1440,960,TextureFormat.RGB24,false);owned.Add(texture);
                look.ApplyGlobals();TestHelpers.InvokePrivate(field,"LateUpdate");
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1440,960),0,0);texture.Apply();
                int GreenPixels() => texture.GetPixels32().Count(c => c.g > 140 && c.g > c.r * 1.1f && c.g > c.b * 1.3f);
                int firstGrassPixels=GreenPixels(); Assert.Greater(firstGrassPixels,20000,"First camera render contains the carpet.");
                // An Editor repaint can occur on another frame without a simulation LateUpdate.
                // Keep prepared buffers, advance the Editor once, then render the same camera again.
                yield return null;
                look.ApplyGlobals();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1440,960),0,0);texture.Apply();
                Assert.Greater(GreenPixels(),firstGrassPixels*.9f,"Repaint must resubmit prepared grass without another field LateUpdate.");
                var bytes=texture.EncodeToPNG();Assert.Greater(bytes.Length,10000);Assert.AreEqual(16384,field.BladeCount);
                const string directory="/Users/fc/Documents/healerlike-render-specs/captures";Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory,"wave9-ground-fixture.png"),bytes);
                field.SetZoneSnapshot(null,0);
                yield return null;
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1440,960),0,0);texture.Apply();
                Assert.Less(GreenPixels(),firstGrassPixels*.1f,"Revoking the borrowed zone snapshot must stop camera submissions.");
                Debug.Log("HL wave9 ground fixture: broad grass, three seeded clumps, explicit heal/hostile/trample zones. Visual fixture, not gameplay events.");
            }
            finally
            {
                RenderTexture.active=previousTarget;QualitySettings.renderPipeline=previousPipeline;RenderSettings.sun=previousSun;
                for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);
                foreach(var owner in owners)if(owner)owner.enabled=true;
            }
        }
    }
}
