using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneDynamicsTests
    {
        GameObject root,projectile,fxRoot;
        HLStoneEnemyVisual visual;
        HLStoneEffects fx;
        ResourceAttribute health;
        [SetUp] public void SetUp()
        {
            root=new GameObject("HLRoot"); projectile=new GameObject("HLProjectile"); fxRoot=new GameObject("HLFX");
            health=TestHelpers.CreateResourceAttribute(root,AttributeType.HealthMax,100);
            fx=fxRoot.AddComponent<HLStoneEffects>(); visual=root.AddComponent<HLStoneEnemyVisual>(); visual.Initialize(health,17,fx);
        }
        [TearDown] public void TearDown() { TestHelpers.InvokePrivate(fx,"OnDestroy"); Object.DestroyImmediate(root); Object.DestroyImmediate(projectile); Object.DestroyImmediate(fxRoot); }
        [Test] public void AimAnticipationAndLaunchRotateOnlyPresentation()
        {
            var pivot=visual.Parts[0].Transform.parent;
            visual.AdvancePresentation(Vector3.forward,1,1);
            Assert.Greater((pivot.rotation*Vector3.up).z,0);
            visual.AdvancePresentation(Vector3.forward,0,1);
            Assert.Less((pivot.rotation*Vector3.up).z,0);
            Assert.True(visual.BeginDelivery(1,HLDeliveryStyle.Thrown,projectile.transform,Vector3.forward*5));
            Assert.Greater((pivot.rotation*Vector3.up).z,0);
            Assert.AreEqual(Quaternion.identity,root.transform.rotation);
            Assert.AreEqual(Quaternion.identity,pivot.parent.localRotation);
            Assert.AreEqual(Vector3.zero,root.transform.position);
        }
        [TestCase(HLStonePreset.Cairn)] [TestCase(HLStonePreset.Monolith)]
        public void RigidPresetsIgnoreTargetCooldownAndLaunch(HLStonePreset preset)
        {
            TestHelpers.SetPrivateField(visual,"preset",preset); visual.Initialize(health,17,fx);
            var pivot=visual.Parts[0].Transform.parent;
            visual.AdvancePresentation(Vector3.forward,0,.2f); var first=pivot.localRotation;
            visual.Initialize(health,17,fx); visual.AdvancePresentation(Vector3.left,1,.2f);
            Assert.AreEqual(first,pivot.localRotation);
            visual.BeginDelivery(1,HLDeliveryStyle.Direct,projectile.transform,Vector3.back);
            Assert.AreEqual(first,pivot.localRotation);
            visual.AdvancePresentation(Vector3.forward,0,10);
            Assert.Less(Quaternion.Angle(Quaternion.identity,pivot.localRotation),.001f);
        }
        [TestCase(HLDeliveryStyle.Thrown)] [TestCase(HLDeliveryStyle.Direct)] [TestCase(HLDeliveryStyle.Rigid)]
        public void DeliveryFollowsContactsAndCleansUp(HLDeliveryStyle style)
        {
            Assert.True(visual.BeginDelivery(1,style,projectile.transform,Vector3.forward));
            Assert.False(visual.BeginDelivery(1,style,projectile.transform,Vector3.forward));
            projectile.transform.position=new Vector3(4,3,2); TestHelpers.InvokePrivate(visual,"LateUpdate");
            Assert.AreEqual(projectile.transform.position,root.transform.Find("HLThrownShard").position);
            visual.ContactDelivery(1,Vector3.one*7,null); Assert.AreEqual(0,visual.LiveDeliveryCount);
            Assert.That(fx.LiveCount,Is.InRange(8,10));
            foreach(var filter in fxRoot.GetComponentsInChildren<MeshFilter>()) Assert.AreEqual(Vector3.one*7,filter.transform.position);
            int count=fx.LiveCount; visual.ContactDelivery(1,Vector3.zero,null); visual.EndDelivery(1); Assert.AreEqual(count,fx.LiveCount);
        }
        [Test] public void UnsupportedStylesAndDisableDoNotLeak()
        {
            foreach(var style in new[]{HLDeliveryStyle.Arc,HLDeliveryStyle.Swarm,HLDeliveryStyle.Bounce,HLDeliveryStyle.ChainSync})
                Assert.False(visual.BeginDelivery(1,style,projectile.transform,Vector3.one));
            visual.BeginDelivery(1,HLDeliveryStyle.Thrown,projectile.transform,Vector3.one);
            visual.BeginDelivery(2,HLDeliveryStyle.Thrown,projectile.transform,Vector3.one);
            visual.enabled=false; TestHelpers.InvokePrivate(visual,"OnDisable"); Assert.AreEqual(0,visual.LiveDeliveryCount);
            Assert.AreEqual(0,fx.LiveCount);
        }
        [Test] public void LateUpdatePollsPublicSkillCooldownAndFirstTarget()
        {
            Entity owner=null; TargetProvider provider=null;
            TestHelpers.WithLoggingDisabled(()=> { owner=root.AddComponent<Entity>(); provider=root.AddComponent<TargetProvider>(); });
            TestHelpers.SetPrivateField(owner,"_health",health);
            TestHelpers.SetPrivateField(provider,"_targets",new System.Collections.Generic.List<GameObject>{projectile});
            projectile.transform.position=Vector3.forward*8;
            visual.Init(owner);
            // Entity creates skills after model Init. Verify they are discovered on a later poll.
            var skill=root.AddComponent<ShootProjectileSkill>();
            TestHelpers.SetPrivateField(skill,"_cooldownDuration",new Attribute(1)); skill.isEnabled=true;
            var cooldown=typeof(ACooldownSkill<ShootProjectileSkillData>).GetField("_cooldown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            cooldown.SetValue(skill,1f);
            TestHelpers.InvokePrivate(visual,"LateUpdate");
            var read=typeof(HLStoneEnemyVisual).GetMethod("ReadCooldown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(1f,(float)read.Invoke(visual,null));
            cooldown.SetValue(skill,.05f); Assert.AreEqual(.05f,(float)read.Invoke(visual,null));
            skill.isEnabled=false; Assert.True(float.IsNaN((float)read.Invoke(visual,null)));
            skill.isEnabled=true; TestHelpers.SetPrivateField(owner,"_isDraggable",true);
            Assert.True(float.IsNaN((float)read.Invoke(visual,null)));
        }
        [Test] public void DestroyedProjectileIsReleasedWithoutContact()
        {
            visual.BeginDelivery(1,HLDeliveryStyle.Thrown,projectile.transform,Vector3.one);
            Object.DestroyImmediate(projectile); TestHelpers.InvokePrivate(visual,"LateUpdate");
            Assert.AreEqual(0,visual.LiveDeliveryCount); Assert.AreEqual(0,fx.LiveCount);
        }
    }
}
