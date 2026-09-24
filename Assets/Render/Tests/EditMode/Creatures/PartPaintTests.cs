using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class PartPaintTests
{
    GameObject _partGo;
    MeshRenderer _renderer;
    PartPaint _paint;

    [SetUp]
    public void SetUp()
    {
        _partGo = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        _renderer = _partGo.GetComponent<MeshRenderer>();
        _paint = new PartPaint();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_partGo);
    }

    [Test]
    public void Paint_Tip_DrawsAWiderOutline()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        _paint.Paint(_renderer, true, Color.red, 0f);

        _renderer.GetPropertyBlock(block);
        Assert.AreEqual(PartPaint.TipOutlineWidth, block.GetFloat("_HLOutlineWidthMultiplier"));
    }

    [Test]
    public void Paint_Body_KeepsTheMaterialOutline()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        _paint.Paint(_renderer, true, Color.red, 0f);
        _paint.Paint(_renderer, false, Color.green, 0f);

        _renderer.GetPropertyBlock(block);
        Assert.IsFalse(block.HasFloat("_HLOutlineWidthMultiplier"));
    }

    [Test]
    public void Paint_Glow_BrightensTheColourAndKeepsAlpha()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        _paint.Paint(_renderer, false, new Color(0.2f, 0.4f, 0.1f, 0.7f), 2f);

        _renderer.GetPropertyBlock(block);
        Assert.AreEqual(1.2f, block.GetColor("_BaseColor").g, 0.0001f); // 0.4 lit by glow 2
        Assert.AreEqual(0.7f, block.GetColor("_BaseColor").a, 0.0001f);
    }

    [Test]
    public void Paint_TwoFacedStone_DrawsOchreOnTheSecondSubmesh()
    {
        Color ochre = new Color(0.72f, 0.62f, 0.43f, 1f);
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        _paint.Paint(_renderer, false, Color.grey, ochre, 0f);

        _renderer.GetPropertyBlock(block, 1);
        Assert.AreEqual(ochre, block.GetColor("_BaseColor"));
        _renderer.GetPropertyBlock(block, 0);
        Assert.AreEqual(Color.grey, block.GetColor("_BaseColor"));
    }
}

}
