using System;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassBoundsTests
    {
        [Test] public void MainAndShiftedBoundsContainDeformedTips()
        {
            var main = HLGrassBounds.Calculate(16, 16, 1, Vector3.zero, 0.5f);
            Assert.AreEqual(new Vector3(0, 0.505f, 0), main.center);
            Assert.AreEqual(new Vector3(17.7f, 1.7f, 17.7f), main.size);
            var b = HLGrassBounds.Calculate(3, 7, 2, new Vector3(4, 9, -5), 3);
            Assert.AreEqual(new Vector3(4, 3.005f, -5), b.center);
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
            {
                Assert.IsTrue(b.Contains(b.center + new Vector3(x * (3 + 0.24f + 0.065f), 0.648f, z * (7 + 0.24f + 0.065f))));
                Assert.IsTrue(b.Contains(b.center + new Vector3(x * (3 + 0.065f), 0.54f, z * (7 + 0.065f))));
            }
        }
        [Test] public void RejectsInvalidEnvelopeAndFootprint()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassBounds.Calculate(1, 1, 1, Vector3.zero, 0, 0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassBounds.Calculate(-1, 1, 1, Vector3.zero, 0));
        }
    }
}
