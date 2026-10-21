using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render
{

public class GraphicsDeviceReportTests
{
    static GraphicsDeviceReport.Facts Mali()
    {
        return new GraphicsDeviceReport.Facts
        {
            appVersion = "0.1.12",
            isDevelopmentBuild = true,
            deviceModel = "Google Pixel 6a",
            operatingSystem = "Android OS 16 / API-36",
            deviceType = GraphicsDeviceType.OpenGLES3,
            deviceName = "Mali-G78",
            deviceVersion = "OpenGL ES 3.2 v1.r46p0-01eac0.abc",
            deviceVendor = "ARM",
            shaderLevel = 45,
            supportsComputeShaders = true,
            maxComputeBufferInputsVertex = 0,
            maxComputeBufferInputsFragment = 35,
            supportsArgbHalf = true,
            supportsRHalf = false,
            groundUnsupportedReason = "vertex storage buffers unavailable"
        };
    }

    [Test]
    public void Compose_PrintsEveryAskedField()
    {
        string text = GraphicsDeviceReport.Compose(Mali());

        StringAssert.Contains("graphicsDeviceType: OpenGLES3", text);
        StringAssert.Contains("graphicsDeviceName: Mali-G78", text);
        StringAssert.Contains("graphicsDeviceVersion: OpenGL ES 3.2 v1.r46p0-01eac0.abc", text);
        StringAssert.Contains("graphicsDeviceVendor: ARM", text);
        StringAssert.Contains("graphicsShaderLevel: 45", text);
        StringAssert.Contains("supportsComputeShaders: True", text);
        StringAssert.Contains("maxComputeBufferInputsVertex: 0", text);
        StringAssert.Contains("maxComputeBufferInputsFragment: 35", text);
        StringAssert.Contains("renderTexture ARGBHalf: True", text);
        StringAssert.Contains("renderTexture RHalf: False", text);
        StringAssert.Contains("device model: Google Pixel 6a", text);
        StringAssert.Contains("app version: 0.1.12 (development build)", text);
    }

    [Test]
    public void Compose_EveryLineCarriesTheTag()
    {
        string[] lines = GraphicsDeviceReport.Compose(Mali()).Split('\n');

        Assert.That(lines.Length, Is.GreaterThan(10));
        foreach (string line in lines)
        {
            StringAssert.StartsWith(GraphicsDeviceReport.Tag + " ", line);
        }
    }

    [Test]
    public void Compose_GroundOff_NamesTheReason()
    {
        StringAssert.Contains("ground simulation: off, vertex storage buffers unavailable",
            GraphicsDeviceReport.Compose(Mali()));
    }

    [Test]
    public void Compose_GroundSupportedAndRelease_SaySo()
    {
        GraphicsDeviceReport.Facts facts = Mali();
        facts.groundUnsupportedReason = null;
        facts.isDevelopmentBuild = false;

        string text = GraphicsDeviceReport.Compose(facts);

        StringAssert.Contains("ground simulation: supported", text);
        StringAssert.Contains("app version: 0.1.12 (release build)", text);
    }

    [Test]
    public void Capture_ReadsThisDevicesSystemInfo()
    {
        GraphicsDeviceReport.Facts facts = GraphicsDeviceReport.Capture();

        Assert.AreEqual(SystemInfo.graphicsDeviceType, facts.deviceType);
        Assert.AreEqual(SystemInfo.graphicsDeviceVersion, facts.deviceVersion);
        Assert.AreEqual(SystemInfo.maxComputeBufferInputsVertex, facts.maxComputeBufferInputsVertex);
        Assert.AreEqual(SystemInfo.maxComputeBufferInputsFragment, facts.maxComputeBufferInputsFragment);
        Assert.AreEqual(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf), facts.supportsRHalf);
        Assert.AreEqual(Grass.GroundSimulation.UnsupportedReason(), facts.groundUnsupportedReason);
    }
}

}
