using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Look
{

// The opt-in capture scene of the outline pass: a pipeline copy with only the outline feature, a key light, a look,
// a ground and a casting sphere, and the PNGs it writes
public class OutlinesCaptureScene
{
    public Outlines outlines;
    public LookSettings settings;
    public Material material;
    public GameObject ground;
    public GameObject sphere;
    public string directory;
    LookTestScene _scene;

    public void Build(LookTestScene scene, string captureDirectory)
    {
        _scene = scene;
        outlines = _scene.Track(ScriptableObject.CreateInstance<Outlines>());
        outlines.layerMask = 0;
        outlines.depthNormalEdges = false;
        outlines.Create();
        _scene.UsePipeline(outlines);
        _scene.CreateCamera("Portrait camera", 40f, 1080f / 1920f, 50f, 31f, new Color(0.75f, 0.82f, 0.88f));
        Light light = _scene.CreateKeyLight("Upper left key", Quaternion.Euler(50f, 40f, 0f));
        light.shadowBias = 0.03f;
        light.shadowNormalBias = 0.15f;
        settings = LookSettings.Default;
        settings.fogStart = 70f;
        settings.fogEnd = 100f;
        settings.inkStrength = 0f;
        settings.outlineWidthPixels = 1f;
        _scene.CreateLook("Capture look", settings);
        Shader look = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader");
        material = _scene.Track(new Material(look));
        material.SetColor("_BaseColor", new Color(0.6f, 0.78f, 0.3f));

        ground = CreatePrimitive(PrimitiveType.Plane, "Receiving ground", Vector3.zero, Vector3.one * 3f, 1f);
        sphere = CreatePrimitive(PrimitiveType.Sphere, "Casting sphere", new Vector3(-1f, 2.1f, 0f),
                                  Vector3.one * 4f, 1f);
        MaterialPropertyBlock sphereProperties = new MaterialPropertyBlock();
        sphereProperties.SetColor("_BaseColor", new Color(0.55f, 0.58f, 0.64f));
        sphereProperties.SetFloat("_HLNormalEdges", 1f);
        sphere.GetComponent<Renderer>().SetPropertyBlock(sphereProperties);
        _scene.CreateTarget(1080, 1920);
        directory = captureDirectory;
        Directory.CreateDirectory(directory);
    }

    public GameObject CreatePrimitive(PrimitiveType shape, string name, Vector3 position, Vector3 scale,
        float normalMask)
    {
        GameObject go = _scene.Track(GameObject.CreatePrimitive(shape));
        go.name = name;
        go.layer = LookTestScene.Layer;
        go.transform.position = position;
        go.transform.localScale = scale;
        Renderer meshRenderer = go.GetComponent<Renderer>();
        meshRenderer.sharedMaterial = material;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetFloat("_HLNormalEdges", normalMask);
        meshRenderer.SetPropertyBlock(properties);
        return go;
    }

    public byte[] Capture(string name)
    {
        _scene.Render();
        byte[] bytes = _scene.texture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), bytes);
        Assert.That(bytes.Length, Is.GreaterThan(10000));
        Debug.Log("[OutlinesCaptureScene] Capture: " + name + " on " + SystemInfo.graphicsDeviceType);
        return bytes;
    }
}

}
