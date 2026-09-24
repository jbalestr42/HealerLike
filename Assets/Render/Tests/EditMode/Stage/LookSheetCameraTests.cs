using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class LookSheetCameraTests
{
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
}

}
