using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Grass
{

public class GrassGpuTests
{
    [Test]
    public void MetalComputeReadsZoneLanesListsVisibleSeedsAndClearsRemovedInfluence()
    {
        if (!SystemInfo.supportsComputeShaders || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            Assert.Ignore("Requires a graphics device; run the grass suite with -force-metal.");
        var asset = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
        Assert.NotNull(asset);
        var compute = Object.Instantiate(asset);
        var buffers = new List<GraphicsBuffer>();
        GraphicsBuffer Make(GraphicsBuffer.Target target, int count, int stride)
        {
            var buffer = new GraphicsBuffer(target, count, stride); buffers.Add(buffer); return buffer;
        }
        try
        {
            int kernel = compute.FindKernel("HLUpdateGrass");
            var layout = GrassLayout.Generate(1, 1, 1, Vector3.zero, 0.5f, 65);
            for (int i = 0; i < layout.Length; i++) layout[i].positionYaw = new Vector4(i == 0 ? -2 : i == 1 ? 2 : 20, 0.505f, 0, 0);
            var seedBuffer = Make(GraphicsBuffer.Target.Structured, 65, BladeSeed.Stride); seedBuffer.SetData(layout);
            var stateBuffer = Make(GraphicsBuffer.Target.Structured, 66, BladeState.Stride);
            var initial = new BladeState[66]; initial[65].leanHeightSpike = Vector4.one * 123; stateBuffer.SetData(initial);
            var zones = Make(GraphicsBuffer.Target.Structured, 64, Zone.Stride);
            var zoneData = new Zone[64];
            zoneData[0] = new Zone { position = new Vector3(-2, 90, 0), radius = 1, kind = 1, strength = 0.8f, age = 0.06f };
            zoneData[1] = new Zone { position = new Vector3(2, -90, 0), radius = 1, kind = 2, strength = 0.9f, age = 0.09f };
            zones.SetData(zoneData);
            var visible = Make(GraphicsBuffer.Target.Append, 65, 4);
            var counter = Make(GraphicsBuffer.Target.Raw, 1, 4);
            var planes = new Vector4[6]; for (int i = 0; i < 6; i++) planes[i] = new Vector4(0, 0, 0, 100);
            compute.SetBuffer(kernel, "_HL_BladeSeeds", seedBuffer); compute.SetBuffer(kernel, "_HL_BladeStates", stateBuffer);
            compute.SetBuffer(kernel, "_HL_Zones", zones); compute.SetBuffer(kernel, "_HL_VisibleBlades", visible);
            compute.SetInt("_HL_BladeCount", 65); compute.SetInt("_HL_ZoneCount", 2);
            compute.SetVectorArray("_HL_FrustumPlanes", planes); compute.SetVector("_HL_Wind", new Vector4(1, 0, 1.2f, 0.065f));
            uint Dispatch()
            {
                visible.SetCounterValue(0); compute.Dispatch(kernel, 2, 1, 1);
                GraphicsBuffer.CopyCount(visible, counter, 0);
                var counts = new uint[1]; counter.GetData(counts); return counts[0];
            }
            Assert.AreEqual(65u, Dispatch());
            var states = new BladeState[66]; stateBuffer.GetData(states);
            Assert.That(states[0].rampHealReserved.y, Is.EqualTo(0.4f).Within(0.0001));
            Assert.That(states[0].leanHeightSpike.z, Is.EqualTo(1.32f).Within(0.0001));
            Assert.That(states[1].leanHeightSpike.w, Is.EqualTo(0.759375f).Within(0.0001));
            Assert.AreEqual(2, states[1].rampHealReserved.x); Assert.That(states[1].leanHeightSpike.z, Is.EqualTo(1.5f * 0.759375f * 0.648f).Within(0.0001));
            Assert.AreEqual(Vector4.one * 123, states[65].leanHeightSpike);
            var ids = new uint[65]; visible.GetData(ids, 0, 0, 65); var unique = new HashSet<uint>(ids);
            Assert.AreEqual(65, unique.Count); Assert.IsTrue(unique.Contains(1), "The spike is drawn from the same list.");
            var hostileState = states[1]; compute.SetFloat("_HL_Time", 50); Dispatch(); stateBuffer.GetData(states);
            Assert.AreEqual(hostileState, states[1], "Spikes remain rigid as wind time changes.");
            compute.SetInt("_HL_ZoneCount", 64); Assert.AreEqual(65u, Dispatch()); stateBuffer.GetData(states);
            Assert.GreaterOrEqual(states[1].leanHeightSpike.w, 0.5f);
            compute.SetInt("_HL_ZoneCount", 0); Assert.AreEqual(65u, Dispatch());
            stateBuffer.GetData(states); Assert.AreEqual(0, states[1].leanHeightSpike.w); Assert.AreEqual(1, states[0].leanHeightSpike.z);
            BladeState Sample(int kind, Vector3 point, float radius, float age, float strength = 1, uint heading = 0)
            {
                layout[0].positionYaw = new Vector4(point.x, 0.505f, point.z, 0); seedBuffer.SetData(layout);
                zoneData[0] = new Zone { radius = radius, kind = kind, strength = strength, age = age, reserved = heading };
                zones.SetData(zoneData); compute.SetInt("_HL_ZoneCount", 1);
                compute.SetVector("_HL_Wind", new Vector4(1, 0, 1.2f, 0)); Dispatch(); stateBuffer.GetData(states);
                return states[0];
            }
            var range = Sample(3, Vector3.right, 3, 1);
            Assert.Greater(range.leanHeightSpike.x, 0); Assert.That(range.rampHealReserved.y, Is.InRange(0.01f, 0.4f));
            Assert.AreEqual(0, range.leanHeightSpike.w);
            var bruise = Sample(4, Vector3.zero, 3, 1);
            Assert.Less(bruise.leanHeightSpike.z, 1); Assert.AreEqual(0, bruise.leanHeightSpike.w);
            Assert.AreEqual(1, bruise.rampHealReserved.z);
            var calm = Sample(5, Vector3.back * 2, 4, 0.2f, strength: 0);
            var launch = Sample(5, Vector3.forward * 2, 4, 0.2f, heading: 1073741824u);
            Assert.Greater(launch.leanHeightSpike.y, 0.1f); Assert.That(launch.leanHeightSpike.x, Is.EqualTo(calm.leanHeightSpike.x).Within(0.0001));
            var behind = Sample(5, Vector3.back * 2, 4, 0.2f, heading: 1073741824u);
            Assert.That(behind.leanHeightSpike.y, Is.EqualTo(calm.leanHeightSpike.y).Within(0.0001));
            var passed = Sample(5, Vector3.forward * 2, 4, 0.4f, heading: 1073741824u);
            Assert.AreEqual(calm.leanHeightSpike.y, passed.leanHeightSpike.y);
            Assert.AreEqual(0, Sample(1, Vector3.right, 2, 0).rampHealReserved.y);
            Assert.AreEqual(0, Sample(1, Vector3.right, 2, 0.15f).rampHealReserved.y);
            Assert.AreEqual(1, Sample(1, Vector3.right, 2, 0.3f).rampHealReserved.y);
            float rising = Sample(2, Vector3.zero, 3, 0.075f).leanHeightSpike.z;
            float peak = Sample(2, Vector3.zero, 3, 0.15f).leanHeightSpike.z;
            float sinking = Sample(2, Vector3.zero, 3, 0.7f, 0.125f).leanHeightSpike.z;
            Assert.Less(rising, peak); Assert.Less(sinking, rising);
            Assert.GreaterOrEqual(states[0].leanHeightSpike.w, 0.5f, "Sinking spikes retain their geometry until expiry.");
            var trample = Sample(6, Vector3.zero, 1, 1);
            Assert.That(trample.leanHeightSpike.z * layout[0].heightPhaseWidthRandom.x, Is.EqualTo(0.055f).Within(0.0001));
            Assert.AreEqual(0, trample.leanHeightSpike.w);
            var outside = Sample(6, Vector3.right * 1.1f, 1, 1);
            Assert.AreEqual(1, outside.leanHeightSpike.z);
            Sample(6, Vector3.zero, 1, 1);
            zoneData[1] = new Zone { radius = 2, kind = 2, strength = 1, age = 1 };
            zones.SetData(zoneData); compute.SetInt("_HL_ZoneCount", 2); Dispatch(); stateBuffer.GetData(states);
            Assert.AreEqual(0, states[0].leanHeightSpike.w, "Obstacles remain flattened through hostile overlap.");
            planes[0] = new Vector4(1, 0, 0, -100); compute.SetVectorArray("_HL_FrustumPlanes", planes);
            Assert.AreEqual(0u, Dispatch());
            foreach (var message in ShaderUtil.GetComputeShaderMessages(compute)) Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
        }
        finally { foreach (var b in buffers) b.Dispose(); Object.DestroyImmediate(compute); }
    }

    [Test]
    public void LookAndRingShaderPassesCompileWithGrassInstancingOnGraphicsDevice()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Shader compilation needs a graphics device.");
        foreach (string path in new[] { "Assets/Render/Shaders/Look.shader", "Assets/Render/Shaders/GrassRing.shader" })
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path); Assert.NotNull(shader);
            var material = new Material(shader) { enableInstancing = true };
            try
            {
                foreach (string shadow in new[] { "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN" })
                foreach (bool oct in new[] { false, true })
                {
                    material.shaderKeywords = new[] { "PROCEDURAL_INSTANCING_ON", GrassPalette.InstancedKeyword, shadow, oct ? "_GBUFFER_NORMALS_OCT" : "", "_SHADOWS_SOFT" };
                    for (int pass = 0; pass < material.passCount; pass++)
                    {
                        ShaderUtil.CompilePass(material, pass, true);
                        Assert.IsTrue(ShaderUtil.IsPassCompiled(material, pass), path + " pass " + pass);
                    }
                }
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
            }
            finally { Object.DestroyImmediate(material); }
        }
    }
}

}
