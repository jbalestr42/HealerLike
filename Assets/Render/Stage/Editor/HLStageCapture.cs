using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    [InitializeOnLoad]
    public static class HLStageCapture
    {
        const string Key = "HLStageCapture.Active";
        const string DirectoryPath = "/Users/fc/Documents/healerlike-render-specs/captures/";
        // Simulated seconds after the Start button: a wave has spawned by the second, attacks and heals by the last.
        public static readonly float[] CaptureTimes = { 1f, 4f, 8f };
        static double started;
        static int count, attacks, heals;
        static bool gameStarted;
        static float gameStartTime;
        static readonly HashSet<ResourceAttribute> observed = new HashSet<ResourceAttribute>();
        static HLStageCapture()
        {
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Tick;
        }
        public static void Run()
        {
            Directory.CreateDirectory(DirectoryPath);
            EditorSceneManager.OpenScene(HLStageBuilder.ScenePath);
            SessionState.SetBool(Key,true); SessionState.SetInt(Key+"Count",0);
            SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+180);
            EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(state==PlayModeStateChange.EnteredPlayMode) { started=EditorApplication.timeSinceStartup; count=attacks=heals=0; gameStarted=false; observed.Clear(); }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                bool success=SessionState.GetInt(Key+"Count",0)==CaptureTimes.Length;
                SessionState.SetBool(Key,false); EditorApplication.Exit(success?0:1);
            }
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Deadline",0)) { Debug.LogError("HL capture timed out"); SessionState.SetBool(Key,false); EditorApplication.Exit(2); return; }
            if(!EditorApplication.isPlaying || started==0) return;
            if(!gameStarted && EditorApplication.timeSinceStartup-started>.25)
            {
                var view=UnityEngine.Object.FindAnyObjectByType<GameView>();
                if(view && view.gameHUD && view.gameHUD.startGameButton) { view.gameHUD.startGameButton.onClick.Invoke(); gameStarted=true; gameStartTime=Time.time; }
            }
            if(!gameStarted) return;
            Observe();
            if(count>=CaptureTimes.Length) { Debug.Log($"HL capture events: attacks={attacks} heals={heals}"); EditorApplication.isPlaying=false; return; }
            if(Time.time-gameStartTime >= CaptureTimes[count]) Capture();
        }
        // Count resolved outcomes (signed pre-clamp deltas) on every live resource, without touching gameplay.
        static void Observe()
        {
            foreach(var resource in UnityEngine.Object.FindObjectsByType<ResourceAttribute>(FindObjectsSortMode.None))
                if(observed.Add(resource))
                    resource.OnAllConsumerProcessed.AddListener((owner,modifier,value,critical)=>{ if(value<0) attacks++; else if(value>0) heals++; });
        }
        static void Capture()
        {
            var camera=Camera.main;
            if(!camera) throw new InvalidOperationException("HL capture requires the gameplay camera.");
            var target=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                var request=new RenderPipeline.StandardRequest { destination=target };
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
                string path=DirectoryPath+"wave3-"+(count+1)+".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());
                count++; SessionState.SetInt(Key+"Count",count);
                Debug.Log($"HL screenshot: {path} at game time {Time.time-gameStartTime:F2}s; attacks={attacks} heals={heals} zones={HealerLike.Render.Zones.HLZoneRegistry.Current?.Count ?? -1}");
            }
            finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
