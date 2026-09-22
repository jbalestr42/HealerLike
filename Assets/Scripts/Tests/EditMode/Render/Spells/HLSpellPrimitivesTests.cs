using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrimitivesTests
    {
        [TestCase(HLOperation.Resource,AttributeType.HealthMax,HLSign.Positive,HLTempo.Immediate,HLSpellEffectKind.Heal)]
        [TestCase(HLOperation.Resource,AttributeType.HealthMax,HLSign.Negative,HLTempo.HandlerTick,HLSpellEffectKind.Drip)]
        [TestCase(HLOperation.Attribute,AttributeType.FlatArmor,HLSign.Positive,HLTempo.Continuous,HLSpellEffectKind.Shield)]
        [TestCase(HLOperation.Attribute,AttributeType.AttackRate,HLSign.Conditional,HLTempo.Continuous,HLSpellEffectKind.Buff)]
        public void SemanticAxesChooseShape(HLOperation operation,AttributeType attribute,HLSign sign,HLTempo tempo,HLSpellEffectKind expected)
            => Assert.AreEqual(expected,HLSpellPrimitives.Kind(new HLSpellSignature{operation=operation,attribute=attribute,hasAttribute=true,sign=sign,tempo=tempo}));
        [TestCase(HLSpellEffectKind.Drip,5)] [TestCase(HLSpellEffectKind.Area,1)] [TestCase(HLSpellEffectKind.Buff,3)] [TestCase(HLSpellEffectKind.Shield,6)] [TestCase(HLSpellEffectKind.Heal,7)] [TestCase(HLSpellEffectKind.Impact,16)] [TestCase(HLSpellEffectKind.Chain,32)]
        public void AssembliesArePrimitiveOnlyAndHaveNoColliders(HLSpellEffectKind kind,int count)
        {
            var go=new GameObject("HLPrimitiveTest");try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=kind;fx.Initialize();Assert.AreEqual(count,fx.parts.Length);Assert.IsEmpty(go.GetComponentsInChildren<Collider>());foreach(var filter in go.GetComponentsInChildren<MeshFilter>())Assert.Greater(filter.sharedMesh.vertexCount,0);}finally{Object.DestroyImmediate(go);}
        }
    }
}
