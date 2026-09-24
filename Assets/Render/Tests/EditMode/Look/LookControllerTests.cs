using System;
using System.Collections.Generic;
using System.Reflection;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Look
{

public class LookControllerTests
{
    static readonly int applied = Shader.PropertyToID("_HLLookApplied");
    static readonly string ownerWarning = "LookController already has an active owner; "
                                          + "this controller remains inactive.";

    readonly Dictionary<int, float> _floats = new Dictionary<int, float>();
    readonly Dictionary<int, Vector4> _vectors = new Dictionary<int, Vector4>();
    readonly List<GameObject> _objects = new List<GameObject>();

    // The settings fields are camelCase, their shader globals are _HL plus the PascalCase name
    static int GlobalId(FieldInfo field)
    {
        string name = char.ToUpperInvariant(field.Name[0]) + field.Name.Substring(1);
        return Shader.PropertyToID("_HL" + name);
    }

    static FieldInfo[] SettingsFields()
    {
        return typeof(LookSettings).GetFields(BindingFlags.Public | BindingFlags.Instance);
    }

    [SetUp]
    public void SetUp()
    {
        foreach (FieldInfo field in SettingsFields())
        {
            int id = GlobalId(field);
            if (field.FieldType == typeof(Color))
            {
                _vectors[id] = Shader.GetGlobalVector(id);
            }
            else
            {
                _floats[id] = Shader.GetGlobalFloat(id);
            }
        }

        _floats[applied] = Shader.GetGlobalFloat(applied);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        _objects.Clear();
        foreach (KeyValuePair<int, float> pair in _floats)
        {
            Shader.SetGlobalFloat(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<int, Vector4> pair in _vectors)
        {
            Shader.SetGlobalVector(pair.Key, pair.Value);
        }

        _floats.Clear();
        _vectors.Clear();
    }

    LookController CreateController(bool active = true)
    {
        GameObject go = new GameObject("Look test");
        _objects.Add(go);
        go.SetActive(false);
        LookController controller = go.AddComponent<LookController>();
        if (active)
        {
            go.SetActive(true);
        }

        return controller;
    }

    [Test]
    public void ApplyGlobals_SteadyFrames_AllocatesNothing()
    {
        LookController controller = CreateController();
        for (int i = 0; i < 8; i++)
        {
            controller.ApplyGlobals();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++)
        {
            controller.ApplyGlobals();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [TestCase(ColorSpace.Gamma)]
    [TestCase(ColorSpace.Linear)]
    public void ToWorkingColor_ColorSpace_ConvertsExplicitlyWithOpaqueAlpha(ColorSpace space)
    {
        Color color = new Color(0.2f, 0.5f, 0.8f, 0.3f);
        Color expected = space == ColorSpace.Linear ? color.linear : color;

        Assert.That(LookController.ToWorkingColor(color, space),
                    Is.EqualTo(new Vector4(expected.r, expected.g, expected.b, 1f)));
    }

    [Test]
    public void UploadGlobals_InvalidInput_PublishesValidatedValuesAndTheFlag()
    {
        LookSettings input = LookSettings.Default;
        input.shadowStrength = float.NaN;
        input.fogBands = 0;
        LookSettings validated = input.Validated();

        LookController.UploadGlobals(in input);

        Assert.That(Shader.GetGlobalFloat(applied), Is.EqualTo(1f));
        foreach (FieldInfo field in SettingsFields())
        {
            int id = GlobalId(field);
            if (field.FieldType == typeof(Color))
            {
                Vector4 expected = LookController.ToWorkingColor((Color)field.GetValue(validated),
                                                                   QualitySettings.activeColorSpace);
                Assert.That(Shader.GetGlobalVector(id), Is.EqualTo(expected));
            }
            else
            {
                Assert.That(Shader.GetGlobalFloat(id), Is.EqualTo(Convert.ToSingle(field.GetValue(validated))));
            }
        }
    }

    [Test]
    public void OnDisable_AppliedLook_ClearsTheAppliedFlag()
    {
        LookController controller = CreateController();
        controller.ApplyGlobals();

        controller.enabled = false;
        TestHelpers.InvokePrivate(controller, "OnDisable");

        Assert.That(Shader.GetGlobalFloat(applied), Is.Zero);
    }

    [Test]
    public void OnValidate_InvalidSettings_ValidatesLocallyWithoutPublishing()
    {
        LookController controller = CreateController();
        controller.ApplyGlobals();
        LookSettings input = LookSettings.Default;
        input.fogBands = 0;

        TestHelpers.SetPrivateField(controller, "_settings", input);
        TestHelpers.InvokePrivate(controller, "OnValidate");

        Assert.That(controller.settings.fogBands, Is.EqualTo(1));
        Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(6f));

        controller.ApplyGlobals();
        Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(1f));
    }

    Camera CreatePortraitCamera()
    {
        GameObject go = new GameObject("Look camera");
        _objects.Add(go);
        Camera camera = go.AddComponent<Camera>();
        camera.fieldOfView = 40f;
        camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, -15f), Quaternion.Euler(52f, 0f, 0f));
        return camera;
    }

    [Test]
    public void Init_CameraAndBoard_SetsFogFromTheBoardCorners()
    {
        LookController controller = CreateController();
        Camera camera = CreatePortraitCamera();
        Bounds board = new Bounds(Vector3.zero, new Vector3(6f, 0f, 10f));

        controller.Init(camera, board);

        Vector2 fog = StageCalibration.BackgroundFog(camera.transform.position, board);
        Assert.AreEqual(fog.x, controller.settings.fogStart);
        Assert.AreEqual(fog.y, controller.settings.fogEnd);
    }

    [Test]
    public void Init_CameraAndBoard_KeepsTheAuthoredHatch()
    {
        LookController controller = CreateController();
        LookSettings authored = LookSettings.Default;
        authored.inkScale = 0.08f;
        controller.settings = authored;

        controller.Init(CreatePortraitCamera(), new Bounds(Vector3.zero, new Vector3(6f, 0f, 10f)));

        Assert.AreEqual(0.08f, controller.settings.inkScale);
        Assert.AreEqual(authored.inkWidth, controller.settings.inkWidth);
        Assert.AreEqual(authored.inkDistStart, controller.settings.inkDistStart);
        Assert.AreEqual(authored.inkFarSpacing, controller.settings.inkFarSpacing);
    }

    [Test]
    public void Init_CameraAndBoard_KeepsTheAuthoredColours()
    {
        LookController controller = CreateController();
        LookSettings authored = LookSettings.Default;
        authored.shadowTint = new Color(63f / 255f, 91f / 255f, 148f / 255f, 1f);
        authored.inkStrength = 0.75f;
        controller.settings = authored;

        controller.Init(CreatePortraitCamera(), new Bounds(Vector3.zero, new Vector3(6f, 0f, 10f)));

        Assert.AreEqual(authored.shadowTint, controller.settings.shadowTint);
        Assert.AreEqual(0.75f, controller.settings.inkStrength);
    }

    [Test]
    public void Init_NoCamera_LogsAndKeepsTheSettings()
    {
        LookController controller = CreateController();
        LookSettings before = controller.settings;

        LogAssert.Expect(LogType.Error, "[LookController] Init needs the camera the look is calibrated for.");
        controller.Init(null, new Bounds(Vector3.zero, Vector3.one));

        Assert.AreEqual(before.fogStart, controller.settings.fogStart);
        Assert.AreEqual(before.inkScale, controller.settings.inkScale);
    }
}

}
