using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class StonePresetsTests
    {
        [Test]
        public void PresetsAreValidDistinctParameterSets()
        {
            Assert.IsTrue(StoneMesh.IsValid(StonePresets.Boulder));
            Assert.IsTrue(StoneMesh.IsValid(StonePresets.Cairn));
            Assert.IsTrue(StoneMesh.IsValid(StonePresets.Monolith));
            Assert.AreEqual(0.58f, StonePresets.Boulder.size);
            Assert.AreEqual(0.65f, StonePresets.Cairn.elongation);
            Assert.AreEqual(2.8f, StonePresets.Monolith.elongation);
        }
    }
}
