using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{

public class CreatureBuilderTests
{
    GameObject _owner;
    GameObject _model;
    GameObject _source;
    GameObject _target;
    CreatureRecipe _recipe;
    Material _material;
    Entity _entity;
    ResourceAttribute _health;
    CreatureBuilder _builder;
    StatusObserver _statusObserver;
    RenderRegistry _registry;
    RecordingSpellSink _spellSink;
    RecordingHealthSink _healthSink;
    readonly List<Object> _objects = new List<Object>();
    readonly List<CreatureBuilder> _views = new List<CreatureBuilder>();

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
        _recipe = RenderTestAssets.CreateRecipe();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        // Like the derived prefabs, the view carries the status observer that wires the outcome observers
        _statusObserver = _model.AddComponent<StatusObserver>();
        _builder = _model.AddComponent<CreatureBuilder>();
        RenderTestAssets.SetRecipe(_builder, _recipe, _material, RenderTestAssets.LoadMeshes());
        _spellSink = new RecordingSpellSink();
        _healthSink = new RecordingHealthSink();
        _registry = new RenderRegistry();
        _registry.Register(_source, _healthSink);
        TestHelpers.SetPrivateField(_builder, "_spellSink", _spellSink);
        TestHelpers.SetPrivateField(_builder, "_registry", _registry);
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

        foreach (CreatureBuilder view in _views)
        {
            TestHelpers.InvokePrivate(view, "OnDestroy");
        }

        _views.Clear();
        Object.DestroyImmediate(_owner);
        // A test can detach the model from its owner
        if (_model)
        {
            Object.DestroyImmediate(_model);
        }

        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }

        _objects.Clear();
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    [Test]
    public void TryGetAnchors_EveryHeadOnBothSides_PutsTheHeadAboveTheNeckAndOutsideTheBody()
    {
        foreach (LookSide side in System.Enum.GetValues(typeof(LookSide)))
        {
            foreach (HeadKind head in System.Enum.GetValues(typeof(HeadKind)))
            {
                CreatureRecipe recipe = LookComposer.Compose(
                    RenderTestAssets.CreateChannels(side, head),
                    RenderTestAssets.LoadLookVocabulary()
                );
                _objects.Add(recipe);
                RenderTestAssets.SetRecipe(_builder, recipe, _material, RenderTestAssets.LoadMeshes());
                _builder.Init(_entity);
                bool hasAnchors = _builder.TryGetAnchors(out EffectAnchors anchors);
                Assert.IsTrue(hasAnchors, $"{side} {head}");
                Assert.Greater(anchors.headCentre.y, anchors.neck.y, $"{side} {head}");
                Assert.Greater(
                    Vector3.Distance(anchors.headCentre, anchors.bodyCentre),
                    anchors.bodyRadius,
                    $"{side} {head}"
                );
                Assert.Greater(anchors.neck.y, anchors.foot.y, $"{side} {head}");
                string sourceId = CreatureSources.Select(_builder.rig, 0);
                Assert.IsTrue(CreatureSources.Resolve(_builder.rig, sourceId, out Vector3 outlet));
                Assert.Less(Vector3.Distance(outlet, anchors.castPoint), 0.00001f, $"{side} {head}");
            }
        }
    }

    [Test]
    public void Rebuild_RegisteredLivingView_KeepsOneListenerAndAuthoredSockets()
    {
        CreatureRig rig = _builder.rig;
        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(_builder.Rebuild(null));
        }

        Assert.AreSame(rig, _builder.rig);
        Assert.AreSame(_source.GetComponent<SkillSource>(), _model.GetComponent<EntityModel>().GetSourcePoint());
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 20f, true);
        Assert.AreEqual(1, _healthSink.healCount);
        Assert.AreEqual(1, _spellSink.impactCount);
        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void TryGetAnchors_BeforeTheRig_ReturnsFalse()
    {
        CreatureBuilder builder = _owner.AddComponent<CreatureBuilder>();
        bool hasAnchors = builder.TryGetAnchors(out _);
        Assert.IsFalse(hasAnchors);
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
        Assert.AreEqual(1, _healthSink.healCount);
        Assert.AreEqual(1, _spellSink.impactCount);
        Assert.AreSame(_owner, _spellSink.lastTarget);
        Assert.IsNull(rig.root.Find("HealMote"));
    }

    [Test]
    public void OnDisable_ThenEnabledAndInitWithoutEntity_DetachesOldHealthOnce()
    {
        _builder.enabled = false;
        _statusObserver.enabled = false;
        TestHelpers.InvokePrivate(_builder, "OnDisable");
        TestHelpers.InvokePrivate(_statusObserver, "OnDisable");
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 10f, false);
        Assert.AreEqual(0, _healthSink.healCount);
        _builder.enabled = true;
        _statusObserver.enabled = true;
        TestHelpers.InvokePrivate(_builder, "OnEnable");
        TestHelpers.InvokePrivate(_statusObserver, "OnEnable");
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 10f, false);
        Assert.AreEqual(1, _healthSink.healCount);
        _builder.Init(null);
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 10f, false);
        Assert.AreEqual(1, _healthSink.healCount);
    }

    [Test]
    public void Init_DamageZeroOverhealSourceless_ReportsSignedOutcome()
    {
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 0f, false);
        Assert.AreEqual(0, _spellSink.impactCount);
        Assert.AreEqual(0, _healthSink.healCount);
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, -7f, true);
        Assert.AreEqual(1, _spellSink.impactCount);
        Assert.AreEqual(0, _healthSink.healCount);
        Assert.AreEqual(-7, _spellSink.lastAmount);
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier { source = _source }, 200f, false);
        Assert.AreEqual(1, _healthSink.healCount);
        Assert.AreEqual(200, _spellSink.lastAmount);
        Assert.AreEqual(100, _health.Value);
        _health.OnAllConsumerProcessed.Invoke(_owner, new ResourceModifier(), 2f, false);
        Assert.IsNull(_builder.rig.root.Find("HealMote"));
    }

    [Test]
    public void OnDisable_EntityDestroyed_StillUnregistersBuilder()
    {
        _model.transform.SetParent(null);
        _registry.Unregister(_source, _healthSink);
        Object.DestroyImmediate(_owner);
        _builder.enabled = false;
        TestHelpers.InvokePrivate(_builder, "OnDisable");
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo field = typeof(RenderRegistry).GetField("_healthSinks", flags);
        Assert.AreEqual(0, ((IDictionary)field.GetValue(_registry)).Count);
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
        Assert.AreEqual(new Vector3(4f, 2f, 3f), _builder.rig.root.position); // the rig stands where its view is
        Assert.AreEqual(new Vector3(4f, 2f, 3f), _owner.transform.position);
    }

    [Test]
    public void InitAndLateUpdate_AssignedCameraTurnsOnlyTheGeneratedCreature()
    {
        GameObject managerObject = new GameObject("Facing manager");
        _objects.Add(managerObject);
        RenderManager manager = managerObject.AddComponent<RenderManager>();
        GameObject cameraObject = new GameObject("Assigned facing camera");
        _objects.Add(cameraObject);
        Camera camera = cameraObject.AddComponent<Camera>();
        TestHelpers.SetPrivateField(manager, "_gameCamera", camera);
        Vector3 source = _source.transform.localPosition;
        Vector3 target = _target.transform.localPosition;
        Quaternion ownerRotation = Quaternion.Euler(0f, 23f, 0f);
        _owner.transform.rotation = ownerRotation;
        camera.transform.rotation = Quaternion.Euler(52f, 90f, 0f);
        _builder.Init(_entity, manager);
        foreach (float yaw in new[] { 90f, -45f, 170f })
        {
            camera.transform.rotation = Quaternion.Euler(52f, yaw, 0f);
            TestHelpers.InvokePrivate(_builder, "LateUpdate");
            Vector3 facing = Vector3.ProjectOnPlane(_builder.rig.armRotation * Vector3.forward, Vector3.up);
            Vector3 toViewer = Vector3.ProjectOnPlane(-camera.transform.forward, Vector3.up);
            Assert.That(Vector3.Angle(facing, toViewer), Is.LessThan(24f));
            Assert.That(Quaternion.Angle(ownerRotation, _owner.transform.rotation), Is.LessThan(0.001f));
            Assert.AreEqual(source, _source.transform.localPosition);
            Assert.AreEqual(target, _target.transform.localPosition);
            Assert.AreEqual(100f, _health.Value);
            Assert.IsTrue(_builder.Rebuild(manager));
        }
    }

    [Test]
    public void BeginDelivery_ThrownOrDirect_ClaimsOnlyWhatAnArmDraws()
    {
        bool isThrownClaimed = _builder.BeginDelivery(200, DeliveryStyle.Thrown, null, Vector3.one);
        bool isDirectClaimed = _builder.BeginDelivery(201, DeliveryStyle.Direct, null, Vector3.one);
        Assert.IsFalse(isThrownClaimed);
        Assert.IsTrue(isDirectClaimed);
    }

    [Test]
    public void Init_ManagerOnly_TakesMeshesFromManager()
    {
        GameObject managerGo = new GameObject("RenderManager");
        _objects.Add(managerGo);
        RenderManager manager = managerGo.AddComponent<RenderManager>();
        TestHelpers.SetPrivateField(manager, "_meshes", RenderTestAssets.LoadMeshes());
        GameObject viewGo = new GameObject("View");
        viewGo.transform.SetParent(_model.transform, false);
        CreatureBuilder view = viewGo.AddComponent<CreatureBuilder>();
        _views.Add(view);
        RenderTestAssets.SetRecipe(view, _recipe, _material, null);
        view.Init(_entity, manager);
        Assert.NotNull(view.rig);
    }
}
}
