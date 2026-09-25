using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class StageCalibrationTests
{
    GameObject _go;
    Camera _camera;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Camera");
        _camera = _go.AddComponent<Camera>();
        _camera.fieldOfView = 40f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void BackgroundFog_PortraitCamera_StartsAtTheFarEdgeAndSpansTheFogDepth()
    {
        Bounds board = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(16f, 0f, 16f));
        Vector3 camera = new Vector3(3f, 20f, -17f);

        Vector2 fog = StageCalibration.BackgroundFog(camera, board);

        float farEdge = Vector3.Distance(camera, new Vector3(3f, 0.5f, 8f));
        Assert.That(fog.x, Is.EqualTo(farEdge).Within(0.001f));
        Assert.That(fog.y - fog.x, Is.EqualTo(StageCalibration.BackgroundFogDepth).Within(0.001f));
    }

    [Test]
    public void BackgroundFog_CameraPastTheSide_MeasuresToTheFarCorner()
    {
        Bounds board = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(16f, 0f, 16f));
        Vector3 camera = new Vector3(12f, 20f, -17f);

        Vector2 fog = StageCalibration.BackgroundFog(camera, board);

        Assert.That(fog.x, Is.EqualTo(Vector3.Distance(camera, new Vector3(8f, 0.5f, 8f))).Within(0.001f));
    }

    [Test]
    public void Frame_PortraitBoard_FitsWidthAtNearEdgeAndPlacesTheCentre()
    {
        _camera.aspect = StageCalibration.PortraitAspect;
        _camera.fieldOfView = StageCalibration.PortraitFov;
        Bounds board = new Bounds(new Vector3(0f, 0.505f, 0f), new Vector3(16f, 0f, 16f));

        Pose pose = StageCalibration.Frame(board, StageCalibration.PortraitPitch, StageCalibration.PortraitFov,
                                           StageCalibration.PortraitAspect, 0.5f, StageCalibration.PortraitCentreY);
        _go.transform.SetPositionAndRotation(pose.position, pose.rotation);

        Assert.That(pose.rotation.eulerAngles.x, Is.EqualTo(StageCalibration.PortraitPitch).Within(0.01f));
        float centreY = _camera.WorldToViewportPoint(board.center).y;
        Assert.That(centreY, Is.EqualTo(StageCalibration.PortraitCentreY).Within(0.002f));
        Vector3 nearLeft = _camera.WorldToViewportPoint(new Vector3(-8f, 0.505f, -8f));
        Vector3 nearRight = _camera.WorldToViewportPoint(new Vector3(8f, 0.505f, -8f));
        float margin = 0.5f / 17f; // half a unit of 17 across
        Assert.That(nearLeft.x, Is.EqualTo(margin).Within(0.002f));
        Assert.That(nearRight.x, Is.EqualTo(1f - margin).Within(0.002f));
        Vector3 far = _camera.WorldToViewportPoint(new Vector3(8f, 0.505f, 8f));
        Assert.That(far.x, Is.InRange(0f, 1f));
        Assert.That(far.y, Is.InRange(0f, 1f));
    }

    [Test]
    public void Contains_FittedBox_KeepsEveryCornerInItsFrame()
    {
        Bounds box = new Bounds(new Vector3(3f, 1f, -2f), new Vector3(6f, 2f, 4f));
        Rect frame = Rect.MinMaxRect(0.1f, 0.2f, 0.9f, 0.8f);

        Pose pose = StageCalibration.Fit(box, StageCalibration.PortraitPitch, StageCalibration.PortraitFov,
                                         StageCalibration.PortraitAspect, frame, 0f);

        Rect edge = Rect.MinMaxRect(0.0999f, 0.1999f, 0.9001f, 0.8001f); // the fit touches the frame
        Assert.IsTrue(StageCalibration.Contains(box, pose, StageCalibration.PortraitFov,
            StageCalibration.PortraitAspect, edge));
        Pose closer = new Pose(pose.position + pose.rotation * Vector3.forward, pose.rotation);
        Assert.IsFalse(StageCalibration.Contains(box, closer, StageCalibration.PortraitFov,
                                                 StageCalibration.PortraitAspect, frame));
    }

    [TestCase(16f, 16f)]
    [TestCase(12f, 20f)]
    public void PlayableFrame_PortraitHeading_KeepsPlacementBoardBalancedBelowEnemyRows(float width, float depth)
    {
        Bounds board = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(width, 0f, depth));
        Pose pose = StageCalibration.PlayableFrame(board, StageCalibration.PortraitPitch,
            StageCalibration.PortraitFov, StageCalibration.PortraitAspect, StageCalibration.PortraitCentreY,
            StageCalibration.PortraitYaw);
        _camera.aspect = StageCalibration.PortraitAspect;
        _go.transform.SetPositionAndRotation(pose.position, pose.rotation);
        Vector3 boardCentre = _camera.WorldToViewportPoint(new Vector3(0f, 0.5f, 0f));
        Vector3 enemy = _camera.WorldToViewportPoint(new Vector3(5f, 0.5f, 0f));
        Assert.That(boardCentre.y, Is.InRange(0.44f, 0.48f));
        Assert.That(boardCentre.y, Is.LessThan(enemy.y));
        Assert.That(boardCentre.x, Is.EqualTo(enemy.x).Within(0.001f));
        Assert.IsTrue(StageCalibration.Contains(board, pose, _camera.fieldOfView, _camera.aspect,
            Rect.MinMaxRect(0.0149f, 0.1199f, 0.9851f, 0.8401f)));
    }

    [Test]
    public void BackgroundFog_RotatedPortrait_StartsBeyondPositiveXEdge()
    {
        Bounds board = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(12f, 0f, 20f));
        Vector3 camera = new Vector3(-17f, 20f, 3f);
        Vector2 fog = StageCalibration.BackgroundFog(camera, board, StageCalibration.PortraitYaw);
        Assert.That(fog.x, Is.EqualTo(Vector3.Distance(camera, new Vector3(6f, 0.5f, 3f))).Within(0.001f));
    }

    [Test]
    public void Contains_BoxBehindTheCamera_IsOutside()
    {
        Bounds box = new Bounds(Vector3.back * 5f, Vector3.one);
        Pose pose = new Pose(Vector3.zero, Quaternion.identity);

        bool isInside = StageCalibration.Contains(box, pose, 40f, 1f, Rect.MinMaxRect(0f, 0f, 1f, 1f));

        Assert.IsFalse(isInside);
    }
}

}
