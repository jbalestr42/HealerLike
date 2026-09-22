using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Creatures
{
    public class HLCreatureBuilderTests
    {
        GameObject owner, model, source, target;
        HLCreatureRecipe recipe;
        Material material;
        Entity entity;
        ResourceAttribute health;
        HLCreatureBuilder builder;
        HLRenderRegistry registry;
        HLSink sink;
        internal sealed class HLSink : IHLHealVisualSink, IHLSpellVisualSink
        {
            public int heals, impacts; public GameObject target, source; public float amount; public bool critical;
            public void OnHealResolved(GameObject target, float value, bool critical) { heals++; this.target = target; amount = value; this.critical = critical; }
            public void ShowImpact(GameObject source, GameObject target, HLResourceKind resource, float amount, bool critical) { impacts++; this.source = source; this.target = target; this.amount = amount; this.critical = critical; }
            public void SetStatus(GameObject s, GameObject t, ABuffHandlerFactory f, int stacks, float e, float d, HLClockKind c) { }
            public void RemoveStatus(GameObject s, GameObject t, ABuffHandlerFactory f) { }
            public void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength) { }
        }
        [SetUp] public void Setup()
        {
            owner = new GameObject("HLEntityFixture"); health = TestHelpers.CreateResourceAttribute(owner, AttributeType.HealthMax, 100);
            TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>()); TestHelpers.SetPrivateField(entity, "_health", health);
            model = new GameObject("HLModel"); model.transform.SetParent(owner.transform, false); var entityModel = model.AddComponent<EntityModel>();
            source = new GameObject("HLAuthoredSource"); source.transform.SetParent(model.transform, false); source.transform.localPosition = Vector3.up * 1.7f; source.AddComponent<SkillSource>();
            target = new GameObject("HLAuthoredTarget"); target.transform.SetParent(model.transform, false); target.transform.localPosition = Vector3.up; target.AddComponent<SkillTargetPointTag>();
            recipe = HLCreatureValidatorTests.Recipe(); material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            builder = model.AddComponent<HLCreatureBuilder>(); builder.SetRecipe(recipe, material);
            sink = new HLSink(); registry = new HLRenderRegistry { SpellSink = sink }; registry.Register(source, sink);
            builder.Configure(registry, 1, Vector3.zero, Vector3.up); entityModel.Init(entity);
        }
        [TearDown] public void Cleanup() { if (builder) TestHelpers.InvokePrivate(builder, "OnDestroy"); Object.DestroyImmediate(owner); Object.DestroyImmediate(recipe); Object.DestroyImmediate(material); HLPrimitiveMeshes.ReleaseAll(); }
        [Test] public void DoubleInitKeepsOneRigAndListenerAndPreservesSourceCache()
        {
            var rig = builder.Rig; int count = model.GetComponentsInChildren<Transform>().Length;
            builder.Init(entity); builder.Init(entity); Assert.AreSame(rig, builder.Rig); Assert.AreEqual(count, model.GetComponentsInChildren<Transform>().Length);
            Assert.AreSame(source.GetComponent<SkillSource>(), model.GetComponent<EntityModel>().GetSourcePoint());
            health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, 20, true);
            Assert.AreEqual(1, sink.heals); Assert.AreEqual(1, sink.impacts); Assert.AreSame(owner, sink.target); Assert.IsNull(rig.Root.Find("HLHealMote"));
        }
        [Test] public void DisableEnableAndRebindDetachOldHealthExactlyOnce()
        {
            builder.enabled = false; TestHelpers.InvokePrivate(builder, "OnDisable"); health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, 10, false); Assert.AreEqual(0, sink.heals);
            builder.enabled = true; TestHelpers.InvokePrivate(builder, "OnEnable"); health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, 10, false); Assert.AreEqual(1, sink.heals);
            builder.Init(null); health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, 10, false); Assert.AreEqual(1, sink.heals);
        }
        [Test] public void DamageZeroOverhealAndSourcelessHealUseSignedOutcome()
        {
            health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, 0, false);
            Assert.AreEqual(0, sink.impacts); Assert.AreEqual(0, sink.heals);
            health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, -7, true);
            Assert.AreEqual(1, sink.impacts); Assert.AreEqual(0, sink.heals); Assert.AreEqual(-7, sink.amount);
            health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier { source = source }, 200, false);
            Assert.AreEqual(1, sink.heals); Assert.AreEqual(200, sink.amount); Assert.AreEqual(100, health.Value);
            health.OnAllConsumerProcessed.Invoke(owner, new ResourceModifier(), 2, false); Assert.IsNull(builder.Rig.Root.Find("HLHealMote"));
        }
        [Test] public void DestroyedEntityStillUnregistersExternallyAnchoredBuilder()
        {
            model.transform.SetParent(null);
            registry.Unregister(source, sink);
            Object.DestroyImmediate(owner);
            builder.enabled = false; TestHelpers.InvokePrivate(builder, "OnDisable");
            var field = typeof(HLRenderRegistry).GetField("_healSinks",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual(0, ((System.Collections.IDictionary)field.GetValue(registry)).Count);
            TestHelpers.InvokePrivate(builder, "OnDestroy");
            Object.DestroyImmediate(model);
        }
        [Test] public void BreathingAndDragNeverRelocateAuthoredSockets()
        {
            Vector3 s = source.transform.localPosition, t = target.transform.localPosition;
            owner.transform.position = new Vector3(4, 2, 3); TestHelpers.InvokePrivate(builder, "LateUpdate");
            Assert.AreEqual(s, source.transform.localPosition); Assert.AreEqual(t, target.transform.localPosition);
            Assert.AreEqual(new Vector3(4, 0, 3), builder.Rig.Root.position); Assert.AreEqual(new Vector3(4, 2, 3), owner.transform.position);
        }
        [Test] public void PublicTargetsHealthAndLateAddedCooldownArePolled()
        {
            TargetProvider provider = null; TestHelpers.WithLoggingDisabled(() => provider = owner.AddComponent<TargetProvider>());
            target.transform.position = Vector3.right * 3;
            TestHelpers.SetPrivateField(provider, "_targets", new System.Collections.Generic.List<GameObject> { target });
            var skill = owner.AddComponent<ShootProjectileSkill>();
            TestHelpers.SetPrivateField(skill, "_cooldownDuration", new Attribute(2));
            skill.isEnabled = true;
            TestHelpers.SetPrivateField(health, "_value", 25f);
            TestHelpers.InvokePrivate(builder, "LateUpdate");
            Assert.AreEqual(1, builder.CooldownSkillCount); Assert.AreEqual(1, builder.Rig.Charge);
            Assert.AreEqual(.25f, builder.Rig.HealthFraction);
            typeof(ACooldownSkill<ShootProjectileSkillData>).GetField("_cooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(skill, .5f);
            TestHelpers.InvokePrivate(builder, "LateUpdate"); Assert.AreEqual(.75f, builder.Rig.Charge);
            typeof(ACooldownSkill<ShootProjectileSkillData>).GetField("_cooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(skill, 2f);
            TestHelpers.InvokePrivate(builder, "LateUpdate"); Assert.AreEqual(0, builder.Rig.Charge);
            Assert.IsFalse(builder.BeginDelivery(200, HLDeliveryStyle.Thrown, null, Vector3.one));
            Assert.IsTrue(builder.BeginDelivery(201, HLDeliveryStyle.Direct, null, Vector3.one));
            Assert.AreEqual(0, builder.Rig.Charge);
            Object.DestroyImmediate(skill); TestHelpers.InvokePrivate(builder, "LateUpdate");
            Assert.AreEqual(0, builder.CooldownSkillCount); Assert.AreEqual(0, builder.Rig.Charge);
            TestHelpers.SetPrivateField(health, "_value", 100f); TestHelpers.InvokePrivate(builder, "LateUpdate");
            Assert.AreEqual(1, builder.Rig.HealthFraction);
        }
    }
}
