using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneAssemblyTests
    {
        [TestCase(HLStonePreset.Boulder, 3, 0.8f)]
        [TestCase(HLStonePreset.Cairn, 3, 1.05f)]
        [TestCase(HLStonePreset.Monolith, 1, 1.45f)]
        public void LayoutFitsFootprintAndDisposes(HLStonePreset preset, int count, float height)
        {
            GameObject go = new GameObject("HLTest");
            HLStoneAssembly assembly = new HLStoneAssembly();
            StoneMeshCache meshes = new StoneMeshCache();
            assembly.Init(meshes);
            try
            {
                assembly.BuildEnemy(go.transform, 17, preset, null);
                Assert.AreEqual(count, assembly.parts.Count);
                Assert.That(assembly.localBounds.size.x, Is.LessThanOrEqualTo(0.90001f));
                Assert.That(assembly.localBounds.size.z, Is.LessThanOrEqualTo(0.90001f));
                Assert.That(assembly.localBounds.min.y, Is.EqualTo(0).Within(1e-5));
                Assert.That(assembly.localBounds.size.y, Is.EqualTo(height).Within(1e-5));
                if (preset == HLStonePreset.Cairn)
                {
                    for (int i = 1; i < count; i++)
                    {
                        float below = assembly.parts[i - 1].renderer.bounds.max.y;
                        Assert.That(assembly.parts[i].renderer.bounds.min.y, Is.LessThan(below));
                    }
                }
            }
            finally
            {
                assembly.Dispose();
                meshes.Clear();
                Object.DestroyImmediate(go);
            }
        }
    }
}
