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
        public static readonly float[] PortraitTimes = { 1f, 4f, 8f, 12f };
        // HL_CAPTURE_LANDSCAPE=1: one frame at 8 s in the bootstrap's landscape framing, named <Prefix>land.png.
        public static readonly float[] LandscapeTimes = { 8f };
        static bool Landscape => System.Environment.GetEnvironmentVariable("HL_CAPTURE_LANDSCAPE")=="1";
        static float[] CaptureTimes => Landscape ? LandscapeTimes : PortraitTimes;
        public const string Prefix = "wave9-stage-";
        public static int EnemyHits => enemyHits;
        static double started;
        static int count, attacks, heals, enemyHits, castSlot, allyHits, launched, allyLaunched;
        static bool waveStarted;
        static readonly HashSet<EntityId> projectiles = new HashSet<EntityId>();
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
            if(state==PlayModeStateChange.EnteredPlayMode) { started=EditorApplication.timeSinceStartup; count=attacks=heals=enemyHits=castSlot=allyHits=launched=allyLaunched=0; gameStarted=waveStarted=false; projectiles.Clear(); observed.Clear(); placed=false; nextHeal=HealFrom; allies.Clear();
                new GameObject("HLCaptureHook").AddComponent<HLCaptureHook>().Late=Late;
                if(Landscape) FrameLandscape(); }
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
            if(count>=CaptureTimes.Length)
            {
                Debug.Log($"HL capture events: negative={attacks} (on enemies {enemyHits}, from ally entities {allyHits}) heals={heals} projectiles={launched} (from allies {allyLaunched}) pipeline={(GraphicsSettings.currentRenderPipeline?GraphicsSettings.currentRenderPipeline.name:"none")}");
                var key=UnityEngine.Object.FindAnyObjectByType<HLStageKeyLight>();
                Debug.Log(key ? $"HL capture key light: real shadows={key.RealShadows} cheap ellipses switched off={key.Suppressed} light={(key.KeyLight?key.KeyLight.name:"none")}" : "HL capture key light: none");
                Debug.Log(allyLaunched>0 && allyHits>0 ? "HL capture: ally auto-attack confirmed (projectile launched and a negative outcome from an ally entity)" : "HL capture: NO ally auto-attack observed");
                EditorApplication.isPlaying=false;
            }
        }
        // Runs after grass LateUpdate (10000) in the same player-loop frame, so indirect draws queued this frame are included.
        static void Late()
        {
            if(!gameStarted || count>=CaptureTimes.Length) return;
            // Keep grass frustum culling (next frame) and the render request on the same 9:16 aspect.
            if(Camera.main) Camera.main.aspect=Landscape ? (float)HLStageCalibration.PortraitHeight/HLStageCalibration.PortraitWidth : (float)HLStageCalibration.PortraitWidth/HLStageCalibration.PortraitHeight;
            Observe();
            float t=Time.time-gameStartTime;
            if(!placed && t>=PlaceAt) { placed=true; PlaceAllies(); }
            // probe-attacks.md: Start only loads the round; Next Wave is the player action that enables combat. After placement,
            // because Entity.Init disables every newly spawned unit and an empty roster could end the game.
            if(placed && !waveStarted) StartWave();
            TrackProjectiles();
            if(placed && heals==0 && t>=nextHeal) { nextHeal=t+1f; CastOn(allies.Find(a=>a)); }
            else if(placed && heals>0 && enemyHits==0 && t>=Mathf.Max(nextHeal,StrikeFrom)) { nextHeal=t+1f; CastOn(Array.Find(UnityEngine.Object.FindObjectsByType<Entity>(FindObjectsSortMode.InstanceID),e=>e.entityType!=Entity.EntityType.Player)); }
            if(t>=CaptureTimes[count]) Capture();
        }
        // Environment beauty: re-run the ridge (and the cropped foreground) after switching framing.
        static void FrameLandscape()
        {
            var bootstrap=UnityEngine.Object.FindAnyObjectByType<HLRenderBootstrap>();
            if(!bootstrap) { Debug.LogWarning("HL capture: no bootstrap, landscape framing not applied"); return; }
            bootstrap.CameraFraming=HLRenderBootstrap.Framing.Landscape;
            if(Camera.main) Camera.main.aspect=(float)HLStageCalibration.PortraitHeight/HLStageCalibration.PortraitWidth;
            foreach(var f in UnityEngine.Object.FindObjectsByType<HealerLike.Render.Environment.HLEnvironmentForeground>(FindObjectsSortMode.None)) f.Build();
            foreach(var r in UnityEngine.Object.FindObjectsByType<HealerLike.Render.Environment.HLEnvironmentRidge>(FindObjectsSortMode.None)) r.Build();
            Debug.Log("HL capture: landscape framing, foreground and ridge rebuilt");
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
        static void StartWave()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<GameView>();
            if(!view || !view.gameHUD || !view.gameHUD.nextWaveButton) return;
            waveStarted=true; view.gameHUD.nextWaveButton.onClick.Invoke();
            Debug.Log($"HL capture: Next Wave pressed at game time {Time.time-gameStartTime:F2}s with {allies.Count} allies placed");
        }
        // Distinct live projectiles, by source; editor fixture only, one scan per frame while capturing.
        static void TrackProjectiles()
        {
            foreach(var projectile in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if(!projectiles.Add(projectile.GetEntityId())) continue;
                launched++;
                var entity=projectile.source?projectile.source.GetComponent<Entity>():null;
                bool ally=entity && entity.entityType==Entity.EntityType.Player;
                if(ally) allyLaunched++;
                if(launched<=12) Debug.Log($"HL capture projectile: {projectile.name} from {(projectile.source?projectile.source.name:"none")}{(ally?" (ally)":"")} at game time {Time.time-gameStartTime:F2}s");
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
                        var from=modifier?.source?modifier.source.GetComponent<Entity>():null;
                        if(value<0 && from && from.entityType==Entity.EntityType.Player) allyHits++;
                        if(attacks+heals<=16) Debug.Log($"HL capture outcome: {(modifier?.source?modifier.source.name:"none")} -> {(owner?owner.name:"none")} {value:F1}");
                    });
        }
        static void Capture()
        {
            var camera=Camera.main;
            if(!camera) throw new InvalidOperationException("HL capture requires the gameplay camera.");
            // Portrait, as Julien's device autorotates: the aspect follows the target, not the batchmode screen.
            int width=Landscape?HLStageCalibration.PortraitHeight:HLStageCalibration.PortraitWidth, height=Landscape?HLStageCalibration.PortraitWidth:HLStageCalibration.PortraitHeight;
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
                string path=DirectoryPath+Prefix+(Landscape?"land":(count+1).ToString())+".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());
                count++; SessionState.SetInt(Key+"Count",count);
                foreach(var field in UnityEngine.Object.FindObjectsByType<HealerLike.Render.Grass.HLGrassField>(FindObjectsSortMode.InstanceID))
                {
                    // Instance count the compute wrote this frame: blades that survived frustum culling and were drawn.
                    var args=typeof(HealerLike.Render.Grass.HLGrassField).GetField("grassArgs",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(field) as GraphicsBuffer;
                    var data=new uint[5]; if(args!=null && args.IsValid()) args.GetData(data);
                    Debug.Log($"HL capture grass: {field.name} enabled={field.isActiveAndEnabled} ready={field.IsReady} blades={field.BladeCount} drawn={data[1]}");
                }
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
