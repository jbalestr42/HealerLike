using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render
{

public class RenderMathTests
{
    [Test]
    public void IsFinite_FiniteVector_IsTrue()
    {
        Assert.IsTrue(RenderMath.IsFinite(new Vector3(1f, -2f, 0f)));
    }

    [Test]
    public void IsFinite_AnyComponentNaNOrInfinite_IsFalse()
    {
        Assert.IsFalse(RenderMath.IsFinite(new Vector3(float.NaN, 0f, 0f)));
        Assert.IsFalse(RenderMath.IsFinite(new Vector3(0f, float.PositiveInfinity, 0f)));
        Assert.IsFalse(RenderMath.IsFinite(new Vector3(0f, 0f, float.NegativeInfinity)));
    }

    [Test]
    public void FiniteOr_Finite_ReturnsTheValue()
    {
        Assert.AreEqual(2f, RenderMath.FiniteOr(2f, 5f));
    }

    [Test]
    public void FiniteOr_NaNOrInfinite_ReturnsTheFallback()
    {
        Assert.AreEqual(5f, RenderMath.FiniteOr(float.NaN, 5f));
        Assert.AreEqual(5f, RenderMath.FiniteOr(float.NegativeInfinity, 5f));
    }

    [Test]
    public void IsPositive_AboveZero_IsTrue()
    {
        Assert.IsTrue(RenderMath.IsPositive(0.001f));
    }

    [Test]
    public void IsPositive_ZeroNegativeNaNOrInfinite_IsFalse()
    {
        Assert.IsFalse(RenderMath.IsPositive(0f));
        Assert.IsFalse(RenderMath.IsPositive(-1f));
        Assert.IsFalse(RenderMath.IsPositive(float.NaN));
        Assert.IsFalse(RenderMath.IsPositive(float.PositiveInfinity));
    }

    [Test]
    public void CornerSign_EveryCorner_PicksOneAxisPerBit()
    {
        Assert.AreEqual(new Vector3(-1f, -1f, -1f), RenderMath.CornerSign(0));
        Assert.AreEqual(new Vector3(1f, -1f, -1f), RenderMath.CornerSign(1));
        Assert.AreEqual(new Vector3(-1f, 1f, -1f), RenderMath.CornerSign(2));
        Assert.AreEqual(new Vector3(1f, 1f, 1f), RenderMath.CornerSign(7));
    }

    [Test]
    public void Corner_EightCorners_SpanTheBox()
    {
        Bounds box = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(2f, 4f, 6f));
        Bounds spanned = new Bounds(RenderMath.Corner(box, 0), Vector3.zero);

        for (int corner = 1; corner < 8; corner++)
        {
            spanned.Encapsulate(RenderMath.Corner(box, corner));
        }

        Assert.AreEqual(box.min, spanned.min);
        Assert.AreEqual(box.max, spanned.max);
    }
}

}
