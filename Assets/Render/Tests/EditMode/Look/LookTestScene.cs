using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HealerLike.Render.Stage;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{

// A scene for one render test on its own layer: a private copy of the project pipeline running only the given
// renderer feature, a camera, a key light, a look and a target. Release puts back the pipeline, the sun, the
// active target, the globals the tests write, and every look it turned off.
public class LookTestScene
{
    public static readonly int Layer = 30;
    static readonly string pipelinePath = "Assets/Settings/Very High_PipelineAsset.asset";
    static readonly string[] savedGlobals =
    {
        "_HLGridCell", "_HLGridStrength",
        "_HLLookApplied", "_HLToonThreshold", "_HLToonSoftness", "_HLFogStart", "_HLFogEnd", "_HLFogBands",
        "_HLInkStrength", "_HLContrast", "_HLInkScale", "_HLInkWidth", "_HLInkStart", "_HLInkRange",
        "_HLDensityMul", "_HLInkWarp", "_HLDashAmount", "_HLDashScale", "_HLInkDistStart", "_HLInkFarSpacing"
    };

    readonly List<Object> _owned = new List<Object>();
    readonly List<LookController> _disabledLooks = new List<LookController>();
    RenderPipelineAsset _previousPipeline;
    Light _previousSun;
    RenderTexture _previousTarget;
    float[] _previousGlobals;
    Vector4 _previousGridOrigin;
    Vector4 _previousGridExtent;
    RenderTexture _target;

    Camera _camera;
    public Camera camera { get { return _camera; } }

    LookController _look;
    public LookController look { get { return _look; } }

    Texture2D _texture;
    public Texture2D texture { get { return _texture; } }

    // Saves what a test may change: the pipeline, the sun, the active target and the globals
    public void Init()
    {
        _previousPipeline = QualitySettings.renderPipeline;
        _previousSun = RenderSettings.sun;
        _previousTarget = RenderTexture.active;
        _previousGlobals = savedGlobals.Select(Shader.GetGlobalFloat).ToArray();
        _previousGridOrigin = Shader.GetGlobalVector("_HLGridOrigin");
        _previousGridExtent = Shader.GetGlobalVector("_HLGridExtent");
    }

    // Turns every other look off and swaps in the pipeline copy; a null feature leaves its renderer with none
    public void UsePipeline(ScriptableRendererFeature feature)
    {
        foreach (LookController controller in Object.FindObjectsByType<LookController>(FindObjectsSortMode.None))
        {
            if (controller.enabled)
            {
                controller.enabled = false;
                _disabledLooks.Add(controller);
            }
        }

        RenderPipelineAsset pipeline = Track(Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath)));
        SerializedObject pipelineData = new SerializedObject(pipeline);
        SerializedProperty rendererProperty = pipelineData.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
        Object renderer = Track(Object.Instantiate(rendererProperty.objectReferenceValue));
        rendererProperty.objectReferenceValue = renderer;
        pipelineData.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject rendererData = new SerializedObject(renderer);
        SerializedProperty features = rendererData.FindProperty("m_RendererFeatures");
        features.arraySize = 0;
        if (feature != null)
        {
            features.arraySize = 1;
            features.GetArrayElementAtIndex(0).objectReferenceValue = feature;
        }

        rendererData.ApplyModifiedPropertiesWithoutUndo();
        QualitySettings.renderPipeline = pipeline;
    }

    // Looks down at the origin from the given pitch and distance, onto a solid clear
    public Camera CreateCamera(string name, float fieldOfView, float aspect, float pitch, float distance,
                               Color background)
    {
        _camera = Track(new GameObject(name)).AddComponent<Camera>();
        _camera.cullingMask = 1 << Layer;
        _camera.fieldOfView = fieldOfView;
        _camera.aspect = aspect;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 100f;
        _camera.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        _camera.transform.position = -_camera.transform.forward * distance;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = background;
        return _camera;
    }

    public Light CreateKeyLight(string name, Quaternion rotation)
    {
        Light light = Track(new GameObject(name)).AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = rotation;
        RenderSettings.sun = light;
        return light;
    }

    public LookController CreateLook(string name, LookSettings settings)
    {
        _look = Track(new GameObject(name)).AddComponent<LookController>();
        _look.settings = settings;
        return _look;
    }

    // A square view down the stage pitch at the origin, lit by the stage key light, on a white clear, the fog
    // pushed past the view
    public void BuildKeyLight(float fieldOfView, float distance)
    {
        UsePipeline(null);
        CreateCamera("Key light camera", fieldOfView, 1f, StageCalibration.PortraitPitch, distance, Color.white);
        CreateKeyLight("Key light", StageKeyLight.Aim(StageKeyLight.KeyDirection));
        LookSettings settings = LookSettings.Default;
        settings.fogStart = 90f;
        settings.fogEnd = 99f;
        CreateLook("Key light look", settings);
        CreateTarget(256, 256);
    }

    public void CreateTarget(int width, int height)
    {
        _target = Track(new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32));
        _target.Create();
        _texture = Track(new Texture2D(width, height, TextureFormat.RGB24, false));
    }

    public ItemType Track<ItemType>(ItemType item) where ItemType : Object
    {
        _owned.Add(item);
        return item;
    }

    // Renders the camera into the target and reads it back into the texture
    public void Render()
    {
        _look.ApplyGlobals();
        RenderPipeline.SubmitRenderRequest(_camera, new RenderPipeline.StandardRequest { destination = _target });
        RenderTexture.active = _target;
        _texture.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
        _texture.Apply();
    }

    public void Release()
    {
        if (_previousGlobals != null)
        {
            for (int i = 0; i < savedGlobals.Length; i++)
            {
                Shader.SetGlobalFloat(savedGlobals[i], _previousGlobals[i]);
            }

            Shader.SetGlobalVector("_HLGridOrigin", _previousGridOrigin);
            Shader.SetGlobalVector("_HLGridExtent", _previousGridExtent);
            RenderTexture.active = _previousTarget;
            QualitySettings.renderPipeline = _previousPipeline;
            RenderSettings.sun = _previousSun;
        }

        for (int i = _owned.Count - 1; i >= 0; i--)
        {
            if (_owned[i])
            {
                Object.DestroyImmediate(_owned[i]);
            }
        }

        _owned.Clear();
        foreach (LookController controller in _disabledLooks)
        {
            if (controller)
            {
                controller.enabled = true;
            }
        }

        _disabledLooks.Clear();
    }

    // Channel by channel median of the square of the given half size around a pixel
    public static Color32 MedianColour(Texture2D texture, int x, int y, int halfSize)
    {
        List<byte> reds = new List<byte>();
        List<byte> greens = new List<byte>();
        List<byte> blues = new List<byte>();
        for (int dy = -halfSize; dy <= halfSize; dy++)
        {
            for (int dx = -halfSize; dx <= halfSize; dx++)
            {
                Color32 pixel = texture.GetPixel(x + dx, y + dy);
                reds.Add(pixel.r);
                greens.Add(pixel.g);
                blues.Add(pixel.b);
            }
        }

        reds.Sort();
        greens.Sort();
        blues.Sort();
        int middle = reds.Count / 2;
        return new Color32(reds[middle], greens[middle], blues[middle], 255);
    }

    // Share of the covered pixels in the magenta shade marker, which reads redder than green, on a white clear
    public static float ShadeShare(Texture2D texture)
    {
        int covered = 0;
        int shade = 0;
        foreach (Color32 pixel in texture.GetPixels32())
        {
            if (pixel.r == 255 && pixel.g == 255 && pixel.b == 255)
            {
                continue;
            }

            covered++;
            if (pixel.r > pixel.g)
            {
                shade++;
            }
        }

        if (covered == 0)
        {
            return 0f;
        }
        return (float)shade / covered;
    }
}

}
