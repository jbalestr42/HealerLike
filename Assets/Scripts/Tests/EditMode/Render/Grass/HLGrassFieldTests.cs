using System;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public class HLGrassFieldTests
    {
        [Test] public void DefaultsBudgetClampNullSnapshotAndIdempotentRelease()
        {
            var go = new GameObject("HLGrassFieldTest");
            try
            {
                var field = go.AddComponent<HLGrassField>();
                Assert.AreEqual(65536, field.BladeBudget);
                field.BladeBudget = int.MaxValue; Assert.AreEqual(98304, field.BladeBudget);
                field.BladeBudget = -1; Assert.AreEqual(0, field.BladeBudget);
                field.SetZoneSnapshot(null, 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => field.SetZoneSnapshot(null, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => field.SetZoneCount(65));
                Assert.Throws<ArgumentException>(() => field.Initialize(null, null, null, null, 64));
                TestHelpers.InvokePrivate(field, "LateUpdate");
                field.Release(); field.Release(); Assert.IsFalse(field.IsReady); Assert.AreEqual(0, field.BladeCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
