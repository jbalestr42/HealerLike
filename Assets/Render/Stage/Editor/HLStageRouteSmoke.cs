using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace HealerLike.Render.Stage
{
    // Exercise a genuine loss with an empty roster, then the actual render restart button.
    [InitializeOnLoad]
    public static class HLStageRouteSmoke
    {
        const string Key="HLStageRouteSmoke.Active";
        const string Output="/Users/fc/Documents/healerlike-render-specs/captures/wave9-route/";
        static int step; static int queuedFrame=-1;
        static double after,deadline;
        static EditorWindow gameView;
        static HLStageRouteSmoke() { EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=Changed; }
        public static void Run()
        {
            HLStageMenu.BuildMenuScene(); Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(HLStageMenu.MenuPath);
            SessionState.SetBool(Key,true); SessionState.SetInt(Key+"Code",1); EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(state==PlayModeStateChange.EnteredPlayMode) { step=0; queuedFrame=-1; after=EditorApplication.timeSinceStartup+.5; deadline=after+90; }
            if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key,false); EditorApplication.Exit(SessionState.GetInt(Key+"Code",1)); }
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying) return;
            if(!gameView) gameView=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"),false,null,false);
            gameView.Repaint();
            double now=EditorApplication.timeSinceStartup;
            if(now>deadline) { Debug.LogError("HL route FAIL step="+step); SessionState.SetInt(Key+"Code",1); EditorApplication.isPlaying=false; return; }
            if(now<after) return;
            string scene=SceneManager.GetActiveScene().name;
            if((step==0 || step==4) && scene==HLStageSceneLoader.MenuName)
            {
                if(queuedFrame<0) { ScreenCapture.CaptureScreenshot(Output+(step==0?"menu.png":"returned-menu.png")); queuedFrame=Time.frameCount; return; }
                if(Time.frameCount<=queuedFrame) return; queuedFrame=-1;
                var loader=Object.FindAnyObjectByType<HLStageSceneLoader>(); var button=loader?loader.GetComponent<UnityEngine.UI.Button>():null;
                if(!button) return; button.onClick.Invoke(); step++; after=now+1; Debug.Log("HL route menu Start -> gameplay"); return;
            }
            if(step==1 && scene==HLStageSceneLoader.SceneName)
            {
                var view=Object.FindAnyObjectByType<GameView>(); if(!view) return;
                view.gameHUD.startGameButton.onClick.Invoke(); step++; after=now+2; return;
            }
            if(step==2)
            {
                var view=Object.FindAnyObjectByType<GameView>(); if(!view || !view.gameHUD.nextWaveButton.interactable) return;
                view.gameHUD.nextWaveButton.onClick.Invoke(); step++; after=now+1; Debug.Log("HL route actual empty-roster battle started"); return;
            }
            if(step==3)
            {
                var view=Object.FindAnyObjectByType<GameOverView>(); if(!view || !view.gameObject.activeInHierarchy) return;
                var button=view.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b=>b.name=="RestartButton"); if(!button) return;
                if(queuedFrame<0) { ScreenCapture.CaptureScreenshot(Output+"gameover.png"); queuedFrame=Time.frameCount; return; }
                if(Time.frameCount<=queuedFrame) return; queuedFrame=-1; button.onClick.Invoke(); step++; after=now+1; Debug.Log("HL route GameOver restart -> render menu"); return;
            }
            if(step==5 && scene==HLStageSceneLoader.SceneName)
            {
                Debug.Log("HL route PASS menu -> play -> real gameover -> render menu -> play"); SessionState.SetInt(Key+"Code",0); EditorApplication.isPlaying=false;
            }
        }
    }
}
