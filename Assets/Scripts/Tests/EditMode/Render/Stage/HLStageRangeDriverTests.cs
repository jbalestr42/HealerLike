using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Stage
{
    public class HLStageRangeDriverTests
    {
        GameObject root, first, second;
        HLRangePreview a, b;
        HLStageRangeDriver driver;
        [SetUp] public void Setup()
        {
            root = new GameObject("HLDriver"); first = new GameObject("HLFirst"); second = new GameObject("HLSecond");
            a = first.AddComponent<HLRangePreview>(); b = second.AddComponent<HLRangePreview>();
            driver = root.AddComponent<HLStageRangeDriver>();
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        [Test] public void FeaturedSkipsInactivePreviews()
        {
            first.SetActive(false);
            Assert.AreEqual(1, HLStageRangeDriver.SelectFeatured(new List<HLRangePreview> { a, b }));
            second.SetActive(false);
            Assert.AreEqual(-1, HLStageRangeDriver.SelectFeatured(new List<HLRangePreview> { a, b }));
            Assert.AreEqual(-1, HLStageRangeDriver.SelectFeatured(null));
        }
        [Test] public void ExplicitModesDisablePointerAndPointerModeRestoresIt()
        {
            driver.Mode = HLStageRangeDriver.PreviewMode.Featured;
            driver.Apply(new List<HLRangePreview> { a, b });
            Assert.AreSame(a, driver.Featured);
            Assert.IsFalse(a.ObservePointer); Assert.IsFalse(b.ObservePointer);
            driver.Mode = HLStageRangeDriver.PreviewMode.Hidden;
            driver.Apply(new List<HLRangePreview> { a, b });
            Assert.IsNull(driver.Featured);
            driver.Mode = HLStageRangeDriver.PreviewMode.Pointer;
            driver.Apply(new List<HLRangePreview> { a, b });
            Assert.IsTrue(a.ObservePointer); Assert.IsTrue(b.ObservePointer);
        }
    }
}
