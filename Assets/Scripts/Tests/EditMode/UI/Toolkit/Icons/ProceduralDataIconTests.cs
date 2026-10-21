using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UI.Toolkit.Icons
{

public class ProceduralDataIconTests
{
    Texture2D _texture;

    static DataIconDescriptor CreateDescriptor(string key, string label, DataIconKind kind = DataIconKind.Spell)
    {
        return new DataIconDescriptor(key, label, kind);
    }

    [TearDown]
    public void TearDown()
    {
        if (_texture != null)
        {
            Object.DestroyImmediate(_texture);
        }
    }

    [Test]
    public void Render_SameDescriptor_IsDeterministic()
    {
        DataIconDescriptor descriptor = CreateDescriptor("spell:heal", "Heal");

        Color32[] first = ProceduralDataIcon.Render(descriptor, 32);
        Color32[] second = ProceduralDataIcon.Render(descriptor, 32);

        CollectionAssert.AreEqual(first, second);
    }

    [Test]
    public void Render_DistinctKeys_DrawsDistinctArtwork()
    {
        Color32[] heal = ProceduralDataIcon.Render(CreateDescriptor("heal", "Heal"), 32);
        Color32[] fire = ProceduralDataIcon.Render(CreateDescriptor("fire", "Fire"), 32);

        CollectionAssert.AreNotEqual(heal, fire);
    }

    [TestCase(DataIconKind.Spell)]
    [TestCase(DataIconKind.Creature)]
    [TestCase(DataIconKind.Character)]
    [TestCase(DataIconKind.Data)]
    public void Render_EveryKind_HasTransparentCornerAndOpaqueCenter(DataIconKind kind)
    {
        Color32[] pixels = ProceduralDataIcon.Render(CreateDescriptor("key", "Label", kind), 32);

        Assert.AreEqual(1024, pixels.Length); // 32 * 32
        Assert.AreEqual(0, pixels[0].a);
        Assert.AreEqual(255, pixels[16 * 32 + 16].a);
    }

    [TestCase(0)]
    [TestCase(2048)]
    public void Render_InvalidSize_LogsErrorAndReturnsNull(int size)
    {
        LogAssert.Expect(LogType.Error, new Regex("out of range"));

        Color32[] pixels = ProceduralDataIcon.Render(CreateDescriptor("key", "Label"), size);

        Assert.IsNull(pixels);
    }

    [Test]
    public void Create_Descriptor_ReturnsDefaultSizeTexture()
    {
        _texture = ProceduralDataIcon.Create(CreateDescriptor("key", "Label"));

        Assert.AreEqual(ProceduralDataIcon.DefaultSize, _texture.width);
        Assert.AreEqual(ProceduralDataIcon.DefaultSize, _texture.height);
    }
}
}
