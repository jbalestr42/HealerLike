using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellPrimitivesTests
    {
        static void DestroyHost(GameObject go)
        {
            if (!go) return;
            foreach(var effect in go.GetComponentsInChildren<HLSpellEffect>(true)) TestHelpers.InvokePrivate(effect,"OnDestroy");
            foreach(var sink in go.GetComponentsInChildren<HLSpellVisualSink>(true)) TestHelpers.InvokePrivate(sink,"OnDestroy");
            Object.DestroyImmediate(go);
        }
        [Test] public void BeautyMeshesArePlanarSpikyAndFlatShaded()
        {
            var star=HLSpellPrimitives.Star;
            foreach(var vertex in star.vertices) Assert.AreEqual(0,vertex.z);
            Assert.AreEqual(34,star.vertexCount);
            Assert.Greater(star.bounds.size.x,1.9f);
            foreach(var normal in star.normals) Assert.Greater(normal.sqrMagnitude,.99f);
            var boulder=HLSpellPrimitives.Boulder;
            Assert.AreEqual(24,boulder.vertexCount);
            var normals=boulder.normals;
            for(int i=0;i<normals.Length;i+=3) { Assert.AreEqual(normals[i],normals[i+1]);Assert.AreEqual(normals[i],normals[i+2]); }
            HLSpellPrimitives.Release();
        }
        [Test] public void HealingHasOneThinGroundedStalkPerSphere()
        {
            var go=new GameObject("HLHeal");try {
                var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Heal;fx.Initialize();
                Assert.AreEqual(fx.parts.Length,fx.stalks.Length);
                for(int i=0;i<fx.parts.Length;i++) {
                    Assert.Less(fx.stalks[i].localScale.x,.015f);
                    Assert.AreEqual(fx.parts[i].localPosition.y,fx.stalks[i].localPosition.y+fx.stalks[i].localScale.y,.00001f);
                }
            } finally {DestroyHost(go);}
        }
        [Test] public void ReleaseDoesNotDestroyPersistentMesh()
        {
            HLSpellPrimitives.Release();
            var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Render/Spells/Data/HLTorus.asset");
            Assert.IsNotNull(mesh);
            // Model an external authoring tool persisting the public cached mesh.
            typeof(HLSpellPrimitives).GetField("_torus",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,mesh);
            HLSpellPrimitives.Release();
            Assert.IsTrue(mesh);
            Assert.IsTrue(UnityEditor.EditorUtility.IsPersistent(mesh));
            Assert.AreNotSame(mesh,HLSpellPrimitives.Torus);
            HLSpellPrimitives.Release();
        }
        [Test] public void LastSinkReleasesMeshesAndFallbackButOtherSinkKeepsThemAlive()
        {
            var a = new GameObject("HLFirstSink"); var b = new GameObject("HLSecondSink");
            try
            {
                var first = a.AddComponent<HLSpellVisualSink>(); var second = b.AddComponent<HLSpellVisualSink>();
                TestHelpers.InvokePrivate(first,"OnEnable"); TestHelpers.InvokePrivate(second,"OnEnable");
                first.ShowImpact(null,b,HLResourceKind.Health,-1,false);
                var torus = HLSpellPrimitives.Torus; var cone = HLSpellPrimitives.Cone;
                var fallback = a.GetComponentInChildren<Renderer>().sharedMaterial;
                DestroyHost(a);
                Assert.IsTrue(torus); Assert.IsTrue(cone); Assert.IsTrue(fallback);
                DestroyHost(b);
                Assert.IsFalse(torus); Assert.IsFalse(cone); Assert.IsFalse(fallback);
                HLSpellPrimitives.Release(); // Idempotent.
                Assert.IsTrue(HLSpellPrimitives.Torus);
                Assert.AreNotSame(torus,HLSpellPrimitives.Torus);
            }
            finally { if(a) DestroyHost(a); if(b) DestroyHost(b); HLSpellPrimitives.Release(); }
        }
        [Test] public void StandaloneEffectKeepsCacheAliveAfterSinkDestruction()
        {
            var host = new GameObject("HLSink"); var effectHost = new GameObject("HLEffect");
            try
            {
                TestHelpers.InvokePrivate(host.AddComponent<HLSpellVisualSink>(),"OnEnable");
                effectHost.AddComponent<HLSpellEffect>().Initialize();
                var mesh = HLSpellPrimitives.Torus;
                DestroyHost(host); Assert.IsTrue(mesh);
                DestroyHost(effectHost); Assert.IsFalse(mesh);
            }
            finally { if(host) DestroyHost(host); if(effectHost) DestroyHost(effectHost); }
        }
        [TestCase(HLOperation.Resource,AttributeType.HealthMax,HLSign.Positive,HLTempo.Immediate,HLSpellEffectKind.Heal)]
        [TestCase(HLOperation.Resource,AttributeType.HealthMax,HLSign.Negative,HLTempo.HandlerTick,HLSpellEffectKind.Drip)]
        [TestCase(HLOperation.Attribute,AttributeType.FlatArmor,HLSign.Positive,HLTempo.Continuous,HLSpellEffectKind.Shield)]
        [TestCase(HLOperation.Attribute,AttributeType.AttackRate,HLSign.Conditional,HLTempo.Continuous,HLSpellEffectKind.Buff)]
        public void SemanticAxesChooseShape(HLOperation operation,AttributeType attribute,HLSign sign,HLTempo tempo,HLSpellEffectKind expected)
            => Assert.AreEqual(expected,HLSpellPrimitives.Kind(new HLSpellSignature{operation=operation,attribute=attribute,hasAttribute=true,sign=sign,tempo=tempo}));
        [TestCase(HLSpellEffectKind.Litter,5)] [TestCase(HLSpellEffectKind.Drip,5)] [TestCase(HLSpellEffectKind.Area,1)] [TestCase(HLSpellEffectKind.Buff,3)] [TestCase(HLSpellEffectKind.Shield,6)] [TestCase(HLSpellEffectKind.Heal,7)] [TestCase(HLSpellEffectKind.Impact,5)] [TestCase(HLSpellEffectKind.Chain,32)]
        public void AssembliesArePrimitiveOnlyAndHaveNoColliders(HLSpellEffectKind kind,int count)
        {
            var go=new GameObject("HLPrimitiveTest");try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=kind;fx.Initialize();Assert.AreEqual(count,fx.parts.Length);Assert.IsEmpty(go.GetComponentsInChildren<Collider>());foreach(var filter in go.GetComponentsInChildren<MeshFilter>())Assert.Greater(filter.sharedMesh.vertexCount,0);}finally{DestroyHost(go);}
        }
    }
}
