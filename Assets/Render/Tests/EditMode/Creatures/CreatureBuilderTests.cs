using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{

public class CreatureBuilderTests
{
    public class Sink : IHealVisualSink, ISpellVisualSink
    {
        public int heals;
        public int impacts;
        public GameObject target;
        public GameObject source;
        public float amount;
        public bool critical;

        // Like every heal sink, only the positive changes are heals
        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (value <= 0f)
            {
                return;
            }

            heals++;
            this.target = target;
            amount = value;
            this.critical = critical;
        }

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float amount,
            bool critical)
        {
            impacts++;
            this.source = source;
            this.target = target;
            this.amount = amount;
            this.critical = critical;
        }

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
            float elapsed, float duration, ClockKind clock)
        {
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
        }

        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
        {
        }
    }

    GameObject _owner;
    GameObject _model;
    GameObject _source;
    GameObject _target;
    CreatureRecipe _recipe;
    Material _material;
    Entity _entity;
    ResourceAttribute _health;
    CreatureBuilder _builder;
    RenderRegistry _registry;
    Sink _sink;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("EntityFixture");
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        TestHelpers.WithLoggingDisabled(() => _entity = _owner.AddComponent<Entity>());
        TestHelpers.SetPrivateField(_entity, "_health", _health);
        _model = new GameObject("Model");
        _model.transform.SetParent(_owner.transform, false);
        EntityModel entityModel = _model.AddComponent<EntityModel>();
        _source = new GameObject("AuthoredSource");
        _source.transform.SetParent(_model.transform, false);
        _source.transform.localPosition = Vector3.up * 1.7f;
        _source.AddComponent<SkillSource>();
        _target = new GameObject("AuthoredTarget");
        _target.transform.SetParent(_model.transform, false);
        _target.transform.localPosition = Vector3.up;
        _target.AddComponent<SkillTargetPointTag>();
        _recipe = CreatureValidatorTests.Recipe();
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"));
        _builder = _model.AddComponent<CreatureBuilder>();
        _builder.SetRecipe(_recipe, _material, PrimitiveMeshesTests.Meshes());
        _sink = new Sink();
        _registry = new RenderRegistry { spellSink = _sink };
        _registry.Register(_source, _sink);
        _builder.Configure(_registry, 1f, Vector3.zero, Vector3.up);
        entityModel.Init(_entity);
        _builder.Init(_entity);
    }

    [TearDown]
    public void TearDown()
    {
        if (_builder)
        {
            TestHelpers.InvokePrivate(_builder, "OnDestroy");
        }

        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [Test]
    public void Init_CalledAgain_KeepsOneRigListenerAndSourceCache()
    {
        CreatureRig rig = _builder.rig;
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
        Assert.IsNull(rig.root.Find("HealMote"));
    }

    [Test]
    public void OnDisableAndRebind_HealthEvent_DetachesOldHealthOnce()
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
    public void OnAllConsumerProcessed_DamageZeroOverhealSourceless_ReportsSignedOutcome()
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
        Assert.IsNull(_builder.rig.root.Find("HealMote"));
    }

    [Test]
    public void OnDisable_EntityDestroyed_StillUnregistersBuilder()
    {
        _model.transform.SetParent(null);
        _registry.Unregister(_source, _sink);
        Object.DestroyImmediate(_owner);
        _builder.enabled = false;
        TestHelpers.InvokePrivate(_builder, "OnDisable");
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo field = typeof(RenderRegistry).GetField("_healSinks", flags);
        Assert.AreEqual(0, ((IDictionary)field.GetValue(_registry)).Count);
        TestHelpers.InvokePrivate(_builder, "OnDestroy");
        Object.DestroyImmediate(_model);
    }

    [Test]
    public void LateUpdate_OwnerMoved_KeepsAuthoredSockets()
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
    public void LateUpdate_LateAddedCooldownSkill_PollsChargeAndHealth()
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
        Assert.IsFalse(_builder.BeginDelivery(200, DeliveryStyle.Thrown, null, Vector3.one));
        Assert.IsTrue(_builder.BeginDelivery(201, DeliveryStyle.Direct, null, Vector3.one));
        Assert.AreEqual(0, _builder.rig.charge);
        Object.DestroyImmediate(skill);
        TestHelpers.InvokePrivate(_builder, "LateUpdate");
        Assert.AreEqual(0, _builder.cooldownSkillCount);
        Assert.AreEqual(0, _builder.rig.charge);
        TestHelpers.SetPrivateField(_health, "_value", 100f);
        TestHelpers.InvokePrivate(_builder, "LateUpdate");
        Assert.AreEqual(1, _builder.rig.healthFraction);
    }

    [Test]
    public void Configure_NonfiniteGround_LogsAndKeepsFrame()
    {
        CreatureRig rig = _builder.rig;
        LogAssert.Expect(LogType.Error, "[CreatureBuilder] Invalid ground frame.");

        _builder.Configure(_registry, float.NaN, Vector3.zero, Vector3.up);

        Assert.AreSame(rig, _builder.rig);
    }

    [Test]
    public void Init_ManagerOnly_TakesMeshesFromManager()
    {
        GameObject managerGo = new GameObject("RenderManager");
        RenderManager manager = managerGo.AddComponent<RenderManager>();
        TestHelpers.SetPrivateField(manager, "_meshes", PrimitiveMeshesTests.Meshes());
        GameObject viewGo = new GameObject("View");
        viewGo.transform.SetParent(_model.transform, false);
        CreatureBuilder view = viewGo.AddComponent<CreatureBuilder>();
        view.SetRecipe(_recipe, _material, null);

        view.Init(_entity, manager);

        Assert.NotNull(view.rig);
        TestHelpers.InvokePrivate(view, "OnDestroy");
        Object.DestroyImmediate(managerGo);
    }
}

}
