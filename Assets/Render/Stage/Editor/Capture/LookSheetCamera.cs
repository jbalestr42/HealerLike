using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The two cameras a sheet is rendered at: today's portrait overview, and the board camera the reference study
    // names, the 16-cell board filling 1080 px so a cell at the board centre is about 67 px
    public static class LookSheetCamera
    {
        public static readonly int BoardCells = 16;

        // Same pitch and field of view as the portrait, pulled back along its view until a cell at the board centre
        // spans width / BoardCells pixels
        public static Pose Board(Vector3 centre, float cellSize, Quaternion rotation, float fieldOfView, float aspect, int width)
        {
            float cellPixels = (float)width / BoardCells;
            float tanH = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect;
            float distance = cellSize * width / (2f * tanH * cellPixels);
            return new Pose(centre - rotation * Vector3.forward * distance, rotation);
        }
    }
}
