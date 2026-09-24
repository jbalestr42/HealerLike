using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
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

    GameObject _owner;
    GameObject _source;
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
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        TestHelpers.CreateAttributeManager(_source);
        _fx = RenderTestAssets.CreateStoneEffects();
        _fxObject = _fx.gameObject;
        _recipe = RenderTestAssets.CreateStoneRecipe();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _body = RenderTestAssets.CreateStoneBody(_owner, RenderTestAssets.CreateStoneEntity(_owner, _health), _recipe, _material);
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
        int hit = StoneEffects.DustPuffs + StoneEffects.HitChips;
        Assert.AreEqual(2 * hit + 1, _fx.liveCount); // two hits of 8, and the falling limb

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

        _body.RecordImpact(modifier, new StoneImpact(point, Vector3.up));
        Drain();

        Assert.AreEqual(0, _body.pendingImpactCount);
        int hit = StoneEffects.DustPuffs + StoneEffects.HitChips;
        Assert.AreEqual(hit, _fx.liveCount); // 5 dust and 3 chips
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

        Assert.AreEqual(StoneEffects.DustPuffs + StoneEffects.HitChips, _fx.liveCount); // one hit
    }

    [Test]
    public void EstimateImpact_QueryAboveTheStone_LandsOnTheHead()
    {
        Transform head = _body.parts[3];

        StoneImpact impact = _body.EstimateImpact(head.position + Vector3.up * 5f);

        Assert.Greater(impact.point.y, head.GetComponent<Renderer>().bounds.center.y);
        Assert.Greater(impact.normal.y, 0f);
    }

    [Test]
    public void CompleteHealthBatch_Lethal_CollapsesOnceAndDebrisOutlivesTheStone()
    {
        Queue(-100);
        Drain();
        Assert.AreEqual(0, visibleCount);
        Assert.IsTrue(_body.isCollapsed);
        int hitAndCollapse = StoneEffects.DustPuffs + StoneEffects.HitChips
            + StoneEffects.CollapseDebris + StoneEffects.DustPuffs;
        Assert.AreEqual(hitAndCollapse, _fx.liveCount); // 8 for the hit, 12 debris and 5 dust for the collapse

        _body.Collapse(null);
        Assert.AreEqual(hitAndCollapse, _fx.liveCount);

        TestHelpers.InvokePrivate(_body, "OnDestroy");
        TestHelpers.InvokePrivate(_body.GetComponent<CreatureBuilder>(), "OnDestroy");
        Object.DestroyImmediate(_owner);
        _owner = null;
        _body = null;
        Assert.AreEqual(hitAndCollapse, _fx.liveCount);

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
        StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_body.transform, true);
        TestHelpers.SetPrivateField(_body, "_groundShadow", shadow);

        _body.Init(_health, 15, _fx);
        Assert.IsTrue(shadow.gameObject.activeSelf);
        Assert.Less(shadow.transform.position.x, _body.transform.position.x); // the light is upper right

        _body.Collapse(null);
        Assert.IsFalse(shadow.gameObject.activeSelf);
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
