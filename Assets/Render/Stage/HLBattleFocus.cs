using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HealerLike.Render.Environment;
namespace HealerLike.Render.Stage
{
    // Camera-only adapter: public wave button and round event, cached public entity lists.
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class HLBattleFocus : MonoBehaviour
    {
        [SerializeField] Camera stageCamera;
        [SerializeField] HLRenderBootstrap bootstrap;
        [SerializeField] Button nextWave;
        readonly Dictionary<Transform,Renderer[]> bodies=new();
        readonly List<Transform> live=new();
        EntityManager manager;
        HLEnvironmentForeground[] foreground;
        Canvas canvas; Text label; Button toggle;
        Pose target; Vector3 velocity; float refreshAt, fogAt;
        bool focused, settled, initialized;
        public bool IsFocused=>focused;
        public Button ToggleButton=>toggle;
        public Bounds CombatBounds { get; private set; }
        public int BodyCount=>live.Count;
        public bool IsSettled=>focused && AllBodiesVisible && Vector3.Distance(stageCamera.transform.position,target.position)<.1f;
        public bool AllBodiesVisible
        {
            get {
                if(!stageCamera || live.Count==0) return false;
                for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2) {
                    var p=stageCamera.WorldToViewportPoint(CombatBounds.center+Vector3.Scale(CombatBounds.extents,new Vector3(x,y,z)));
                    if(p.z<=stageCamera.nearClipPlane || p.x<.06f || p.x>.94f || p.y<.18f || p.y>.84f) return false;
                }
                return true;
            }
        }
        void MarkDirty(Entity entity) { refreshAt=0; }
        void BindEntities() { if(manager) { manager.OnEntitySpawned.AddListener(MarkDirty); manager.OnEntityKilled.AddListener(MarkDirty); } }
        public void Configure(Camera camera,HLRenderBootstrap owner,Button wave) { stageCamera=camera; bootstrap=owner; nextWave=wave; }
        void OnEnable()
        {
            if(nextWave) nextWave.onClick.AddListener(Focus);
            AscensionGameType.OnRoundEnd.AddListener(Overview);
            if(canvas) canvas.gameObject.SetActive(true);
            if(initialized) BindEntities();
        }
        void Start()
        {
            if(!stageCamera || !bootstrap) { enabled=false; return; }
            manager=EntityManager.instance; BindEntities(); foreground=GetComponentsInChildren<HLEnvironmentForeground>(true);
            target=bootstrap.OverviewPose; initialized=true; CreateToggle();
        }
        void OnDisable()
        {
            if(nextWave) nextWave.onClick.RemoveListener(Focus);
            AscensionGameType.OnRoundEnd.RemoveListener(Overview);
            if(canvas) canvas.gameObject.SetActive(false);
            if(manager) { manager.OnEntitySpawned.RemoveListener(MarkDirty); manager.OnEntityKilled.RemoveListener(MarkDirty); }
            if(initialized && stageCamera && bootstrap) { var pose=bootstrap.OverviewPose; stageCamera.transform.SetPositionAndRotation(pose.position,pose.rotation); bootstrap.UpdateCameraFog(pose.position); }
            foreach(var f in foreground ?? System.Array.Empty<HLEnvironmentForeground>()) if(f) f.enabled=true;
            bodies.Clear(); live.Clear();
        }
        public void Toggle() { if(focused) Overview(); else Focus(); }
        public void Focus()
        {
            if(!initialized) return;
            focused=true; settled=false; refreshAt=0;
            foreach(var f in foreground) if(f) f.enabled=false;
            RefreshBounds(); if(label) label.text="Overview";
            Debug.Log("HL camera Focus via public wave/toggle; bodies="+BodyCount);
        }
        public void Overview()
        {
            if(!initialized) return;
            focused=false; settled=false; target=bootstrap.OverviewPose;
            if(label) label.text="Focus battle";
            Debug.Log("HL camera Overview via public round-end/toggle");
        }
        void LateUpdate()
        {
            if(!initialized) return;
            if(focused && Time.unscaledTime>=refreshAt) RefreshBounds();
            if(!focused) target=bootstrap.OverviewPose;
            stageCamera.transform.position=Vector3.SmoothDamp(stageCamera.transform.position,target.position,ref velocity,.28f,Mathf.Infinity,Time.unscaledDeltaTime);
            stageCamera.transform.rotation=Quaternion.Slerp(stageCamera.transform.rotation,target.rotation,1-Mathf.Exp(-10*Time.unscaledDeltaTime));
            // Widen immediately when a spawn or spread leaves the safe viewport; only zoom-in eases.
            if(focused && !AllBodiesVisible) {
                var safe=HLBattleFocusBounds.Fit(CombatBounds,stageCamera.transform.eulerAngles.x,stageCamera.fieldOfView,stageCamera.aspect);
                stageCamera.transform.position=safe.position; velocity=Vector3.zero;
            }
            if(Time.unscaledTime>=fogAt) { fogAt=Time.unscaledTime+.1f; bootstrap.UpdateCameraFog(stageCamera.transform.position); }
            if(!focused && !settled && Vector3.Distance(stageCamera.transform.position,target.position)<.03f)
            {
                settled=true;
                foreach(var f in foreground) if(f) { f.enabled=true; f.Build(); }
            }
        }
        void RefreshBounds()
        {
            refreshAt=Time.unscaledTime+.4f;
            live.Clear();
            if(!manager) manager=EntityManager.instance;
            if(manager && manager.entities!=null)
            {
                Add(manager.GetEntities(Entity.EntityType.Player)); Add(manager.GetEntities(Entity.EntityType.Computer));
            }
            var character=PlayerBehaviour.instance ? PlayerBehaviour.instance.character : null;
            if(character) live.Add(character.transform);
            bool any=false; Bounds bounds=default;
            foreach(var body in live)
            {
                if(!bodies.TryGetValue(body,out var renderers)) bodies[body]=renderers=body.GetComponentsInChildren<Renderer>();
                // Actual authored mesh bounds include each head/root; exclude transient effects and lines.
                bool mesh=false;
                foreach(var r in renderers)
                    if(r && r.enabled && r.gameObject.activeInHierarchy && r is MeshRenderer && r.bounds.size.magnitude<8)
                    { if(!any) { bounds=r.bounds; any=true; } else bounds.Encapsulate(r.bounds); mesh=true; }
                if(!mesh) { var fallback=new Bounds(body.position+Vector3.up, new Vector3(1.2f,2,1.2f)); if(!any) { bounds=fallback; any=true; } else bounds.Encapsulate(fallback); }
            }
            var dead=new List<Transform>(); foreach(var pair in bodies) if(!pair.Key || !live.Contains(pair.Key)) dead.Add(pair.Key); foreach(var t in dead) bodies.Remove(t);
            if(!any) { Overview(); return; }
            bounds.Expand(new Vector3(.8f,.5f,.8f)); CombatBounds=bounds;
            target=HLBattleFocusBounds.Fit(bounds,52,stageCamera.fieldOfView,stageCamera.aspect);
        }
        void Add(List<GameObject> entries) { foreach(var go in entries) if(go && go.activeInHierarchy) live.Add(go.transform); }
        void CreateToggle()
        {
            var root=new GameObject("HLBattleCameraControls",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=120;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=1;
            var button=new GameObject("FocusOverview",typeof(RectTransform),typeof(Image),typeof(Button)); button.transform.SetParent(root.transform,false);
            var rect=button.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1); rect.anchoredPosition=new Vector2(-24,-142); rect.sizeDelta=new Vector2(260,70);
            button.GetComponent<Image>().color=new Color(.12f,.22f,.22f,.94f); toggle=button.GetComponent<Button>(); toggle.onClick.AddListener(Toggle);
            var text=new GameObject("Label",typeof(RectTransform),typeof(Text)); text.transform.SetParent(button.transform,false); var tr=text.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=tr.offsetMax=Vector2.zero;
            label=text.GetComponent<Text>(); label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=30; label.color=new Color(.94f,.97f,.83f); label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false; label.text="Focus battle";
        }
    }
    public static class HLBattleFocusBounds
    {
        // All eight 3D corners fit x=.08..92, y=.20..82, including tall heads and HUD room.
        public static Pose Fit(Bounds bounds,float pitch,float fov,float aspect)
        {
            var rotation=Quaternion.Euler(pitch,0,0); var inverse=Quaternion.Inverse(rotation);
            float tan=Mathf.Tan(fov*Mathf.Deg2Rad*.5f), distance=2;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
            {
                var q=inverse*Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                distance=Mathf.Max(distance,Mathf.Abs(q.x)/(.84f*tan*aspect)-q.z,(q.y-.64f*tan*q.z)/(.58f*tan),(-q.y-.6f*tan*q.z)/(.66f*tan));
            }
            return new Pose(bounds.center-rotation*Vector3.forward*distance-rotation*Vector3.up*(.06f*tan*distance),rotation);
        }
    }
}
