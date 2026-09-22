using System;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneLifeStateTests
    {
        [Test] public void ThresholdStartsBoundedTrickleAndHealingRearms()
        {
            var state=new HLStoneLifeState();
            Assert.False(state.PollHealth(.5f,.1f)); Assert.True(state.PollHealth(.49f,.1f));
            int count=0; for(int i=0;i<50;i++) if(state.PollHealth(.4f,.1f)) count++;
            Assert.That(count,Is.InRange(10,16)); Assert.False(state.PollHealth(.3f,1));
            state.PollHealth(1,0); Assert.True(state.PollHealth(.4f,.1f));
            Assert.False(state.PollHealth(0,.1f)); Assert.False(state.PollHealth(float.NaN,.1f));
        }
        [Test] public void HostileOverlapIsOncePerPulseAndIgnoresSnapshotCompaction()
        {
            var state=new HLStoneLifeState();
            var z=new HLZone{kind=(int)HLZoneKind.Hostile,position=Vector3.zero,radius=1,strength=1,age=0};
            var heal=z; heal.kind=(int)HLZoneKind.Heal;
            Assert.AreEqual(1,state.PollZones(new[]{heal,z},Vector3.right*1.2f,.3f));
            z.age=.1f; z.strength=.8f;
            Assert.AreEqual(0,state.PollZones(new[]{z},Vector3.right*1.2f,.3f));
            z.age=0; z.strength=1;
            Assert.AreEqual(1,state.PollZones(new[]{z},Vector3.right*1.2f,.3f));
            state.PollZones(ReadOnlySpan<HLZone>.Empty,Vector3.zero,.3f);
            Assert.AreEqual(0,state.PollZones(new[]{z},Vector3.right*3,.3f));
            Assert.AreEqual(0,state.PollZones(new[]{heal},Vector3.zero,.3f));
        }
        [Test] public void ConcurrentOverlappingPulsesEachTriggerAndThenStayQuiet()
        {
            var state=new HLStoneLifeState(); var zones=new[]{new HLZone{kind=2,radius=1,strength=1},new HLZone{kind=2,radius=1,strength=1}};
            Assert.AreEqual(2,state.PollZones(zones,Vector3.zero,.2f));
            Assert.AreEqual(0,state.PollZones(zones,Vector3.zero,.2f));
        }
        [Test] public void WobbleSettlesAndOchreFrequencyIsOneInFive()
        {
            Assert.AreEqual(0,HLStoneLifeState.Wobble(0)); Assert.AreEqual(0,HLStoneLifeState.Wobble(1.2f));
            Assert.Greater(Mathf.Abs(HLStoneLifeState.Wobble(.05f)),3);
            int count=0; for(uint i=0;i<100;i++) if(HLStoneLifeState.Ochre(i)) count++;
            Assert.AreEqual(20,count);
        }
        [Test] public void PollingAllocatesNothingAfterConstruction()
        {
            var state=new HLStoneLifeState(); var zones=new[]{new HLZone{kind=2,radius=1,strength=1}};
            state.PollZones(zones,Vector3.zero,1); state.PollHealth(1,.016f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++) { state.PollZones(zones,Vector3.zero,1); state.PollHealth(1,.016f); }
            Assert.AreEqual(0,GC.GetAllocatedBytesForCurrentThread()-before);
        }
    }
}
