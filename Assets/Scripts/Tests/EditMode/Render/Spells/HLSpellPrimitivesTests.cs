using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrimitivesTests
    {
        [Test] public void LastSinkReleasesMeshesAndFallbackButOtherSinkKeepsThemAlive()
        {
            var a = new GameObject("HLFirstSink"); var b = new GameObject("HLSecondSink");
            try
            {
                var first = a.AddComponent<HLSpellVisualSink>(); b.AddComponent<HLSpellVisualSink>();
                first.ShowImpact(null,b,HLResourceKind.Health,-1,false);
                var torus = HLSpellPrimitives.Torus; var cone = HLSpellPrimitives.Cone;
                var fallback = a.GetComponentInChildren<Renderer>().sharedMaterial;
                Object.DestroyImmediate(a);
                Assert.IsTrue(torus); Assert.IsTrue(cone); Assert.IsTrue(fallback);
                Object.DestroyImmediate(b);
                Assert.IsFalse(torus); Assert.IsFalse(cone); Assert.IsFalse(fallback);
                HLSpellPrimitives.Release(); // Idempotent.
                Assert.IsTrue(HLSpellPrimitives.Torus);
                Assert.AreNotSame(torus,HLSpellPrimitives.Torus);
            }
            finally { if(a) Object.DestroyImmediate(a); if(b) Object.DestroyImmediate(b); HLSpellPrimitives.Release(); }
        }
        [Test] public void StandaloneEffectKeepsCacheAliveAfterSinkDestruction()
        {
            var host = new GameObject("HLSink"); var effectHost = new GameObject("HLEffect");
            try
            {
                host.AddComponent<HLSpellVisualSink>();
                effectHost.AddComponent<HLSpellEffect>().Initialize();
                var mesh = HLSpellPrimitives.Torus;
                Object.DestroyImmediate(host); Assert.IsTrue(mesh);
                Object.DestroyImmediate(effectHost); Assert.IsFalse(mesh);
            }
            finally { if(host) Object.DestroyImmediate(host); if(effectHost) Object.DestroyImmediate(effectHost); }
        }
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
