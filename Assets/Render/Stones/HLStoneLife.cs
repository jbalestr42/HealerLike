using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneLife : MonoBehaviour
    {
        HLStoneLifeState state=new HLStoneLifeState();
        HLStoneEffects effects;
        Transform top;
        Quaternion rest;
        float wobbleAge=2,radius;
        uint seed,index;
        bool terrain,subscribed;
        public void Configure(HLStoneEffects owner,uint visualSeed,float footprint,bool isTerrain,Transform cairnTop=null)
        {
            Restore(); state=new HLStoneLifeState(); effects=owner; seed=visualSeed; index=0;
            radius=footprint; terrain=isTerrain; top=cairnTop; rest=top!=null?top.localRotation:Quaternion.identity;
            wobbleAge=2; Subscribe();
        }
        void Subscribe() { if(subscribed) return; HLStoneEffects.ImpactRecorded+=OnImpact; subscribed=true; }
        void OnEnable()=>Subscribe();
        void OnDisable() { if(subscribed) HLStoneEffects.ImpactRecorded-=OnImpact; subscribed=false; Restore(); wobbleAge=2; }
        void OnDestroy()=>OnDisable();
        void Restore() { if(top!=null) top.localRotation=rest; }
        void OnImpact(Vector3 position)
        { if(top!=null && (position-transform.position).sqrMagnitude<=(radius+2)*(radius+2)) wobbleAge=0; }
        HLStoneEffects Effects()
        {
            if(effects==null && Application.isPlaying) effects=HLStoneEffects.ForScene(gameObject.scene,null);
            return effects;
        }
        public void PollHealth(float fraction,float dt,Vector3 origin)
        { if(isActiveAndEnabled && state.PollHealth(fraction,dt)) Effects()?.EmitTrickle(origin,seed+ ++index); }
        public void Advance(float dt)
        {
            if(!isActiveAndEnabled) return;
            wobbleAge+=float.IsFinite(dt)?Mathf.Max(0,dt):0;
            if(top!=null && top.gameObject.activeSelf) top.localRotation=rest*Quaternion.Euler(HLStoneLifeState.Wobble(wobbleAge),0,HLStoneLifeState.Wobble(wobbleAge)*.4f);
            if(!terrain) return;
            var registry=HLZoneRegistry.current;
            var snapshot=registry!=null?registry.snapshot:default;
            int pulses=state.PollZones(snapshot,transform.position,radius);
            for(int i=0;i<pulses;i++) Effects()?.EmitDust(transform.position+Vector3.up*.1f,seed+ ++index);
        }
        void LateUpdate()=>Advance(Time.deltaTime);
    }
}
