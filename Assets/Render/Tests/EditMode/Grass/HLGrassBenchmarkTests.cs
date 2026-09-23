using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class HLGrassBenchmarkTests
{
    GameObject _go;
    HLGrassBenchmark _benchmark;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("HLGrassBenchmarkTest");
        _benchmark = _go.AddComponent<HLGrassBenchmark>();
        _benchmark.ResetCapture();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void RecordFrame_WarmupAndSamples_CompletesOnTheLastSample()
    {
        for (int i = 0; i < HLGrassBenchmark.WarmupFrames; i++)
        {
            Assert.IsFalse(_benchmark.RecordFrame(999));
        }
        for (int i = 0; i < HLGrassBenchmark.SampleFrames - 1; i++)
        {
            Assert.IsFalse(_benchmark.RecordFrame(10));
        }

        Assert.IsTrue(_benchmark.RecordFrame(40));

        Assert.AreEqual(300, _benchmark.collectedFrames);
        Assert.IsTrue(_benchmark.isComplete);
        Assert.AreEqual(10.1, _benchmark.averageFrameMilliseconds, 0.00001); // (299 * 10 + 40) / 300
        Assert.IsFalse(_benchmark.RecordFrame(100));
    }

    [Test]
    public void ResetCapture_AfterSamples_StartsOver()
    {
        for (int i = 0; i < HLGrassBenchmark.WarmupFrames + 5; i++)
        {
            _benchmark.RecordFrame(10);
        }

        _benchmark.ResetCapture();

        Assert.AreEqual(0, _benchmark.collectedFrames);
        Assert.IsFalse(_benchmark.isComplete);
    }

    [Test]
    public void RecordFrame_InvalidTime_IsIgnored()
    {
        bool isNaNComplete = true;
        bool isNegativeComplete = true;

        TestHelpers.WithLoggingDisabled(() => isNaNComplete = _benchmark.RecordFrame(double.NaN));
        TestHelpers.WithLoggingDisabled(() => isNegativeComplete = _benchmark.RecordFrame(-1));

        Assert.IsFalse(isNaNComplete);
        Assert.IsFalse(isNegativeComplete);
        Assert.AreEqual(0, _benchmark.collectedFrames);
    }
}
}
