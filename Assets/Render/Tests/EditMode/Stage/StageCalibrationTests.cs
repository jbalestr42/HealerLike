using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stage
{

public class StageCalibrationTests
{
    [Test]
    public void FogLeavesForegroundClearAndFarCornerBelowFullFog()
    {
        var board = new Bounds(new Vector3(0,.5f,0), new Vector3(16,0,16));
        var camera = new Vector3(0,20,-17);
        var range = StageCalibration.FogRange(camera, board);
        Assert.That(range.x, Is.EqualTo(Vector3.Distance(camera, board.ClosestPoint(camera))).Within(.001f));
        Assert.That(range.y, Is.GreaterThan(Vector3.Distance(camera, new Vector3(8,.5f,8))));
    }
    [Test]
    public void HatchScalesWithProjectionAndResolution()
    {
        var go = new GameObject("Camera");
        try {
            var camera = go.AddComponent<Camera>(); camera.fieldOfView = 40;
            Assert.That(StageCalibration.HatchSpacing(camera, 30,1080), Is.EqualTo(.08088f).Within(.0001));
            camera.orthographic = true; camera.orthographicSize = 10;
            Assert.That(StageCalibration.HatchSpacing(camera, 5,1000), Is.EqualTo(.08f).Within(.0001));
            Assert.That(StageCalibration.HatchSpacing(camera, 50,2000), Is.EqualTo(.04f).Within(.0001));
        } finally { Object.DestroyImmediate(go); }
    }
    [Test]
    public void PortraitFrameFitsTheBoardWidthAtTheNearEdgeAndPlacesTheCentre()
    {
        var go = new GameObject("Camera");
        try {
            var camera = go.AddComponent<Camera>(); camera.fieldOfView = 40; camera.aspect = 9f/16f;
            var board = new Bounds(new Vector3(0,.505f,0), new Vector3(16,0,16));
            var pose = StageCalibration.Frame(board, 73.7f, 40, 9f/16f, .5f, .42f);
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            Assert.That(pose.rotation.eulerAngles.x, Is.EqualTo(73.7f).Within(.01f));
            Assert.That(camera.WorldToViewportPoint(board.center).y, Is.EqualTo(.42f).Within(.002f));
            var nearLeft = camera.WorldToViewportPoint(new Vector3(-8,.505f,-8)); var nearRight = camera.WorldToViewportPoint(new Vector3(8,.505f,-8));
            float margin = .5f / 17f; // half a unit of 17 across
            Assert.That(nearLeft.x, Is.EqualTo(margin).Within(.002f)); Assert.That(nearRight.x, Is.EqualTo(1 - margin).Within(.002f));
            var far = camera.WorldToViewportPoint(new Vector3(8,.505f,8));
            Assert.That(far.x, Is.InRange(0f, 1f)); Assert.That(far.y, Is.InRange(0f, 1f));
        } finally { Object.DestroyImmediate(go); }
    }
}

}
