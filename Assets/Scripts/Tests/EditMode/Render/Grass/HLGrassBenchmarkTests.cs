using System;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassBenchmarkTests
    {
        [Test] public void CapturesExactly300FramesAfter120WarmupAndCanRestart()
        {
            var go = new GameObject("HLGrassBenchmarkTest");
            try
            {
                var benchmark = go.AddComponent<HLGrassBenchmark>(); benchmark.ResetCapture();
                for (int i = 0; i < 120; i++) Assert.IsFalse(benchmark.RecordFrame(999));
                for (int i = 0; i < 299; i++) Assert.IsFalse(benchmark.RecordFrame(10));
                Assert.IsTrue(benchmark.RecordFrame(40));
                Assert.AreEqual(300, benchmark.CollectedFrames); Assert.IsTrue(benchmark.Complete);
                Assert.AreEqual(10.1, benchmark.AverageFrameMilliseconds, 0.00001);
                Assert.IsFalse(benchmark.RecordFrame(100));
                benchmark.ResetCapture(); Assert.AreEqual(0, benchmark.CollectedFrames); Assert.IsFalse(benchmark.Complete);
                Assert.Throws<ArgumentOutOfRangeException>(() => benchmark.RecordFrame(double.NaN));
                Assert.Throws<ArgumentOutOfRangeException>(() => benchmark.RecordFrame(-1));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
