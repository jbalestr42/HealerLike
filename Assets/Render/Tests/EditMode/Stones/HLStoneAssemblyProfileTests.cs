using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneAssemblyProfileTests
    {
        [Test]
        public void ProfileKeepsDefaultSingleThreshold()
        {
            HLStoneAssemblyProfile profile = ScriptableObject.CreateInstance<HLStoneAssemblyProfile>();
            try
            {
                Assert.AreEqual(0.5f, profile.shedHealthFraction);
                Assert.AreEqual(2, profile.detachablePartIndex);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
