using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundVolumeTests
{
    [Test]
    public void Create_Area_TilesItWithTexelsNoWiderThanAsked()
    {
        GroundVolume volume = GroundVolume.Create(new Rect(-3f, 2f, 10f, 4.1f), 0.5f);

        Assert.IsTrue(volume.isValid);
        Assert.AreEqual(20, volume.width);
        Assert.AreEqual(9, volume.height);
        Assert.LessOrEqual(volume.texelSize.x, 0.5f);
        Assert.LessOrEqual(volume.texelSize.y, 0.5f);
    }

    [Test]
    public void Create_HugeArea_CapsTheResolution()
    {
        GroundVolume volume = GroundVolume.Create(new Rect(0f, 0f, 1000f, 1f), 0.01f);

        Assert.AreEqual(GroundVolume.MaxResolution, volume.width);
        Assert.AreEqual(100, volume.height);
    }

    [Test]
    public void Create_InvalidInput_LogsAndIsInvalid()
    {
        TestHelpers.WithLoggingDisabled(() =>
        {
            Assert.IsFalse(GroundVolume.Create(new Rect(0f, 0f, 0f, 1f), 0.1f).isValid);
            Assert.IsFalse(GroundVolume.Create(new Rect(0f, 0f, 1f, 1f), 0f).isValid);
            Assert.IsFalse(GroundVolume.Create(new Rect(float.NaN, 0f, 1f, 1f), 0.1f).isValid);
        });
    }

    [Test]
    public void ToUV_Corners_AreZeroAndOneAndRoundTrip()
    {
        GroundVolume volume = GroundVolume.Create(new Rect(-3f, 2f, 10f, 4f), 0.25f);

        Assert.AreEqual(Vector2.zero, volume.ToUV(new Vector2(-3f, 2f)));
        Assert.AreEqual(Vector2.one, volume.ToUV(new Vector2(7f, 6f)));
        Vector2 point = new Vector2(1.3f, 4.4f);
        Assert.That(Vector2.Distance(point, volume.ToWorld(volume.ToUV(point))), Is.LessThan(1e-5f));
    }

    [Test]
    public void ShaderRect_MapsWorldToUVAsTheShadersDo()
    {
        GroundVolume volume = GroundVolume.Create(new Rect(-3f, 2f, 10f, 4f), 0.25f);
        Vector4 rect = volume.ShaderRect();
        Vector2 point = new Vector2(1.3f, 4.4f);

        Vector2 uv = new Vector2((point.x - rect.x) * rect.z, (point.y - rect.y) * rect.w);

        Assert.That(Vector2.Distance(volume.ToUV(point), uv), Is.LessThan(1e-6f));
    }
}

}
