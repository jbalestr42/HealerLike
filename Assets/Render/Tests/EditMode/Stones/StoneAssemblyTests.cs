using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneAssemblyTests
{
    GameObject _root;
    StoneAssembly _assembly;
    StoneMeshCache _meshes;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Assembly");
        _assembly = new StoneAssembly();
        _meshes = new StoneMeshCache();
        _assembly.Init(_meshes);
    }

    [TearDown]
    public void TearDown()
    {
        _assembly.Dispose();
        _meshes.Clear();
        Object.DestroyImmediate(_root);
    }

    Color PartColor(int index, MaterialPropertyBlock block)
    {
        _assembly.parts[index].renderer.GetPropertyBlock(block);
        return (Color)block.GetVector("_BaseColor");
    }

    [TestCase(StonePreset.Boulder, 3, 0.8f)]
    [TestCase(StonePreset.Cairn, 3, 1.05f)]
    [TestCase(StonePreset.Monolith, 1, 1.45f)]
    public void BuildEnemy_Preset_FitsTheFootprintAndHeight(StonePreset preset, int count, float height)
    {
        _assembly.BuildEnemy(_root.transform, 17, preset, null);

        Assert.AreEqual(count, _assembly.parts.Count);
        Assert.That(_assembly.localBounds.size.x, Is.LessThanOrEqualTo(0.90001f));
        Assert.That(_assembly.localBounds.size.z, Is.LessThanOrEqualTo(0.90001f));
        Assert.That(_assembly.localBounds.min.y, Is.EqualTo(0).Within(0.00001));
        Assert.That(_assembly.localBounds.size.y, Is.EqualTo(height).Within(0.00001));
        if (preset == StonePreset.Cairn)
        {
            for (int i = 1; i < count; i++)
            {
                float below = _assembly.parts[i - 1].renderer.bounds.max.y;
                Assert.That(_assembly.parts[i].renderer.bounds.min.y, Is.LessThan(below));
            }
        }
    }

    [Test]
    public void ApplyFracture_StatusTint_ComposesWithFractureAndWhiteRestoresHealthColour()
    {
        _assembly.BuildEnemy(_root.transform, 17, StonePreset.Boulder, null);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _assembly.ApplyFracture(0.3f, 17);
        Color healthColor = PartColor(0, block);
        Color tint = new Color(0.5f, 0.12f, 0.55f, 1f);

        _assembly.ApplyFracture(0.3f, 17, tint);
        Vector4 tinted = PartColor(0, block);
        Assert.Less(((Vector4)Color.Lerp(healthColor, tint, 0.42f) - tinted).magnitude, 0.000001f);

        _assembly.ApplyFracture(0.3f, 17, Color.white);
        Vector4 restored = PartColor(0, block);
        Assert.Less(((Vector4)healthColor - restored).magnitude, 0.000001f);
    }

    [Test]
    public void ApplyFracture_FallingHealth_DarkensASeededSubsetAndFullHealthRestoresIt()
    {
        _assembly.BuildEnemy(_root.transform, 17, StonePreset.Boulder, null);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _assembly.ApplyFracture(0.7f, 17);
        Color[] colors = new Color[_assembly.parts.Count];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = PartColor(i, block);
        }

        _assembly.ApplyFracture(0.2f, 17);

        int changed = 0;
        for (int i = 0; i < colors.Length; i++)
        {
            Color color = PartColor(i, block);
            if (color != colors[i])
            {
                changed++;
                Assert.Less(color.grayscale, colors[i].grayscale);
            }
        }
        Assert.AreEqual(2, changed);

        _assembly.ApplyFracture(1f, 17);
        foreach (StoneAssembly.Part part in _assembly.parts)
        {
            part.renderer.GetPropertyBlock(block);
            Vector4 current = (Color)block.GetVector("_BaseColor");
            Assert.Less(((Vector4)part.baseColor - current).magnitude, 0.000001f);
        }
    }
}

}
