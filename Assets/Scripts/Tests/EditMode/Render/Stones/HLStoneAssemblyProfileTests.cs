using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneAssemblyProfileTests
    {
        [Test] public void ProfileKeepsDefaultSingleThreshold()
        {
            var p=ScriptableObject.CreateInstance<HLStoneAssemblyProfile>();
            try { Assert.AreEqual(.5f,p.ShedHealthFraction); Assert.AreEqual(2,p.DetachablePartIndex); }
            finally { Object.DestroyImmediate(p); }
        }
    }
}
