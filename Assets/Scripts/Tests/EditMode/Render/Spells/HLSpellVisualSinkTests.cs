using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Spells
{
    public class HLSpellVisualSinkTests
    {
        static void DestroyHost(GameObject go)
        {
            if (!go) return;
            foreach(var effect in go.GetComponentsInChildren<HLSpellEffect>(true)) TestHelpers.InvokePrivate(effect,"OnDestroy");
            foreach(var sink in go.GetComponentsInChildren<HLSpellVisualSink>(true)) TestHelpers.InvokePrivate(sink,"OnDestroy");
            Object.DestroyImmediate(go);
        }
        GameObject host,target,other;HLSpellVisualSink sink;BuffHandlerFactory factory,second;FlatModifierFactory modifier;
        [SetUp] public void Setup()
        {
            host=new GameObject("HLHost");target=new GameObject("HLTarget");other=new GameObject("HLOther");sink=host.AddComponent<HLSpellVisualSink>();
            factory=ScriptableObject.CreateInstance<BuffHandlerFactory>();second=ScriptableObject.CreateInstance<BuffHandlerFactory>();modifier=ScriptableObject.CreateInstance<FlatModifierFactory>();modifier.data=new FlatModifierData {type=AttributeType.Damage,value=2};
            factory.data=new BuffHandlerData {durationType=DurationType.Duration,duration=4,buffFactoryList=new List<ABuffFactory>{modifier}};second.data=factory.data;
        }
        [TearDown] public void Cleanup(){DestroyHost(host);DestroyHost(target);DestroyHost(other);Object.DestroyImmediate(factory);Object.DestroyImmediate(second);Object.DestroyImmediate(modifier);}
        [Test] public void HostilePulseSpawnsSlateLitterAndContactLinkUsesThreads()
        {
            var go=new GameObject("HLBeautySink");try {
                var sink=go.AddComponent<HLSpellVisualSink>();TestHelpers.InvokePrivate(sink,"OnEnable");
                sink.AreaPulse=(center,radius,kind,strength)=>{};
                sink.PulseArea(Vector3.one,2,HealerLike.Render.Zones.HLZoneKind.Hostile,1);
                var effect=go.GetComponentInChildren<HLSpellEffect>();Assert.AreEqual(HLSpellEffectKind.Litter,effect.kind);
                Assert.AreEqual(HLSpellVisualSink.PulseSeconds,effect.lifetime);Assert.AreEqual(Vector3.one,effect.transform.position);
                var block=new MaterialPropertyBlock();effect.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.Less(Vector4.Distance((Color)new Color32(58,66,87,255),block.GetColor("_BaseColor")),.00001f);
                var thread=sink.ShowContactLink(Vector3.zero,Vector3.right);Assert.IsTrue(thread.ContactThread);Assert.IsFalse(thread.parts[1].gameObject.activeSelf);
                TestHelpers.InvokePrivate(sink,"OnDestroy");
            } finally {Object.DestroyImmediate(go);}
        }
        [Test] public void RegistryCallsCannotRepopulateDisabledSinkAndEnableStartsClean()
        {
            var previous = HLRenderRegistry.current;
            try
            {
                HLRenderRegistry.current = new HLRenderRegistry { spellSink = sink };
                sink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);
                var root = sink.GetStatus(target,factory);
                sink.enabled = false; TestHelpers.InvokePrivate(sink,"OnDisable");
                Assert.IsFalse(root);
                HLRenderRegistry.current.spellSink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);
                HLRenderRegistry.current.spellSink.ShowImpact(null,target,HLResourceKind.Health,2,false);
                sink.PulseArea(Vector3.zero,1,HLZoneKind.Heal,1);
                Assert.IsNull(sink.ShowLink(Vector3.zero,Vector3.one));
                Assert.AreEqual(0,sink.StatusCount);
                Assert.AreEqual(0,sink.ImpactCount);
                sink.enabled = true; TestHelpers.InvokePrivate(sink,"OnEnable");
                Assert.AreSame(sink,HLRenderRegistry.current.spellSink);
                Assert.AreEqual(0,sink.StatusCount);
                sink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);
                Assert.AreEqual(1,sink.StatusCount);
                TestHelpers.InvokePrivate(sink,"OnEnable");
                Assert.AreEqual(0,sink.StatusCount);
            }
            finally { HLRenderRegistry.current = previous; }
        }
        [TestCase(false)] [TestCase(true)]
        public void RepeatedStatusAndIdleSinkFramesAllocateNothing(bool populated)
        {
            if(populated) sink.SetStatus(null,target,factory,1,1,4,HLClockKind.Simulation);
            var root=sink.GetStatus(target,factory);
            var method=typeof(HLSpellVisualSink).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var update=(System.Action)System.Delegate.CreateDelegate(typeof(System.Action),sink,method);
            for(int i=0;i<32;i++) { if(populated) sink.SetStatus(null,target,factory,1,1,4,HLClockKind.Simulation); update(); }
            long before=System.GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<32;i++) { if(populated) sink.SetStatus(null,target,factory,1,1,4,HLClockKind.Simulation); update(); }
            long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.AreEqual(0,allocated);
            Assert.AreSame(root,sink.GetStatus(target,factory));
        }
        [Test] public void SpeedStatusSitsLowAndRemovalKeepsOnlyCosmeticTail()
        {
            modifier.data.type=AttributeType.Speed;
            sink.SetStatus(null,target,factory,1,.25f,4,HLClockKind.Simulation);
            var root=sink.GetStatus(target,factory);var effect=root.GetComponentInChildren<HLSpellEffect>();
            Assert.AreEqual(-.35f,effect.transform.localPosition.y);
            sink.RemoveStatus(null,target,factory);Assert.AreEqual(0,sink.StatusCount);
            Assert.IsTrue(effect);effect.Advance(.25f);Assert.IsTrue(effect.RemovalComplete);
        }
        [Test] public void AreaRingKeepsExactRadiusAndResolvedAmountScalesImpact()
        {
            sink.PulseArea(Vector3.right,2,HLZoneKind.Heal,.3f);
            var ring=host.GetComponentInChildren<HLSpellEffect>();Assert.AreEqual(HLSpellEffectKind.Area,ring.kind);
            Assert.AreEqual(Vector3.right,ring.transform.position);Assert.AreEqual(Vector3.one*2,ring.transform.localScale);
            sink.Clear();sink.ShowImpact(null,target,HLResourceKind.Health,1,false);
            var small=host.GetComponentInChildren<HLSpellEffect>().transform.localScale.x;
            sink.Clear();sink.ShowImpact(null,target,HLResourceKind.Health,100,false);
            Assert.Greater(host.GetComponentInChildren<HLSpellEffect>().transform.localScale.x,small);
        }
        [Test] public void SameFrameCharacterHealsPairEachRecipientAndIgnoreOtherOutcomes()
        {
            sink.IsCharacterSource=source=>source==host;
            sink.HealerAnchor=source=>other.transform;
            other.transform.position=Vector3.up*2;
            int links=0;sink.LinkObserved=(a,b)=>{links++;Assert.AreEqual(other.transform.position,a);};
            sink.ShowImpact(host,target,HLResourceKind.Health,3,false);
            sink.ShowImpact(host,target,HLResourceKind.Health,4,false);
            sink.ShowImpact(host,other,HLResourceKind.Health,2,false);
            sink.ShowImpact(other,target,HLResourceKind.Health,2,false);
            sink.ShowImpact(host,target,HLResourceKind.Health,-2,false);
            sink.ShowImpact(host,target,HLResourceKind.Mana,2,false);
            Assert.AreEqual(0,links);sink.FlushHealLinks();Assert.AreEqual(2,links);
            sink.FlushHealLinks();Assert.AreEqual(2,links);
            foreach(var effect in host.GetComponentsInChildren<HLSpellEffect>())
                if(effect.kind==HLSpellEffectKind.Chain) Assert.AreEqual(.6f,effect.lifetime);
        }
        [Test] public void UnknownMappedSignatureFailsClosedAndLogsOnce()
        {
            sink.styles=ScriptableObject.CreateInstance<HLSpellStyleTable>();
            try {
                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,"HL unmapped spell signature: Resource/Positive/HealthMax/Single/Instant/Immediate/v0");
                sink.ShowImpact(null,target,HLResourceKind.Health,3,false);
                sink.ShowImpact(null,target,HLResourceKind.Health,3,false);
                Assert.AreEqual(0,sink.ImpactCount);
            } finally {Object.DestroyImmediate(sink.styles);}
        }
        [Test] public void KeyIsTargetAndFactoryNotSourceOrSignature()
        {
            sink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);var first=sink.GetStatus(target,factory);
            sink.SetStatus(other,target,factory,3,2,4,HLClockKind.Realtime);Assert.AreSame(first,sink.GetStatus(target,factory));Assert.AreEqual(3,first.GetComponentInChildren<HLSpellEffect>().Stacks);
            sink.SetStatus(null,other,factory,1,0,4,HLClockKind.Simulation);sink.SetStatus(null,target,second,1,0,4,HLClockKind.Simulation);Assert.AreEqual(3,sink.StatusCount);
            sink.RemoveStatus(other,target,factory);Assert.AreEqual(2,sink.StatusCount);sink.RemoveStatus(other,target,factory);Assert.AreEqual(2,sink.StatusCount);
        }
        [Test] public void AnchorUsesTargetPointAndPlainObjectsFallBack()
        {
            Entity entity=null;TestHelpers.WithLoggingDisabled(()=>entity=target.AddComponent<Entity>());var anchor=new GameObject("HLAnchor");anchor.transform.SetParent(target.transform);TestHelpers.SetPrivateField(entity,"_targetPoint",anchor);
            sink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);Assert.AreEqual(anchor.transform,sink.GetStatus(target,factory).transform.parent);
            sink.SetStatus(null,other,factory,1,0,4,HLClockKind.Simulation);Assert.AreEqual(other.transform,sink.GetStatus(other,factory).transform.parent);
        }
        [Test] public void OnlySignedFiniteOutcomesEmitAndResourceChoosesShape()
        {
            sink.ShowImpact(null,target,HLResourceKind.Health,0,false);sink.ShowImpact(null,target,HLResourceKind.Health,float.NaN,false);Assert.AreEqual(0,sink.ImpactCount);
            sink.ShowImpact(null,target,HLResourceKind.Health,5,false);sink.ShowImpact(null,target,HLResourceKind.Health,-5,false);sink.ShowImpact(null,target,HLResourceKind.Mana,-5,false);
            Assert.AreEqual(3,sink.ImpactCount);var fx=host.GetComponentsInChildren<HLSpellEffect>();Assert.AreEqual(HLSpellEffectKind.Heal,fx[0].kind);Assert.AreEqual(HLSpellEffectKind.Impact,fx[1].kind);Assert.AreEqual(HLSpellEffectKind.Mana,fx[2].kind);
        }
        [Test] public void PulseForwardsExactRadiusAndRejectsInvalidGeometry()
        {
            int calls=0;sink.AreaPulse=(p,r,k,s)=>{calls++;Assert.AreEqual(2,r);Assert.AreEqual(HLZoneKind.Heal,k);};
            sink.PulseArea(Vector3.zero,2,HLZoneKind.Heal,.5f);sink.PulseArea(Vector3.zero,-1,HLZoneKind.Heal,.5f);Assert.AreEqual(1,calls);
        }
        sealed class HLOwnerSpy : IHLZoneOwner
        {
            public int calls;public float seconds,radius;public HLZoneKind kind;
            public int AddPulse(HLZoneKind k,Vector3 c,float r,float s,float t){calls++;kind=k;radius=r;seconds=t;return calls;}
        }
        [Test] public void NullDelegateFallsBackToRegistryZoneOwner()
        {
            var previous=HLRenderRegistry.current;var owner=new HLOwnerSpy();
            try
            {
                HLRenderRegistry.current=new HLRenderRegistry{zoneOwner=owner};
                sink.PulseArea(Vector3.one,3,HLZoneKind.Hostile,.5f);
                Assert.AreEqual(1,owner.calls);Assert.AreEqual(3,owner.radius);Assert.AreEqual(HLZoneKind.Hostile,owner.kind);Assert.AreEqual(HLSpellVisualSink.PulseSeconds,owner.seconds);
                sink.PulseArea(Vector3.one,-1,HLZoneKind.Hostile,.5f);Assert.AreEqual(1,owner.calls);
                int injected=0;sink.AreaPulse=(p,r,k,s)=>injected++;sink.PulseArea(Vector3.one,3,HLZoneKind.Heal,.5f);
                Assert.AreEqual(1,injected);Assert.AreEqual(1,owner.calls);
                sink.AreaPulse=null;HLRenderRegistry.current=null;Assert.DoesNotThrow(()=>sink.PulseArea(Vector3.one,3,HLZoneKind.Heal,.5f));
            }
            finally{HLRenderRegistry.current=previous;}
        }
        [Test] public void DestroyedTargetAndDisableReleaseVisuals()
        {
            sink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);DestroyHost(target);TestHelpers.InvokePrivate(sink,"LateUpdate");Assert.AreEqual(0,sink.StatusCount);
            sink.ShowImpact(null,other,HLResourceKind.Health,1,false);sink.Clear();Assert.AreEqual(0,sink.ImpactCount);
        }
    }
}
