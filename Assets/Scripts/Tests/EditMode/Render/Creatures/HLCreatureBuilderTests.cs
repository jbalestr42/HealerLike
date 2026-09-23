using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{
    public class HLCreatureBuilderTests
    {
        public class HLSink : IHLHealVisualSink, IHLSpellVisualSink
        {
            public int heals;
            public int impacts;
            public GameObject target;
            public GameObject source;
            public float amount;
            public bool critical;

            public void OnHealResolved(GameObject target, float value, bool critical)
            {
                heals++;
                this.target = target;
                amount = value;
                this.critical = critical;
            }

            public void ShowImpact(GameObject source, GameObject target, HLResourceKind resource, float amount,
                bool critical)
            {
                impacts++;
                this.source = source;
                this.target = target;
                this.amount = amount;
                this.critical = critical;
            }

            public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
                float elapsed, float duration, HLClockKind clock)
            {
            }

            public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
            {
            }

            public void PulseArea(Vector3 center, float radius, HLZoneKind kind, float strength)
            {
            }
        }

        GameObject _owner;
        GameObject _model;
        GameObject _source;
        GameObject _target;
        HLCreatureRecipe _recipe;
        Material _material;
        Entity _entity;
        ResourceAttribute _health;
        HLCreatureBuilder _builder;
        HLRenderRegistry _registry;
        HLSink _sink;

        [SetUp]
        public void Setup()
        {
            _owner = new GameObject("HLEntityFixture");
            _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
            TestHelpers.WithLoggingDisabled(() => _entity = _owner.AddComponent<Entity>());
            TestHelpers.SetPrivateField(_entity, "_health", _health);
            _model = new GameObject("HLModel");
            _model.transform.SetParent(_owner.transform, false);
            EntityModel entityModel = _model.AddComponent<EntityModel>();
            _source = new GameObject("HLAuthoredSource");
            _source.transform.SetParent(_model.transform, false);
            _source.transform.localPosition = Vector3.up * 1.7f;
            _source.AddComponent<SkillSource>();
            _target = new GameObject("HLAuthoredTarget");
            _target.transform.SetParent(_model.transform, false);
            _target.transform.localPosition = Vector3.up;
            _target.AddComponent<SkillTargetPointTag>();
            _recipe = HLCreatureValidatorTests.Recipe();
            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _builder = _model.AddComponent<HLCreatureBuilder>();
            _builder.SetRecipe(_recipe, _material);
            _sink = new HLSink();
            _registry = new HLRenderRegistry { SpellSink = _sink };
            _registry.Register(_source, _sink);
            _builder.Configure(_registry, 1f, Vector3.zero, Vector3.up);
            entityModel.Init(_entity);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_builder)
            {
                TestHelpers.InvokePrivate(_builder, "OnDestroy");
            }

            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_material);
            HLPrimitiveMeshes.ReleaseAll();
        }

        [Test]
        public void DoubleInitKeepsOneRigAndListenerAndPreservesSourceCache()
        {
            HLCreatureRig rig = _builder.rig;
            int count = _model.GetComponentsInChildren<Transform>().Length;

            _builder.Init(_entity);
            _builder.Init(_entity);

            Assert.AreSame(rig, _builder.rig);
            Assert.AreEqual(count, _model.GetComponentsInChildren<Transform>().Length);
            Assert.AreSame(_source.GetComponent<SkillSource>(), _model.GetComponent<EntityModel>().GetSourcePoint());
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 20f, true);
            Assert.AreEqual(1, _sink.heals);
            Assert.AreEqual(1, _sink.impacts);
            Assert.AreSame(_owner, _sink.target);
            Assert.IsNull(rig.root.Find("HLHealMote"));
        }

        [Test]
        public void DisableEnableAndRebindDetachOldHealthExactlyOnce()
        {
            _builder.enabled = false;
            TestHelpers.InvokePrivate(_builder, "OnDisable");
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 10f, false);
            Assert.AreEqual(0, _sink.heals);
            _builder.enabled = true;
            TestHelpers.InvokePrivate(_builder, "OnEnable");
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 10f, false);
            Assert.AreEqual(1, _sink.heals);
            _builder.Init(null);
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 10f, false);
            Assert.AreEqual(1, _sink.heals);
        }

        [Test]
        public void DamageZeroOverhealAndSourcelessHealUseSignedOutcome()
        {
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 0f, false);
            Assert.AreEqual(0, _sink.impacts);
            Assert.AreEqual(0, _sink.heals);
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, -7f, true);
            Assert.AreEqual(1, _sink.impacts);
            Assert.AreEqual(0, _sink.heals);
            Assert.AreEqual(-7, _sink.amount);
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 200f, false);
            Assert.AreEqual(1, _sink.heals);
            Assert.AreEqual(200, _sink.amount);
            Assert.AreEqual(100, _health.Value);
            _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier(), 2f, false);
            Assert.IsNull(_builder.rig.root.Find("HLHealMote"));
        }

        [Test]
        public void DestroyedEntityStillUnregistersExternallyAnchoredBuilder()
        {
            _model.transform.SetParent(null);
            _registry.Unregister(_source, _sink);
            Object.DestroyImmediate(_owner);
            _builder.enabled = false;
            TestHelpers.InvokePrivate(_builder, "OnDisable");
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(HLRenderRegistry).GetField("_healSinks", flags);
            Assert.AreEqual(0, ((IDictionary)field.GetValue(_registry)).Count);
            TestHelpers.InvokePrivate(_builder, "OnDestroy");
            Object.DestroyImmediate(_model);
        }

        [Test]
        public void BreathingAndDragNeverRelocateAuthoredSockets()
        {
            Vector3 source = _source.transform.localPosition;
            Vector3 target = _target.transform.localPosition;

            _owner.transform.position = new Vector3(4f, 2f, 3f);
            TestHelpers.InvokePrivate(_builder, "LateUpdate");

            Assert.AreEqual(source, _source.transform.localPosition);
            Assert.AreEqual(target, _target.transform.localPosition);
            Assert.AreEqual(new Vector3(4f, 0f, 3f), _builder.rig.root.position);
            Assert.AreEqual(new Vector3(4f, 2f, 3f), _owner.transform.position);
        }

        [Test]
        public void PublicTargetsHealthAndLateAddedCooldownArePolled()
        {
            TargetProvider provider = null;
            TestHelpers.WithLoggingDisabled(() => provider = _owner.AddComponent<TargetProvider>());
            _target.transform.position = Vector3.right * 3f;
            TestHelpers.SetPrivateField(provider, "_targets", new List<GameObject> { _target });
            ShootProjectileSkill skill = _owner.AddComponent<ShootProjectileSkill>();
            TestHelpers.SetPrivateField(skill, "_cooldownDuration", new Attribute(2));
            skill.isEnabled = true;
            TestHelpers.SetPrivateField(_health, "_value", 25f);
            FieldInfo cooldown = typeof(ACooldownSkill<ShootProjectileSkillData>).GetField("_cooldown",
                BindingFlags.NonPublic | BindingFlags.Instance);

            TestHelpers.InvokePrivate(_builder, "LateUpdate");

            Assert.AreEqual(1, _builder.cooldownSkillCount);
            Assert.AreEqual(1, _builder.rig.charge);
            Assert.AreEqual(0.25f, _builder.rig.healthFraction);
            cooldown.SetValue(skill, 0.5f);
            TestHelpers.InvokePrivate(_builder, "LateUpdate");
            Assert.AreEqual(0.75f, _builder.rig.charge);
            cooldown.SetValue(skill, 2f);
            TestHelpers.InvokePrivate(_builder, "LateUpdate");
            Assert.AreEqual(0, _builder.rig.charge);
            Assert.IsFalse(_builder.BeginDelivery(200, HLDeliveryStyle.Thrown, null, Vector3.one));
            Assert.IsTrue(_builder.BeginDelivery(201, HLDeliveryStyle.Direct, null, Vector3.one));
            Assert.AreEqual(0, _builder.rig.charge);
            Object.DestroyImmediate(skill);
            TestHelpers.InvokePrivate(_builder, "LateUpdate");
            Assert.AreEqual(0, _builder.cooldownSkillCount);
            Assert.AreEqual(0, _builder.rig.charge);
            TestHelpers.SetPrivateField(_health, "_value", 100f);
            TestHelpers.InvokePrivate(_builder, "LateUpdate");
            Assert.AreEqual(1, _builder.rig.healthFraction);
        }
    }
}
