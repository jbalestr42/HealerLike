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
}

}
