using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    public static class LookShaderProbe
    {
        static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";

        // Both colour plumbing and hatch resolution need an actual URP light/shadow state, not globals left by
        // whichever camera rendered previously. All objects remain on the fixture's private layer.
        public static MeshRenderer BuildLitQuad(LookTestScene scene, float lightAngle = 0f)
        {
            scene.BuildKeyLight(20f, 4f);
            scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
            LookSettings settings = scene.look.settings;
            settings.toonThreshold = 0.1f;
            settings.toonSoftness = 0.01f;
            settings.inkStrength = 0f;
            settings.contrast = 1f;
            scene.look.settings = settings;
            Mesh mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            GameObject surface = scene.Track(new GameObject("Lit shader probe"));
            surface.layer = LookTestScene.Layer;
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = scene.Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath)));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Vector3 direction = Quaternion.Euler(0f, lightAngle, 0f) * mesh.normals[0];
            RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            return renderer;
        }

        public static Color ReadCentre(LookTestScene scene, Renderer surface, MaterialPropertyBlock block)
        {
            surface.SetPropertyBlock(block);
            scene.Render();
            return scene.texture.GetPixel(128, 128).linear;
        }
    }
}
