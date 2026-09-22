using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace HealerLike.Render.Stage
{
    // Batchmode smoke run of Julien's round loop on the stage scene, driven only by the public calls a click makes:
    // Start, place two allies, Next Wave; after each round pick the first upgrade, then Next Wave again.
    // Round state is read from AscensionGameType's own "[AscensionGameType] A -> B" transition log (its state is private).
    [InitializeOnLoad]
    public static class HLStageSmoke
    {
        const string Key = "HLStageSmoke.Active";
        const int Rounds = 3;
        // Settle: real seconds a state must hold before acting (grid graph scan, deferred Destroy of old choices).
        const float Deadline = 420, Round3Grace = 120, Settle = 1, Speed = 3;
        static readonly string[] Allies = { "Data/Entities/NormalEntity/HLNormalEntity.asset", "Data/Entities/ChainLightningEntity/HLChainLightningEntity.asset" };
        static readonly string[] KnownErrors = { "Insecure connection not allowed", "SearchDatabase" };
        static readonly Vector3[] Offsets = { Vector3.left, Vector3.back, Vector3.right, Vector3.forward, 2*Vector3.left, 2*Vector3.back, 2*Vector3.right, 3*Vector3.back, 3*Vector3.left, 4*Vector3.back };
        static readonly UnityAction<GameObject,ResourceModifier,float,bool> outcome = Outcome;
        static readonly UnityAction roundEnd = RoundEnd;
        static readonly HashSet<ResourceAttribute> observed = new HashSet<ResourceAttribute>();
        static readonly HashSet<EntityId> seen = new HashSet<EntityId>();
        static readonly List<string> errors = new List<string>();
        static double started, round3Battle;
        static bool gameStarted, finished, warnedGame, gameOver;
        static AscensionGameType.State state;
        static float stateTime;
        static int actedRound, pickedRound, roundsDone, exceptions, roundExceptions, projectiles, negative, positive, frames;
        static double frameMs;
        static EditorWindow editorGameView;
        static HLStageSmoke()
        {
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Tick;
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(HLStageBuilder.ScenePath);
            SessionState.SetBool(Key,true); SessionState.SetInt(Key+"Code",1);
            SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+Deadline);
            EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Key,false)) return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                started=EditorApplication.timeSinceStartup; round3Battle=0; gameStarted=finished=warnedGame=gameOver=false; state=AscensionGameType.State.None; stateTime=0;
                actedRound=pickedRound=roundsDone=exceptions=0; ResetRound(); observed.Clear(); seen.Clear(); errors.Clear();
                Application.logMessageReceived+=OnLog;
                AscensionGameType.OnRoundEnd.AddListener(roundEnd);
                new GameObject("HLSmokeHook").AddComponent<HLSmokeHook>().Late=Late;
            }
            if(change==PlayModeStateChange.EnteredEditMode)
            {
                int code=SessionState.GetInt(Key+"Code",1);
                SessionState.SetBool(Key,false); EditorApplication.Exit(code);
            }
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)) return;
            double now=EditorApplication.timeSinceStartup, deadline=SessionState.GetFloat(Key+"Deadline",0);
            if(now>deadline)
            {
                Unsubscribe(); if(EditorApplication.isPlaying && !finished) LogRound(actedRound,true);
                Debug.Log($"HL smoke result: FAIL rounds={roundsDone} exceptions={exceptions} reason=timeout after {Deadline}s in state {state}");
                SessionState.SetBool(Key,false); EditorApplication.Exit(2); return;
            }
            if(!EditorApplication.isPlaying || started==0 || finished) return;
            if(!gameStarted && now-started>.25)
            {
                var view=UnityEngine.Object.FindAnyObjectByType<GameView>();
                if(view && view.gameHUD && view.gameHUD.startGameButton) { view.gameHUD.startGameButton.onClick.Invoke(); gameStarted=true; SetSpeed(view.gameHUD); }
                else if(now-started>10) Finish(false,"no GameView/startGameButton in the stage scene");
            }
            RepaintGameView();
            // Round 3 has started its battle and is still running: a pass once the grace expires, or just before the hard deadline.
            if(round3Battle>0 && state==AscensionGameType.State.OnGoingBattle && (now-round3Battle>Round3Grace || now>deadline-15))
            {
                LogRound(actedRound,true);
                Finish(exceptions==0,$"round {actedRound} still running after {now-round3Battle:F0}s");
            }
        }
        // The HUD's own x3 speed button (TimeManager); a direct timeScale only if that button has no effect in this scene.
        static void SetSpeed(GameHUD hud)
        {
            if(hud.playSpeedx3Button) hud.playSpeedx3Button.onClick.Invoke();
            string how="playSpeedx3Button";
            if(!Mathf.Approximately(Time.timeScale,Speed)) { Time.timeScale=Speed; how="direct (no TimeManager listener)"; }
            Debug.Log($"HL smoke: Time.timeScale={Time.timeScale} via {how}");
        }
        static void Late()
        {
            if(!gameStarted || finished) return;
            Observe();
            frames++; frameMs+=Time.unscaledDeltaTime*1000.0;
            var game=UnityEngine.Object.FindAnyObjectByType<AscensionGameType>();
            if(!game) { if(!warnedGame) { warnedGame=true; Debug.LogWarning("HL smoke: no AscensionGameType in the stage scene"); } return; }
            // GameOver lasts one Update (it moves straight to None), so it is latched from the transition log.
            if(gameOver) { LogRound(actedRound,true); Finish(false,$"GameOver in round {game.currentRound}"); return; }
            float inState=Time.realtimeSinceStartup-stateTime;
            switch(state)
            {
                case AscensionGameType.State.WaitForRoundToStart:
                    if(actedRound!=game.currentRound && inState>=Settle) StartRound(game.currentRound);
                    break;
                case AscensionGameType.State.OnGoingBattle:
                    if(game.currentRound>=Rounds && round3Battle==0) round3Battle=EditorApplication.timeSinceStartup;
                    break;
                case AscensionGameType.State.SelectUpgrade:
                    if(pickedRound!=game.currentRound && inState>=Settle) { pickedRound=game.currentRound; PickUpgrade(); }
                    break;
            }
        }
        static void StartRound(int round)
        {
            actedRound=round;
            var view=UnityEngine.Object.FindAnyObjectByType<GameView>();
            if(round==1) PlaceAllies();
            else
            {
                // Julien's loop grants no units or gold per round: the pool is fixed at InitializeGame and gold is never earned or spent.
                var player=PlayerBehaviour.instance;
                Debug.Log($"HL smoke: round {round} nothing new to place (pool={(player && player.character ? player.character.entityPool.Count : -1)} gold={(player ? player.gold : -1)})");
            }
            if(!view || !view.gameHUD || !view.gameHUD.nextWaveButton) { Finish(false,"no nextWaveButton"); return; }
            if(!view.gameHUD.nextWaveButton.interactable) Debug.LogWarning($"HL smoke: nextWaveButton not interactable in round {round}, invoking anyway");
            view.gameHUD.nextWaveButton.onClick.Invoke();
            Debug.Log($"HL smoke: round {round} Next Wave at game time {Time.time:F2}s");
        }
        // First choice in the upgrade container, through the button's own onClick (-> Select*UpgradeButton.SelectUpgrade).
        static void PickUpgrade()
        {
            var choices=new List<MonoBehaviour>();
            choices.AddRange(UnityEngine.Object.FindObjectsByType<SelectItemUpgradeButton>());
            choices.AddRange(UnityEngine.Object.FindObjectsByType<SelectPlayerItemUpgradeButton>());
            choices.RemoveAll(c=>!c);
            if(choices.Count==0) { Finish(false,"upgrade view offered no choice"); return; }
            choices.Sort((a,b)=>a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            var first=choices[0]; var button=first.GetComponent<UnityEngine.UI.Button>();
            Debug.Log($"HL smoke: pick upgrade 1/{choices.Count} {first.GetType().Name} {first.name}");
            if(button && button.onClick.GetPersistentEventCount()>0) button.onClick.Invoke();
            else if(first is SelectItemUpgradeButton item) item.SelectUpgrade();
            else ((SelectPlayerItemUpgradeButton)first).SelectUpgrade();
        }
        static void PlaceAllies()
        {
            var manager=EntityManager.instance;
            var grid=PlayerBehaviour.instance ? PlayerBehaviour.instance.grid : null;
            if(!grid || !manager) { Debug.LogWarning("HL smoke: no grid or entity manager, allies not placed"); return; }
            var enemies=manager.GetEntities(Entity.EntityType.Computer);
            var enemy=enemies.Count>0 ? enemies[0] : null;
            Vector3 anchor=enemy ? enemy.transform.position : Vector3.zero;
            int next=0;
            foreach(var relative in Allies)
            {
                var data=AssetDatabase.LoadAssetAtPath<EntityData>(HLStageBuilder.Root+relative);
                if(!data) { Debug.LogWarning("HL smoke: missing "+relative); continue; }
                GameObject go=null; Vector3 position=anchor;
                // Nearest free walkable cell around the first enemy; SpawnEntity refuses occupied cells, so try the next offset.
                while(!go && next<Offsets.Length)
                {
                    try { position=grid.GetNearestWalkablePosition(anchor+Offsets[next++]*grid.size); go=manager.SpawnEntity(data,position,Entity.EntityType.Player); }
                    catch(Exception e) { Debug.LogWarning("HL smoke: placement failed: "+e.Message); break; }
                }
                Debug.Log($"HL smoke: placed {data.name} at {position} next to {(enemy?enemy.name:"no enemy")} -> {(go?go.name:"refused")}");
            }
        }
        static void Observe()
        {
            foreach(var resource in UnityEngine.Object.FindObjectsByType<ResourceAttribute>())
                if(observed.Add(resource)) resource.OnAllConsumerProcessed.AddListener(outcome);
            foreach(var projectile in UnityEngine.Object.FindObjectsByType<Projectile>())
                if(seen.Add(projectile.GetEntityId())) projectiles++;
        }
        static void Outcome(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if(value<0) negative++; else if(value>0) positive++;
        }
        static void RoundEnd()
        {
            if(finished) return;
            roundsDone++; LogRound(actedRound,false);
            if(roundsDone>=Rounds) Finish(exceptions==0,null);
        }
        static void LogRound(int round, bool partial)
        {
            var manager=EntityManager.instance;
            int allies=manager ? manager.GetEntities(Entity.EntityType.Player).Count : -1, enemies=manager ? manager.GetEntities(Entity.EntityType.Computer).Count : -1;
            Debug.Log($"HL smoke round {round}{(partial?" (partial)":"")}: alive allies={allies} enemies={enemies} projectiles launched={projectiles} negative={negative} positive={positive} frame ms avg={(frames>0?frameMs/frames:0):F2} exceptions={roundExceptions}");
            ResetRound();
        }
        static void ResetRound() { projectiles=negative=positive=frames=roundExceptions=0; frameMs=0; }
        static void OnLog(string message, string stack, LogType type)
        {
            if((type==LogType.Exception || type==LogType.Error) && Array.TrueForAll(KnownErrors,k=>!message.Contains(k)))
            {
                exceptions++; roundExceptions++;
                if(errors.Count<10) errors.Add(type+": "+message+"\n"+stack);
            }
            const string prefix="[AscensionGameType] ";
            int arrow=message.IndexOf(" -> ",StringComparison.Ordinal);
            if(message.StartsWith(prefix,StringComparison.Ordinal) && arrow>0 && Enum.TryParse(message.Substring(arrow+4).Trim(),out AscensionGameType.State next)) { state=next; stateTime=Time.realtimeSinceStartup; gameOver|=next==AscensionGameType.State.GameOver; }
        }
        static void Unsubscribe()
        {
            Application.logMessageReceived-=OnLog;
            AscensionGameType.OnRoundEnd.RemoveListener(roundEnd);
            foreach(var resource in observed) if(resource) resource.OnAllConsumerProcessed.RemoveListener(outcome);
            observed.Clear();
        }
        static void Finish(bool pass, string reason)
        {
            if(finished) return;
            finished=true; Unsubscribe();
            foreach(var error in errors) Debug.Log("HL smoke error: "+error);
            Debug.Log($"HL smoke result: {(pass?"PASS":"FAIL")} rounds={roundsDone} exceptions={exceptions}{(reason!=null?" reason="+reason:"")}");
            SessionState.SetInt(Key+"Code",pass?0:1);
            EditorApplication.isPlaying=false;
        }
        // Batchmode has no visible Game view; asking it to repaint is what lets WaitForEndOfFrame coroutines resume.
        static void RepaintGameView()
        {
            try
            {
                if(!editorGameView) editorGameView=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"),false,null,false);
                editorGameView.Repaint();
            }
            catch(Exception e) { if(editorGameView==null) { Debug.LogWarning("HL smoke: no Game view repaint in this session: "+e.Message); editorGameView=null; } }
        }
    }
    // Editor-only play-mode hook: polls after every gameplay Update/LateUpdate of the frame.
    [DefaultExecutionOrder(32000), AddComponentMenu("")]
    sealed class HLSmokeHook : MonoBehaviour
    {
        public Action Late;
        void LateUpdate() => Late?.Invoke();
    }
}
