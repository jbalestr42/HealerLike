using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneAssemblyProfileTests
    {
        [Test]
        public void ProfileKeepsDefaultSingleThreshold()
        {
            StoneAssemblyProfile profile = ScriptableObject.CreateInstance<StoneAssemblyProfile>();
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
