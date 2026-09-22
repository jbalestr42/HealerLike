using System.Collections.Generic;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneEnemyVisual : MonoBehaviour,IVisualBehaviour
    {
        [SerializeField] Transform bodyPivot;
        [SerializeField] HLStonePreset preset;
        [SerializeField] HLStoneAssemblyProfile profile;
        [SerializeField] Material stoneMaterial;
        [SerializeField] uint seed=1;
        [SerializeField] HLStoneEffects effects;
        readonly HLStoneAssembly assembly=new HLStoneAssembly();
        readonly HLStoneHealthState state=new HLStoneHealthState();
        readonly HLStoneMotionSampler sampler=new HLStoneMotionSampler();
        readonly Dictionary<ResourceModifier,ImpactRecord> impacts=new Dictionary<ResourceModifier,ImpactRecord>();
        readonly List<ResourceModifier> expired=new List<ResourceModifier>();
        struct ImpactRecord { public HLStoneImpact Impact; public int Frame; }
        Entity entity; ResourceAttribute health; IHLStoneMotionSource motion;
        bool bound,collapsed; int completedFrames; uint hitIndex;
        public IReadOnlyList<HLStoneAssembly.Part> Parts=>assembly.Parts;
        public Vector3 PlanarVelocity { get; private set; }
        public float GroundY=>transform.position.y;
        public int PendingImpactCount=>impacts.Count;
        public void Init(Entity owner)
        {
            entity=owner;
            Initialize(owner.health,seed,effects);
            foreach(var component in owner.GetComponents<MonoBehaviour>()) if(component is IHLStoneMotionSource source) { motion=source; break; }
        }
        // Explicit resource injection also permits isolated tests without Entity.Init or global managers.
        public void Initialize(ResourceAttribute resource,uint visualSeed,HLStoneEffects effectsOwner)
        {
            Unbind(); health=resource; seed=visualSeed; effects=effectsOwner;
            if(bodyPivot==null)
            {
                var child=transform.Find("BodyPivot");
                if(child==null) { child=new GameObject("BodyPivot").transform; child.SetParent(transform,false); }
                bodyPivot=child;
            }
            assembly.BuildEnemy(bodyPivot,seed,preset,stoneMaterial,profile);
            state.Reset(profile!=null?profile.ShedHealthFraction:.5f); collapsed=false; hitIndex=0; completedFrames=0;
            motion=null; sampler.Reset(); PlanarVelocity=Vector3.zero; Bind();
        }
        public void RecordImpact(ResourceModifier modifier,in HLStoneImpact impact)
        { if(modifier!=null && bound && isActiveAndEnabled) impacts[modifier]=new ImpactRecord{Impact=impact,Frame=completedFrames}; }
        public HLStoneImpact EstimateImpact(Vector3 queryWS,Vector3 incomingVelocityWS)
        {
            Vector3 point=transform.position+Vector3.up*.5f,normal=Vector3.up; float best=float.PositiveInfinity;
            foreach(var part in assembly.Parts)
            {
                if(!part.Transform.gameObject.activeSelf) continue;
                if(HLStoneImpactLocator.TryClosestPoint(part.Lease.Data,part.Transform.localToWorldMatrix,queryWS,out var p,out var n))
                { float distance=(p-queryWS).sqrMagnitude; if(distance<best){best=distance;point=p;normal=n;} }
            }
            return new HLStoneImpact(point,normal,incomingVelocityWS,true);
        }
        HLStoneEffects Effects()
        {
            if(effects==null && Application.isPlaying) effects=HLStoneEffects.ForScene(gameObject.scene,stoneMaterial);
            return effects;
        }
        void OnConsumersProcessed(GameObject owner,ResourceModifier modifier,float delta,bool critical)
        {
            bool recorded=modifier!=null && impacts.TryGetValue(modifier,out _);
            HLStoneImpact impact=recorded?impacts[modifier].Impact:default;
            if(modifier!=null) impacts.Remove(modifier);
            state.RecordProcessedDelta(delta);
            if(!(delta<0) || collapsed) return;
            if(!recorded) impact=EstimateImpact(modifier?.source!=null?modifier.source.transform.position:transform.position+Vector3.up*2,Vector3.zero);
            Effects()?.EmitHit(impact,critical,HLStoneSeed.ForPart(seed,++hitIndex+100));
        }
        void OnHealthChanged(ResourceAttribute resource)
        {
            // The callback is after clamp. Synchronous fallback survives Entity's earlier destruction callback.
            if(resource.Value<=0) Collapse();
        }
        public void CompleteHealthBatch()
        {
            if(health==null) return;
            var action=state.CompleteBatch(health.Value,health.Max);
            if(action==HLStoneHealthAction.Collapse) Collapse();
            else if(action==HLStoneHealthAction.ShedPart)
            {
                int index=profile!=null?profile.DetachablePartIndex:preset==HLStonePreset.Monolith?-1:2;
                if(index<0 || index>=assembly.Parts.Count) return;
                var part=assembly.Parts[index];
                Effects()?.EmitDetachedPart(part.Lease.Mesh,part.Renderer.sharedMaterial,part.Transform.localToWorldMatrix,PlanarVelocity,GroundY,HLStoneSeed.ForPart(seed,201));
                part.Transform.gameObject.SetActive(false);
            }
        }
        public void Collapse(HLStoneEffects owner=null)
        {
            if(owner!=null) effects=owner;
            var fx=Effects(); if(fx!=null) fx.CollapseOnce(this,HLStoneSeed.ForPart(seed,301));
            else if(TryBeginCollapse()) HideParts();
        }
        public bool TryBeginCollapse()
        { if(collapsed) return false; collapsed=true; state.TryBeginCollapse(); return true; }
        public void HideParts() { foreach(var p in assembly.Parts) p.Transform.gameObject.SetActive(false); }
        void LateUpdate()
        {
            if(!bound) return;
            CompleteHealthBatch();
            Vector3 position=entity!=null?entity.transform.position:transform.position;
            PlanarVelocity=sampler.Sample(position,Time.deltaTime,entity!=null && entity.isDraggable);
            Quaternion facing=Quaternion.identity; Vector3 velocity=Vector3.zero;
            bool explicitFacing=motion!=null && motion.TrySample(out velocity,out facing);
            if(explicitFacing && (entity==null || !entity.isDraggable)) { velocity.y=0; PlanarVelocity=velocity.magnitude<.02f?Vector3.zero:velocity; }
            if(PlanarVelocity!=Vector3.zero && bodyPivot.GetComponent<LookAtTarget>()==null)
                bodyPivot.rotation=explicitFacing?Quaternion.Euler(0,facing.eulerAngles.y,0):Quaternion.LookRotation(PlanarVelocity,Vector3.up);
            completedFrames++; expired.Clear();
            foreach(var pair in impacts) if(completedFrames-pair.Value.Frame>=2) expired.Add(pair.Key);
            foreach(var key in expired) impacts.Remove(key);
        }
        void Bind()
        {
            if(bound || health==null || !isActiveAndEnabled) return;
            health.OnAllConsumerProcessed.AddListener(OnConsumersProcessed); health.OnValueChanged.AddListener(OnHealthChanged); bound=true;
        }
        void Unbind()
        {
            if(bound && health!=null) { health.OnAllConsumerProcessed.RemoveListener(OnConsumersProcessed); health.OnValueChanged.RemoveListener(OnHealthChanged); }
            bound=false; impacts.Clear(); sampler.Reset(); state.CompleteBatch(float.PositiveInfinity,1);
        }
        void OnEnable() => Bind();
        void OnDisable() => Unbind();
        void OnDestroy() { Unbind(); assembly.Dispose(); }
    }
}
