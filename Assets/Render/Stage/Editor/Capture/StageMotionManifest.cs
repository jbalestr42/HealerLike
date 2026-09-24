using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    [Serializable]
    public class StageMotionManifest
    {
        public string source = "ScreenCapture.CaptureScreenshot, real Game view including screen overlay HUD";
        public string condition = "Static placement scene; fixed camera; two paused controls then nine moving frames over four game seconds";
        public string timing = "Times mark screenshot requests; completedFrame brackets the asynchronous file write";
        public string region = "Projected unoccupied back-left board patch, normalized bottom-left origin";
        public Rect grassRegion;
        public string unityVersion;
        public string gpu;
        public string revision;
        public float inkScale;
        public float inkStart;
        public bool grassNormalEdges;
        public bool grassCastsShadows;
        public bool cameraFixed;
        public bool isPassed;
        public float controlMeanDifference;
        public float motionMeanDifference;
        public float motionChangedFraction;
        public float maximumCameraPositionError;
        public float maximumCameraRotationError;
        public List<StageMotionFrame> frames = new List<StageMotionFrame>();
    }
}
