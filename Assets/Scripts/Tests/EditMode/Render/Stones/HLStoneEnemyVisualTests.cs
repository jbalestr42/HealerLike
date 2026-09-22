using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneEnemyVisualTests
    {
        sealed class MotionSource : IHLStoneMotionSource
        {
            public bool TrySample(out Vector3 velocityWS,out Quaternion facingWS)
            { velocityWS=Vector3.right; facingWS=Quaternion.Euler(0,90,0); return true; }
        }
        [Test] public void MotionUsesLookAtTargetCachedDuringInitialization()
        {
            var pivot=target.transform.Find("BodyPivot");
            var look=pivot.gameObject.AddComponent<LookAtTarget>();
            visual.Initialize(health,15,fx);
            TestHelpers.SetPrivateField(visual,"motion",new MotionSource());
            TestHelpers.InvokePrivate(visual,"LateUpdate");
            Assert.AreEqual(Quaternion.identity,pivot.rotation);
            Object.DestroyImmediate(look);
            TestHelpers.InvokePrivate(visual,"LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0,90,0),pivot.rotation),.001f);
            // New components are picked up only on initialization, never by the moving frame path.
            pivot.gameObject.AddComponent<LookAtTarget>(); pivot.rotation=Quaternion.identity;
            TestHelpers.InvokePrivate(visual,"LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0,90,0),pivot.rotation),.001f);
            visual.Initialize(health,15,fx); pivot.rotation=Quaternion.identity;
            TestHelpers.SetPrivateField(visual,"motion",new MotionSource());
            TestHelpers.InvokePrivate(visual,"LateUpdate");
            Assert.AreEqual(Quaternion.identity,pivot.rotation);
        }
        sealed class Consumer : AConsumer
        {
            readonly float amount; public Consumer(float amount){this.amount=amount;}
            public override float GetValue()=>amount;
            public override bool ignoreDamageReduction=>true;
            public override bool ignoreConsumerPrevention=>false;
        }
        GameObject target,source,fxObject; ResourceAttribute health; HLStoneEnemyVisual visual; HLStoneEffects fx;
        [SetUp] public void Setup()
        {
            target=new GameObject("HLTarget"); source=new GameObject("HLSource"); fxObject=new GameObject("HLEffects");
            health=TestHelpers.CreateResourceAttribute(target,AttributeType.HealthMax,100); TestHelpers.CreateAttributeManager(source);
            fx=fxObject.AddComponent<HLStoneEffects>(); visual=target.AddComponent<HLStoneEnemyVisual>(); visual.Initialize(health,15,fx);
        }
        [TearDown] public void Teardown(){if(visual!=null)TestHelpers.InvokePrivate(visual,"OnDestroy");TestHelpers.InvokePrivate(fx,"OnDestroy");Object.DestroyImmediate(target);Object.DestroyImmediate(source);Object.DestroyImmediate(fxObject);}
        ResourceModifier Queue(float delta)
        {
            var m=new ResourceModifier{source=source}; m.consumers.Add(new Consumer(delta)); health.AddResourceModifier(m); return m;
        }
        void Drain(){TestHelpers.InvokePrivate(health,"Update");visual.CompleteHealthBatch();}
        int Visible=>visual.Parts.Count(p=>p.Transform.gameObject.activeSelf);
        [Test] public void FakeHealthShedsOnceAtFiftyAndSurvivorsNeverMove()
        {
            var first=visual.Parts[0].Transform.localToWorldMatrix; var second=visual.Parts[1].Transform.localToWorldMatrix;
            Queue(-49); Drain(); Assert.AreEqual(3,Visible);
            Queue(-1); Drain(); Assert.AreEqual(2,Visible); Assert.AreEqual(first,visual.Parts[0].Transform.localToWorldMatrix); Assert.AreEqual(second,visual.Parts[1].Transform.localToWorldMatrix);
            Queue(50); Drain(); Queue(-70); Drain(); Assert.AreEqual(2,Visible);
            visual.Initialize(health,15,fx); Assert.AreEqual(3,Visible);
        }
        [Test] public void DamageAndHealingInSameBatchAndMaxOnlyChangesDoNotShed()
        {
            Queue(-80);Queue(80);Drain(); Assert.AreEqual(3,Visible);
            var max=target.GetComponent<AttributeManager>().Get(AttributeType.HealthMax); max.BaseValue=200; max.Update();
            visual.CompleteHealthBatch(); Assert.AreEqual(3,Visible);
            // No final value event is emitted when damage and healing return to the previous value.
            Queue(-160);Queue(160);Drain(); TestHelpers.InvokePrivate(visual,"LateUpdate"); Assert.AreEqual(3,Visible);
        }
        [Test] public void RecordedContactConsumedOnlyByExactModifierAndZeroDamageEmitsOnlyDust()
        {
            var m=Queue(-1); var point=new Vector3(23,7,4);
            visual.RecordImpact(m,new HLStoneImpact(point,Vector3.up,Vector3.zero,false));
            Drain(); Assert.AreEqual(0,visual.PendingImpactCount); Assert.AreEqual(14,fx.LiveCount);
            foreach(var filter in fxObject.GetComponentsInChildren<MeshFilter>()) Assert.That(Vector3.Distance(filter.transform.position,filter.sharedMesh.name=="HLFlatCone"?point+Vector3.up*.005f:point),Is.LessThan(1e-5));
            fx.Advance(1); var zero=Queue(0); visual.RecordImpact(zero,new HLStoneImpact(point,Vector3.up,Vector3.zero,false)); Drain();
            Assert.AreEqual(0,visual.PendingImpactCount); Assert.AreEqual(5,fx.LiveCount);
        }
        [Test] public void ExpiryUnbindReenableAndReinitDoNotDuplicateListeners()
        {
            visual.RecordImpact(new ResourceModifier(),default); TestHelpers.InvokePrivate(visual,"LateUpdate"); Assert.AreEqual(1,visual.PendingImpactCount);
            TestHelpers.InvokePrivate(visual,"LateUpdate"); Assert.AreEqual(0,visual.PendingImpactCount);
            visual.enabled=false; TestHelpers.InvokePrivate(visual,"OnDisable"); visual.RecordImpact(new ResourceModifier(),default); Assert.AreEqual(0,visual.PendingImpactCount);
            fx.Advance(1); visual.enabled=true; visual.Initialize(health,15,fx); Queue(-1);Drain();Assert.AreEqual(14,fx.LiveCount);
        }
        [Test] public void LethalBatchCollapsesOnlyOnceAndDebrisSurvivesOwner()
        {
            Queue(-100);Drain(); Assert.AreEqual(0,Visible); Assert.AreEqual(31,fx.LiveCount);
            visual.Collapse(); Assert.AreEqual(31,fx.LiveCount);
            TestHelpers.InvokePrivate(visual,"OnDestroy"); Object.DestroyImmediate(target); target=null; Assert.AreEqual(31,fx.LiveCount); fx.Advance(.81f); Assert.AreEqual(0,fx.LiveCount);
        }
        [Test] public void DisableOrLivingRemovalDoesNotEmitDeath()
        {visual.enabled=false;TestHelpers.InvokePrivate(visual,"OnDestroy");Object.DestroyImmediate(target);target=null;Assert.AreEqual(0,fx.LiveCount);}
    }
}
