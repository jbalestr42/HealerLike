using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class PartPaintTests
{
    GameObject _partGo;
    MeshRenderer _renderer;
    Material _material;
    PartPaint _paint;

    [SetUp]
    public void SetUp()
    {
        _partGo = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
        _renderer = _partGo.GetComponent<MeshRenderer>();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _paint = new PartPaint();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_partGo);
        Object.DestroyImmediate(_material);
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
        // A two-faced stone draws its two submeshes with one material each, as the rig sets it up
        _renderer.sharedMaterials = new Material[] { _material, _material };
        _paint.Paint(_renderer, false, Color.grey, ochre, 0f);
        _renderer.GetPropertyBlock(block, 1);
        Assert.Less(((Vector4)ochre - (Vector4)block.GetColor("_BaseColor")).magnitude, 0.0001f);
        _renderer.GetPropertyBlock(block, 0);
        Assert.AreEqual(Color.grey, block.GetColor("_BaseColor"));
    }
}
}
