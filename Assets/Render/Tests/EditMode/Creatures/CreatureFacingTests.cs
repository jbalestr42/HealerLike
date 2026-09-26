using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureFacingTests : CreatureRigFixture
{
    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void PresentationFacing_TargetFreeForkRetainsWidthAcrossCamerasAndRecompose(LookSide side)
    {
        _rig.Dispose();
        Object.DestroyImmediate(_recipe);
        _recipe = LookComposer.Compose(
            RenderTestAssets.CreateChannels(side, HeadKind.Fork),
            RenderTestAssets.LoadLookVocabulary()
        );
        _recipe.idle = default;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        _rig.Tick(0f, 0f, ground);
        float authoredWidth = ProjectedHeadWidth(Vector3.right);
        float oldPortraitWidth = ProjectedHeadWidth(Vector3.forward);
        Transform root = _rig.root;
        Transform firstPart = _rig.partTransforms[0];
        _rig.SetPresentationForward(Vector3.left);
        _rig.Tick(0f, 0f, ground);
        Assert.That(
            ProjectedHeadWidth(Vector3.forward),
            Is.GreaterThan(oldPortraitWidth * 1.4f),
            "The actual Fork mesh must expose its spread in the first portrait frame"
        );
        foreach (float yaw in new[] { 90f, 0f, -55f, 170f })
        {
            Quaternion camera = Quaternion.Euler(52f, yaw, 0f);
            _rig.SetPresentationForward(-(camera * Vector3.forward));
            _rig.SetReadout(null, 1f, 0f, 0f);
            _rig.Tick(0f, 1f, ground);
            Assert.That(ProjectedHeadWidth(camera * Vector3.right), Is.GreaterThan(authoredWidth * 0.88f));

            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            _rig.Tick(1f, 0f, ground);
            Assert.AreSame(root, _rig.root);
            Assert.AreSame(firstPart, _rig.partTransforms[0]);
            Assert.That(ProjectedHeadWidth(camera * Vector3.right), Is.GreaterThan(authoredWidth * 0.88f));
            Assert.IsTrue(_rig.TryGetAnchors(out EffectAnchors anchors));
            Assert.That(Vector3.Distance(anchors.foot, _rig.root.position), Is.LessThan(0.00001f));
            Assert.That(anchors.headRadius, Is.GreaterThan(0f));
        }
    }

    [Test]
    public void CombatTarget_OverridesCameraAndRetargetsSmoothlyWithoutRotatingOwner()
    {
        var facing = new CreatureFacing();
        _parent.transform.rotation = Quaternion.Euler(0f, 37f, 0f);
        Quaternion owner = _parent.transform.rotation;
        facing.SetForward(Vector3.back);
        Quaternion rest = facing.Evaluate(0f, 0f, Vector3.zero, _parent.transform, null);
        Assert.Less(Vector3.Angle(_parent.transform.TransformDirection(rest * Vector3.forward), Vector3.back), 0.01f);
        foreach (Vector3 target in new[] { Vector3.right, Vector3.left, Vector3.forward })
        {
            Quaternion before = facing.Evaluate(0f, 0f, Vector3.zero, _parent.transform, target * 4f);
            Quaternion step = facing.Evaluate(0f, 0.02f, Vector3.zero, _parent.transform, target * 4f);
            Assert.That(Quaternion.Angle(before, step), Is.InRange(1f, 25f));
            Quaternion settled = facing.Evaluate(0f, 1f, Vector3.zero, _parent.transform, target * 4f);
            Assert.Less(Vector3.Angle(_parent.transform.TransformDirection(settled * Vector3.forward), target), 0.2f);
            Assert.AreEqual(owner, _parent.transform.rotation);
        }
        Quaternion paused = facing.Evaluate(0f, 0f, Vector3.zero, _parent.transform, null);
        Quaternion returning = facing.Evaluate(0f, 0.02f, Vector3.zero, _parent.transform, null);
        Assert.That(Quaternion.Angle(paused, returning), Is.InRange(1f, 25f));
        Quaternion recovered = facing.Evaluate(0f, 1f, Vector3.zero, _parent.transform, null);
        Assert.Less(Vector3.Angle(_parent.transform.TransformDirection(recovered * Vector3.forward), Vector3.back), 0.2f);
    }

    [Test]
    public void InvalidOrCoincidentTargetsAndTime_NeverPoisonFacing()
    {
        var facing = new CreatureFacing();
        facing.SetForward(Vector3.left);
        foreach (Vector3 target in new[] { Vector3.zero, Vector3.up, new Vector3(float.NaN, 0f, 0f),
            new Vector3(float.PositiveInfinity, 0f, 0f), Vector3.one * float.MaxValue })
        {
            Quaternion pose = facing.Evaluate(float.NaN, float.NaN, Vector3.zero, _parent.transform, target);
            Assert.Less(Quaternion.Angle(pose, Quaternion.LookRotation(Vector3.left)), 0.001f);
        }
    }

    float ProjectedHeadWidth(Vector3 screenRight)
    {
        float minimum = float.PositiveInfinity;
        float maximum = float.NegativeInfinity;
        for (int i = 0; i < _recipe.parts.Length; i++)
        {
            if (_recipe.parts[i].role != PartRole.Head)
            {
                continue;
            }

            Transform part = _rig.partTransforms[i];
            foreach (Vector3 vertex in part.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                float x = Vector3.Dot(part.TransformPoint(vertex), screenRight);
                minimum = Mathf.Min(minimum, x);
                maximum = Mathf.Max(maximum, x);
            }
        }

        return maximum - minimum;
    }

    [Test]
    public void PresentationFacing_DegenerateViewAndTargetsKeepFinitePoseAndGroundedFeet()
    {
        _rig.SetPresentationForward(Vector3.left);
        _rig.SetReadout(Vector3.zero, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Transform sway = _rig.root.Find("Sway");
        Quaternion valid = sway.rotation;
        Vector3[] feet = RootFeet();
        foreach (
            Vector3 invalid in new[]
            {
                Vector3.zero,
                Vector3.up,
                new Vector3(float.NaN, 0f, 0f),
                new Vector3(float.PositiveInfinity, 0f, 0f),
            }
        )
        {
            _rig.SetPresentationForward(invalid);
            _rig.SetReadout(invalid, 1f, 0f, 0f);
            _rig.Tick(0f, 0.1f, ground);
            Assert.That(Quaternion.Angle(valid, sway.rotation), Is.LessThan(0.001f));
        }

        _rig.SetPresentationForward(Vector3.back);
        _rig.SetReadout(Vector3.right * 4f, 1f, 0f, 0f);
        _rig.Tick(0f, 1f, ground);
        Vector3[] turnedFeet = RootFeet();
        Assert.That(feet.Length, Is.GreaterThan(0));
        for (int i = 0; i < feet.Length; i++)
        {
            Assert.That(Vector3.Distance(feet[i], turnedFeet[i]), Is.LessThan(0.0001f));
        }

        Assert.AreEqual(Vector3.zero, _parent.transform.position);
        Assert.AreEqual(Quaternion.identity, _parent.transform.rotation);
    }

    Vector3[] RootFeet()
    {
        System.Collections.Generic.List<Vector3> feet = new System.Collections.Generic.List<Vector3>();
        int segment = 0;
        foreach (Transform child in _rig.root)
        {
            if (child.name != "Root")
            {
                continue;
            }

            if (++segment % _recipe.roots.segments == 0)
            {
                Vector3 top = _recipe.roots.segmentShape.isProcedural
                    ? ProceduralShapeMeshes.Anchor(_recipe.roots.segmentShape, ShapeAnchor.Top)
                    : Vector3.up * 0.5f;
                feet.Add(child.TransformPoint(top));
            }
        }

        return feet.ToArray();
    }

    [Test]
    public void SetReadout_AimAndHealth_DampAndRecoverWithoutMovingRoot()
    {
        _rig.SetReadout(Vector3.right * 4f, 0.2f, 0.8f, 0.8f);
        _rig.Tick(1f, 0.1f, ground);
        Transform sway = _rig.root.Find("Sway");
        Assert.Greater(sway.localEulerAngles.y, 0); // turning toward the target on the right, damped
        Assert.Less(sway.localEulerAngles.y, 90);
        Assert.Less(sway.localPosition.y, 0);
        _rig.SetReadout(Vector3.right * 4f, 1f, 0f, 0f);
        _rig.Tick(2f, 1f, ground);
        Assert.AreEqual(0, sway.localPosition.y);
        Assert.AreEqual(Vector3.zero, _rig.root.position);
    }
}
}
