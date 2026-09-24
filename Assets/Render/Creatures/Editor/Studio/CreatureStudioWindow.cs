using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    public sealed partial class CreatureStudioWindow : EditorWindow
    {
        private static readonly Color Background = new Color(.055f,.065f,.083f);
        private static readonly Color Panel = new Color(.085f,.098f,.12f);
        private static readonly Color Accent = new Color(.75f,.68f,1f);
        private readonly List<CreatureRecipe> drafts = new List<CreatureRecipe>();
        private readonly List<CreatureRecipe> assets = new List<CreatureRecipe>();
        private CreatureRecipe selected;
        private SerializedObject serialized;
        private CreatureStudioPreview preview;
        private int selectedPart, selectedArm;
        private Primitive newPrimitive = Primitive.Sphere;
        private string search = "";
        private Vector2 libraryScroll, inspectorScroll, partsScroll;
        private GUIStyle titleStyle, sectionStyle, smallStyle, cardStyle;
        private bool playing = true, loop = true, validationDirty = true;
        private float time, speed = 1, duration = 8;
        private double lastTick;
        private string[] warnings = Array.Empty<string>();
        [Serializable] private sealed class DraftRecord { public string name; public string json; }
        [Serializable] private sealed class DraftCollection
        {
            public List<DraftRecord> items = new List<DraftRecord>();
            public int selectedIndex = -1;
            public string selectedAsset;
        }
        private static string DraftKey => "HealerLike.CreatureStudio.Drafts." + Application.dataPath;

        [MenuItem("Tools/Render/Creature Studio", false, 111)]
        [MenuItem("Tools/Render/Render Studio", false, 109)]
        public static void Open()
        {
            bool existing = HasOpenInstances<CreatureStudioWindow>();
            var window = GetWindow<CreatureStudioWindow>();
            if (!existing) window.position = new Rect(80,80,1360,850);
            window.titleContent = new GUIContent("Creature Studio");
            window.Show();
        }

        public static CreatureStudioWindow OpenRecipe(CreatureRecipe recipe)
        {
            Open();
            var window = GetWindow<CreatureStudioWindow>();
            if (recipe != null) window.SwitchToParts(recipe);
            window.Focus();
            return window;
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        private static bool OnOpenRecipeAsset(EntityId entityId, int line)
        {
            var recipe = EditorUtility.EntityIdToObject(entityId) as CreatureRecipe;
            if (recipe == null) return false;
            OpenRecipe(recipe);
            return true;
        }

        internal static void NotifyRecipeChanged(CreatureRecipe recipe)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CreatureStudioWindow>())
                if (window.selected == recipe) { window.serialized?.Update(); window.RefreshPreview(); window.Repaint(); }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Creature Studio");
            minSize = new Vector2(1120,680);
            preview = new CreatureStudioPreview();
            if (!RestoreDrafts())
                for (int i=0;i<CreatureStudioAuthoring.SampleNames.Length;i++)
                {
                    var draft = CreatureStudioAuthoring.BuildSample(i);
                    if (draft != null) { draft.hideFlags = HideFlags.HideAndDontSave; drafts.Add(draft); }
                }
            ReloadAssets();
            Select(selected != null ? selected : drafts.Count > 0 ? drafts[0] : null);
            InitializeGrammar();
            lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.projectChanged += ReloadAssets;
            Undo.undoRedoPerformed += OnUndo;
        }

        private void OnDisable()
        {
            PersistDrafts();
            DisposeGrammar();
            EditorApplication.update -= Tick;
            EditorApplication.projectChanged -= ReloadAssets;
            Undo.undoRedoPerformed -= OnUndo;
            preview?.Dispose(); preview = null;
            serialized?.Dispose(); serialized = null;
            foreach (var draft in drafts) if (draft != null) DestroyImmediate(draft);
            drafts.Clear();
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing && selected != null)
            {
                time += (float)System.Math.Min(now-lastTick,.1)*speed;
                if (time >= duration) { if (loop) time %= duration; else { time=duration; playing=false; } }
                Repaint();
            }
            lastTick=now;
        }

        private void OnUndo() { serialized?.Update(); if (grammarMode) RegenerateGrammar(); RefreshPreview(); Repaint(); }
        private void RefreshPreview() { validationDirty=true; preview?.Refresh(); }

        private void ReloadAssets()
        {
            assets.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:CreatureRecipe"))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe != null) assets.Add(recipe);
            }
            assets.Sort((a,b)=>string.Compare(a.name,b.name,StringComparison.OrdinalIgnoreCase));
            ReloadGrammarAssets();
            RefreshPreview(); Repaint();
        }

        private void Select(CreatureRecipe recipe)
        {
            if (serialized != null && selected != null) serialized.ApplyModifiedProperties();
            serialized?.Dispose();
            selected=recipe;
            serialized=recipe != null ? new SerializedObject(recipe) : null;
            selectedPart=0; time=0; inspectorScroll=Vector2.zero;
            RefreshPreview(); Repaint();
        }

        private void PersistDrafts()
        {
            var selection=grammarMode ? partsSelection : selected;
            var collection = new DraftCollection { selectedIndex=drafts.IndexOf(selection),
                selectedAsset=selection != null && AssetDatabase.Contains(selection) ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selection)) : "" };
            foreach (var draft in drafts) if (draft != null) collection.items.Add(new DraftRecord { name=draft.name,json=JsonUtility.ToJson(draft) });
            EditorPrefs.SetString(DraftKey,JsonUtility.ToJson(collection));
        }

        private bool RestoreDrafts()
        {
            if (!EditorPrefs.HasKey(DraftKey)) return false;
            try
            {
                var collection=JsonUtility.FromJson<DraftCollection>(EditorPrefs.GetString(DraftKey));
                if (collection?.items == null) return false;
                foreach (var item in collection.items)
                {
                    var draft=CreateInstance<CreatureRecipe>(); drafts.Add(draft);
                    JsonUtility.FromJsonOverwrite(item.json,draft);
                    draft.name=item.name; draft.hideFlags=HideFlags.HideAndDontSave;
                }
                if (!string.IsNullOrEmpty(collection.selectedAsset)) selected=AssetDatabase.LoadAssetAtPath<CreatureRecipe>(AssetDatabase.GUIDToAssetPath(collection.selectedAsset));
                if (selected == null && collection.selectedIndex >= 0 && collection.selectedIndex < drafts.Count) selected=drafts[collection.selectedIndex];
                return drafts.Count>0;
            }
            catch (Exception exception)
            {
                foreach (var draft in drafts) if (draft != null) DestroyImmediate(draft);
                drafts.Clear(); selected=null;
                Debug.LogWarning("Creature Studio could not restore local drafts: "+exception.Message);
                return false;
            }
        }

        private void InitStyles()
        {
            if (titleStyle != null) return;
            titleStyle=new GUIStyle(EditorStyles.boldLabel) { fontSize=22, normal={textColor=Color.white} };
            sectionStyle=new GUIStyle(EditorStyles.boldLabel) { fontSize=10, normal={textColor=Accent} };
            smallStyle=new GUIStyle(EditorStyles.label) { fontSize=10,wordWrap=true,normal={textColor=new Color(.62f,.68f,.75f)} };
            cardStyle=new GUIStyle(EditorStyles.label) { padding=new RectOffset(12,8,6,6),fontSize=12,normal={textColor=new Color(.85f,.88f,.94f)} };
        }

        private void OnGUI()
        {
            InitStyles();
            Event input=Event.current;
            if (input.type == EventType.KeyDown && !EditorGUIUtility.editingTextField && !input.alt && !input.control && !input.command)
            {
                if (input.keyCode == KeyCode.Space) { TogglePlayback(); input.Use(); }
                else if (input.keyCode == KeyCode.F) { preview.ResetCamera(); input.Use(); Repaint(); }
            }
            EditorGUI.DrawRect(new Rect(0,0,position.width,position.height),Background);
            DrawHeader();
            const float left=210,right=370;
            float height=position.height-87;
            if (grammarMode) DrawGrammarLibrary(new Rect(10,77,left,height));
            else DrawLibrary(new Rect(10,77,left,height));
            DrawViewport(new Rect(left+18,77,position.width-left-right-36,height));
            if (grammarMode) DrawGrammarInspector(new Rect(position.width-right-10,77,right,height));
            else DrawInspector(new Rect(position.width-right-10,77,right,height));
            if (GUI.changed) Repaint();
        }

        private void DrawHeader()
        {
            GUI.Label(new Rect(20,12,250,30),"Creature Studio",titleStyle);
            if (GUI.Button(new Rect(270,17,100,24),"Spells →")) EditorApplication.ExecuteMenuItem("Tools/Render/Spell Studio");
            GUI.Label(new Rect(21,43,570,22),"RENDER LAB  /  Assemble, sculpt and animate your creature recipes",smallStyle);
            int mode=GUI.Toolbar(new Rect(390,17,180,25),grammarMode ? 0 : 1,new[]{"Grammar","Parts"});
            if ((mode==0)!=grammarMode) { if (mode==0) SwitchToGrammar(); else SwitchToParts(partsSelection); GUIUtility.ExitGUI(); }
            Rect rect=new Rect(position.width-325,24,95,26);
            if (GUI.Button(rect,grammarMode ? "New grammar" : "New creature")) { if (grammarMode) NewGrammarDraft(); else NewDraft(); GUIUtility.ExitGUI(); }
            rect.x+=102;
            using (new EditorGUI.DisabledScope(grammarMode ? grammarSelected == null : selected == null))
            {
                if (GUI.Button(rect,grammarMode ? "Save preset…" : "Save recipe…")) { if (grammarMode) SaveGrammarAs(); else SaveAs(); GUIUtility.ExitGUI(); }
                rect.x+=102;
                using (new EditorGUI.DisabledScope(grammarMode ? grammarSelected == null || !AssetDatabase.Contains(grammarSelected) : selected == null || !AssetDatabase.Contains(selected)))
                    if (GUI.Button(rect,"Save")) { AssetDatabase.SaveAssetIfDirty(grammarMode ? (UnityEngine.Object)grammarSelected : selected); ShowNotification(new GUIContent(grammarMode ? "Grammar preset saved" : "Recipe saved")); }
            }
            EditorGUI.DrawRect(new Rect(10,69,position.width-20,1),new Color(.17f,.20f,.24f));
        }

        private void DrawLibrary(Rect rect)
        {
            EditorGUI.DrawRect(rect,Panel);
            GUILayout.BeginArea(new Rect(rect.x+10,rect.y+12,rect.width-20,rect.height-24));
            GUILayout.Label("CREATURE LIBRARY",sectionStyle); GUILayout.Space(8);
            search=EditorGUILayout.TextField(search,EditorStyles.toolbarSearchField); GUILayout.Space(10);
            libraryScroll=EditorGUILayout.BeginScrollView(libraryScroll);
            GUILayout.Label("STARTERS & DRAFTS  ·  "+drafts.Count,smallStyle);
            foreach (var recipe in drafts) DrawCard(recipe,false);
            GUILayout.Space(16); GUILayout.Label("SAVED RECIPES  ·  "+assets.Count,smallStyle);
            foreach (var recipe in assets) DrawCard(recipe,true);
            EditorGUILayout.EndScrollView();
            GUILayout.Label("Drafts are kept locally. Save as a recipe asset to use your creature in the renderer.",smallStyle);
            GUILayout.EndArea();
        }

        private void DrawCard(CreatureRecipe recipe,bool saved)
        {
            if (recipe == null || (!string.IsNullOrEmpty(search) && recipe.name.IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)) return;
            Rect rect=GUILayoutUtility.GetRect(10,33,GUILayout.ExpandWidth(true));
            if (recipe == selected) { EditorGUI.DrawRect(rect,new Color(.22f,.18f,.32f)); EditorGUI.DrawRect(new Rect(rect.x,rect.y,3,rect.height),Accent); }
            if (GUI.Button(rect,(saved ? "◇  " : "·  ")+recipe.name,cardStyle)) { SwitchToParts(recipe); GUIUtility.ExitGUI(); }
        }

        private void DrawViewport(Rect rect)
        {
            EditorGUI.DrawRect(rect,Panel);
            GUI.Label(new Rect(rect.x+14,rect.y+10,220,20),grammarMode ? (previewGameOverride ? "AUTHORED GAME OVERRIDE" : "LIVE GRAMMAR OUTPUT") : "AUTHORED RECIPE",sectionStyle);
            if (selected == null) return;
            if (GUI.Button(new Rect(rect.xMax-180,rect.y+7,87,23),"Export PNG")) ExportPreview();
            if (GUI.Button(new Rect(rect.xMax-87,rect.y+7,75,23),"Reset view")) { preview.ResetCamera(); Repaint(); }
            Rect render=new Rect(rect.x+1,rect.y+38,rect.width-2,rect.height-223);
            preview.SelectedPartIndex=grammarMode ? -1 : selectedPart;
            preview.Draw(render,selected,time);
            GUI.Label(new Rect(render.x+14,render.y+12,render.width-28,22),selected.name,EditorStyles.boldLabel);
            GUI.Label(new Rect(render.x+14,render.y+35,render.width-28,20),(selected.parts?.Length ?? 0)+" parts  /  "+(selected.arms?.Length ?? 0)+" arms",smallStyle);
            GUILayout.BeginArea(new Rect(rect.x+12,render.yMax+10,rect.width-24,174));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(playing ? "Pause" : "Play",GUILayout.Width(60))) TogglePlayback();
            if (GUILayout.Button("Restart",GUILayout.Width(60))) { time=0; RefreshPreview(); }
            loop=GUILayout.Toggle(loop,"Loop",GUILayout.Width(48));
            GUILayout.Label("Speed",smallStyle,GUILayout.Width(35));
            speed=EditorGUILayout.Slider(speed,.1f,3f);
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck(); time=EditorGUILayout.Slider(time,0,duration);
            if (EditorGUI.EndChangeCheck()) { playing=false; Repaint(); }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(time.ToString("0.00")+" / "+duration.ToString("0.0")+" s  ·  Space: play  ·  F: frame",smallStyle);
            preview.ShowGround=GUILayout.Toggle(preview.ShowGround,"Ground",GUILayout.Width(65));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10); GUILayout.Label("VISUAL READOUTS",sectionStyle);
            EditorGUIUtility.labelWidth=65;
            preview.Health=EditorGUILayout.Slider("Vitality",preview.Health,0,1);
            preview.Charge=EditorGUILayout.Slider("Charge",preview.Charge,0,1);
            preview.Glow=EditorGUILayout.Slider("Glow",preview.Glow,0,1);
            EditorGUIUtility.labelWidth=0;
            GUILayout.EndArea();
        }

        private void TogglePlayback()
        {
            if (!playing && time>=duration) time=0;
            playing=!playing; lastTick=EditorApplication.timeSinceStartup; Repaint();
        }

        private void DrawInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect,Panel);
            GUILayout.BeginArea(new Rect(rect.x+12,rect.y+12,rect.width-24,rect.height-24));
            GUILayout.Label("CREATOR",sectionStyle);
            if (selected == null) { GUILayout.Label("Choose a creature to begin."); GUILayout.EndArea(); return; }
            GUILayout.Space(6); GUILayout.Label(AssetDatabase.Contains(selected) ? "Saved recipe · edits support Undo" : "Local draft · edits support Undo",smallStyle);
            inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            serialized.Update(); EditorGUIUtility.labelWidth=112;
            EditorGUI.BeginChangeCheck();
            Section("IDENTITY"); Field("m_Name","Name");
            if (EditorGUI.EndChangeCheck()) ApplyEdits();
            Section("PART ASSEMBLY");
            var parts=serialized.FindProperty("parts");
            selectedPart=Mathf.Clamp(selectedPart,0,Mathf.Max(0,parts.arraySize-1));
            partsScroll=EditorGUILayout.BeginScrollView(partsScroll,GUILayout.Height(145));
            for (int i=0;i<parts.arraySize;i++)
            {
                var part=parts.GetArrayElementAtIndex(i);
                string id=part.FindPropertyRelative("id").stringValue;
                string label=i+"  "+(string.IsNullOrWhiteSpace(id) ? "Unnamed part" : id);
                Rect row=GUILayoutUtility.GetRect(10,26,GUILayout.ExpandWidth(true));
                if (i == selectedPart) EditorGUI.DrawRect(row,new Color(.22f,.18f,.32f));
                if (GUI.Button(row,label,cardStyle)) { selectedPart=i; GUI.FocusControl(null); Repaint(); }
            }
            EditorGUILayout.EndScrollView();
            newPrimitive=(Primitive)EditorGUILayout.EnumPopup("New primitive",newPrimitive);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add child")) { AddPart(); EndMutationGUI(); }
            using (new EditorGUI.DisabledScope(parts.arraySize == 0))
                if (GUILayout.Button("Duplicate")) { DuplicatePart(); EndMutationGUI(); }
            using (new EditorGUI.DisabledScope(parts.arraySize == 0 || selectedPart == 0))
                if (GUILayout.Button("Remove")) { RemovePart(); EndMutationGUI(); }
            EditorGUILayout.EndHorizontal();
            if (parts.arraySize > 0)
            {
                Section("SELECTED PART  ·  "+selectedPart);
                var part=parts.GetArrayElementAtIndex(selectedPart);
                EditorGUI.BeginChangeCheck();
                string[] names={"id","parent","primitive","localPosition","localEuler","dimensions","colour","glow","role","variant"};
                foreach (string name in names)
                {
                    if (name != "parent") { EditorGUILayout.PropertyField(part.FindPropertyRelative(name)); continue; }
                    var parent=part.FindPropertyRelative("parent");
                    if (selectedPart == 0)
                    {
                        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Parent","Root (−1)");
                        if (parent.intValue != -1) { parent.intValue=-1; GUI.changed=true; }
                    }
                    else
                    {
                        var parents=new string[selectedPart];
                        for (int i=0;i<selectedPart;i++) parents[i]=i+"  "+parts.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                        parent.intValue=EditorGUILayout.Popup("Parent",parent.intValue,parents);
                    }
                }
                if (EditorGUI.EndChangeCheck()) ApplyEdits();
                GUILayout.Label("Parent indexes reference the list above. Part 0 is the root; removal includes descendants and attached arms.",smallStyle);
            }
            Section("RIG & MOTION");
            EditorGUI.BeginChangeCheck();
            Field("roots","Roots / feet",true); Field("arms","Arms",true);
            if (EditorGUI.EndChangeCheck()) ApplyEdits();
            if (GUILayout.Button("Add arm to selected part")) { AddArm(); EndMutationGUI(); }
            if (selected.arms != null && selected.arms.Length > 0)
            {
                selectedArm=Mathf.Clamp(selectedArm,0,selected.arms.Length-1);
                var armLabels=new string[selected.arms.Length];
                for (int i=0;i<armLabels.Length;i++) armLabels[i]="Arm "+i+" · part "+selected.arms[i].bodyPart;
                selectedArm=EditorGUILayout.Popup("Arm tools",selectedArm,armLabels);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Rebuild rest pose")) { RebuildArm(); EndMutationGUI(); }
                if (GUILayout.Button("Remove arm")) { RemoveArm(); EndMutationGUI(); }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Label("Arm tools keep joints and source sockets consistent. Rebuild rest pose after changing segment count or length.",smallStyle);
            EditorGUI.BeginChangeCheck();
            Field("idle","Idle motion",true);
            Section("SOCKETS & MATERIAL");
            Field("sourceLocal","Source sockets",true); Field("neckLocal","Neck socket");
            Field("wiltColour","Wilt colour"); Field("stoneOchre","Stone ochre");
            if (EditorGUI.EndChangeCheck()) ApplyEdits();
            if (validationDirty) { warnings=CreatureStudioAuthoring.Validate(selected); validationDirty=false; }
            if (warnings.Length>0) { Section("CHECKS"); foreach (string warning in warnings) EditorGUILayout.HelpBox(warning,MessageType.Warning); }
            GUILayout.Space(16);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate creature")) { Duplicate(); EndMutationGUI(); }
            using (new EditorGUI.DisabledScope(!AssetDatabase.Contains(selected)))
                if (GUILayout.Button("Locate asset")) EditorGUIUtility.PingObject(selected);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Ctrl / Cmd + Z to undo. Changes are visual recipe data, ready for the existing renderer.",smallStyle);
            EditorGUIUtility.labelWidth=0;
            EditorGUILayout.EndScrollView(); GUILayout.EndArea();
        }

        private void Section(string label) { GUILayout.Space(14); GUILayout.Label(label,sectionStyle); GUILayout.Space(5); }
        private void Field(string name,string label,bool children=false) { var p=serialized.FindProperty(name); if (p!=null) EditorGUILayout.PropertyField(p,new GUIContent(label),children); }
        private void ApplyEdits() { serialized.ApplyModifiedProperties(); RefreshPreview(); }
        private static void EndMutationGUI() { EditorGUIUtility.labelWidth=0; GUIUtility.ExitGUI(); }
        private void RecordMutation(string name) { serialized.ApplyModifiedProperties(); Undo.RecordObject(selected,name); }
        private void FinishMutation() { EditorUtility.SetDirty(selected); serialized.Update(); RefreshPreview(); Repaint(); }
        private void AddPart()
        {
            RecordMutation("Add creature part");
            int index=CreatureStudioAuthoring.AddPart(selected,selected.parts?.Length>0 ? selectedPart : -1,newPrimitive);
            if (index>=0) selectedPart=index;
            else ShowNotification(new GUIContent("Repair the recipe checks before adding parts"));
            FinishMutation();
        }
        private void AddArm() { RecordMutation("Add creature arm"); int index=CreatureStudioAuthoring.AddArm(selected,selectedPart); if (index>=0) selectedArm=index; else ShowNotification(new GUIContent("Select a valid body part before adding an arm")); FinishMutation(); }
        private void RemoveArm() { RecordMutation("Remove creature arm"); CreatureStudioAuthoring.RemoveArm(selected,selectedArm); selectedArm=0; FinishMutation(); }
        private void RebuildArm() { RecordMutation("Rebuild arm rest pose"); if (!CreatureStudioAuthoring.RebuildArmRestPose(selected,selectedArm)) ShowNotification(new GUIContent("Unable to rebuild this arm")); FinishMutation(); }
        private void DuplicatePart() { RecordMutation("Duplicate creature part"); int index=CreatureStudioAuthoring.DuplicatePart(selected,selectedPart); if (index>=0) selectedPart=index; FinishMutation(); }
        private void RemovePart() { RecordMutation("Remove creature subtree"); if (CreatureStudioAuthoring.RemovePart(selected,selectedPart)) selectedPart=0; FinishMutation(); }
        private void NewDraft() { var draft=CreatureStudioAuthoring.BuildSample(1); if (draft == null) { ShowNotification(new GUIContent("Starter assets are unavailable")); return; } draft.name="Untitled creature"; draft.hideFlags=HideFlags.HideAndDontSave; drafts.Add(draft); SwitchToParts(draft); }
        private void Duplicate() { if (selected==null) return; var draft=CreatureStudioAuthoring.Clone(selected); draft.name=selected.name+" copy"; draft.hideFlags=HideFlags.HideAndDontSave; drafts.Add(draft); SwitchToParts(draft); }

        private void SaveAs()
        {
            if (selected==null) return;
            string path=EditorUtility.SaveFilePanelInProject("Save creature recipe",selected.name,"asset","Choose where to save your creature recipe.","Assets/Render/Creatures/Data");
            if (string.IsNullOrEmpty(path)) return;
            var copy=CreatureStudioAuthoring.Clone(selected); copy.hideFlags=HideFlags.None;
            copy.name=System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(path)); AssetDatabase.SaveAssets();
            ReloadAssets(); SwitchToParts(copy); EditorGUIUtility.PingObject(copy); ShowNotification(new GUIContent("Creature recipe saved"));
        }

        private void ExportPreview()
        {
            if (selected==null) return;
            string path=EditorUtility.SaveFilePanel("Export creature preview","",selected.name+".png","png");
            if (string.IsNullOrEmpty(path)) return;
            Texture2D image=null;
            try { preview.SelectedPartIndex=-1; image=preview.Capture(selected,time,1600,1000); System.IO.File.WriteAllBytes(path,image.EncodeToPNG()); ShowNotification(new GUIContent("Preview exported at 1600 × 1000")); }
            catch (Exception exception) { Debug.LogException(exception); EditorUtility.DisplayDialog("Could not export preview",exception.Message,"OK"); }
            finally { preview.SelectedPartIndex=grammarMode ? -1 : selectedPart; if (image!=null) DestroyImmediate(image); }
        }
    }
}
