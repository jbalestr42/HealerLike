using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class StageCalibration
    {
        // Matches the game's Main camera: rotation x 0.5998, w 0.8002, FOV 40, portrait autorotation
        public static readonly float PortraitPitch = 52f;
        public static readonly float PortraitFov = 40f;
        public static readonly float PortraitAspect = 9f / 16f;
        public static readonly float PortraitMargin = 0.35f;
        public static readonly float PortraitCentreY = 0.48f;
        public static readonly int PortraitWidth = 1080;
        public static readonly int PortraitHeight = 1920;

        public static Vector2 FogRange(Vector3 camera, Bounds board)
        {
            float near = Vector3.Distance(camera, board.ClosestPoint(camera));
            float far = Mathf.Max(near, FarthestCorner(camera, board));

            // Keep the foreground clear and some colour in the far row (5/6 fog at most)
            return new Vector2(near, Mathf.Max(near + 1f, far + (far - near) * 0.2f));
        }

        public static float HatchSpacing(Camera camera, float depth, int height)
        {
            float span;
            if (camera.orthographic)
            {
                span = 2f * camera.orthographicSize;
            }
            else
            {
                span = 2f * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            }

            // Four target pixels per stroke
            return Mathf.Max(0.0001f, span / Mathf.Max(1, height) * 4f);
        }

        // Perspective pose at a fixed pitch whose view fits the board's width plus margin at its near edge
        // (the widest it projects), with the board centre drawn at the given viewport height
        public static Pose Frame(Bounds board, float pitchDegrees, float fieldOfView, float aspect, float margin,
            float centreViewportY)
        {
            float pitch = pitchDegrees * Mathf.Deg2Rad;
            float tanV = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * aspect;
            float n = 2f * centreViewportY - 1f;
            float hx = board.extents.x + margin;
            float hz = board.extents.z;
            float cos = Mathf.Cos(pitch);
            float sin = Mathf.Sin(pitch);
            float shift = 0f;
            float distance = 0f;

            // The shift moves the near edge, which changes the distance that fits it; this converges in a few steps
            for (int i = 0; i < 16; i++)
            {
                distance = hx / tanH + (hz + shift) * cos;
                if (Mathf.Abs(n) < 0.000001f)
                {
                    shift = 0f;
                }
                else
                {
                    shift = n * tanV * distance / (n * tanV * cos - sin);
                }
            }

            Quaternion rotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            Vector3 target = board.center + Vector3.forward * shift;
            return new Pose(target - rotation * Vector3.forward * distance, rotation);
        }

        // The fog starts past every playable corner, so the board keeps its authored colours
        public static Vector2 BackgroundFog(Vector3 camera, Bounds board)
        {
            float far = Mathf.Max(0f, FarthestCorner(camera, board));
            return new Vector2(far + 2f, far + 36f);
        }

        // Fits both axes, keeping room above the back row and below the front row for the HUD
        public static Pose PlayableFrame(Bounds board, float pitch, float fov, float aspect, float centreY)
        {
            Pose pose = Frame(board, pitch, fov, aspect, 0.35f, centreY);
            Vector3 forward = pose.rotation * Vector3.forward;
            float tan = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            for (int pass = 0; pass < 80; pass++)
            {
                if (FitsCorners(board, pose, tan, aspect))
                {
                    return pose;
                }

                pose.position -= forward * 0.35f;
            }

            return pose;
        }

        static bool FitsCorners(Bounds board, Pose pose, float tan, float aspect)
        {
            bool fits = true;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = board.center + Vector3.Scale(board.extents, new Vector3(x, 0f, z));
                    Vector3 q = Quaternion.Inverse(pose.rotation) * (corner - pose.position);
                    float vx = 0.5f + q.x / (2f * q.z * tan * aspect);
                    float vy = 0.5f + q.y / (2f * q.z * tan);
                    fits &= vx >= 0.015f && vx <= 0.985f && vy >= 0.12f && vy <= 0.84f;
                }
            }

            return fits;
        }

        static float FarthestCorner(Vector3 camera, Bounds board)
        {
            float far = float.NegativeInfinity;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = board.center + Vector3.Scale(board.extents, new Vector3(x, 0f, z));
                    far = Mathf.Max(far, Vector3.Distance(camera, corner));
                }
            }

            return far;
        }
    }
}
