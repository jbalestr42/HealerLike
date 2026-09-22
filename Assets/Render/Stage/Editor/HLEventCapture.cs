using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace HealerLike.Render.Stage
{
    // An Editor-only player driver. It selects existing inventory and validated skill buttons;
    // it never changes health, mana, item rolls or gameplay clocks to manufacture an effect.
    [InitializeOnLoad]
    public static class HLEventCapture
    {
        const string Key="HLEventCapture.Active";
        const string Output="/Users/fc/Documents/healerlike-render-specs/captures/wave9-events/";
        static bool Landscape=>System.Environment.GetEnvironmentVariable("HL_CAPTURE_LANDSCAPE")=="1";
        static readonly Dictionary<ResourceAttribute,UnityAction<GameObject,ResourceModifier,float,bool>> listeners=new();
        static readonly List<Entity> allies=new();
        static readonly List<string> events=new();
        static double start;
        static float gameStart, nextFrame, nextHeal;
        static int frame, positiveHealth, negativeHealth, casts, projectiles;
        static bool menuPressed, gameStarted, placed, wave, groupCast, buffCast, finished, firstHit, screenshotHeal;
        static EditorWindow gameView;
        static readonly HashSet<EntityId> seen=new();
        static string Folder=>Output+(Landscape?"landscape/":"portrait/");
        static HLEventCapture() { EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=Changed; }
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            HLStageMenu.BuildMenuScene();
            SetGameViewSize(Landscape?1280:720,Landscape?720:1280);
            EditorSceneManager.OpenScene(HLStageMenu.MenuPath);
            SessionState.SetBool(Key,true); SessionState.SetInt(Key+"Code",1);
            SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+240);
            EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                start=EditorApplication.timeSinceStartup; frame=positiveHealth=negativeHealth=casts=projectiles=0;
                gameStart=nextFrame=nextHeal=0; menuPressed=gameStarted=placed=wave=groupCast=buffCast=finished=firstHit=screenshotHeal=false;
                events.Clear(); seen.Clear(); allies.Clear(); listeners.Clear();
                var go=new GameObject("HLRealEventCapture"); Object.DontDestroyOnLoad(go); go.AddComponent<HLEventCaptureHook>().Late=Late;
                ScreenCapture.CaptureScreenshot(Folder+"menu-ui.png");
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                Unsubscribe(); SessionState.SetBool(Key,false); EditorApplication.Exit(SessionState.GetInt(Key+"Code",1));
            }
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Deadline",0)) { Finish(false,"timeout"); return; }
            if(!EditorApplication.isPlaying || finished) return;
            Repaint();
            if(!menuPressed && EditorApplication.timeSinceStartup-start>.75)
            {
                var loader=Object.FindAnyObjectByType<HLStageSceneLoader>();
                var button=loader?loader.GetComponent<UnityEngine.UI.Button>():null;
                if(button) { menuPressed=true; button.onClick.Invoke(); Log("menu Start invoked"); }
            }
            if(menuPressed && !gameStarted && SceneManager.GetActiveScene().name==HLStageSceneLoader.SceneName)
            {
                var view=Object.FindAnyObjectByType<GameView>();
                if(view && view.gameHUD && view.gameHUD.startGameButton)
                {
                    view.gameHUD.startGameButton.onClick.Invoke();
                    if(view.gameHUD.playSpeedx1Button) view.gameHUD.playSpeedx1Button.onClick.Invoke();
                    gameStarted=true; gameStart=Time.time;
                    var bootstrap=Object.FindAnyObjectByType<HLRenderBootstrap>();
                    if(bootstrap) bootstrap.CameraFraming=Landscape?HLRenderBootstrap.Framing.Landscape:HLRenderBootstrap.Framing.Portrait;
                    foreach(var r in Object.FindObjectsByType<HealerLike.Render.Environment.HLEnvironmentRidge>(FindObjectsSortMode.None)) r.Build();
                    Log("game Start invoked; timeScale="+Time.timeScale);
                }
            }
        }
        static void Late()
        {
            if(!gameStarted || finished) return;
            float t=Time.time-gameStart;
            Observe();
            if(!placed && t>.7f) { placed=true; Place(); }
            if(placed && !wave && allies.Count>0)
            {
                var view=Object.FindAnyObjectByType<GameView>();
                if(view && view.gameHUD.nextWaveButton.interactable) { view.gameHUD.nextWaveButton.onClick.Invoke(); wave=true; Log("Next Wave invoked"); }
            }
            foreach(var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                if(seen.Add(p.GetEntityId())) { projectiles++; if(projectiles<8) Log("projectile "+p.name+" source="+(p.source?p.source.name:"null")); }
            var injured=allies.Find(a=>a && a.health && a.health.Value<a.health.Max-.5f);
            if(injured && t>nextHeal && positiveHealth==0) { nextHeal=t+1; Cast("Heal",injured); }
            if(t>8 && !groupCast) { groupCast=Cast("Heal group",null); }
            if(t>10 && !buffCast) { buffCast=Cast("Buff attack speed",null); }
            if(t>=nextFrame)
            {
                nextFrame=t+1f/12;
                RenderFrame("motion-"+frame.ToString("D5")+".png",Landscape?960:540,Landscape?540:960);
                events.Add($"FRAME,{frame},{t:F5},{Time.realtimeSinceStartup:F5}"); frame++;
                if(frame==12) { RenderFrame("gameplay.png",Landscape?1920:1080,Landscape?1080:1920); ScreenCapture.CaptureScreenshot(Folder+"gameplay-ui.png"); }
            }
            if(negativeHealth>0 && !firstHit) { firstHit=true; RenderFrame("first-contact.png",Landscape?1920:1080,Landscape?1080:1920); }
            if(positiveHealth>0 && !screenshotHeal) { screenshotHeal=true; RenderFrame("heal-outcome.png",Landscape?1920:1080,Landscape?1080:1920); ScreenCapture.CaptureScreenshot(Folder+"heal-ui.png"); }
            if(t>=15) Finish(wave && projectiles>0 && negativeHealth>0 && Mathf.Approximately(Time.timeScale,1),"15 second normal-speed sequence complete");
        }
        static void Place()
        {
            var player=PlayerBehaviour.instance; if(!player || !player.grid || !player.character) { Log("no player/grid/character"); return; }
            Log("character="+player.character.data.name+" skills="+string.Join("|",player.character.skillSlots.Select(s=>s.data.name)));
            var buttons=Object.FindObjectsByType<SelectEntityButton>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).Where(b=>b.data).ToArray();
            Log("inventory="+string.Join("|",buttons.Select(b=>b.data.name)));
            var enemy=Object.FindObjectsByType<Entity>(FindObjectsSortMode.InstanceID).FirstOrDefault(e=>e.entityType==Entity.EntityType.Computer);
            Vector3 anchor=enemy?enemy.transform.position:Vector3.zero;
            int index=0;
            foreach(string fragment in new[]{"Normal","ChainLightning","HitArmor"})
            {
                var button=buttons.FirstOrDefault(b=>b.data.name.Contains(fragment));
                if(!button) { Log("inventory absent: "+fragment); continue; }
                Vector3 position=player.grid.GetNearestWalkablePosition(anchor+new[]{Vector3.left,Vector3.back,Vector3.right}[index++%3]*player.grid.size);
                button.SelectEntity();
                var interaction=InteractionManager.instance.GetInteraction();
                var ray=new Ray(position+Vector3.up*30,Vector3.down);
                if(interaction!=null && Physics.Raycast(ray,out var hit,100,interaction.GetLayerMask()))
                {
                    interaction.OnMouseOver(hit); interaction.OnMouseClick(hit); Log("placed through inventory: "+button.data.name+" at "+position);
                }
                else { InteractionManager.instance.CancelInteraction(); Log("placement ray failed: "+fragment); }
            }
            allies.AddRange(Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Where(e=>e.entityType==Entity.EntityType.Player));
            Log("live allies="+allies.Count);
        }
        static bool Cast(string name,Entity target)
        {
            var character=Object.FindAnyObjectByType<Character>(); if(!character) return false;
            var slot=character.skillSlots.FirstOrDefault(s=>s && s.data!=null && s.data.name==name); if(!slot) return false;
            var button=Object.FindObjectsByType<UseCharacterSkillButton>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(b=>ReferenceEquals(b.data,slot.data));
            if(!button || !button.button.interactable) return false;
            bool single=slot.data is BaseCharacterSkillData d && d.isSingle;
            if(single && !target) return false;
            InteractionManager.instance.CancelInteraction(); button.button.onClick.Invoke();
            if(single)
            {
                var interaction=InteractionManager.instance.GetInteraction(); if(interaction==null) return false;
                var aim=target.targetPoint?target.targetPoint.transform.position:target.transform.position;
                var ray=new Ray(Camera.main.transform.position,aim-Camera.main.transform.position);
                if(!Physics.Raycast(ray,out var hit,1000,interaction.GetLayerMask()) || !interaction.IsValidTarget(hit.transform.gameObject)) { InteractionManager.instance.CancelInteraction(); return false; }
                interaction.OnMouseClick(hit);
            }
            casts++; Log("validated skill button: "+name+" single="+single); return true;
        }
        static void Observe()
        {
            foreach(var e in Object.FindObjectsByType<Entity>(FindObjectsSortMode.None)) if(e.health) Attach(e.health,"health");
            foreach(var c in Object.FindObjectsByType<Character>(FindObjectsSortMode.None)) if(c.mana) Attach(c.mana,"mana");
        }
        static void Attach(ResourceAttribute resource,string kind)
        {
            if(listeners.ContainsKey(resource)) return;
            UnityAction<GameObject,ResourceModifier,float,bool> callback=(owner,modifier,value,critical)=> {
                if(kind=="health") { if(value>0) positiveHealth++; if(value<0) negativeHealth++; }
                Log(kind+" delta="+value+" owner="+(owner?owner.name:"null")+" source="+(modifier?.source?modifier.source.name:"null"));
            };
            listeners.Add(resource,callback); resource.OnAllConsumerProcessed.AddListener(callback);
        }
        static void Unsubscribe() { foreach(var pair in listeners) if(pair.Key) pair.Key.OnAllConsumerProcessed.RemoveListener(pair.Value); listeners.Clear(); }
        static void Log(string s) { var line=$"EVENT,{Time.time-gameStart:F5},{s}"; events.Add(line); Debug.Log("HL real capture "+line); }
        static void Finish(bool success,string reason)
        {
            if(finished) return; finished=true; Unsubscribe();
            Log($"result={success} {reason}; frames={frame}; healthPositive={positiveHealth}; healthNegative={negativeHealth}; projectiles={projectiles}; casts={casts}; no transient hostile AoE is claimed without its real item event");
            File.WriteAllLines(Folder+"events.csv",events); SessionState.SetInt(Key+"Code",success?0:1); EditorApplication.isPlaying=false;
        }
        static void RenderFrame(string name,int width,int height)
        {
            // Capture the composed Game view after rendering. A manual URP StandardRequest before
            // the Game view consumes the one-frame indirect draws and makes its grass disappear.
            ScreenCapture.CaptureScreenshot(Folder+name);
        }
        static void Repaint() { if(!gameView) gameView=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"),false,null,false); gameView.Repaint(); }
        static void SetGameViewSize(int width,int height)
        {
            // Editor-only internal window API; fail visibly if this Unity version changes it.
            var assembly=typeof(Editor).Assembly; var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizes);
            var instance=singleton.GetProperty("instance").GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizes.GetMethod("GetGroup").Invoke(instance,new[]{Enum.Parse(groupType,"Standalone")});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize"); var type=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(type,"FixedResolution"),width,height,"HL event capture"});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            Repaint(); gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView,count-1);
        }
    }
    [DefaultExecutionOrder(32000)] sealed class HLEventCaptureHook:MonoBehaviour { public Action Late; void LateUpdate()=>Late?.Invoke(); }
}
