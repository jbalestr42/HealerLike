using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{

public class HLGrassBoundsTests
{
    [Test]
    public void Calculate_MainAndShiftedGrids_ContainDeformedTips()
    {
        Bounds main = HLGrassBounds.Calculate(16, 16, 1f, Vector3.zero, 0.5f);
        Bounds shifted = HLGrassBounds.Calculate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f);

        Assert.AreEqual(new Vector3(0f, 0.505f, 0f), main.center);
        Assert.AreEqual(new Vector3(17.7f, 1.7f, 17.7f), main.size);
        Assert.AreEqual(new Vector3(4f, 3.005f, -5f), shifted.center);
        foreach (int x in new[] { -1, 1 })
        {
            foreach (int z in new[] { -1, 1 })
            {
                Assert.IsTrue(shifted.Contains(shifted.center + new Vector3(x * (3f + 0.24f + 0.065f), 0.648f, z * (7f + 0.24f + 0.065f))));
                Assert.IsTrue(shifted.Contains(shifted.center + new Vector3(x * (3f + 0.065f), 0.54f, z * (7f + 0.065f))));
            }
        }
    }

    [Test]
    public void Calculate_EnvelopeUnderTheBlade_LogsAndReturnsEmptyBounds()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"^\[HLGrassBounds\] Rejected a 1 x 1 footprint"));

        Bounds bounds = HLGrassBounds.Calculate(1, 1, 1f, Vector3.zero, 0f, 0.1f);

        Assert.AreEqual(new Bounds(), bounds);
    }

    [Test]
    public void Calculate_NegativeWidth_LogsAndReturnsEmptyBounds()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"^\[HLGrassBounds\] Rejected a -1 x 1 footprint"));

        Bounds bounds = HLGrassBounds.Calculate(-1, 1, 1f, Vector3.zero, 0f);

        Assert.AreEqual(new Bounds(), bounds);
    }
}
}
