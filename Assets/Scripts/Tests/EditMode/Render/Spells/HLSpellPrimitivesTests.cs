using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrimitivesTests
    {
        [TestCase(HLSpellEffectKind.Buff,3)] [TestCase(HLSpellEffectKind.Shield,6)] [TestCase(HLSpellEffectKind.Heal,7)] [TestCase(HLSpellEffectKind.Impact,16)] [TestCase(HLSpellEffectKind.Chain,32)]
        public void AssembliesArePrimitiveOnlyAndHaveNoColliders(HLSpellEffectKind kind,int count)
        {
            var go=new GameObject("HLPrimitiveTest");try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=kind;fx.Initialize();Assert.AreEqual(count,fx.parts.Length);Assert.IsEmpty(go.GetComponentsInChildren<Collider>());foreach(var filter in go.GetComponentsInChildren<MeshFilter>())Assert.Greater(filter.sharedMesh.vertexCount,0);}finally{Object.DestroyImmediate(go);}
        }
    }
}
