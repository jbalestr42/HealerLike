using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneAssemblyTests
    {
        [TestCase(StonePreset.Boulder, 3, 0.8f)]
        [TestCase(StonePreset.Cairn, 3, 1.05f)]
        [TestCase(StonePreset.Monolith, 1, 1.45f)]
        public void LayoutFitsFootprintAndDisposes(StonePreset preset, int count, float height)
        {
            GameObject go = new GameObject("HLTest");
            StoneAssembly assembly = new StoneAssembly();
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
                if (preset == StonePreset.Cairn)
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
