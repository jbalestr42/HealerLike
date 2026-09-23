using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneFractureTests
    {
        [Test]
        public void StatusTintComposesWithCurrentFractureAndRemovalRestoresHealthColor()
        {
            GameObject root = new GameObject("HLTintAssembly");
            HLStoneAssembly assembly = new HLStoneAssembly();
            StoneMeshCache meshes = new StoneMeshCache();
            assembly.Init(meshes);
            try
            {
                assembly.BuildEnemy(root.transform, 17, HLStonePreset.Boulder, null);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                assembly.ApplyFracture(0.3f, 17);
                assembly.parts[0].renderer.GetPropertyBlock(block);
                Color healthColor = (Color)block.GetVector("_BaseColor");

                Color tint = new Color(0.5f, 0.12f, 0.55f, 1f);
                assembly.ApplyFracture(0.3f, 17, tint);
                assembly.parts[0].renderer.GetPropertyBlock(block);
                Vector4 tinted = (Color)block.GetVector("_BaseColor");
                Assert.Less(((Vector4)Color.Lerp(healthColor, tint, 0.42f) - tinted).magnitude, 1e-6f);

                assembly.ApplyFracture(0.3f, 17, Color.white);
                assembly.parts[0].renderer.GetPropertyBlock(block);
                Vector4 restored = (Color)block.GetVector("_BaseColor");
                Assert.Less(((Vector4)healthColor - restored).magnitude, 1e-6f);
            }
            finally
            {
                assembly.Dispose();
                meshes.Clear();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SeededSubsetDarkensProgressivelyAndRestoresWithoutMaterialMutation()
        {
            GameObject root = new GameObject("HLAssembly");
            HLStoneAssembly assembly = new HLStoneAssembly();
            StoneMeshCache meshes = new StoneMeshCache();
            assembly.Init(meshes);
            try
            {
                assembly.BuildEnemy(root.transform, 17, HLStonePreset.Boulder, null);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                int changed = 0;
                assembly.ApplyFracture(0.7f, 17);
                Color[] colors = new Color[assembly.parts.Count];
                for (int i = 0; i < colors.Length; i++)
                {
                    assembly.parts[i].renderer.GetPropertyBlock(block);
                    colors[i] = (Color)block.GetVector("_BaseColor");
                }

                assembly.ApplyFracture(0.2f, 17);
                for (int i = 0; i < colors.Length; i++)
                {
                    assembly.parts[i].renderer.GetPropertyBlock(block);
                    Color color = (Color)block.GetVector("_BaseColor");
                    if (color != colors[i])
                    {
                        changed++;
                        Assert.Less(color.grayscale, colors[i].grayscale);
                    }
                }
                Assert.AreEqual(2, changed);

                assembly.ApplyFracture(1f, 17);
                foreach (HLStoneAssembly.Part part in assembly.parts)
                {
                    part.renderer.GetPropertyBlock(block);
                    Vector4 current = (Color)block.GetVector("_BaseColor");
                    Assert.Less(((Vector4)part.baseColor - current).magnitude, 1e-6f);
                }
            }
            finally
            {
                assembly.Dispose();
                meshes.Clear();
                Object.DestroyImmediate(root);
            }
        }
    }
}
