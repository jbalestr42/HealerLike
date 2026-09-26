using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StonePartsTests : AStoneBodyTests
{
    [Test]
    public void RefreshRig_ReorderedParts_KeepsTheSameLostLimbByItsId()
    {
        Queue(-60);
        Drain();
        string lost = _recipe.parts[_body.shedPart].id;
        CreaturePart swap = _recipe.parts[1];
        _recipe.parts[1] = _recipe.parts[2];
        _recipe.parts[2] = swap;
        CreatureRig rig = _body.GetComponent<CreatureBuilder>().rig;
        Assert.IsTrue(rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        _body.RefreshRig();
        Assert.AreEqual(lost, _recipe.parts[_body.shedPart].id);
        Assert.IsFalse(_body.parts[_body.shedPart].gameObject.activeSelf);
        Assert.AreEqual(3, visibleCount);
    }

    [Test]
    public void RefreshRig_InPlaceRecompose_KeepsShedLimbAndCollapsedBodyHidden()
    {
        Queue(-60);
        Drain();
        CreatureBuilder builder = _body.GetComponent<CreatureBuilder>();
        CreatureRig rig = builder.rig;
        int effects = _fx.liveCount;
        Assert.IsTrue(rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        _body.RefreshRig();
        rig.SetPresentationForward(Vector3.left);
        rig.SetReadout(Vector3.forward * 4f, 0.4f, 0f, 0f);
        rig.Tick(0f, 1f, new FootFrame(_owner.transform.position, Vector3.up, 1f));
        Assert.AreSame(rig, builder.rig);
        Assert.AreEqual(3, visibleCount);
        Assert.AreEqual(effects, _fx.liveCount);
        _body.Collapse(null);
        effects = _fx.liveCount;
        Assert.IsTrue(rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        _body.RefreshRig();
        rig.SetPresentationForward(Vector3.back);
        rig.Tick(0f, 1f, new FootFrame(_owner.transform.position, Vector3.up, 1f));
        Assert.AreEqual(0, visibleCount);
        Assert.IsTrue(_body.isCollapsed);
        Assert.AreEqual(effects, _fx.liveCount);
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
    public void Init_GroundShadow_FallsAwayFromTheKeyLightAndGoesWithTheCollapse()
    {
        StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_body.transform, true);
        TestHelpers.SetPrivateField(_body, "_groundShadow", shadow);

        _body.Init(_health, 15, _fx);
        Assert.IsTrue(shadow.gameObject.activeSelf);
        Bounds bounds = StoneGroundDisc.Measure(_body.transform, _body.parts);
        Vector3 centre = _body.transform.TransformPoint(bounds.center);
        Vector3 offset = Vector3.ProjectOnPlane(shadow.transform.position - centre, Vector3.up);
        Vector3 toLight = Vector3.ProjectOnPlane(StageKeyLight.KeyDirection, Vector3.up).normalized;
        Assert.Greater(offset.sqrMagnitude, 0f);
        Assert.Less(Vector3.Dot(offset.normalized, toLight), -0.999f);
        Assert.Greater(Vector3.Dot(shadow.transform.forward, -toLight), 0.999f);

        _body.Collapse(null);
        Assert.IsFalse(shadow.gameObject.activeSelf);
    }

    [Test]
    public void Init_KeyLightWithRealShadows_HidesTheGroundShadow()
    {
        StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_body.transform, true);
        StageKeyLight keyLight = _owner.AddComponent<StageKeyLight>();
        TestHelpers.SetPrivateField(keyLight, "_realShadows", true);
        TestHelpers.SetPrivateField(_body, "_groundShadow", shadow);
        TestHelpers.SetPrivateField(_body, "_keyLight", keyLight);

        _body.Init(_health, 15, _fx);

        Assert.IsFalse(shadow.gameObject.activeSelf);
    }

    [Test]
    public void LateUpdate_RebuiltRig_KeepsTheShedLimbHidden()
    {
        Queue(-60);
        Drain();
        CreatureBuilder builder = _body.GetComponent<CreatureBuilder>();

        TestHelpers.InvokePrivate(builder, "ReleaseRig");
        TestHelpers.InvokePrivate(builder, "EnsureRig");
        TestHelpers.InvokePrivate(_body, "LateUpdate");

        Assert.AreEqual(3, visibleCount);
        Assert.IsFalse(_body.parts[_body.shedPart].gameObject.activeSelf);
    }}

}
