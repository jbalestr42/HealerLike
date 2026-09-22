using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneAssemblyTests
    {
        [TestCase(HLStonePreset.Boulder,3,.8f)] [TestCase(HLStonePreset.Cairn,3,1.05f)] [TestCase(HLStonePreset.Monolith,1,1.45f)]
        public void LayoutFitsFootprintAndDisposes(HLStonePreset preset,int count,float height)
        {
            var go=new GameObject("HLTest"); var assembly=new HLStoneAssembly();
            try {
                assembly.BuildEnemy(go.transform,17,preset,null); Assert.AreEqual(count,assembly.Parts.Count);
                Assert.That(assembly.LocalBounds.size.x,Is.LessThanOrEqualTo(.90001f)); Assert.That(assembly.LocalBounds.size.z,Is.LessThanOrEqualTo(.90001f));
                Assert.That(assembly.LocalBounds.min.y,Is.EqualTo(0).Within(1e-5)); Assert.That(assembly.LocalBounds.size.y,Is.EqualTo(height).Within(1e-5));
                if(preset==HLStonePreset.Cairn)
                    for(int i=1;i<count;i++) Assert.That(assembly.Parts[i].Renderer.bounds.min.y,Is.LessThan(assembly.Parts[i-1].Renderer.bounds.max.y));
            } finally { assembly.Dispose(); Object.DestroyImmediate(go); }
        }
    }
}
