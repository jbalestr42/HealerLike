using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneBodyTests : AStoneBodyTests
{
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
        int hit = StoneEffects.DustPuffs + StoneEmitters.HitChips;
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

        int hit = StoneEffects.DustPuffs + StoneEmitters.HitChips;
        Assert.AreEqual(hit, _fx.liveCount); // 5 dust and 3 chips
        foreach (MeshFilter filter in _fxObject.GetComponentsInChildren<MeshFilter>())
        {
            Vector3 expected = filter.sharedMesh.name == "Pyramid" ? point + Vector3.up * 0.005f : point;
            Assert.That(Vector3.Distance(filter.transform.position, expected), Is.LessThan(0.00001));
        }
    }

    [Test]
    public void Init_AfterDisableAndReenable_KeepsOneListener()
    {
        _body.enabled = false;
        TestHelpers.InvokePrivate(_body, "OnDisable");
        _body.RecordImpact(new ResourceModifier(), default);
        Assert.AreEqual(0, _fx.liveCount); // a recorded contact would have raised dust

        _fx.Advance(1f);
        _body.enabled = true;
        TestHelpers.InvokePrivate(_body, "OnEnable");
        _body.Init(_health, 15, _fx);
        Queue(-1);
        Drain();

        Assert.AreEqual(StoneEffects.DustPuffs + StoneEmitters.HitChips, _fx.liveCount); // one hit
    }

    [Test]
    public void CompleteHealthBatch_Lethal_CollapsesOnceAndDebrisOutlivesTheStone()
    {
        Queue(-100);
        Drain();
        Assert.AreEqual(0, visibleCount);
        Assert.IsTrue(_body.isCollapsed);
        int hitAndCollapse = StoneEffects.DustPuffs + StoneEmitters.HitChips
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

}

}
