using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class LookSheetCameraTests
{
    GameObject _cameraGo;

    [TearDown]
    public void TearDown()
    {
        if (_cameraGo != null)
        {
            Object.DestroyImmediate(_cameraGo);
        }
    }

    [Test]
    public void Board_SixteenCellsAcross_DrawsACellAtTheCentreInASixteenthOfTheWidth()
    {
        Quaternion rotation = Quaternion.Euler(52f, 0f, 0f);
        Vector3 centre = new Vector3(3f, 0.5f, -2f);

        Pose pose = LookSheetCamera.Board(centre, 1f, rotation, 40f, 9f / 16f, 1080);

        // The view is BoardCells cells wide at the centre's depth, so a cell spans 1080 / 16 = 67.5 px
        float depth = Vector3.Distance(pose.position, centre);
        float viewWidth = 2f * depth * Mathf.Tan(40f * Mathf.Deg2Rad * 0.5f) * (9f / 16f);
        Assert.AreEqual(LookSheetCamera.BoardCells, viewWidth, 0.001f);
        Assert.AreEqual(rotation, pose.rotation);
    }
    [TestCase(0f)]
    [TestCase(90f)]
    public void CellPixels_RotatedBoard_UsesTheHorizontalCameraAxis(float yaw)
    {
        _cameraGo = new GameObject("Sheet camera");
        Camera camera = _cameraGo.AddComponent<Camera>();
        camera.aspect = 9f / 16f;
        camera.fieldOfView = 40f;
        Pose pose = LookSheetCamera.Board(Vector3.zero, 1f, Quaternion.Euler(52f, yaw, 0f),
            camera.fieldOfView, camera.aspect, 1080);
        camera.transform.SetPositionAndRotation(pose.position, pose.rotation);

        float pixels = LookSheetCamera.CellPixels(camera, Vector3.zero, 1f, 1080);

        Assert.AreEqual(1080f / LookSheetCamera.BoardCells, pixels, 0.01f);
    }

}

}
