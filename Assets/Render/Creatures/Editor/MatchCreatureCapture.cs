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
    public static class MatchCreatureCapture
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            List<CreatureRig> rigs = new List<CreatureRig>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
            string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);

            Camera camera = new GameObject("GalleryCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 5.5f, -10f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 2.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.75f, 0.82f, 0.86f);
            camera.aspect = 2f;

            Light light = new GameObject("GallerySun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.sun = light;

            LookController look = new GameObject("GalleryLook").AddComponent<LookController>();
            LookSettings settings = LookSettings.Default;
            settings.fogStart = 50f;
            settings.fogEnd = 80f;
            look.settings = settings;
            look.ApplyGlobals();

            string[] names = { "SpiralFern", "HangingArch", "Healer", "SphereStack", "BladeRosette" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject root = new GameObject(names[i]);
                root.transform.position = new Vector3((i - 2) * 2.0f, 0f, 0f);
                string path = "Assets/Render/Creatures/Data/" + names[i] + ".asset";
                CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
                CreatureRig rig = new CreatureRig();
                if (!rig.Init(recipe, root.transform, material, meshes))
                {
                    return;
                }

                rigs.Add(rig);
                rig.SetReadout(null, 1f, 0f, 0.8f);
                rig.Tick(0f, 0.016f, new FootFrame(root.transform.position, Vector3.up, 1f));
            }

            Vector3 reach = new Vector3(-2.1f, 1.3f, -1f);
            rigs[0].BeginDelivery(1001, DeliveryStyle.Direct, null, reach);
            rigs[0].ContactDelivery(1001, reach, null);
            rigs[0].Tick(0.1f, 0.016f, new FootFrame(new Vector3(-4f, 0f, 0f), Vector3.up, 1f));

            RenderTexture target = new RenderTexture(1600, 800, 24, RenderTextureFormat.ARGB32);
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            Texture2D texture = new Texture2D(1600, 800, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, 1600f, 800f), 0, 0);
            texture.Apply();
            string capturePath = "/Users/fc/Documents/healerlike-render-specs/captures/wave9-creature-gallery.png";
            File.WriteAllBytes(capturePath, texture.EncodeToPNG());
            Debug.Log("Visual-only gallery: " + capturePath);
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
            foreach (CreatureRig rig in rigs)
            {
                rig.Dispose();
            }
        }
    }
}
