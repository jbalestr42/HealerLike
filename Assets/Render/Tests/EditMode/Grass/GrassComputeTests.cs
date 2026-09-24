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
    TuftSeed[] _layout;
    Zone[] _zones;
    TuftState[] _states;
    Vector4[] _planes;

    // 65 upright tufts of the mean size, so a zone's push is the whole lean
    static TuftSeed[] CreateSeeds()
    {
        TuftSeed[] seeds = new TuftSeed[65];
        for (int i = 0; i < seeds.Length; i++)
        {
            seeds[i].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0f);
        }

        return seeds;
    }

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

        // Tuft 0 sits under the heal zone, tuft 1 under the hostile one, the rest far away
        _layout = CreateSeeds();
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
        _seedBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 65, TuftSeed.Stride);
        _seedBuffer.SetData(_layout);

        // One state past the tuft count guards against writes beyond it
        _stateBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 66, TuftState.Stride);
        _states = new TuftState[66];
        _states[65].leanHeightSpike = Vector4.one * 123f;
        _stateBuffer.SetData(_states);

        _zoneBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 64, Zone.Stride);
        _zones = new Zone[64];
        _zones[0] = new Zone
        {
            position = new Vector3(-2f, 90f, 0f), radius = 1f, kind = 1, strength = 0.8f, age = 0.06f
        };
        _zones[1] = new Zone
        {
            position = new Vector3(2f, -90f, 0f), radius = 1f, kind = 2, strength = 0.9f, age = 0.09f
        };
        _zoneBuffer.SetData(_zones);

        _visible = CreateBuffer(GraphicsBuffer.Target.Append, 65, 4);
        _counter = CreateBuffer(GraphicsBuffer.Target.Raw, 1, 4);
        _planes = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            _planes[i] = new Vector4(0f, 0f, 0f, 100f);
        }

        _compute.SetBuffer(_kernel, "_HLBladeSeeds", _seedBuffer);
        _compute.SetBuffer(_kernel, "_HLBladeStates", _stateBuffer);
        _compute.SetBuffer(_kernel, "_HLZones", _zoneBuffer);
        _compute.SetBuffer(_kernel, "_HLVisibleBlades", _visible);
        _compute.SetInt("_HLBladeCount", 65);
        _compute.SetInt("_HLZoneCount", 2);
        _compute.SetVectorArray("_HLFrustumPlanes", _planes);
        _compute.SetFloat("_HLCullMargin", GrassBounds.Envelope(1f));
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

    // Runs the kernel and returns how many tuft ids it appended to the visible list
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

    // Moves tuft 0 to the point under a single zone and returns its state after one dispatch
    TuftState Sample(int kind, Vector3 point, float radius, float age, float strength = 1f, uint heading = 0)
    {
        _layout[0].positionYaw = new Vector4(point.x, 0.505f, point.z, 0f);
        _seedBuffer.SetData(_layout);
        _zones[0] = new Zone { radius = radius, kind = kind, strength = strength, age = age, reserved = heading };
        _zoneBuffer.SetData(_zones);
        _compute.SetInt("_HLZoneCount", 1);
        Dispatch();
        ReadStates();
        return _states[0];
    }

    [Test]
    public void Dispatch_ZoneLanes_ShapesVisibleTuftsAndClearsRemovedInfluence()
    {
        Assert.AreEqual(65u, Dispatch());

        ReadStates();
        Assert.That(_states[0].leanHeightSpike.z, Is.EqualTo(1.32f).Within(0.0001)); // 1 + 0.8 * heal 0.4
        Assert.That(_states[1].leanHeightSpike.w, Is.EqualTo(0.759375f).Within(0.0001));
        float spikeHeight = 0.759375f * 0.648f; // weight * rise
        float spikeTall = _states[1].leanHeightSpike.z * _layout[1].heightWidthLean.x;
        Assert.That(spikeTall, Is.InRange(0.26f * spikeHeight - 0.0001f, 0.63f * spikeHeight + 0.0001f));
        Assert.AreEqual(Vector4.one * 123f, _states[65].leanHeightSpike);
        uint[] ids = new uint[65];
        _visible.GetData(ids, 0, 0, 65);
        HashSet<uint> unique = new HashSet<uint>(ids);
        Assert.AreEqual(65, unique.Count);
        Assert.IsTrue(unique.Contains(1), "The spike is drawn from the same list.");

        TuftState hostileState = _states[1];
        Dispatch();
        ReadStates();
        Assert.AreEqual(hostileState, _states[1], "Spikes stay still from frame to frame.");

        _compute.SetInt("_HLZoneCount", ZonePacker.MaxZones);
        Assert.AreEqual(65u, Dispatch());
        ReadStates();
        Assert.GreaterOrEqual(_states[1].leanHeightSpike.w, 0.5f);

        _compute.SetInt("_HLZoneCount", 0);
        Assert.AreEqual(65u, Dispatch());
        ReadStates();
        Assert.AreEqual(0f, _states[1].leanHeightSpike.w);
        Assert.AreEqual(1f, _states[0].leanHeightSpike.z);

        TuftState range = Sample(3, Vector3.right, 3f, 1f);
        Assert.Greater(range.leanHeightSpike.x, 0f);
        Assert.That(range.leanHeightSpike.z, Is.InRange(1.008f, 1.2801f)); // heal 0.01 to 0.35
        Assert.AreEqual(0f, range.leanHeightSpike.w);

        TuftState bruise = Sample(4, Vector3.zero, 3f, 1f);
        Assert.Less(bruise.leanHeightSpike.z, 1f);
        Assert.AreEqual(0f, bruise.leanHeightSpike.w);

        TuftState calm = Sample(5, Vector3.back * 2f, 4f, 0.2f, strength: 0f);
        TuftState launch = Sample(5, Vector3.forward * 2f, 4f, 0.2f, heading: quarterTurn);
        Assert.Greater(launch.leanHeightSpike.y, 0.1f);
        Assert.That(launch.leanHeightSpike.x, Is.EqualTo(calm.leanHeightSpike.x).Within(0.0001));
        TuftState behind = Sample(5, Vector3.back * 2f, 4f, 0.2f, heading: quarterTurn);
        Assert.That(behind.leanHeightSpike.y, Is.EqualTo(calm.leanHeightSpike.y).Within(0.0001));
        TuftState passed = Sample(5, Vector3.forward * 2f, 4f, 0.4f, heading: quarterTurn);
        Assert.AreEqual(calm.leanHeightSpike.y, passed.leanHeightSpike.y);

        Assert.AreEqual(1f, Sample(1, Vector3.right, 2f, 0f).leanHeightSpike.z);
        // grown, heal 1
        Assert.That(Sample(1, Vector3.right, 2f, 0.3f).leanHeightSpike.z, Is.EqualTo(1.8f).Within(0.0001));

        float rising = Sample(2, Vector3.zero, 3f, 0.075f).leanHeightSpike.z;
        float peak = Sample(2, Vector3.zero, 3f, 0.15f).leanHeightSpike.z;
        float sinking = Sample(2, Vector3.zero, 3f, 0.7f, 0.125f).leanHeightSpike.z;
        Assert.Less(rising, peak);
        Assert.Less(sinking, rising);
        Assert.GreaterOrEqual(_states[0].leanHeightSpike.w, 0.5f, "Sinking spikes retain their geometry until expiry.");

        TuftState trample = Sample(6, Vector3.zero, 1f, 1f);
        Assert.That(trample.leanHeightSpike.z * _layout[0].heightWidthLean.x, Is.EqualTo(0.055f).Within(0.0001));
        Assert.AreEqual(0f, trample.leanHeightSpike.w);
        TuftState outside = Sample(6, Vector3.right * 1.1f, 1f, 1f);
        Assert.AreEqual(1f, outside.leanHeightSpike.z);

        Sample(6, Vector3.zero, 1f, 1f);
        _zones[1] = new Zone { radius = 2f, kind = 2, strength = 1f, age = 1f };
        _zoneBuffer.SetData(_zones);
        _compute.SetInt("_HLZoneCount", 2);
        Dispatch();
        ReadStates();
        Assert.AreEqual(0f, _states[0].leanHeightSpike.w, "Obstacles remain flattened through hostile overlap.");

        _planes[0] = new Vector4(1f, 0f, 0f, -100f);
        _compute.SetVectorArray("_HLFrustumPlanes", _planes);
        Assert.AreEqual(0u, Dispatch());
        foreach (ShaderMessage message in ShaderUtil.GetComputeShaderMessages(_compute))
        {
            Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
        }
    }

    [Test]
    public void Dispatch_RootOutsideAPlane_IsKeptOnlyWithinTheCullMargin()
    {
        float margin = GrassBounds.Envelope(1f);
        _planes[0] = new Vector4(1f, 0f, 0f, 2f - 0.5f * margin);
        _compute.SetVectorArray("_HLFrustumPlanes", _planes);

        uint kept = Dispatch();

        _planes[0] = new Vector4(1f, 0f, 0f, 2f - 1.5f * margin);
        _compute.SetVectorArray("_HLFrustumPlanes", _planes);

        Assert.AreEqual(kept - 1, Dispatch()); // tuft 0 at x -2 falls outside the margin
    }

    [Test]
    public void Dispatch_NoZones_KeepsEachTuftsRestLean()
    {
        for (int i = 0; i < _layout.Length; i++)
        {
            _layout[i].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, i / 128f);
        }
        _seedBuffer.SetData(_layout);
        _compute.SetInt("_HLZoneCount", 0);

        Dispatch();
        ReadStates();
        TuftState[] first = (TuftState[])_states.Clone();
        Dispatch();
        ReadStates();

        for (int i = 0; i < 65; i++)
        {
            Assert.AreEqual(new Vector4(0f, i / 128f, 1f, 0f), _states[i].leanHeightSpike);
            Assert.AreEqual(first[i], _states[i], "No wind, nothing moves between frames.");
        }
    }

    [Test]
    public void Dispatch_HealZone_PushesOnTopOfTheRestLean()
    {
        _layout[0].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0.5f);
        _seedBuffer.SetData(_layout);

        TuftState pushed = Sample(1, Vector3.right, 3f, 1f);

        Assert.Greater(pushed.leanHeightSpike.x, 0.3f); // outward, away from the zone centre
        Assert.That(pushed.leanHeightSpike.y, Is.EqualTo(0.5f).Within(0.0001f));
    }
}

}
