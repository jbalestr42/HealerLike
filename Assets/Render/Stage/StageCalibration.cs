using UnityEngine;
namespace HealerLike.Render.Stage
{
    public static class StageCalibration
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
        /// <summary>Perspective pose at a fixed pitch whose view fits the board's width plus margin at the board's
        /// near edge (the widest it projects), with the board centre drawn at the given viewport height.</summary>
        public static Pose Frame(Bounds board, float pitchDegrees, float fieldOfView, float aspect, float margin, float centreViewportY)
        {
            float p = pitchDegrees * Mathf.Deg2Rad, tanV = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * .5f), tanH = tanV * aspect;
            float n = 2 * centreViewportY - 1, hx = board.extents.x + margin, hz = board.extents.z, cos = Mathf.Cos(p), sin = Mathf.Sin(p);
            float shift = 0, distance = 0;
            // The shift moves the near edge, which changes the distance that fits it; this converges in a few steps.
            for (int i = 0; i < 16; i++)
            {
                distance = hx / tanH + (hz + shift) * cos;
                shift = Mathf.Abs(n) < 1e-6f ? 0 : n * tanV * distance / (n * tanV * cos - sin);
            }
            var rotation = Quaternion.Euler(pitchDegrees, 0, 0);
            var target = board.center + Vector3.forward * shift;
            return new Pose(target - rotation * Vector3.forward * distance, rotation);
        }
        // The fog starts past every playable corner, so the board keeps its authored colours.
        public static Vector2 BackgroundFog(Vector3 camera, Bounds board)
        {
            float far = 0;
            for (int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                far = Mathf.Max(far, Vector3.Distance(camera, board.center + Vector3.Scale(board.extents,new Vector3(x,0,z))));
            return new Vector2(far + 2, far + 36);
        }
        // Fit both axes, preserving room above the back row and below the front row for the HUD.
        public static Pose PlayableFrame(Bounds board, float pitch, float fov, float aspect, float centreY)
        {
            var pose=Frame(board,pitch,fov,aspect,.35f,centreY);
            var forward=pose.rotation*Vector3.forward;
            float tan=Mathf.Tan(fov*Mathf.Deg2Rad*.5f);
            for(int pass=0;pass<80;pass++)
            {
                bool fits=true;
                for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                {
                    var q=Quaternion.Inverse(pose.rotation)*(board.center+Vector3.Scale(board.extents,new Vector3(x,0,z))-pose.position);
                    float vx=.5f+q.x/(2*q.z*tan*aspect), vy=.5f+q.y/(2*q.z*tan);
                    fits &= vx>=.015f && vx<=.985f && vy>=.12f && vy<=.84f;
                }
                if(fits) return pose;
                pose.position-=forward*.35f;
            }
            return pose;
        }
        // Julien's Main camera: rotation x .5998, w .8002, FOV 40, portrait autorotation.
        public const float PortraitPitch = 52f, PortraitFov = 40, PortraitAspect = 9f / 16f, PortraitMargin = .35f, PortraitCentreY = .48f;
        public const int PortraitWidth = 1080, PortraitHeight = 1920;
    }
}
