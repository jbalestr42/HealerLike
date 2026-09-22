using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    public class HLStageCalibrationTests
    {
        [Test] public void FogLeavesForegroundClearAndFarCornerBelowFullFog()
        {
            var board = new Bounds(new Vector3(0,.5f,0), new Vector3(16,0,16));
            var camera = new Vector3(0,20,-17);
            var range = HLStageCalibration.FogRange(camera, board);
            Assert.That(range.x, Is.EqualTo(Vector3.Distance(camera, board.ClosestPoint(camera))).Within(.001f));
            Assert.That(range.y, Is.GreaterThan(Vector3.Distance(camera, new Vector3(8,.5f,8))));
        }
        [Test] public void HatchScalesWithProjectionAndResolution()
        {
            var go = new GameObject("HLCamera");
            try {
                var camera = go.AddComponent<Camera>(); camera.fieldOfView = 40;
                Assert.That(HLStageCalibration.HatchSpacing(camera, 30,1080), Is.EqualTo(.08088f).Within(.0001));
                camera.orthographic = true; camera.orthographicSize = 10;
                Assert.That(HLStageCalibration.HatchSpacing(camera, 5,1000), Is.EqualTo(.08f).Within(.0001));
                Assert.That(HLStageCalibration.HatchSpacing(camera, 50,2000), Is.EqualTo(.04f).Within(.0001));
            } finally { Object.DestroyImmediate(go); }
        }
    }
}
