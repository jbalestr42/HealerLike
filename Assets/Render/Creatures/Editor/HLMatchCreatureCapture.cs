#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using HealerLike.Render.Look;
using HealerLike.Render.Spells;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace HealerLike.Render.Creatures
{
    /// <summary>Explicit visual-only gallery, never represented as gameplay evidence.</summary>
    public static class HLMatchCreatureCapture
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rigs = new List<HLCreatureRig>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            if (!material) material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Creatures/Data/HLPlaceholder.mat");
            var camera = new GameObject("HLGalleryCamera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0,5.5f,-10);
            camera.transform.LookAt(new Vector3(0,1,0)); camera.orthographic=true; camera.orthographicSize=2.8f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.75f,.82f,.86f); camera.aspect=2;
            var light=new GameObject("HLGallerySun").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1;
            light.transform.rotation=Quaternion.Euler(45,-35,0); RenderSettings.sun=light;
            var look=new GameObject("HLGalleryLook").AddComponent<HLLookController>();
            var settings=HLLookSettings.Default; settings.FogStart=50; settings.FogEnd=80; look.Settings=settings; look.ApplyGlobals();
            string[] names={"HLSpiralFern","HLHangingArch","HLHealer","HLSphereStack","HLBladeRosette"};
            for(int i=0;i<names.Length;i++)
            {
                var root=new GameObject(names[i]); root.transform.position=new Vector3((i-2)*2.0f,0,0);
                var recipe=AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>("Assets/Render/Creatures/Data/"+names[i]+".asset");
                var rig=HLCreatureRig.Build(recipe,root.transform,material); rigs.Add(rig);
                rig.SetReadout(null,1,0,.8f); rig.Tick(0,.016f,new HLFootFrame(root.transform.position,Vector3.up,1));
            }
            rigs[0].BeginDelivery(1001,HLDeliveryStyle.Direct,null,new Vector3(-2.1f,1.3f,-1)); rigs[0].ContactDelivery(1001,new Vector3(-2.1f,1.3f,-1),null);
            rigs[0].Tick(.1f,.016f,new HLFootFrame(new Vector3(-4,0,0),Vector3.up,1));
            var target=new RenderTexture(1600,800,24,RenderTextureFormat.ARGB32);target.Create();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
            RenderTexture.active=target;var texture=new Texture2D(1600,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,800),0,0);texture.Apply();
            string path="/Users/fc/Documents/healerlike-render-specs/captures/wave9-creature-gallery.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());Debug.Log("HL visual-only gallery: "+path);
            RenderTexture.active=null; Object.DestroyImmediate(texture);target.Release();Object.DestroyImmediate(target);
            foreach(var rig in rigs)rig.Dispose();
        }
    }
}
#endif
