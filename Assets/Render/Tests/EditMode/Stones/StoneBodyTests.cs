using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneBodyTests
{
    class Consumer : AConsumer
    {
        readonly float _amount;

        public Consumer(float amount)
        {
            _amount = amount;
        }

        public override float GetValue()
        {
            return _amount;
        }

        public override bool ignoreDamageReduction { get { return true; } }

        public override bool ignoreConsumerPrevention { get { return false; } }
    }

    // A stone the way the composer lays one out: a body on two limbs, a head on top
    public static CreatureRecipe Recipe()
    {
        CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
        recipe.parts = new CreaturePart[]
        {
            Part("Body", -1, Vector3.up, Vector3.one, PartRole.Body),
            Part("LimbLeft", 0, new Vector3(-0.4f, -0.7f, 0f), Vector3.one * 0.4f, PartRole.Limb),
            Part("LimbRight", 0, new Vector3(0.4f, -0.7f, 0f), Vector3.one * 0.4f, PartRole.Limb),
            Part("Head", 0, Vector3.up * 0.8f, Vector3.one * 0.5f, PartRole.Head)
        };
        recipe.roots.count = 0;
        recipe.sourceLocal = new Vector3[] { Vector3.up * 2f };
        return recipe;
    }

    static CreaturePart Part(string id, int parent, Vector3 position, Vector3 dimensions, PartRole role)
    {
        return new CreaturePart
        {
            id = id,
            parent = parent,
            primitive = Primitive.Stone,
            localPosition = position,
            dimensions = dimensions,
            colour = Color.grey,
            role = role
        };
    }

    // A derived stone the way the prefab lays it out: the builder builds the rig, then the body joins it
    public static StoneBody CreateBody(GameObject owner, Entity entity, CreatureRecipe recipe, Material material)
    {
        GameObject viewGo = new GameObject("DerivedStone");
        viewGo.transform.SetParent(owner.transform, false);
        CreatureBuilder builder = viewGo.AddComponent<CreatureBuilder>();
        builder.SetRecipe(recipe, material,
            AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset"));
        builder.Init(entity);
        return viewGo.AddComponent<StoneBody>();
    }

    public static Entity CreateEntity(GameObject owner, ResourceAttribute health)
    {
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_health", health);
        return entity;
    }

    GameObject _owner;
    GameObject _source;
    GameObject _projectile;
    GameObject _fxObject;
    ResourceAttribute _health;
    CreatureRecipe _recipe;
    Material _material;
    StoneBody _body;
    StoneEffects _fx;

    int visibleCount
    {
        get
        {
            int count = 0;
            foreach (Transform part in _body.parts)
            {
                if (part.gameObject.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Stone");
        _source = new GameObject("Source");
        _projectile = new GameObject("Projectile");
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        TestHelpers.CreateAttributeManager(_source);
        _fx = StoneEffectsTests.CreateEffects();
        _fxObject = _fx.gameObject;
        _recipe = Recipe();
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(
            "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"));
        _body = CreateBody(_owner, CreateEntity(_owner, _health), _recipe, _material);
        _body.Init(_health, 15, _fx);
    }

    [TearDown]
    public void TearDown()
    {
        if (_body != null)
        {
            TestHelpers.InvokePrivate(_body, "OnDestroy");
            TestHelpers.InvokePrivate(_body.GetComponent<CreatureBuilder>(), "OnDestroy");
        }
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_projectile);
        Object.DestroyImmediate(_fxObject);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    ResourceModifier Queue(float delta)
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };
        modifier.consumers.Add(new Consumer(delta));
        _health.AddResourceModifier(modifier);
        return modifier;
    }

    void Drain()
    {
        TestHelpers.InvokePrivate(_health, "Update");
        _body.CompleteHealthBatch();
    }

    [Test]
    public void Init_BuiltRig_ReadsEveryPartOfTheRig()
    {
        CreatureRig rig = _body.GetComponent<CreatureBuilder>().rig;

        Assert.AreEqual(4, _body.parts.Count);
        Assert.AreSame(rig.partTransforms[3], _body.parts[3]);
        Assert.AreEqual(4, visibleCount);
    }

    [Test]
    public void CompleteHealthBatch_HalfHealth_ShedsOneLimbOnce()
    {
        Queue(-49);
        Drain();
        Assert.AreEqual(4, visibleCount);

        Queue(-1);
        Drain();
        Assert.AreEqual(3, visibleCount);
        Assert.AreEqual(PartRole.Limb, _recipe.parts[_body.shedPart].role);
        Assert.AreEqual(29, _fx.liveCount); // two hits of 14, and the falling limb

        Queue(50);
        Drain();
        Queue(-70);
        Drain();
        Assert.AreEqual(3, visibleCount);

        _body.Init(_health, 15, _fx);
        Assert.AreEqual(4, visibleCount);
    }

    [Test]
    public void CompleteHealthBatch_NoLimbOrAccessory_ShedsNothing()
    {
        for (int i = 1; i < _recipe.parts.Length; i++)
        {
            _recipe.parts[i].role = PartRole.Head;
        }

        Queue(-60);
        Drain();

        Assert.AreEqual(4, visibleCount);
        Assert.AreEqual(-1, _body.shedPart);
    }

    [Test]
    public void CompleteHealthBatch_NetZeroChange_DoesNotShed()
    {
        Queue(-80);
        Queue(80);

        Drain();

        Assert.AreEqual(4, visibleCount);
    }

    [Test]
    public void RecordImpact_MatchingModifier_EmitsAtTheContact()
    {
        ResourceModifier modifier = Queue(-1);
        Vector3 point = new Vector3(23f, 7f, 4f);

        _body.RecordImpact(modifier, new StoneImpact(point, Vector3.up, Vector3.zero, false));
        Drain();

        Assert.AreEqual(0, _body.pendingImpactCount);
        Assert.AreEqual(14, _fx.liveCount); // 5 dust, 6 sparks and 3 chips
        foreach (MeshFilter filter in _fxObject.GetComponentsInChildren<MeshFilter>())
        {
            Vector3 expected = filter.sharedMesh.name == "Pyramid" ? point + Vector3.up * 0.005f : point;
            Assert.That(Vector3.Distance(filter.transform.position, expected), Is.LessThan(0.00001));
        }
    }

    [Test]
    public void LateUpdate_TwoFramesWithoutConsumers_ExpiresTheImpact()
    {
        _body.RecordImpact(new ResourceModifier(), default);
        TestHelpers.InvokePrivate(_body, "LateUpdate");
        Assert.AreEqual(1, _body.pendingImpactCount);

        TestHelpers.InvokePrivate(_body, "LateUpdate");

        Assert.AreEqual(0, _body.pendingImpactCount);
    }

    [Test]
    public void Init_AfterDisableAndReenable_KeepsOneListener()
    {
        _body.enabled = false;
        TestHelpers.InvokePrivate(_body, "OnDisable");
        _body.RecordImpact(new ResourceModifier(), default);
        Assert.AreEqual(0, _body.pendingImpactCount);

        _fx.Advance(1f);
        _body.enabled = true;
        TestHelpers.InvokePrivate(_body, "OnEnable");
        _body.Init(_health, 15, _fx);
        Queue(-1);
        Drain();

        Assert.AreEqual(14, _fx.liveCount);
    }

    [Test]
    public void EstimateImpact_QueryAboveTheStone_LandsOnTheHead()
    {
        Transform head = _body.parts[3];

        StoneImpact impact = _body.EstimateImpact(head.position + Vector3.up * 5f, Vector3.zero);

        Assert.Greater(impact.pointWS.y, head.GetComponent<Renderer>().bounds.center.y);
        Assert.Greater(impact.normalWS.y, 0f);
    }

    [Test]
    public void CompleteHealthBatch_Lethal_CollapsesOnceAndDebrisOutlivesTheStone()
    {
        Queue(-100);
        Drain();
        Assert.AreEqual(0, visibleCount);
        Assert.IsTrue(_body.isCollapsed);
        Assert.AreEqual(31, _fx.liveCount); // 14 for the hit, 12 debris and 5 dust for the collapse

        _body.Collapse(null);
        Assert.AreEqual(31, _fx.liveCount);

        TestHelpers.InvokePrivate(_body, "OnDestroy");
        TestHelpers.InvokePrivate(_body.GetComponent<CreatureBuilder>(), "OnDestroy");
        Object.DestroyImmediate(_owner);
        _owner = null;
        _body = null;
        Assert.AreEqual(31, _fx.liveCount);

        _fx.Advance(0.81f);
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void OnDestroy_LivingStone_EmitsNothing()
    {
        _body.enabled = false;
        TestHelpers.InvokePrivate(_body, "OnDestroy");

        Object.DestroyImmediate(_body);
        _body = null;

        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void Init_GroundShadow_FallsAwayFromTheKeyLightAndGoesWithTheCollapse()
    {
        StoneGroundDisc shadow = StoneGroundDiscTests.CreateDisc(_body.transform, true);
        TestHelpers.SetPrivateField(_body, "_groundShadow", shadow);

        _body.Init(_health, 15, _fx);
        Assert.IsTrue(shadow.gameObject.activeSelf);
        Assert.Less(shadow.transform.position.x, _body.transform.position.x); // the light is upper right

        _body.Collapse(null);
        Assert.IsFalse(shadow.gameObject.activeSelf);
    }

    [Test]
    public void BeginDelivery_RigWithoutArms_TheBodyClaimsWhatTheBuilderRefuses()
    {
        CreatureBuilder builder = _body.GetComponent<CreatureBuilder>();

        bool isBuilderClaiming = builder.BeginDelivery(1, DeliveryStyle.Direct, _projectile.transform, Vector3.one);
        bool isBodyClaiming = _body.BeginDelivery(1, DeliveryStyle.Direct, _projectile.transform, Vector3.one);

        Assert.IsFalse(isBuilderClaiming);
        Assert.IsTrue(isBodyClaiming);
    }

    [TestCase(DeliveryStyle.Direct)]
    [TestCase(DeliveryStyle.Arc)]
    [TestCase(DeliveryStyle.Rigid)]
    [TestCase(DeliveryStyle.Swarm)]
    [TestCase(DeliveryStyle.Bounce)]
    [TestCase(DeliveryStyle.ChainSync)]
    [TestCase(DeliveryStyle.Thrown)]
    public void BeginDelivery_AnyStyle_ThrowsAShardFromTheHeadThatFollowsTheProjectile(DeliveryStyle style)
    {
        Vector3 head = _body.parts[3].GetComponent<Renderer>().bounds.center;

        Assert.IsTrue(_body.BeginDelivery(1, style, _projectile.transform, Vector3.forward));
        Assert.IsFalse(_body.BeginDelivery(1, style, _projectile.transform, Vector3.forward));
        Transform shard = _fxObject.GetComponentInChildren<MeshFilter>().transform;
        Assert.AreEqual(head, shard.position);

        _projectile.transform.position = new Vector3(4f, 3f, 2f);
        TestHelpers.InvokePrivate(_body, "LateUpdate");
        Assert.AreEqual(_projectile.transform.position, shard.position);

        _body.ContactDelivery(1, Vector3.one * 7f, null);
        Assert.AreEqual(0, _body.liveDeliveryCount);
        Assert.That(_fx.liveCount, Is.InRange(8, 10));
    }

    [Test]
    public void BeginDelivery_Collapsed_Refuses()
    {
        _body.Collapse(null);

        bool isClaimed = _body.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        Assert.IsFalse(isClaimed);
    }

    [Test]
    public void OnDisable_LiveDeliveries_LeaksNothing()
    {
        _body.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);
        _body.BeginDelivery(2, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        _body.enabled = false;
        TestHelpers.InvokePrivate(_body, "OnDisable");

        Assert.AreEqual(0, _body.liveDeliveryCount);
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void LateUpdate_DestroyedProjectile_ReleasesTheDeliveryWithoutContact()
    {
        _body.BeginDelivery(1, DeliveryStyle.Thrown, _projectile.transform, Vector3.one);

        Object.DestroyImmediate(_projectile);
        TestHelpers.InvokePrivate(_body, "LateUpdate");

        Assert.AreEqual(0, _body.liveDeliveryCount);
        Assert.AreEqual(0, _fx.liveCount);
    }

    [Test]
    public void LateUpdate_RebuiltRig_KeepsTheShedLimbHidden()
    {
        Queue(-60);
        Drain();
        CreatureBuilder builder = _body.GetComponent<CreatureBuilder>();

        builder.Configure(null, 2f, Vector3.zero, Vector3.up);
        TestHelpers.InvokePrivate(_body, "LateUpdate");

        Assert.AreEqual(3, visibleCount);
        Assert.IsFalse(_body.parts[_body.shedPart].gameObject.activeSelf);
    }
}

}
