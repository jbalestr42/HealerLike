using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{

public class OffscreenPlayerTests
{
    readonly List<Object> _owned = new List<Object>();
    GameObject _source;
    Character _character;
    CharacterView _view;
    Camera _camera;
    GameObject _target;
    SpellVisualSink _sink;
    RenderManager _manager;

    GameObject Make(string name)
    {
        GameObject go = new GameObject(name);
        _owned.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        _source = Make("Character");
        TestHelpers.WithLoggingDisabled(() => _character = _source.AddComponent<Character>());
        _camera = Make("Camera").AddComponent<Camera>();
        Frame(90f, 9f / 16f, Vector3.zero);
        GameObject host = Make("RenderManager");
        _manager = host.AddComponent<RenderManager>();
        _sink = SpellSinkFixture.Add(host);
        TestHelpers.SetPrivateField(_manager, "_gameCamera", _camera);
        TestHelpers.SetPrivateField(_manager, "_spellSink", _sink);
        TestHelpers.SetPrivateField(_manager, "_meshes", RenderTestAssets.LoadMeshes());
        TestHelpers.SetPrivateField(_manager, "_deliveryVocabulary", RenderTestAssets.LoadDeliveryVocabulary());
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Render/Creatures/Prefabs/HealerCharacter.prefab"
        );
        _view = Object.Instantiate(prefab, _source.transform).GetComponent<CharacterView>();
        _view.Init(_character, _manager);
        _target = Make("Target");
        _target.transform.position = new Vector3(5f, 0.5f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        // EditMode-created behaviours have not necessarily received Awake, so Unity may omit OnDestroy.
        // FreeShot's holder lives outside the logical projectile and must be released explicitly.
        foreach (Object owned in _owned)
        {
            if (owned is GameObject root && root)
            {
                foreach (FreeShot shot in root.GetComponentsInChildren<FreeShot>(true))
                {
                    TestHelpers.InvokePrivate(shot, "OnDestroy");
                }
            }
        }

        TestHelpers.InvokePrivate(_sink, "OnDestroy");
        TestHelpers.InvokePrivate(_view, "OnDestroy");
        foreach (Object owned in _owned)
        {
            if (owned)
            {
                Object.DestroyImmediate(owned);
            }
        }

        _owned.Clear();
    }

    void Frame(float yaw, float aspect, Vector3 offset)
    {
        _camera.aspect = aspect;
        _camera.fieldOfView = 40f;
        Quaternion rotation = Quaternion.Euler(52f, yaw, 0f);
        _camera.transform.SetPositionAndRotation(offset - rotation * Vector3.forward * 24f, rotation);
    }

    [TestCase(90f, 0.5625f)]
    [TestCase(0f, 1.7777778f)]
    [TestCase(135f, 0.5625f)]
    public void Anchors_CameraRotatesOrFocuses_StayBelowBottomCentreWithoutMovingCharacter(float yaw, float aspect)
    {
        Vector3 owner = _source.transform.position;
        Frame(yaw, aspect, new Vector3(5f, 0.5f, 2f));
        Assert.IsTrue(_view.TryGetAnchors(out EffectAnchors anchors));
        Vector3 projected = _camera.WorldToViewportPoint(anchors.castPoint);
        Assert.That(projected.x, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(projected.y, Is.EqualTo(-0.08f).Within(0.0001f));
        Assert.Greater(projected.z, 0f);
        Assert.That(anchors.castPoint.y, Is.EqualTo(1.5f).Within(0.0001f));
        Assert.AreEqual(owner, _source.transform.position);
        Assert.IsNull(_view.rig);
        Assert.IsEmpty(_view.GetComponentsInChildren<Renderer>(true));
    }

    [Test]
    public void SingleHealthOutcome_DrawsLinkFromScreenAndKeepsItThereWhenCameraMoves()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100f);
        ResourceOutcomeObserver observer = _target.AddComponent<ResourceOutcomeObserver>();
        observer.Init(health, null, _sink, _manager.registry);
        health.OnAllConsumerProcessed.Invoke(_target, new ResourceModifier { source = _source }, 8f, false);
        _sink.Tick();
        Assert.AreEqual(2, _sink.impactCount, "One target impact and one incoming cast link.");
        SpellEffect link = Link();
        Assert.NotNull(link);
        Frame(90f, 9f / 16f, Vector3.right * 6f);
        link.Advance(0f);
        Vector3 start = (Vector3)
            typeof(SpellEffect)
                .GetField("_linkStart", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(link);
        Assert.That(_camera.WorldToViewportPoint(start).y, Is.EqualTo(-0.08f).Within(0.0001f));
    }

    SpellEffect Link()
    {
        foreach (SpellEffect effect in _sink.GetComponentsInChildren<SpellEffect>())
        {
            if (effect.recipe.socket == EffectSocket.Link)
            {
                return effect;
            }
        }

        return null;
    }

    [Test]
    public void StatusCast_PublishesOnApplicationAndStackIncreaseWithoutRepeatingOnClockUpdates()
    {
        BuffHandlerFactory factory = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _owned);
        _sink.SetStatus(_source, _target, factory, 1, 0f, 8f);
        Assert.AreEqual(1, _sink.impactCount);
        for (int i = 1; i < 10; i++)
        {
            _sink.SetStatus(_source, _target, factory, 1, i * 0.1f, 8f);
        }

        Assert.AreEqual(1, _sink.impactCount);
        _sink.SetStatus(_source, _target, factory, 2, 0f, 8f);
        Assert.AreEqual(2, _sink.impactCount);
        _sink.SetStatus(_source, _target, factory, 1, 1f, 8f);
        Assert.AreEqual(2, _sink.impactCount);
        _sink.RemoveStatus(_source, _target, factory);
        _sink.SetStatus(_source, _target, factory, 1, 0f, 8f);
        Assert.AreEqual(3, _sink.impactCount);
    }

    [Test]
    public void BaneStatusCast_UsesTheLitPaletteTintForItsEntryLinkOnly()
    {
        BuffHandlerFactory factory = SpellSinkFixture.Modifier(AttributeType.Damage, -2f, _owned);
        _sink.SetStatus(_source, _target, factory, 1, 0f, 8f);
        SpellEffect status = _sink.GetStatus(_target, factory).GetComponent<SpellEffect>();
        SpellEffect link = Link();
        Assert.AreEqual(EffectFamily.Bane, status.recipe.family);
        Assert.AreEqual(EffectFamily.Bane, link.recipe.family);
        Assert.AreEqual(status.recipe.palette.bane, status.recipe.colour);
        Assert.AreEqual(status.recipe.palette.baneLit, link.recipe.colour);
        SpellEffect contact = _sink.ShowContactLink(Vector3.zero, Vector3.one);
        Assert.AreEqual(contact.recipe.palette.damage, contact.recipe.colour);
    }

    [Test]
    public void HiddenCharacter_OwnManaAndStatuses_DoNotLeaveFloatingPresentation()
    {
        BuffHandlerFactory factory = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _owned);
        _sink.SetStatus(_source, _source, factory, 1, 0f, 8f);
        _sink.ShowImpact(_source, _source, ResourceKind.Mana, 8f, false);
        _sink.SetCharges(_source, 3f);
        Assert.AreEqual(0, _sink.statusCount);
        Assert.AreEqual(0, _sink.impactCount);
        _manager.registry.NotifyHealth(_source, _target, 10f, false);
        Assert.IsNull(_view.rig);
    }

    [TestCase(DeliveryStyle.Direct)]
    [TestCase(DeliveryStyle.Thrown)]
    public void PlayerProjectile_MapsVisualEntryAndLandsWithoutChangingLogicalTrajectory(DeliveryStyle style)
    {
        Entity target = null;
        TestHelpers.WithLoggingDisabled(() => target = _target.AddComponent<Entity>());
        TestHelpers.SetPrivateField(target, "_targetPoint", _target);
        GameObject go = Make("Projectile");
        go.AddComponent<LineRenderer>();
        Projectile projectile = go.AddComponent<Projectile>();
        ProjectileVisualObserver observer = go.AddComponent<ProjectileVisualObserver>();
        observer.Init(_manager, style);
        projectile.Init(_source, _target, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
        FreeShot shot = go.GetComponent<FreeShot>();
        Assert.IsTrue(shot && shot.enabled && shot.fromScreen);
        Assert.AreEqual(Vector3.zero, projectile.transform.position);
        Assert.That(_camera.WorldToViewportPoint(shot.visualPosition).y, Is.EqualTo(-0.08f).Within(0.0001f));
        Assert.IsFalse(go.GetComponent<LineRenderer>().enabled);
        go.transform.position = _target.transform.position * 0.5f;
        TestHelpers.InvokePrivate(shot, "LateUpdate");
        Assert.AreEqual(_target.transform.position * 0.5f, go.transform.position);
        Assert.AreNotEqual(go.transform.position, shot.visualPosition);
        go.transform.position = _target.transform.position;
        TestHelpers.InvokePrivate(shot, "LateUpdate");
        Assert.That(Vector3.Distance(go.transform.position, shot.visualPosition), Is.LessThan(0.0001f));
        projectile.OnHit.Invoke(new OnHitData { target = _target });
        Assert.IsFalse(shot.fromScreen);
    }
}
}
