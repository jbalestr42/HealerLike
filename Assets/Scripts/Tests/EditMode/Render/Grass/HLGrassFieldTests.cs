using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;
using HealerLike.Render.Zones;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassFieldTests
    {
        [Test] public void LaunchGustDoublesAmplitudePointsAtTargetAndExpires()
        {
            var go = new GameObject("field");
            try
            {
                var field = go.AddComponent<HLGrassField>(); var baseline = field.Wind;
                field.TriggerGust(Vector3.forward);
                Assert.AreEqual(0, field.Wind.x); Assert.AreEqual(1, field.Wind.y);
                Assert.AreEqual(baseline.w * 2, field.Wind.w);
                field.AdvanceGust(0.49f); Assert.AreEqual(baseline.w * 2, field.Wind.w);
                field.AdvanceGust(0.02f); Assert.AreEqual(baseline, field.Wind);
                field.TriggerGust(new Vector3(float.NaN, 0, 0)); Assert.AreEqual(baseline, field.Wind);
                field.TriggerGust(Vector3.zero); Assert.AreEqual(baseline, field.Wind);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [TestCase(false)] [TestCase(true)]
        public void BuiltFieldReleasesEveryOwnedResourceOnDisableOrDestroy(bool destroy)
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsIndirectArgumentsBuffer ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            var go = new GameObject("field lifecycle");
            var borrowed = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64, HLZone.Stride);
            try
            {
                var grid = go.AddComponent<GridManager>();
                grid.width = grid.height = 1; grid.size = 1;
                var field = go.AddComponent<HLGrassField>();
                field.Initialize(grid, go.transform, null, borrowed, 64);
                field.BladeBudget = 65;
                TestHelpers.SetPrivateField(field, "updateGrass", AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/HLGrass.compute"));
                TestHelpers.SetPrivateField(field, "grassShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrass.shader"));
                TestHelpers.SetPrivateField(field, "ringShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader"));
                for (int cycle = 0; cycle < (destroy ? 1 : 3); cycle++)
                {
                    field.enabled = true;
                    var build = typeof(HLGrassField).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.IsTrue((bool)build.Invoke(field, new object[] { 0.5f }));
                    Assert.IsTrue(field.IsReady);
                    var buffers = new List<GraphicsBuffer>();
                    var objects = new List<UnityEngine.Object>();
                    int meshes = 0, materials = 0, computes = 0;
                    foreach (var member in typeof(HLGrassField).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        var value = member.GetValue(field);
                        if (value is GraphicsBuffer buffer && !ReferenceEquals(buffer, borrowed)) buffers.Add(buffer);
                        if (value is Mesh mesh) { meshes++; objects.Add(mesh); }
                        if (value is Material material) { materials++; objects.Add(material); }
                        if (member.Name == "compute" && value is ComputeShader compute) { computes++; objects.Add(compute); }
                    }
                    Assert.AreEqual(7, buffers.Count);
                    Assert.AreEqual(3, meshes); Assert.AreEqual(3, materials); Assert.AreEqual(1, computes);
                    // Runtime messages do not run automatically on this EditMode-only fixture.
                    TestHelpers.InvokePrivate(field, destroy ? "OnDestroy" : "OnDisable");
                    if (destroy) UnityEngine.Object.DestroyImmediate(field);
                    else field.enabled = false;
                    int liveBuffers = 0, liveObjects = 0;
                    foreach (var buffer in buffers) if (buffer.IsValid()) liveBuffers++;
                    foreach (var value in objects) if (value != null) liveObjects++;
                    Assert.AreEqual(0, liveBuffers, "Every captured native buffer must be disposed.");
                    Assert.AreEqual(0, liveObjects, "Every captured mesh, material and compute instance must be destroyed.");
                    Assert.IsTrue(borrowed.IsValid(), "The zone buffer belongs to the registry.");
                    if (!destroy) { Assert.IsFalse(field.IsReady); Assert.AreEqual(0, field.BladeCount); }
                }
            }
            finally
            {
                var survivingField = go.GetComponent<HLGrassField>();
                if (survivingField != null) survivingField.Release();
                UnityEngine.Object.DestroyImmediate(go); borrowed.Dispose();
            }
        }

        [Test] public void DefaultsBudgetClampNullSnapshotAndIdempotentRelease()
        {
            var go = new GameObject("HLGrassFieldTest");
            try
            {
                var field = go.AddComponent<HLGrassField>();
                Assert.AreEqual(65536, field.BladeBudget);
                field.BladeBudget = int.MaxValue; Assert.AreEqual(98304, field.BladeBudget);
                field.BladeBudget = -1; Assert.AreEqual(0, field.BladeBudget);
                field.SetZoneSnapshot(null, 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => field.SetZoneSnapshot(null, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => field.SetZoneCount(65));
                Assert.Throws<ArgumentException>(() => field.Initialize(null, null, null, null, 64));
                TestHelpers.InvokePrivate(field, "LateUpdate");
                field.Release(); field.Release(); Assert.IsFalse(field.IsReady); Assert.AreEqual(0, field.BladeCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
