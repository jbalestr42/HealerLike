using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundWindTests
{
    [Test]
    public void Shader_Strength_IsClampedIntoRadians()
    {
        Assert.AreEqual(GroundWind.MaxStrength, GroundWind.Shader(3f, 1f).z, 1e-6f);
        Assert.AreEqual(0f, GroundWind.Shader(-1f, 1f).z);
        Assert.AreEqual(0f, GroundWind.Shader(float.NaN, 1f).z);
        Assert.AreEqual(2.5f, GroundWind.Shader(1f, 2.5f).w);
    }

    [Test]
    public void Lean_NoWind_IsStill()
    {
        Assert.AreEqual(Vector2.zero, GroundWind.Lean(new Vector2(3f, 4f), GroundWind.Shader(0f, 7f)));
    }

    [Test]
    public void Lean_Wind_BlowsMostlyDownwindAndMovesWithTime()
    {
        Vector4 early = GroundWind.Shader(1f, 0f);
        Vector4 late = GroundWind.Shader(1f, 0.9f);
        float downwind = 0f;
        float change = 0f;
        for (int i = 0; i < 50; i++)
        {
            Vector2 point = new Vector2(i * 0.37f, i * -0.21f);
            Vector2 lean = GroundWind.Lean(point, early);
            downwind += Vector2.Dot(lean, GroundWind.Direction);
            change += Vector2.Distance(lean, GroundWind.Lean(point, late));
            Assert.LessOrEqual(lean.magnitude, GroundWind.MaxStrength * 1.05f);
        }

        Assert.Greater(downwind / 50f, 0.3f * GroundWind.MaxStrength);
        Assert.Greater(change / 50f, 0.004f);
    }

}

}
