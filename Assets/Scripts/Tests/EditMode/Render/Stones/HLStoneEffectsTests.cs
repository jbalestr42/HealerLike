using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneEffectsTests
    {
        [TestCase(false)] [TestCase(true)]
        public void DisableClearsCopiesSlotsAndRejectsEveryEmission(bool deactivateObject)
        {
            var go=new GameObject("HLDisableEffects"); var fx=go.AddComponent<HLStoneEffects>();
            var source=new GameObject("HLSourceVisual"); var visual=source.AddComponent<HLStoneEnemyVisual>();
            var mesh=HLStoneMesh.CreateMesh(1,HLStonePresets.Boulder);
            var other=new GameObject("HLOtherEffects"); var second=other.AddComponent<HLStoneEffects>();
            int baseline=HLStoneEffects.GlobalLiveCount;
            try
            {
                visual.Initialize(null,1,fx);
                second.EmitThrownContact(Vector3.zero,1); int otherCount=second.LiveCount;
                fx.EmitDetachedPart(mesh,null,Matrix4x4.identity,Vector3.zero,0,1);
                var copy=go.GetComponentInChildren<MeshFilter>().sharedMesh;
                fx.EmitThrownContact(Vector3.zero,1);
                if(deactivateObject) go.SetActive(false); else fx.enabled=false;
                TestHelpers.InvokePrivate(fx,"OnDisable");
                Assert.AreEqual(0,fx.LiveCount); Assert.IsTrue(copy==null);
                Assert.AreEqual(baseline+otherCount,HLStoneEffects.GlobalLiveCount);
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
                { Assert.IsFalse(filter.gameObject.activeSelf); Assert.IsNull(filter.sharedMesh); }
                fx.EmitHit(default,false,1); fx.EmitThrownContact(Vector3.zero,1);
                fx.EmitDetachedPart(mesh,null,Matrix4x4.identity,Vector3.zero,0,1); fx.CollapseOnce(visual,1);
                Assert.AreEqual(0,fx.LiveCount); Assert.IsTrue(visual.Parts[0].Transform.gameObject.activeSelf);
                Assert.IsTrue(visual.TryBeginCollapse(),"Inactive effects must not consume collapse state");
                int pooled=go.transform.childCount;
                if(deactivateObject) go.SetActive(true); else fx.enabled=true;
                fx.EmitThrownContact(Vector3.zero,1); Assert.Greater(fx.LiveCount,0);
                Assert.AreEqual(pooled,go.transform.childCount);
                Object.DestroyImmediate(go); Assert.AreEqual(baseline+otherCount,HLStoneEffects.GlobalLiveCount);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(source); Object.DestroyImmediate(other); Object.DestroyImmediate(mesh); }
            Assert.AreEqual(baseline,HLStoneEffects.GlobalLiveCount);
        }
        [Test] public void SceneLookupPreservesDisabledOwnerWithoutSpawning()
        {
            var fx=HLStoneEffects.ForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),null);
            try
            {
                fx.enabled=false; TestHelpers.InvokePrivate(fx,"OnDisable");
                Assert.AreSame(fx,HLStoneEffects.ForScene(fx.gameObject.scene,null));
                fx.EmitThrownContact(Vector3.zero,1); Assert.AreEqual(0,fx.LiveCount);
                Assert.AreEqual(0,fx.transform.childCount);
            }
            finally { Object.DestroyImmediate(fx.gameObject); }
        }
        [Test] public void HitCountsGlobalCapAndLifetime()
        {
            var go=new GameObject("HLEffectsTest"); var fx=go.AddComponent<HLStoneEffects>();
            var other=new GameObject("HLEffectsOther"); var second=other.AddComponent<HLStoneEffects>();
            try {
                var impact=new HLStoneImpact(Vector3.up,Vector3.up,Vector3.zero,true);
                fx.EmitHit(impact,false,1); Assert.AreEqual(9,fx.LiveCount); fx.Advance(.6f); Assert.AreEqual(0,fx.LiveCount);
                fx.EmitHit(impact,true,1); Assert.AreEqual(14,fx.LiveCount);
                for(uint i=0;i<40;i++) (i%2==0?fx:second).EmitHit(impact,true,i);
                Assert.AreEqual(256,HLStoneEffects.GlobalLiveCount); fx.Advance(1); second.Advance(1); Assert.AreEqual(0,HLStoneEffects.GlobalLiveCount);
                Assert.AreEqual(0,go.GetComponentsInChildren<Collider>().Length); Assert.AreEqual(0,go.GetComponentsInChildren<Rigidbody>().Length);
            } finally { TestHelpers.InvokePrivate(fx,"OnDestroy"); TestHelpers.InvokePrivate(second,"OnDestroy"); Object.DestroyImmediate(go); Object.DestroyImmediate(other); }
        }
        [Test] public void DetachedCopySurvivesSourceReleaseAndSplitsIntoThree()
        {
            var go=new GameObject("HLEffectsTest"); var fx=go.AddComponent<HLStoneEffects>();
            var mesh=HLStoneMesh.CreateMesh(1,HLStonePresets.Boulder);
            try {
                fx.EmitDetachedPart(mesh,null,Matrix4x4.TRS(Vector3.up,Quaternion.identity,Vector3.one),Vector3.zero,0,1);
                Object.DestroyImmediate(mesh); fx.Advance(.24f); Assert.AreEqual(1,fx.LiveCount);
                Assert.IsNotNull(go.GetComponentInChildren<MeshFilter>().sharedMesh);
                fx.Advance(.01f); Assert.AreEqual(3,fx.LiveCount); fx.Advance(.25f); Assert.AreEqual(0,fx.LiveCount);
            } finally { if(mesh!=null)Object.DestroyImmediate(mesh); TestHelpers.InvokePrivate(fx,"OnDestroy"); Object.DestroyImmediate(go); }
        }
        [Test] public void OneAnalyticBounceNeverFallsBelowGround()
        {
            for(int i=0;i<100;i++) Assert.That(HLStoneEffects.PositionAt(Vector3.up,Vector3.right,i*.02f,0,true).y,Is.GreaterThanOrEqualTo(0));
            Assert.That(HLStoneEffects.PositionAt(Vector3.up,Vector3.right,1,0,false).y,Is.LessThan(0));
        }
    }
}
