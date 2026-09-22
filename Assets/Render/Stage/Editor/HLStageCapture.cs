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
        public static int EnemyHits => enemyHits;
        static double started;
        static int count, attacks, heals, enemyHits, castSlot;
        static bool gameStarted;
        static float gameStartTime;
        static readonly HashSet<ResourceAttribute> observed = new HashSet<ResourceAttribute>();
        // Scripted player: the capture places allies and casts one heal through the same public calls a click makes.
        static readonly string[] Allies = { "Data/Entities/NormalEntity/HLNormalEntity.asset", "Data/Entities/ChainLightningEntity/HLChainLightningEntity.asset" };
        const float PlaceAt = .5f, HealFrom = 2.5f, StrikeFrom = 6f;
        static bool placed;
        static float nextHeal;
        static readonly List<Entity> allies = new List<Entity>();
        static EditorWindow editorGameView;
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
            if(state==PlayModeStateChange.EnteredPlayMode) { started=EditorApplication.timeSinceStartup; count=attacks=heals=enemyHits=castSlot=0; gameStarted=false; observed.Clear(); placed=false; nextHeal=HealFrom; allies.Clear();
                new GameObject("HLCaptureHook").AddComponent<HLCaptureHook>().Late=Late; }
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
            RepaintGameView();
            if(!gameStarted) return;
            Observe();
            if(count>=CaptureTimes.Length) { Debug.Log($"HL capture events: negative={attacks} (on enemies {enemyHits}) heals={heals}"); EditorApplication.isPlaying=false; }
        }
        // Runs after grass LateUpdate (10000) in the same player-loop frame, so indirect draws queued this frame are included.
        static void Late()
        {
            if(!gameStarted || count>=CaptureTimes.Length) return;
            // Keep grass frustum culling (next frame) and the render request on the same 9:16 aspect.
            if(Camera.main) Camera.main.aspect=(float)HLStageCalibration.PortraitWidth/HLStageCalibration.PortraitHeight;
            Observe();
            float t=Time.time-gameStartTime;
            if(!placed && t>=PlaceAt) { placed=true; PlaceAllies(); }
            if(placed && heals==0 && t>=nextHeal) { nextHeal=t+1f; CastOn(allies.Find(a=>a)); }
            else if(placed && heals>0 && enemyHits==0 && t>=Mathf.Max(nextHeal,StrikeFrom)) { nextHeal=t+1f; CastOn(Array.Find(UnityEngine.Object.FindObjectsByType<Entity>(FindObjectsSortMode.InstanceID),e=>e.entityType!=Entity.EntityType.Player)); }
            if(t>=CaptureTimes[count]) Capture();
        }
        // Batchmode has no visible Game view; asking it to repaint is what lets WaitForEndOfFrame coroutines (grid generation) resume.
        static void RepaintGameView()
        {
            try
            {
                if(!editorGameView) editorGameView=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"),false,null,false);
                editorGameView.Repaint();
            }
            catch(Exception e) { if(editorGameView==null) { Debug.LogWarning("HL capture: no Game view repaint in this session: "+e.Message); editorGameView=null; } }
        }
        static void PlaceAllies()
        {
            var enemy=Array.Find(UnityEngine.Object.FindObjectsByType<Entity>(FindObjectsSortMode.InstanceID),e=>e.entityType!=Entity.EntityType.Player);
            var grid=PlayerBehaviour.instance ? PlayerBehaviour.instance.grid : null;
            if(!grid || !EntityManager.instance) { Debug.LogWarning("HL capture: no grid or entity manager, allies not placed"); return; }
            Vector3 anchor=enemy ? enemy.transform.position : Vector3.zero;
            for(int i=0;i<Allies.Length;i++)
            {
                var data=AssetDatabase.LoadAssetAtPath<EntityData>(HLStageBuilder.Root+Allies[i]);
                if(!data) { Debug.LogWarning("HL capture: missing "+Allies[i]); continue; }
                var position=grid.GetNearestWalkablePosition(anchor+(i==0?Vector3.left:Vector3.back)*grid.size);
                var go=EntityManager.instance.SpawnEntity(data,position,Entity.EntityType.Player);
                if(go) allies.Add(go.GetComponent<Entity>());
                Debug.Log($"HL capture: placed {data.name} at {position} next to {(enemy?enemy.name+" "+enemy.transform.position:"no enemy")} -> {(go?go.name:"refused")}");
            }
        }
        // Tries the next skill slot on the target each attempt, as a player clicking the button then the target would.
        static void CastOn(Entity target)
        {
            var character=UnityEngine.Object.FindAnyObjectByType<Character>();
            if(!character || !target || !InteractionManager.instance) return;
            for(int n=0;n<character.skillSlots.Count;n++)
            {
                var slot=character.skillSlots[castSlot++%character.skillSlots.Count];
                if(!slot || slot.data==null) continue;
                InteractionManager.instance.CancelInteraction();
                slot.UseSkill();
                var interaction=InteractionManager.instance.GetInteraction();
                if(interaction==null) continue;
                var camera=Camera.main; var aim=target.targetPoint ? target.targetPoint.transform.position : target.transform.position;
                var ray=new Ray(camera.transform.position,aim-camera.transform.position);
                if(Physics.Raycast(ray,out var hit,Mathf.Infinity,interaction.GetLayerMask()) && interaction.IsValidTarget(hit.transform.gameObject))
                {
                    interaction.OnMouseClick(hit);
                    Debug.Log($"HL capture: cast skill slot {character.skillSlots.IndexOf(slot)} on {hit.transform.name} at game time {Time.time-gameStartTime:F2}s");
                    return; // one cast per attempt; the heal may resolve on a later frame
                }
                else InteractionManager.instance.CancelInteraction();
            }
        }
        // Count resolved outcomes (signed pre-clamp deltas) on every live resource, without touching gameplay.
        static void Observe()
        {
            foreach(var resource in UnityEngine.Object.FindObjectsByType<ResourceAttribute>(FindObjectsSortMode.None))
                if(observed.Add(resource))
                    resource.OnAllConsumerProcessed.AddListener((owner,modifier,value,critical)=>{
                        if(value<0) attacks++; else if(value>0) heals++;
                        var entity=owner?owner.GetComponent<Entity>():null; if(value<0 && entity && entity.entityType!=Entity.EntityType.Player) enemyHits++;
                        if(attacks+heals<=16) Debug.Log($"HL capture outcome: {(modifier?.source?modifier.source.name:"none")} -> {(owner?owner.name:"none")} {value:F1}");
                    });
        }
        static void Capture()
        {
            var camera=Camera.main;
            if(!camera) throw new InvalidOperationException("HL capture requires the gameplay camera.");
            // Portrait, as Julien's device autorotates: the aspect follows the target, not the batchmode screen.
            const int width=HLStageCalibration.PortraitWidth, height=HLStageCalibration.PortraitHeight;
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            camera.aspect=(float)width/height;
            try
            {
                var request=new RenderPipeline.StandardRequest { destination=target };
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
                string path=DirectoryPath+"wave4-"+(count+1)+".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());
                count++; SessionState.SetInt(Key+"Count",count);
                foreach(var field in UnityEngine.Object.FindObjectsByType<HealerLike.Render.Grass.HLGrassField>(FindObjectsSortMode.InstanceID))
                    Debug.Log($"HL capture grass: {field.name} enabled={field.isActiveAndEnabled} ready={field.IsReady} blades={field.BladeCount}");
                Debug.Log($"HL screenshot: {path} at game time {Time.time-gameStartTime:F2}s; attacks={attacks} heals={heals} zones={HealerLike.Render.Zones.HLZoneRegistry.Current?.Count ?? -1}");
            }
            finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
    // Editor-only play-mode hook; order after grass submission (10000).
    [DefaultExecutionOrder(32000), AddComponentMenu("")]
    sealed class HLCaptureHook : MonoBehaviour
    {
        public Action Late;
        void LateUpdate() => Late?.Invoke();
    }
}
