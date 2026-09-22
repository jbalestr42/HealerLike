using System;
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
        static double started;
        static int count;
        static bool gameStarted;
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
            if(state==PlayModeStateChange.EnteredPlayMode) { started=EditorApplication.timeSinceStartup; count=0; }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                bool success=SessionState.GetInt(Key+"Count",0)==3;
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
                if(view && view.gameHUD && view.gameHUD.startGameButton) { view.gameHUD.startGameButton.onClick.Invoke(); gameStarted=true; }
            }
            if(count>=3 && EditorApplication.timeSinceStartup-started>=3) { EditorApplication.isPlaying=false; return; }
            if(count<3 && EditorApplication.timeSinceStartup-started >= count+1) Capture();
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
                string path=DirectoryPath+"HLRenderLook-"+(count+1)+".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());
                count++; SessionState.SetInt(Key+"Count",count); Debug.Log("HL screenshot: "+path);
            }
            finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
