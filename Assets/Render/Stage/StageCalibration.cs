using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class StageCalibration
    {
        // Matches the game's Main camera: rotation x 0.5998, w 0.8002, FOV 40, portrait autorotation
        public static readonly float PortraitPitch = 52f;
        public static readonly float PortraitFov = 40f;
        public static readonly float PortraitAspect = 9f / 16f;
        public static readonly float PortraitCentreY = 0.48f;
        public static readonly int PortraitWidth = 1080;
        public static readonly int PortraitHeight = 1920;
        // The landscape preview keeps the wide framing from a lower pitch
        public static readonly float LandscapePitch = 46f;
        public static readonly float LandscapeAspect = 16f / 9f;
        public static readonly float LandscapeCentreY = 0.46f;
        // The game's grid cell, in world units; every size the views draw in cells is scaled by it
        public static readonly float CellSize = 1f;
        // Fog from clear to full over this many units past the far edge, well inside the environment ring
        public static readonly float BackgroundFogDepth = 10f;

        // Width margin of the first framing, then the step back until every board corner fits the HUD frame
        static readonly float playableMargin = 0.35f;
        static readonly float playableStepBack = 0.35f;

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

        // The fog starts at the board's far edge, as in the references, and the ring past it turns pale within
        // a few units; the ridge stands in the last band
        public static Vector2 BackgroundFog(Vector3 camera, Bounds board)
        {
            Vector3 farEdge = new Vector3(Mathf.Clamp(camera.x, board.min.x, board.max.x), board.center.y, board.max.z);
            float start = Vector3.Distance(camera, farEdge);
            return new Vector2(start, start + BackgroundFogDepth);
        }

        // Fits both axes, keeping room above the back row and below the front row for the HUD
        public static Pose PlayableFrame(Bounds board, float pitch, float fov, float aspect, float centreY)
        {
            Pose pose = Frame(board, pitch, fov, aspect, playableMargin, centreY);
            Vector3 forward = pose.rotation * Vector3.forward;
            float tan = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            for (int pass = 0; pass < 80; pass++)
            {
                if (FitsCorners(board, pose, tan, aspect))
                {
                    return pose;
                }

                pose.position -= forward * playableStepBack;
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
    }
}
