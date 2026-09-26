using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class PointerBrushTests
{
    GameObject _go;
    Camera _camera;
    Ground _ground;
    PointerBrush _brush;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("brush");
        _camera = new GameObject("camera").AddComponent<Camera>();
        _camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
        _camera.pixelRect = new Rect(0f, 0f, 200f, 200f);
        _ground = new Ground();
        _brush = _go.AddComponent<PointerBrush>();
        _brush.Init(_ground, _camera, 0f, 1f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_camera.gameObject);
        Object.DestroyImmediate(_go);
        _ground.Dispose();
    }

    [Test]
    public void Ground_RayDown_MeetsTheBoardPlane()
    {
        Assert.IsTrue(PointerBrush.Ground(new Ray(new Vector3(1f, 5f, 2f), Vector3.down), 0.5f, out Vector3 point));
        Assert.AreEqual(new Vector3(1f, 0.5f, 2f), point);
        Assert.IsFalse(PointerBrush.Ground(new Ray(Vector3.zero, Vector3.up), 0.5f, out _), "Looking up misses.");
    }

    [Test]
    public void Point_HeldOverOpenGround_BrushesAlongThePath()
    {
        _brush.Point(true, true, new Vector2(100f, 100f));
        _brush.Point(true, false, new Vector2(140f, 100f));
        BodyCapsule[] into = new BodyCapsule[2];

        Assert.AreEqual(1, _ground.bodyCount, "The brush is a body on the ground.");
        Assert.IsTrue(_brush.isBrushing);
        Assert.AreEqual(1, _brush.AppendCapsules(into, 0));
        Assert.Greater(into[0].end.x - into[0].start.x, 0.5f, "From last frame's point to this one's.");
        Assert.AreEqual(PointerBrush.BrushPress, into[0].press);

        _brush.Point(false, false, new Vector2(140f, 100f));

        Assert.IsFalse(_brush.isBrushing);
        Assert.AreEqual(0, _brush.AppendCapsules(into, 0));
    }

    [Test]
    public void Touching_FirstFingerLifts_TheNextFingerDoesNotContinueItsStroke()
    {
        _brush.Touching(0, TouchPhase.Began, new Vector2(100f, 100f));
        _brush.Touching(0, TouchPhase.Moved, new Vector2(120f, 100f));
        Assert.IsTrue(_brush.isBrushing);

        // Finger 0 lifts; finger 1, already down elsewhere, becomes the first touch mid-gesture
        _brush.Touching(1, TouchPhase.Moved, new Vector2(20f, 180f));

        Assert.IsFalse(_brush.isBrushing);
        Assert.AreEqual(0, _brush.AppendCapsules(new BodyCapsule[1], 0), "No streak across the board.");
    }

    [Test]
    public void Point_PressStartingOnACreature_BrushesNothing()
    {
        GameObject creature = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            TestHelpers.WithLoggingDisabled(() => creature.AddComponent<Entity>());
            Physics.SyncTransforms();

            _brush.Point(true, true, new Vector2(100f, 100f));
            _brush.Point(true, false, new Vector2(140f, 100f));

            Assert.IsFalse(_brush.isBrushing, "That press drags or selects the creature.");
        }
        finally
        {
            Object.DestroyImmediate(creature);
        }
    }
}

}
