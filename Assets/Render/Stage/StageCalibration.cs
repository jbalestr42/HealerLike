using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class StageCalibration
    {
        // Portrait looks along +X: Main places the healer at the origin and the enemy wave around x = 5.
        public static readonly float PortraitYaw = 90f;
        public static readonly float PortraitPitch = 52f;
        public static readonly float PortraitFov = 40f;
        public static readonly float PortraitAspect = 9f / 16f;
        public static readonly float PortraitCentreY = 0.35f;
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
        // The game camera's near plane; a corner closer than it is not in view
        public static readonly float NearClip = 0.1f;

        // Width margin of the first framing, then the step back until every board corner fits the HUD frame
        static readonly float playableMargin = 0.35f;
        static readonly float playableStepBack = 0.35f;
        static readonly int playableSteps = 80;
        // The overview keeps the board's corners between the HUD bars, nearly edge to edge across
        static readonly Rect boardFrame = Rect.MinMaxRect(0.015f, 0.12f, 0.985f, 0.84f);

        // Perspective pose at a fixed pitch whose view fits the board's width plus margin at its near edge
        // (the widest it projects), with the board centre drawn at the given viewport height
        public static Pose Frame(Bounds board, float pitchDegrees, float fieldOfView, float aspect, float margin,
            float centreViewportY, float yawDegrees = 0f)
        {
            float pitch = pitchDegrees * Mathf.Deg2Rad;
            float tanV = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * aspect;
            float n = 2f * centreViewportY - 1f;
            Quaternion heading = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 extents = HeadingExtents(board.extents, heading);
            float hx = extents.x + margin;
            float hz = extents.z;
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

            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 target = board.center + heading * Vector3.forward * shift;
            return new Pose(target - rotation * Vector3.forward * distance, rotation);
        }

        // The fog starts at the board's far edge, as in the references, and the ring past it turns pale within
        // a few units; the ridge stands in the last band
        public static Vector2 BackgroundFog(Vector3 camera, Bounds board, float yawDegrees = 0f)
        {
            Quaternion heading = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 localCamera = Quaternion.Inverse(heading) * (camera - board.center);
            Vector3 extents = HeadingExtents(board.extents, heading);
            Vector3 farEdge = board.center + heading * new Vector3(
                Mathf.Clamp(localCamera.x, -extents.x, extents.x), 0f, extents.z);
            float start = Vector3.Distance(camera, farEdge);
            return new Vector2(start, start + BackgroundFogDepth);
        }

        // Fits both axes, keeping room above the back row and below the front row for the HUD
        public static Pose PlayableFrame(Bounds board, float pitch, float fov, float aspect, float centreY,
            float yawDegrees = 0f)
        {
            Pose pose = Frame(board, pitch, fov, aspect, playableMargin, centreY, yawDegrees);
            Vector3 forward = pose.rotation * Vector3.forward;
            for (int step = 0; step < playableSteps; step++)
            {
                if (Contains(board, pose, fov, aspect, boardFrame))
                {
                    return pose;
                }

                pose.position -= forward * playableStepBack;
            }

            return pose;
        }

        // The nearest pose at this pitch that keeps all eight corners of the box inside the viewport frame, with
        // the view lowered by lift times the half height so the box sits that much above the frame's middle
        public static Pose Fit(Bounds box, float pitch, float fov, float aspect, Rect frame, float lift,
            float yawDegrees = 0f)
        {
            return Fit(box, Quaternion.Euler(pitch, yawDegrees, 0f), fov, aspect, frame, lift);
        }

        // Fits in the supplied view orientation, including during a turn between overview and focus.
        public static Pose Fit(Bounds box, Quaternion rotation, float fov, float aspect, Rect frame, float lift)
        {
            Quaternion inverse = Quaternion.Inverse(rotation);
            float tan = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            // Each frame edge as a distance from the view centre, in half heights and half widths
            float right = 2f * frame.xMax - 1f;
            float left = 1f - 2f * frame.xMin;
            float top = 2f * frame.yMax - 1f;
            float bottom = 1f - 2f * frame.yMin;
            float distance = 2f;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = inverse * Vector3.Scale(box.extents, RenderMath.CornerSign(corner));
                distance = Mathf.Max(distance, local.x / (right * tan * aspect) - local.z);
                distance = Mathf.Max(distance, -local.x / (left * tan * aspect) - local.z);
                distance = Mathf.Max(distance, (local.y - top * tan * local.z) / ((top - lift) * tan));
                distance = Mathf.Max(distance, (-local.y - bottom * tan * local.z) / ((bottom + lift) * tan));
            }

            Vector3 position = box.center - rotation * Vector3.forward * distance
                               - rotation * Vector3.up * (lift * tan * distance);
            return new Pose(position, rotation);
        }

        static Vector3 HeadingExtents(Vector3 extents, Quaternion heading)
        {
            Vector3 right = heading * Vector3.right;
            Vector3 forward = heading * Vector3.forward;
            return new Vector3(Mathf.Abs(right.x) * extents.x + Mathf.Abs(right.z) * extents.z, extents.y,
                Mathf.Abs(forward.x) * extents.x + Mathf.Abs(forward.z) * extents.z);
        }

        // Whether all eight corners of the box are in front of the near plane and inside the viewport frame
        public static bool Contains(Bounds box, Pose pose, float fov, float aspect, Rect frame)
        {
            Quaternion inverse = Quaternion.Inverse(pose.rotation);
            float tan = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = RenderMath.Corner(box, corner);
                Vector3 local = inverse * (point - pose.position);
                if (local.z <= NearClip)
                {
                    return false;
                }

                float x = 0.5f + local.x / (2f * local.z * tan * aspect);
                float y = 0.5f + local.y / (2f * local.z * tan);
                if (x < frame.xMin || x > frame.xMax || y < frame.yMin || y > frame.yMax)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
