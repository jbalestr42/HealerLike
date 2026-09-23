using System.Collections.Generic;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{

// Grass.compute is the kernel GrassField dispatches; these tests run it directly on Metal
public class GrassComputeTests
{
    static readonly uint quarterTurn = 1073741824u;

    ComputeShader _compute;
    int _kernel;
    readonly List<GraphicsBuffer> _buffers = new List<GraphicsBuffer>();
    GraphicsBuffer _seedBuffer;
    GraphicsBuffer _stateBuffer;
    GraphicsBuffer _zoneBuffer;
    GraphicsBuffer _visible;
    GraphicsBuffer _counter;
    BladeSeed[] _layout;
    Zone[] _zones;
    BladeState[] _states;
    Vector4[] _planes;

    [SetUp]
    public void SetUp()
    {
        if (!SystemInfo.supportsComputeShaders || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires a graphics device; run the grass suite with -force-metal.");
        }

        ComputeShader asset = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
        Assert.NotNull(asset);
        _compute = Object.Instantiate(asset);
        _kernel = _compute.FindKernel("HLUpdateGrass");

        // Blade 0 sits under the heal zone, blade 1 under the hostile one, the rest far away
        _layout = GrassLayout.Generate(1, 1, 1f, Vector3.zero, 0.5f, 65);
        for (int i = 0; i < _layout.Length; i++)
        {
            float x = 20f;
            if (i == 0)
            {
                x = -2f;
            }
            else if (i == 1)
            {
                x = 2f;
            }
            _layout[i].positionYaw = new Vector4(x, 0.505f, 0f, 0f);
        }
        _seedBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 65, BladeSeed.Stride);
        _seedBuffer.SetData(_layout);

        // One state past the blade count guards against writes beyond it
        _stateBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 66, BladeState.Stride);
        _states = new BladeState[66];
        _states[65].leanHeightSpike = Vector4.one * 123f;
        _stateBuffer.SetData(_states);

        _zoneBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 64, Zone.Stride);
        _zones = new Zone[64];
        _zones[0] = new Zone { position = new Vector3(-2f, 90f, 0f), radius = 1f, kind = 1, strength = 0.8f, age = 0.06f };
        _zones[1] = new Zone { position = new Vector3(2f, -90f, 0f), radius = 1f, kind = 2, strength = 0.9f, age = 0.09f };
        _zoneBuffer.SetData(_zones);

        _visible = CreateBuffer(GraphicsBuffer.Target.Append, 65, 4);
        _counter = CreateBuffer(GraphicsBuffer.Target.Raw, 1, 4);
        _planes = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            _planes[i] = new Vector4(0f, 0f, 0f, 100f);
        }

        _compute.SetBuffer(_kernel, "_HL_BladeSeeds", _seedBuffer);
        _compute.SetBuffer(_kernel, "_HL_BladeStates", _stateBuffer);
        _compute.SetBuffer(_kernel, "_HL_Zones", _zoneBuffer);
        _compute.SetBuffer(_kernel, "_HL_VisibleBlades", _visible);
        _compute.SetInt("_HL_BladeCount", 65);
        _compute.SetInt("_HL_ZoneCount", 2);
        _compute.SetVectorArray("_HL_FrustumPlanes", _planes);
        _compute.SetVector("_HL_Wind", new Vector4(1f, 0f, 1.2f, 0.065f));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GraphicsBuffer buffer in _buffers)
        {
            buffer.Dispose();
        }
        _buffers.Clear();
        if (_compute != null)
        {
            Object.DestroyImmediate(_compute);
        }
    }

    GraphicsBuffer CreateBuffer(GraphicsBuffer.Target target, int count, int stride)
    {
        GraphicsBuffer buffer = new GraphicsBuffer(target, count, stride);
        _buffers.Add(buffer);
        return buffer;
    }

    // Runs the kernel and returns how many blade ids it appended to the visible list
    uint Dispatch()
    {
        _visible.SetCounterValue(0);
        _compute.Dispatch(_kernel, 2, 1, 1);
        GraphicsBuffer.CopyCount(_visible, _counter, 0);
        uint[] counts = new uint[1];
        _counter.GetData(counts);
        return counts[0];
    }

    void ReadStates()
    {
        _stateBuffer.GetData(_states);
    }

    // Moves blade 0 to the point under a single zone and returns its state after one dispatch
    BladeState Sample(int kind, Vector3 point, float radius, float age, float strength = 1f, uint heading = 0)
    {
        _layout[0].positionYaw = new Vector4(point.x, 0.505f, point.z, 0f);
        _seedBuffer.SetData(_layout);
        _zones[0] = new Zone { radius = radius, kind = kind, strength = strength, age = age, reserved = heading };
        _zoneBuffer.SetData(_zones);
        _compute.SetInt("_HL_ZoneCount", 1);
        _compute.SetVector("_HL_Wind", new Vector4(1f, 0f, 1.2f, 0f));
        Dispatch();
        ReadStates();
        return _states[0];
    }

    [Test]
    public void Dispatch_ZoneLanes_ShapesVisibleBladesAndClearsRemovedInfluence()
    {
        Assert.AreEqual(65u, Dispatch());

        ReadStates();
        Assert.That(_states[0].rampHealReserved.y, Is.EqualTo(0.4f).Within(0.0001));
        Assert.That(_states[0].leanHeightSpike.z, Is.EqualTo(1.32f).Within(0.0001));
        Assert.That(_states[1].leanHeightSpike.w, Is.EqualTo(0.759375f).Within(0.0001));
        Assert.AreEqual(2f, _states[1].rampHealReserved.x);
        float spikeHeight = 0.759375f * 0.648f; // weight * rise
        float spikeTall = _states[1].leanHeightSpike.z * _layout[1].heightPhaseWidthRandom.x;
        Assert.That(spikeTall, Is.InRange(0.26f * spikeHeight - 0.0001f, 0.63f * spikeHeight + 0.0001f));
        Assert.AreEqual(Vector4.one * 123f, _states[65].leanHeightSpike);
        uint[] ids = new uint[65];
        _visible.GetData(ids, 0, 0, 65);
        HashSet<uint> unique = new HashSet<uint>(ids);
        Assert.AreEqual(65, unique.Count);
        Assert.IsTrue(unique.Contains(1), "The spike is drawn from the same list.");

        BladeState hostileState = _states[1];
        _compute.SetFloat("_HL_Time", 50f);
        Dispatch();
        ReadStates();
        Assert.AreEqual(hostileState, _states[1], "Spikes remain rigid as wind time changes.");

        _compute.SetInt("_HL_ZoneCount", 64);
        Assert.AreEqual(65u, Dispatch());
        ReadStates();
        Assert.GreaterOrEqual(_states[1].leanHeightSpike.w, 0.5f);

        _compute.SetInt("_HL_ZoneCount", 0);
        Assert.AreEqual(65u, Dispatch());
        ReadStates();
        Assert.AreEqual(0f, _states[1].leanHeightSpike.w);
        Assert.AreEqual(1f, _states[0].leanHeightSpike.z);

        BladeState range = Sample(3, Vector3.right, 3f, 1f);
        Assert.Greater(range.leanHeightSpike.x, 0f);
        Assert.That(range.rampHealReserved.y, Is.InRange(0.01f, 0.4f));
        Assert.AreEqual(0f, range.leanHeightSpike.w);

        BladeState bruise = Sample(4, Vector3.zero, 3f, 1f);
        Assert.Less(bruise.leanHeightSpike.z, 1f);
        Assert.AreEqual(0f, bruise.leanHeightSpike.w);
        Assert.AreEqual(1f, bruise.rampHealReserved.z);

        BladeState calm = Sample(5, Vector3.back * 2f, 4f, 0.2f, strength: 0f);
        BladeState launch = Sample(5, Vector3.forward * 2f, 4f, 0.2f, heading: quarterTurn);
        Assert.Greater(launch.leanHeightSpike.y, 0.1f);
        Assert.That(launch.leanHeightSpike.x, Is.EqualTo(calm.leanHeightSpike.x).Within(0.0001));
        BladeState behind = Sample(5, Vector3.back * 2f, 4f, 0.2f, heading: quarterTurn);
        Assert.That(behind.leanHeightSpike.y, Is.EqualTo(calm.leanHeightSpike.y).Within(0.0001));
        BladeState passed = Sample(5, Vector3.forward * 2f, 4f, 0.4f, heading: quarterTurn);
        Assert.AreEqual(calm.leanHeightSpike.y, passed.leanHeightSpike.y);

        Assert.AreEqual(0f, Sample(1, Vector3.right, 2f, 0f).rampHealReserved.y);
        Assert.AreEqual(0f, Sample(1, Vector3.right, 2f, 0.15f).rampHealReserved.y);
        Assert.AreEqual(1f, Sample(1, Vector3.right, 2f, 0.3f).rampHealReserved.y);

        float rising = Sample(2, Vector3.zero, 3f, 0.075f).leanHeightSpike.z;
        float peak = Sample(2, Vector3.zero, 3f, 0.15f).leanHeightSpike.z;
        float sinking = Sample(2, Vector3.zero, 3f, 0.7f, 0.125f).leanHeightSpike.z;
        Assert.Less(rising, peak);
        Assert.Less(sinking, rising);
        Assert.GreaterOrEqual(_states[0].leanHeightSpike.w, 0.5f, "Sinking spikes retain their geometry until expiry.");

        BladeState trample = Sample(6, Vector3.zero, 1f, 1f);
        Assert.That(trample.leanHeightSpike.z * _layout[0].heightPhaseWidthRandom.x, Is.EqualTo(0.055f).Within(0.0001));
        Assert.AreEqual(0f, trample.leanHeightSpike.w);
        BladeState outside = Sample(6, Vector3.right * 1.1f, 1f, 1f);
        Assert.AreEqual(1f, outside.leanHeightSpike.z);

        Sample(6, Vector3.zero, 1f, 1f);
        _zones[1] = new Zone { radius = 2f, kind = 2, strength = 1f, age = 1f };
        _zoneBuffer.SetData(_zones);
        _compute.SetInt("_HL_ZoneCount", 2);
        Dispatch();
        ReadStates();
        Assert.AreEqual(0f, _states[0].leanHeightSpike.w, "Obstacles remain flattened through hostile overlap.");

        _planes[0] = new Vector4(1f, 0f, 0f, -100f);
        _compute.SetVectorArray("_HL_FrustumPlanes", _planes);
        Assert.AreEqual(0u, Dispatch());
        foreach (ShaderMessage message in ShaderUtil.GetComputeShaderMessages(_compute))
        {
            Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
        }
    }

    [Test]
    public void Dispatch_WindAndGust_KeepsLeanUnderRigidTiltLimit()
    {
        _compute.SetInt("_HL_ZoneCount", 0);
        float largest = 0f;
        for (int step = 0; step < 20; step++)
        {
            _compute.SetFloat("_HL_Time", step * 0.37f);
            Dispatch();
            ReadStates();
            for (int i = 0; i < 65; i++)
            {
                largest = Mathf.Max(largest, new Vector2(_states[i].leanHeightSpike.x, _states[i].leanHeightSpike.y).magnitude);
            }
        }

        _compute.SetVector("_HL_Wind", new Vector4(1f, 0f, 1.2f, 0.13f));
        float gustLargest = 0f;
        for (int step = 0; step < 20; step++)
        {
            _compute.SetFloat("_HL_Time", step * 0.37f);
            Dispatch();
            ReadStates();
            for (int i = 0; i < 65; i++)
            {
                gustLargest = Mathf.Max(gustLargest, new Vector2(_states[i].leanHeightSpike.x, _states[i].leanHeightSpike.y).magnitude);
            }
        }

        Assert.That(largest, Is.InRange(0.05f, 0.3501f)); // 20 degrees
        Assert.That(gustLargest, Is.InRange(largest, 0.5201f)); // 4 * 0.13 radians, about 30 degrees
    }
}

}
