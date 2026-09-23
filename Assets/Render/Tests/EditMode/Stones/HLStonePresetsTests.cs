using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class HLStonePresetsTests
    {
        [Test]
        public void PresetsAreValidDistinctParameterSets()
        {
            Assert.IsTrue(HLStoneMesh.IsValid(HLStonePresets.Boulder));
            Assert.IsTrue(HLStoneMesh.IsValid(HLStonePresets.Cairn));
            Assert.IsTrue(HLStoneMesh.IsValid(HLStonePresets.Monolith));
            Assert.AreEqual(0.58f, HLStonePresets.Boulder.size);
            Assert.AreEqual(0.65f, HLStonePresets.Cairn.elongation);
            Assert.AreEqual(2.8f, HLStonePresets.Monolith.elongation);
        }
    }
}
