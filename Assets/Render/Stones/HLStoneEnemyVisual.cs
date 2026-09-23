using System.Collections.Generic;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneEnemyVisual : MonoBehaviour,IVisualBehaviour,IHLDeliverySource
    {
        [SerializeField] Transform bodyPivot;
        [SerializeField] HLStonePreset preset;
        [SerializeField] HLStoneAssemblyProfile profile;
        [SerializeField] Material stoneMaterial;
        [SerializeField] uint seed=1;
        [SerializeField] HLStoneEffects effects;
        [SerializeField] bool groundShadowEnabled=true;
        [SerializeField] Vector3 directionToKeyLight=new Vector3(-1,2,-1);
        LookAtTarget bodyLookAtTarget;
        Transform presentation;
        HLStoneGroundShadow groundShadow;
        HLStoneLife life;
        TargetProvider targets;
        readonly List<ASkill> skills=new List<ASkill>();
        readonly Dictionary<ASkill,System.Func<float>> cooldownReaders=new Dictionary<ASkill,System.Func<float>>();
        readonly Dictionary<int,(Transform shard,Transform projectile,HLStoneMeshCache.Lease lease)> deliveries=new Dictionary<int,(Transform,Transform,HLStoneMeshCache.Lease)>();
        readonly List<int> endedDeliveries=new List<int>();
        float settleAge;
        Vector3 tricklePoint;
        public int LiveDeliveryCount=>deliveries.Count;
        public bool GroundShadowEnabled { get=>groundShadowEnabled; set { groundShadowEnabled=value; if(groundShadow!=null) groundShadow.Visible=value; } }
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
            entity=owner; HealerLike.Render.Spells.HLResourceOutcomeObserver.Ensure(owner); targets=owner.GetComponent<TargetProvider>();
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
            bodyLookAtTarget=bodyPivot.GetComponent<LookAtTarget>();
            if(presentation==null) { presentation=new GameObject("HLStonePresentation").transform; presentation.SetParent(bodyPivot,false); }
            presentation.localRotation=Quaternion.identity; settleAge=0;
            assembly.BuildEnemy(presentation,seed,preset,stoneMaterial,profile);
            var vertices=assembly.Parts[0].Lease.Data.Vertices; tricklePoint=vertices[0];
            for(int i=1;i<vertices.Length;i++) if(vertices[i].x>tricklePoint.x) tricklePoint=vertices[i];
            if(groundShadow==null) groundShadow=gameObject.AddComponent<HLStoneGroundShadow>();
            groundShadow.Configure(assembly.LocalBounds,directionToKeyLight,groundShadowEnabled);
            state.Reset(profile!=null?profile.ShedHealthFraction:.5f); collapsed=false; hitIndex=0; completedFrames=0;
            if(life==null) life=gameObject.AddComponent<HLStoneLife>();
            life.enabled=true; life.Configure(effects,seed,assembly.LocalBounds.extents.magnitude,false,preset==HLStonePreset.Cairn?assembly.Parts[assembly.Parts.Count-1].Transform:null);
            motion=null; sampler.Reset(); PlanarVelocity=Vector3.zero; Bind();
        }
        public void RecordImpact(ResourceModifier modifier,in HLStoneImpact impact)
        {
            if(modifier==null || !bound || !isActiveAndEnabled) return;
            impacts[modifier]=new ImpactRecord{Impact=impact,Frame=completedFrames};
            Effects()?.RecordImpact(impact.PointWS,HLStoneSeed.ForPart(seed,++hitIndex+100));
        }
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
            if(!recorded)
            {
                impact=EstimateImpact(modifier?.source!=null?modifier.source.transform.position:transform.position+Vector3.up*2,Vector3.zero);
                Effects()?.RecordImpact(impact.PointWS,HLStoneSeed.ForPart(seed,++hitIndex+100));
            }
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
            assembly.ApplyFracture(health.Max>0?health.Value/health.Max:1,seed,entity ? HealerLike.Render.Spells.HLBodyTintState.Read(entity.gameObject) : Color.white);
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
        public void HideParts() { if(groundShadow!=null) groundShadow.Visible=false; foreach(var p in assembly.Parts) p.Transform.gameObject.SetActive(false); }
        void LateUpdate()
        {
            FollowDeliveries();
            if(!bound) return;
            CompleteHealthBatch();
            if(life!=null && health!=null && !collapsed)
                life.PollHealth(health.Max>0?health.Value/health.Max:1,Time.deltaTime,assembly.Parts[0].Transform.TransformPoint(tricklePoint));
            Vector3 aim=Vector3.zero;
            var currentTargets=targets!=null?targets.GetTargets():null;
            if(currentTargets!=null && currentTargets.Count>0 && currentTargets[0]!=null)
                aim=currentTargets[0].transform.position-transform.position;
            float remaining=ReadCooldown();
            AdvancePresentation(aim,remaining,Time.deltaTime);
            Vector3 position=entity!=null?entity.transform.position:transform.position;
            PlanarVelocity=sampler.Sample(position,Time.deltaTime,entity!=null && entity.isDraggable);
            Quaternion facing=Quaternion.identity; Vector3 velocity=Vector3.zero;
            bool explicitFacing=motion!=null && motion.TrySample(out velocity,out facing);
            if(explicitFacing && (entity==null || !entity.isDraggable)) { velocity.y=0; PlanarVelocity=velocity.magnitude<.02f?Vector3.zero:velocity; }
            if(PlanarVelocity!=Vector3.zero && bodyLookAtTarget==null)
                bodyPivot.rotation=explicitFacing?Quaternion.Euler(0,facing.eulerAngles.y,0):Quaternion.LookRotation(PlanarVelocity,Vector3.up);
            completedFrames++; expired.Clear();
            foreach(var pair in impacts) if(completedFrames-pair.Value.Frame>=2) expired.Add(pair.Key);
            foreach(var key in expired) impacts.Remove(key);
        }
        // Only public cooldownProgress on an ACooldownSkill<T> is polled. Components are
        // discovered after Init because Entity creates skills after it initializes its model.
        float ReadCooldown()
        {
            if(entity==null || entity.isDraggable) return float.NaN;
            entity.GetComponents(skills); float remaining=float.NaN;
            foreach(var skill in skills)
            {
                if(!skill.isEnabled) continue;
                if(!cooldownReaders.TryGetValue(skill,out var reader))
                {
                    System.Reflection.PropertyInfo property=null;
                    for(var parent=skill.GetType();parent!=null;parent=parent.BaseType)
                        if(parent.IsGenericType && parent.GetGenericTypeDefinition()==typeof(ACooldownSkill<>))
                        { property=parent.GetProperty("cooldownProgress"); break; }
                    reader=property==null?null:(System.Func<float>)System.Delegate.CreateDelegate(typeof(System.Func<float>),skill,property.GetMethod);
                    cooldownReaders[skill]=reader;
                }
                if(reader==null) continue;
                float value=reader();
                if(float.IsFinite(value)) remaining=float.IsNaN(remaining)?Mathf.Clamp01(value):Mathf.Min(remaining,Mathf.Clamp01(value));
            }
            return remaining;
        }
        public void AdvancePresentation(Vector3 targetDirection,float remaining,float deltaTime)
        {
            if(presentation==null || collapsed) return;
            float dt=float.IsFinite(deltaTime)?Mathf.Max(0,deltaTime):0;
            Quaternion desired=Quaternion.identity;
            if(preset!=HLStonePreset.Boulder)
            {
                // A single rigid settle, triggered by visual Init (spawn), never an idle loop.
                settleAge+=dt;
                float angle=1.5f*Mathf.Sin(settleAge*5)*Mathf.Exp(-settleAge*2);
                desired=Quaternion.AngleAxis(angle,Vector3.forward);
            }
            else
            {
                targetDirection.y=0;
                if(targetDirection.sqrMagnitude>1e-6f)
                {
                    float anticipation=float.IsFinite(remaining)?1-Mathf.Clamp01(remaining/.3f):0;
                    Vector3 axis=bodyPivot.InverseTransformDirection(Vector3.Cross(Vector3.up,targetDirection.normalized));
                    desired=Quaternion.AngleAxis(7-21*anticipation,axis);
                }
            }
            presentation.localRotation=Quaternion.Slerp(presentation.localRotation,desired,1-Mathf.Exp(-8*dt));
        }
        public bool BeginDelivery(int token,HLDeliveryStyle style,Transform projectile,Vector3 intendedEnd)
        {
            if(!isActiveAndEnabled || collapsed || projectile==null || presentation==null || deliveries.ContainsKey(token)) return false;
            if(style!=HLDeliveryStyle.Thrown && style!=HLDeliveryStyle.Direct && style!=HLDeliveryStyle.Rigid) return false;
            var lease=HLStoneMeshCache.Acquire(HLStoneSeed.ForPart(seed,701),HLStonePresets.Shape(.15f,1.7f,.65f,.18f,0));
            var shard=new GameObject("HLThrownShard").transform;
            shard.SetParent(transform,false); shard.position=presentation.TransformPoint(assembly.LocalBounds.center);
            shard.gameObject.AddComponent<MeshFilter>().sharedMesh=lease.Mesh;
            var renderer=shard.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=stoneMaterial;
            var block=new MaterialPropertyBlock(); block.SetVector("_BaseColor",HLStoneAssembly.Palette[1].linear); renderer.SetPropertyBlock(block);
            deliveries.Add(token,(shard,projectile,lease));
            if(preset==HLStonePreset.Boulder)
            {
                Vector3 direction=intendedEnd-transform.position; direction.y=0;
                if(direction.sqrMagnitude>1e-6f) presentation.localRotation=Quaternion.AngleAxis(18,bodyPivot.InverseTransformDirection(Vector3.Cross(Vector3.up,direction.normalized)));
            }
            return true;
        }
        public void UpdateDelivery(int token,Vector3 projectilePosition)
        {
            if(!deliveries.TryGetValue(token,out var delivery)) return;
            Vector3 travel=projectilePosition-delivery.shard.position;
            if(travel.sqrMagnitude>1e-8f) delivery.shard.rotation=Quaternion.FromToRotation(Vector3.up,travel.normalized);
            delivery.shard.position=projectilePosition;
        }
        void FollowDeliveries()
        {
            endedDeliveries.Clear();
            foreach(var pair in deliveries)
                if(pair.Value.projectile==null || !pair.Value.projectile.gameObject.activeInHierarchy) endedDeliveries.Add(pair.Key);
                else UpdateDelivery(pair.Key,pair.Value.projectile.position);
            foreach(int token in endedDeliveries) EndDelivery(token);
        }
        public void ContactDelivery(int token,Vector3 contactPosition,GameObject target)
        {
            if(!deliveries.ContainsKey(token)) return;
            Effects()?.EmitThrownContact(contactPosition,HLStoneSeed.ForPart(seed,++hitIndex+801));
            EndDelivery(token);
        }
        public void EndDelivery(int token)
        {
            if(!deliveries.TryGetValue(token,out var delivery)) return;
            deliveries.Remove(token); delivery.shard.gameObject.SetActive(false);
            HLStoneMeshCache.DestroyOwned(delivery.shard.gameObject); delivery.lease.Dispose();
        }
        void ClearDeliveries()
        {
            endedDeliveries.Clear(); endedDeliveries.AddRange(deliveries.Keys);
            foreach(int token in endedDeliveries) EndDelivery(token);
        }
        void Bind()
        {
            if(bound || health==null || !isActiveAndEnabled) return;
            health.OnAllConsumerProcessed.AddListener(OnConsumersProcessed); health.OnValueChanged.AddListener(OnHealthChanged); bound=true;
        }
        void Unbind()
        {
            if(bound && health!=null) { health.OnAllConsumerProcessed.RemoveListener(OnConsumersProcessed); health.OnValueChanged.RemoveListener(OnHealthChanged); }
            if(life!=null) life.enabled=false; cooldownReaders.Clear(); ClearDeliveries(); bound=false; impacts.Clear(); sampler.Reset(); state.CompleteBatch(float.PositiveInfinity,1);
        }
        void SetVisible(bool value)
        {
            if(presentation!=null) presentation.gameObject.SetActive(value);
            if(groundShadow!=null) groundShadow.enabled=value;
        }
        void OnEnable() { SetVisible(true); if(life!=null) life.enabled=true; Bind(); }
        void OnDisable() { Unbind(); SetVisible(false); }
        void OnDestroy() { Unbind(); assembly.Dispose(); }
    }
}
