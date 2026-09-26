using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Grass
{
    public class GrassTuftBatchTests
    {
        readonly List<GraphicsBuffer> _allocated = new List<GraphicsBuffer>();
        GrassTuftBatch _batch;
        PrimitiveMeshes _meshes;

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }
            _meshes = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(
                "Assets/Render/Creatures/Data/PrimitiveMeshes.asset"));
        }

        [TearDown]
        public void TearDown()
        {
            _batch?.Dispose();
            _batch = null;
            foreach (GraphicsBuffer buffer in _allocated)
            {
                if (buffer.IsValid())
                {
                    buffer.Dispose();
                }
            }
            _allocated.Clear();
            UnityEngine.Object.DestroyImmediate(_meshes);
        }

        bool Create(out string error, Func<GraphicsBuffer.Target, int, int, GraphicsBuffer> allocate = null)
        {
            return GrassTuftBatch.TryCreate(new GrassBuildKey(new Rect(-0.5f, -0.5f, 1f, 1f), 1f, 0f, 1, 65),
                4, _meshes, AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat"), 0,
                out _batch, out error, allocate);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void TryCreate_AllocationThrows_ReleasesCompletedBuffersAndAdoptsNothing(int failAt)
        {
            int allocations = 0;
            bool created = Create(out string error, (target, count, stride) =>
            {
                if (++allocations == failAt)
                {
                    throw new InvalidOperationException("Injected native allocation failure");
                }
                GraphicsBuffer buffer = new GraphicsBuffer(target, count, stride);
                _allocated.Add(buffer);
                return buffer;
            });

            Assert.IsFalse(created);
            Assert.IsNull(_batch);
            StringAssert.Contains("Injected native allocation failure", error);
            Assert.AreEqual(failAt - 1, _allocated.Count);
            foreach (GraphicsBuffer buffer in _allocated)
            {
                Assert.IsFalse(buffer.IsValid());
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TryCreate_MissingBorrowedMesh_DoesNotAllocate(bool missingSocle)
        {
            if (missingSocle)
            {
                _meshes.socle = null;
            }
            else
            {
                _meshes.annulus = null;
            }
            Assert.IsFalse(Create(out string error, (target, count, stride) =>
            {
                Assert.Fail("An invalid mesh must be rejected before GPU allocation.");
                return null;
            }));
            Assert.IsNull(_batch);
            StringAssert.Contains("valid meshes", error);
        }
    }
}
