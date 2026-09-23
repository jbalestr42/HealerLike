using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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
        static bool Hostile=>System.Environment.GetEnvironmentVariable("HL_CAPTURE_HOSTILE")=="1";
        static bool Landscape=>System.Environment.GetEnvironmentVariable("HL_CAPTURE_LANDSCAPE")=="1";
        static readonly Dictionary<ResourceAttribute,UnityAction<GameObject,ResourceModifier,float,bool>> listeners=new();
        static readonly List<Entity> allies=new();
        static readonly HashSet<BuffManager> buffManagers=new();
        static readonly UnityAction<BuffManager.BuffHandlerData> buffStarted=OnBuffStarted;
        static uint lifetimeMax; static int lifetimeSample;
        static AreaOfEffect lifetimeArea; static float lifetimeBorn; static bool lifetimeStarted,lifetimeLive,lifetimeDestroyed;
        static float hostileFinishAt; static int hostileAreas; static readonly HashSet<EntityId> seenAreas=new();
        static float overviewAt,refocusAt;
        static int statuses; static bool speedValid; static bool overviewRequested,overviewVerified,refocusRequested;
        static readonly List<string> events=new();
        static readonly Dictionary<string,int> stillFrames=new();
        static readonly List<string> pendingStills=new();
        static double start;
        static float gameStart, nextFrame, nextHeal;
        static int frame, positiveHealth, negativeHealth, casts, projectiles;
        static bool menuPressed, gameStarted, placed, wave, groupCast, buffCast, finished, firstHit, screenshotHeal;
        static EditorWindow gameView;
        static readonly HashSet<EntityId> seen=new();
        static string Folder=>SessionState.GetString(Key+"Folder",Output);
        static HLEventCapture() { EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=Changed; }
        public static void Run()
        {
            SessionState.SetString(Key+"Folder",Output+(Hostile?"hostile-":"")+(Landscape?"landscape-":"portrait-")+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"/");
            Directory.CreateDirectory(Folder); Debug.Log("HL real capture output: "+Folder);
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
                events.Clear(); stillFrames.Clear(); pendingStills.Clear(); seen.Clear(); allies.Clear(); listeners.Clear(); buffManagers.Clear(); lifetimeArea=null; lifetimeBorn=0; lifetimeMax=0; lifetimeSample=-1; lifetimeStarted=lifetimeLive=lifetimeDestroyed=false; hostileFinishAt=0; overviewAt=refocusAt=0; statuses=hostileAreas=0; seenAreas.Clear(); speedValid=true; overviewRequested=overviewVerified=refocusRequested=false;
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
            speedValid &= Mathf.Approximately(Time.timeScale,1);
            Observe();
            if(!placed && t>.7f) { placed=true; Place(); if(Hostile) { EquipExistingExplosion(); var cameraControl=Object.FindAnyObjectByType<HLBattleFocus>(); if(cameraControl && cameraControl.ToggleButton) cameraControl.ToggleButton.onClick.Invoke(); } }
            if(placed && !wave && allies.Count>0 && (!Hostile || Object.FindAnyObjectByType<HLBattleFocus>().IsSettled))
            {
                var view=Object.FindAnyObjectByType<GameView>();
                if(view && view.gameHUD.nextWaveButton.interactable) { view.gameHUD.nextWaveButton.onClick.Invoke(); wave=true; Log("Next Wave invoked"); }
            }
            foreach(var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                if(seen.Add(p.GetEntityId())) { projectiles++; if(projectiles<8) Log("projectile "+p.name+" source="+(p.source?p.source.name:"null")); }
            if(Hostile) foreach(var area in Object.FindObjectsByType<AreaOfEffect>(FindObjectsSortMode.None))
                if(area.source && area.source.TryGetComponent<Entity>(out var enemy) && enemy.entityType==Entity.EntityType.Computer && seenAreas.Add(area.GetEntityId()))
                { if(System.Environment.GetEnvironmentVariable("HL_CAPTURE_LEGACY")=="1") { var mask=area.GetComponent<HLLegacyAreaVisualMask>(); if(mask) mask.enabled=false; } if(!lifetimeStarted) { lifetimeArea=area; lifetimeBorn=t; lifetimeStarted=true; } hostileAreas++; Log("actual hostile AreaOfEffect source="+area.source.name+" radius="+area.radius+" position="+area.transform.position+" pulse="+(area.GetComponent<HealerLike.Render.Zones.HLAreaPulse>()!=null)); if(!stillFrames.ContainsKey("hostile-area-ui.png") && !pendingStills.Contains("hostile-area-ui.png")) pendingStills.Add("hostile-area-ui.png"); }
            if(Hostile && lifetimeStarted) {
                if(lifetimeArea) {
                    var vfx=lifetimeArea.GetComponentsInChildren<Behaviour>(true).FirstOrDefault(b=>b.GetType().FullName=="UnityEngine.VFX.VisualEffect");
                    if(vfx) {
                        uint alive=Convert.ToUInt32(vfx.GetType().GetProperty("aliveParticleCount").GetValue(vfx));
                        lifetimeMax=System.Math.Max(lifetimeMax,alive);
                        int bucket=Mathf.FloorToInt((t-lifetimeBorn)*4);
                        if(bucket>lifetimeSample) { lifetimeSample=bucket; Log("legacy area particle sample alive="+alive+" age="+(t-lifetimeBorn)+" masked="+(lifetimeArea.GetComponent<HLLegacyAreaVisualMask>() ? lifetimeArea.GetComponent<HLLegacyAreaVisualMask>().MaskedPropertyCount : 0)); }
                        if(alive>0 && t-lifetimeBorn>1) lifetimeLive=true;
                    }
                } else if(!lifetimeDestroyed) { lifetimeDestroyed=true; Log("legacy area destroyed naturally at age="+(t-lifetimeBorn)+"; peak particles="+lifetimeMax+"; observed live particles after one second="+lifetimeLive); }
            }
            var injured=allies.Find(a=>a && a.health && a.health.Value<a.health.Max-.5f);
            if(injured && t>3 && t>nextHeal && positiveHealth==0) { nextHeal=t+1; Cast("Heal",injured); }
            if(t>8 && !groupCast) { groupCast=Cast("Heal group",null); }
            if(t>10 && !buffCast) { buffCast=Cast("Buff attack speed",null); }
            if(t<15 && t>=nextFrame && hostileFinishAt==0)
            {
                nextFrame=t+1f/12;
                RenderFrame("motion-"+frame.ToString("D5")+".png",Landscape?960:540,Landscape?540:960);
                events.Add($"FRAME,{frame},{t:F5},{Time.realtimeSinceStartup:F5}"); frame++;
                foreach(var name in pendingStills) stillFrames[name]=frame-1; pendingStills.Clear();
                if(frame==48) stillFrames["gameplay-ui.png"]=frame-1;
                if(Hostile && stillFrames.ContainsKey("hostile-area-ui.png")) hostileFinishAt=t+.5f;
            }
            if(negativeHealth>0 && !firstHit) { firstHit=true; pendingStills.Add("first-contact.png"); }
            if(positiveHealth>0 && !screenshotHeal) { screenshotHeal=true; pendingStills.Add("heal-ui.png"); }
            if(Hostile) {
                if(hostileFinishAt>0 && t>=hostileFinishAt && lifetimeDestroyed) Finish(hostileAreas>0 && negativeHealth>0 && speedValid && lifetimeMax>=1536 && lifetimeLive && t-lifetimeBorn>=2f && t-lifetimeBorn<=3f,"supplemental actual debug-inventory hostile area plus damage; source and radius logged; waited for queued still");
                else if(t>=14) Finish(false,"no real hostile area recorded from existing inventory");
                return;
            }
            var focus=Object.FindAnyObjectByType<HLBattleFocus>();
            if(t>=15.3f && !overviewRequested) {
                if(focus && focus.IsSettled && focus.ToggleButton) {
                    overviewRequested=true; overviewAt=t;
                    Log("verified settled automatic battle focus, current body corners inside safe viewport; bounds="+focus.CombatBounds+"; bodies="+focus.BodyCount);
                    focus.ToggleButton.onClick.Invoke();
                } else if(t>=18.3f) { Finish(false,"automatic battle focus did not settle within three seconds; focused="+(focus && focus.IsFocused)+" visible="+(focus && focus.AllBodiesVisible)); return; }
            }
            if(overviewRequested && t>=overviewAt+1.5f && !overviewVerified) {
                var bootstrap=Object.FindAnyObjectByType<HLRenderBootstrap>();
                overviewVerified=focus && !focus.IsFocused && bootstrap && Vector3.Distance(Camera.main.transform.position,bootstrap.OverviewPose.position)<.1f;
                if(!overviewVerified) { Finish(false,"Overview button did not restore placement frame"); return; }
                ScreenCapture.CaptureScreenshot(Folder+"overview-ui.png"); Log("verified Overview button and restored pose");
            }
            if(overviewVerified && t>=overviewAt+1.7f && !refocusRequested) { refocusRequested=true; refocusAt=t; focus.ToggleButton.onClick.Invoke(); }
            if(refocusRequested && t>=refocusAt+1.5f) {
                if(focus && focus.IsSettled) Finish(wave && projectiles>0 && negativeHealth>0 && positiveHealth>0 && statuses>0 && speedValid,"15 second normal-speed battle sequence plus verified Overview/Focus controls");
                else if(t>=refocusAt+4.5f) Finish(false,"refocus did not settle within bounded wait");
            }
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
        static void EquipExistingExplosion()
        {
            var view=Object.FindAnyObjectByType<GameView>();
            var enemy=Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).FirstOrDefault(e=>e.entityType==Entity.EntityType.Computer && e.data && e.data.name.Contains("Soldier"));
            if(!view || !enemy) { Log("hostile route absent view/enemy"); return; }
            var inventory=view.playerInventory.inventory;
            var data=inventory.inventoryHandler.items.FirstOrDefault(i=>i.item.title=="ExplodeOnHit");
            if(data==null) { Log("existing debug inventory has no ExplodeOnHit; no item created"); return; }
            view.gameHUD.inventoryButton.onClick.Invoke();
            var slot=inventory.GetComponentsInChildren<SlotInventoryItem>(true).FirstOrDefault(i=>i.index==data.inventoryIndex);
            var item=slot?slot.GetComponentInChildren<InventoryItem>(true):null;
            var selectable=enemy.GetComponent<SelectableEntity>();
            if(!item || !selectable) { Log("hostile inventory UI route unavailable"); return; }
            InteractionManager.instance.Select(selectable);
            var panel=Object.FindAnyObjectByType<PanelEntity>();
            var targetInventory=panel?panel.GetComponentsInChildren<SlotInventory>(true).FirstOrDefault(i=>ReferenceEquals(i.inventoryHandler,enemy.inventoryHandler)):null;
            if(!targetInventory) { InteractionManager.instance.CancelSelection(); Log("enemy inventory panel unavailable"); return; }
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { pointerDrag=item.gameObject };
            item.OnBeginDrag(pointer); targetInventory.GetEmptySlot().OnDrop(pointer); item.OnEndDrag(pointer);
            Log("actual debug-inventory drag/drop equipped existing "+data.item.title+" on "+enemy.name+"; sourceType="+enemy.entityType);
            InteractionManager.instance.CancelSelection();
            if(view.playerInventory.IsInventoryVisible()) view.gameHUD.inventoryButton.onClick.Invoke();
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
            foreach(var e in Object.FindObjectsByType<Entity>(FindObjectsSortMode.None)) { if(e.health) Attach(e.health,"health"); if(e.buffManager && buffManagers.Add(e.buffManager)) e.buffManager.OnBuffHandlerStarted.AddListener(buffStarted); }
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
        static void OnBuffStarted(BuffManager.BuffHandlerData data) { statuses++; Log("buff-start factory="+(data.buffHandlerFactory?data.buffHandlerFactory.name:"null")+" target="+(data.target?data.target.name:"null")); }
        static void Unsubscribe() { foreach(var manager in buffManagers) if(manager) manager.OnBuffHandlerStarted.RemoveListener(buffStarted); buffManagers.Clear(); foreach(var pair in listeners) if(pair.Key) pair.Key.OnAllConsumerProcessed.RemoveListener(pair.Value); listeners.Clear(); }
        static void Log(string s) { var line=$"EVENT,{Time.time-gameStart:F5},{s}"; events.Add(line); Debug.Log("HL real capture "+line); }
        static void Finish(bool success,string reason)
        {
            if(finished) return; finished=true; Unsubscribe();
            int missing=0;
            for(int i=0;i<frame;i++) if(!File.Exists(Folder+"motion-"+i.ToString("D5")+".png")) missing++;
            foreach(var pair in stillFrames) {
                string source=Folder+"motion-"+pair.Value.ToString("D5")+".png";
                if(File.Exists(source)) { File.Copy(source,Folder+pair.Key,true); events.Add("STILL,"+pair.Key+","+pair.Value); }
                else missing++;
            }
            success &= missing==0 && stillFrames.ContainsKey(Hostile?"hostile-area-ui.png":"heal-ui.png");
            reason+="; missing current-run frame files="+missing;
            Log($"result={success} {reason}; frames={frame}; healthPositive={positiveHealth}; healthNegative={negativeHealth}; projectiles={projectiles}; casts={casts}; observedStatuses={statuses}; actualHostileAreas={hostileAreas}; normalSpeedThroughout={speedValid}; hostile evidence requires actual source/radius item event");
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
