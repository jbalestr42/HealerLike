using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Spells
{
    public class HLSpellVisualSinkTests
    {
        GameObject host,target,other;HLSpellVisualSink sink;BuffHandlerFactory factory,second;FlatModifierFactory modifier;
        [SetUp] public void Setup()
        {
            host=new GameObject("HLHost");target=new GameObject("HLTarget");other=new GameObject("HLOther");sink=host.AddComponent<HLSpellVisualSink>();
            factory=ScriptableObject.CreateInstance<BuffHandlerFactory>();second=ScriptableObject.CreateInstance<BuffHandlerFactory>();modifier=ScriptableObject.CreateInstance<FlatModifierFactory>();modifier.data=new FlatModifierData {type=AttributeType.Damage,value=2};
            factory.data=new BuffHandlerData {durationType=DurationType.Duration,duration=4,buffFactoryList=new List<ABuffFactory>{modifier}};second.data=factory.data;
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(host);Object.DestroyImmediate(target);Object.DestroyImmediate(other);Object.DestroyImmediate(factory);Object.DestroyImmediate(second);Object.DestroyImmediate(modifier);}
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
            var previous=HLRenderRegistry.Current;var owner=new HLOwnerSpy();
            try
            {
                HLRenderRegistry.Current=new HLRenderRegistry{ZoneOwner=owner};
                sink.PulseArea(Vector3.one,3,HLZoneKind.Hostile,.5f);
                Assert.AreEqual(1,owner.calls);Assert.AreEqual(3,owner.radius);Assert.AreEqual(HLZoneKind.Hostile,owner.kind);Assert.AreEqual(HLSpellVisualSink.PulseSeconds,owner.seconds);
                sink.PulseArea(Vector3.one,-1,HLZoneKind.Hostile,.5f);Assert.AreEqual(1,owner.calls);
                int injected=0;sink.AreaPulse=(p,r,k,s)=>injected++;sink.PulseArea(Vector3.one,3,HLZoneKind.Heal,.5f);
                Assert.AreEqual(1,injected);Assert.AreEqual(1,owner.calls);
                sink.AreaPulse=null;HLRenderRegistry.Current=null;Assert.DoesNotThrow(()=>sink.PulseArea(Vector3.one,3,HLZoneKind.Heal,.5f));
            }
            finally{HLRenderRegistry.Current=previous;}
        }
        [Test] public void DestroyedTargetAndDisableReleaseVisuals()
        {
            sink.SetStatus(null,target,factory,1,0,4,HLClockKind.Simulation);Object.DestroyImmediate(target);TestHelpers.InvokePrivate(sink,"LateUpdate");Assert.AreEqual(0,sink.StatusCount);
            sink.ShowImpact(null,other,HLResourceKind.Health,1,false);sink.Clear();Assert.AreEqual(0,sink.ImpactCount);
        }
    }
}
