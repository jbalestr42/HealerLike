using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneDynamicsTests
{
    static StoneEffects CreateEffects()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
        return Object.Instantiate(prefab).GetComponent<StoneEffects>();
    }

    static StoneEnemyVisual CreateVisual(GameObject target)
    {
        Transform pivot = new GameObject("BodyPivot").transform;
        pivot.SetParent(target.transform, false);
        Transform presentation = new GameObject("StonePresentation").transform;
        presentation.SetParent(pivot, false);
        StoneEnemyVisual visual = target.AddComponent<StoneEnemyVisual>();
        TestHelpers.SetPrivateField(visual, "_bodyPivot", pivot);
        TestHelpers.SetPrivateField(visual, "_presentation", presentation);
        return visual;
    }

    GameObject _root;
    GameObject _projectile;
    GameObject _fxRoot;
    StoneEnemyVisual _visual;
    StoneEffects _fx;
    ResourceAttribute _health;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Root");
        _projectile = new GameObject("Projectile");
        _health = TestHelpers.CreateResourceAttribute(_root, AttributeType.HealthMax, 100);
        _fx = CreateEffects();
        _fxRoot = _fx.gameObject;
        _visual = CreateVisual(_root);
        _visual.Init(_health, 17, _fx);
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_visual, "OnDestroy");
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_projectile);
        Object.DestroyImmediate(_fxRoot);
    }

    [Test]
    public void AimAnticipationAndLaunchRotateOnlyPresentation()
    {
        Transform pivot = _visual.parts[0].transform.parent;
        _visual.AdvancePresentation(Vector3.forward, 1f, 1f);
        Assert.Greater((pivot.rotation * Vector3.up).z, 0);

        _visual.AdvancePresentation(Vector3.forward, 0f, 1f);
        Assert.Less((pivot.rotation * Vector3.up).z, -0.2f);

        Assert.True(_visual.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.forward * 5f));
        Assert.Greater((pivot.rotation * Vector3.up).z, 0.3f);
        Assert.AreEqual(Quaternion.identity, _root.transform.rotation);
        Assert.AreEqual(Quaternion.identity, pivot.parent.localRotation);
        Assert.AreEqual(Vector3.zero, _root.transform.position);
    }

    [TestCase(StonePreset.Cairn)]
    [TestCase(StonePreset.Monolith)]
    public void RigidPresetsIgnoreTargetCooldownAndLaunch(StonePreset preset)
    {
        TestHelpers.SetPrivateField(_visual, "_preset", preset);
        _visual.Init(_health, 17, _fx);
        Transform pivot = _visual.parts[0].transform.parent;
        _visual.AdvancePresentation(Vector3.forward, 0f, 0.2f);
        Quaternion first = pivot.localRotation;

        _visual.Init(_health, 17, _fx);
        _visual.AdvancePresentation(Vector3.left, 1f, 0.2f);
        Assert.AreEqual(first, pivot.localRotation);

        _visual.BeginDelivery(1, DeliveryStyle.Direct, _projectile.transform, Vector3.back);
        Assert.AreEqual(first, pivot.localRotation);

        _visual.AdvancePresentation(Vector3.forward, 0f, 10f);
        Assert.Less(Quaternion.Angle(Quaternion.identity, pivot.localRotation), 0.001f);
    }

    [TestCase(DeliveryStyle.Thrown)]
    [TestCase(DeliveryStyle.Direct)]
    [TestCase(DeliveryStyle.Rigid)]
    public void DeliveryFollowsContactsAndCleansUp(DeliveryStyle style)
    {
        Assert.True(_visual.BeginDelivery(1, style, _projectile.transform, Vector3.forward));
        Assert.False(_visual.BeginDelivery(1, style, _projectile.transform, Vector3.forward));

        _projectile.transform.position = new Vector3(4f, 3f, 2f);
        TestHelpers.InvokePrivate(_visual, "LateUpdate");
        Transform shard = _fxRoot.GetComponentInChildren<MeshFilter>().transform;
        Assert.AreEqual(_projectile.transform.position, shard.position);

        _visual.ContactDelivery(1, Vector3.one * 7f, null);
        Assert.AreEqual(0, _visual.liveDeliveryCount);
        Assert.That(_fx.liveCount, Is.InRange(8, 10));
        foreach (MeshFilter filter in _fxRoot.GetComponentsInChildren<MeshFilter>())
        {
            Assert.AreEqual(Vector3.one * 7f, filter.transform.position);
        }

        int count = _fx.liveCount;
        _visual.ContactDelivery(1, Vector3.zero, null);
        _visual.EndDelivery(1);
        Assert.AreEqual(count, _fx.liveCount);
    }

    [Test]
    public void UnsupportedStylesAndDisableDoNotLeak()
    {
        DeliveryStyle[] unsupported =
        {
            DeliveryStyle.Arc, DeliveryStyle.Swarm, DeliveryStyle.Bounce, DeliveryStyle.ChainSync
        };
        foreach (DeliveryStyle style in unsupported)
        {
            Assert.False(_visual.BeginDelivery(1, style, _projectile.transform, Vector3.one));
        }

        _visual.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _visual.BeginDelivery(2, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _visual.enabled = false;
        TestHelpers.InvokePrivate(_visual, "OnDisable");
        Assert.AreEqual(0, _visual.liveDeliveryCount);
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void LateUpdatePollsPublicSkillCooldownAndFirstTarget()
    {
        Entity owner = null;
        TargetProvider provider = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            owner = _root.AddComponent<Entity>();
            provider = _root.AddComponent<TargetProvider>();
        });
        TestHelpers.SetPrivateField(owner, "_health", _health);
        TestHelpers.SetPrivateField(provider, "_targets", new List<GameObject> { _projectile });
        _projectile.transform.position = Vector3.forward * 8f;
        _visual.Init(owner, (StoneEffects)null);

        // Entity creates skills after model Init. Verify they are discovered on a later poll.
        ShootProjectileSkill skill = _root.AddComponent<ShootProjectileSkill>();
        TestHelpers.SetPrivateField(skill, "_cooldownDuration", new Attribute(1));
        skill.isEnabled = true;
        BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        System.Type cooldownSkill = typeof(ACooldownSkill<ShootProjectileSkillData>);
        FieldInfo cooldown = cooldownSkill.GetField("_cooldown", privateInstance);
        cooldown.SetValue(skill, 1f);
        TestHelpers.InvokePrivate(_visual, "LateUpdate");
        MethodInfo read = typeof(StoneEnemyVisual).GetMethod("ReadCooldown", privateInstance);
        Assert.AreEqual(1f, (float)read.Invoke(_visual, null));

        System.Func<float> poll = (System.Func<float>)System.Delegate.CreateDelegate(typeof(System.Func<float>),
            _visual, read);
        poll();
        _visual.CompleteHealthBatch();
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            poll();
            _visual.CompleteHealthBatch();
        }
        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);

        cooldown.SetValue(skill, 0.05f);
        Assert.AreEqual(0.05f, (float)read.Invoke(_visual, null));

        skill.isEnabled = false;
        Assert.True(float.IsNaN((float)read.Invoke(_visual, null)));

        skill.isEnabled = true;
        TestHelpers.SetPrivateField(owner, "_isDraggable", true);
        Assert.True(float.IsNaN((float)read.Invoke(_visual, null)));
    }

    [Test]
    public void DestroyedProjectileIsReleasedWithoutContact()
    {
        _visual.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        Object.DestroyImmediate(_projectile);
        TestHelpers.InvokePrivate(_visual, "LateUpdate");
        Assert.AreEqual(0, _visual.liveDeliveryCount);
        Assert.AreEqual(0, _fx.liveCount);
    }
}

}
