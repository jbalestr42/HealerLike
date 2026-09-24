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
}

}
