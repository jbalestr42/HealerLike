using NUnit.Framework;
namespace HealerLike.Render.Stones
{
    public class HLStonePresetsTests
    {
        [Test] public void PresetsAreValidDistinctParameterSets()
        {
            HLStoneMesh.Validate(HLStonePresets.Boulder); HLStoneMesh.Validate(HLStonePresets.Cairn); HLStoneMesh.Validate(HLStonePresets.Monolith);
            Assert.AreEqual(.58f,HLStonePresets.Boulder.Size); Assert.AreEqual(.65f,HLStonePresets.Cairn.Elongation); Assert.AreEqual(2.8f,HLStonePresets.Monolith.Elongation);
        }
    }
}
