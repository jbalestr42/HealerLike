using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Creatures
{
    // Visual-only gallery of the shipped recipes, it runs no gameplay
    public static class HLMatchCreatureCapture
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            List<HLCreatureRig> rigs = new List<HLCreatureRig>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
            HLPrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<HLPrimitiveMeshes>(meshesPath);

            Camera camera = new GameObject("HLGalleryCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 5.5f, -10f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 2.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.75f, 0.82f, 0.86f);
            camera.aspect = 2f;

            Light light = new GameObject("HLGallerySun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.sun = light;

            HLLookController look = new GameObject("HLGalleryLook").AddComponent<HLLookController>();
            HLLookSettings settings = HLLookSettings.Default;
            settings.fogStart = 50f;
            settings.fogEnd = 80f;
            look.settings = settings;
            look.ApplyGlobals();

            string[] names = { "HLSpiralFern", "HLHangingArch", "HLHealer", "HLSphereStack", "HLBladeRosette" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject root = new GameObject(names[i]);
                root.transform.position = new Vector3((i - 2) * 2.0f, 0f, 0f);
                string path = "Assets/Render/Creatures/Data/" + names[i] + ".asset";
                HLCreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
                HLCreatureRig rig = new HLCreatureRig();
                if (!rig.Init(recipe, root.transform, material, meshes))
                {
                    return;
                }

                rigs.Add(rig);
                rig.SetReadout(null, 1f, 0f, 0.8f);
                rig.Tick(0f, 0.016f, new HLFootFrame(root.transform.position, Vector3.up, 1f));
            }

            Vector3 reach = new Vector3(-2.1f, 1.3f, -1f);
            rigs[0].BeginDelivery(1001, HLDeliveryStyle.Direct, null, reach);
            rigs[0].ContactDelivery(1001, reach, null);
            rigs[0].Tick(0.1f, 0.016f, new HLFootFrame(new Vector3(-4f, 0f, 0f), Vector3.up, 1f));

            RenderTexture target = new RenderTexture(1600, 800, 24, RenderTextureFormat.ARGB32);
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            Texture2D texture = new Texture2D(1600, 800, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, 1600f, 800f), 0, 0);
            texture.Apply();
            string capturePath = "/Users/fc/Documents/healerlike-render-specs/captures/wave9-creature-gallery.png";
            File.WriteAllBytes(capturePath, texture.EncodeToPNG());
            Debug.Log("HL visual-only gallery: " + capturePath);
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
            foreach (HLCreatureRig rig in rigs)
            {
                rig.Dispose();
            }
        }
    }
}
