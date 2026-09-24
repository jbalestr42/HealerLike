using NUnit.Framework;

namespace HealerLike.Render.Stones
{

public class StonePresetsTests
{
    [Test]
    public void Shape_ShippedPresets_AreValidDistinctParameterSets()
    {
        StoneSettings[] presets = { StonePresets.Boulder, StonePresets.Cairn, StonePresets.Monolith };

        foreach (StoneSettings preset in presets)
        {
            Assert.IsTrue(StoneMesh.IsValid(preset));
        }
        Assert.AreNotEqual(StonePresets.Boulder, StonePresets.Cairn);
        Assert.AreNotEqual(StonePresets.Cairn, StonePresets.Monolith);
    }

    [Test]
    public void Shape_GivenParameters_FillsEveryField()
    {
        StoneSettings settings = StonePresets.Shape(0.4f, 1.2f, 0.7f, 0.1f, 2);

        Assert.AreEqual(0.4f, settings.size);
        Assert.AreEqual(1.2f, settings.elongation);
        Assert.AreEqual(0.7f, settings.depthRatio);
        Assert.AreEqual(0.1f, settings.roughness);
        Assert.AreEqual(2, settings.subdivisions);
    }
}

}
