using UnityEngine;
namespace HealerLike.Render.Stage
{
    public static class HLStageCalibration
    {
        public static Vector2 FogRange(Vector3 camera, Bounds board)
        {
            float near = Vector3.Distance(camera, board.ClosestPoint(camera));
            float far = near;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    far = Mathf.Max(far, Vector3.Distance(camera, board.center + Vector3.Scale(board.extents, new Vector3(x, 0, z))));
            // Keep the foreground clear; retain colour in the far row (maximum 5/6 fog).
            return new Vector2(near, Mathf.Max(near + 1, far + (far - near) * .2f));
        }
        public static float HatchSpacing(Camera camera, float depth, int height)
        {
            float span = camera.orthographic ? 2 * camera.orthographicSize : 2 * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
            return Mathf.Max(.0001f, span / Mathf.Max(1, height) * 4); // four target pixels per stroke
        }
    }
}
