using System;
using HealerLike.Render.Zones;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    // Bounded snapshot comparison: no gameplay queries and no per-frame allocations.
    public sealed class HLStoneLifeState
    {
        readonly HLZone[] previous=new HLZone[64];
        readonly bool[] used=new bool[64];
        int count;
        float health=1,remaining,next;
        public bool PollHealth(float fraction,float dt)
        {
            if(!float.IsFinite(fraction)) return false;
            fraction=Mathf.Clamp01(fraction);
            if(fraction<=0) { remaining=0; health=fraction; return false; }
            if(health>=.5f && fraction<.5f) { remaining=3; next=0; }
            if(fraction>=.5f) remaining=0;
            health=fraction;
            if(remaining<=0) return false;
            dt=float.IsFinite(dt)?Mathf.Max(0,dt):0;
            remaining=Mathf.Max(0,remaining-dt); next-=dt;
            if(next>0 || remaining<=0) return false;
            next=.18f; return true;
        }
        public int PollZones(ReadOnlySpan<HLZone> zones,Vector3 center,float radius)
        {
            Array.Clear(used,0,used.Length); int pulses=0;
            int length=Mathf.Min(64,zones.Length);
            for(int i=0;i<length;i++)
            {
                var z=zones[i];
                if(z.kind!=(int)HLZoneKind.Hostile || z.strength<=0 || !Overlaps(z,center,radius)) continue;
                bool seen=false;
                for(int j=0;j<count;j++)
                {
                    var p=previous[j];
                    if(used[j] || p.kind!=z.kind || p.position!=z.position || p.radius!=z.radius ||
                        z.age<p.age || z.strength>p.strength || !Overlaps(p,center,radius)) continue;
                    used[j]=true; seen=true; break;
                }
                if(!seen) pulses++;
            }
            zones.Slice(0,length).CopyTo(previous); count=length; return pulses;
        }
        static bool Overlaps(HLZone z,Vector3 p,float radius)
        { float x=z.position.x-p.x,y=z.position.z-p.z,r=z.radius+radius; return x*x+y*y<=r*r; }
        public static float Wobble(float age) => age<0 || age>=1.2f?0:7*Mathf.Sin(age*24)*Mathf.Pow(1-age/1.2f,2);
        public static bool Ochre(uint seed) => seed%5==0;
    }
}
