using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    [Serializable]
    public class StageMotionFrame
    {
        public string file;
        public string group;
        public double requestedGameSeconds;
        public double requestedRealSeconds;
        public int requestedFrame;
        public int completedFrame;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;
        public float fieldOfView;
        public int width;
        public int height;
    }
}
