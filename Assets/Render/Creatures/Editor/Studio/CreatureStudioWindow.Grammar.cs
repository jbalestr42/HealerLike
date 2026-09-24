using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures.Studio;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells.Editor.Studio;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    public sealed partial class CreatureStudioWindow
    {
        private bool grammarMode=true, previewGameOverride;
        private CreatureRecipe partsSelection, grammarOutput;
        private CreatureGrammarPreset grammarSelected;
        private SerializedObject grammarSerialized;
        private readonly List<CreatureGrammarPreset> grammarDrafts=new List<CreatureGrammarPreset>();
        private readonly List<CreatureGrammarPreset> grammarAssets=new List<CreatureGrammarPreset>();
        private CreatureLooks creatureLooks;
        private UnitChannels grammarChannels;
        private readonly List<EntityData> grammarEntities=new List<EntityData>();
        private string[] grammarEntityNames={"None"};
        private string[] grammarWarnings=Array.Empty<string>(), grammarDiagnostics=Array.Empty<string>();
        [Serializable] private sealed class GrammarDraftRecord { public string json; public string vocabulary; public string source; }
        [Serializable] private sealed class GrammarDraftCollection
        {
            public List<GrammarDraftRecord> items=new List<GrammarDraftRecord>();
            public int selectedIndex=-1;
            public string selectedAsset;
            public bool grammarMode=true;
        }
        private static string GrammarDraftKey=>"HealerLike.CreatureStudio.GrammarDrafts."+Application.dataPath;

        public static CreatureStudioWindow OpenGrammar(CreatureGrammarPreset preset)
        {
            Open();
            var window=GetWindow<CreatureStudioWindow>();
            if (preset!=null) window.SelectGrammar(preset);
            else window.SwitchToGrammar();
            window.Focus(); return window;
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        private static bool OnOpenGrammarAsset(EntityId entityId,int line)
        {
            var preset=EditorUtility.EntityIdToObject(entityId) as CreatureGrammarPreset;
            if (preset==null) return false;
            OpenGrammar(preset); return true;
        }

        internal static void NotifyGrammarChanged(CreatureGrammarPreset preset)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CreatureStudioWindow>())
                if (window.grammarSelected==preset) { window.grammarSerialized?.Update(); if (window.grammarMode) window.RegenerateGrammar(); }
        }

        private void InitializeGrammar()
        {
            partsSelection=selected;
            creatureLooks=AssetDatabase.LoadAssetAtPath<CreatureLooks>("Assets/Render/Creatures/Data/CreatureLooks.asset");
            RestoreGrammarDrafts();
            grammarMode=true;
            if (grammarDrafts.Count==0) grammarDrafts.Add(CreateGrammarDraft());
            if (grammarSelected==null) grammarSelected=grammarDrafts[0];
            grammarSerialized=new SerializedObject(grammarSelected);
            ReloadGrammarAssets();
            RenderGrammarLibraryWindow.AssetChanged+=OnGrammarLibraryChanged;
            if (grammarMode) RegenerateGrammar();
        }

        private CreatureGrammarPreset CreateGrammarDraft()
        {
            var preset=CreateInstance<CreatureGrammarPreset>();
            preset.name=preset.displayName="Grammar draft";
            preset.hideFlags=HideFlags.HideAndDontSave;
            preset.vocabulary=creatureLooks!=null ? creatureLooks.vocabulary : AssetDatabase.LoadAssetAtPath<LookVocabulary>("Assets/Render/Creatures/Data/LookVocabulary.asset");
            return preset;
        }

        private void OnGrammarLibraryChanged(UnityEngine.Object changed)
        {
            if (grammarMode) RegenerateGrammar();
            else RefreshPreview();
            Repaint();
        }

        private void DisposeGrammar()
        {
            PersistGrammarDrafts();
            RenderGrammarLibraryWindow.AssetChanged-=OnGrammarLibraryChanged;
            grammarSerialized?.Dispose(); grammarSerialized=null;
            if (grammarOutput!=null) DestroyImmediate(grammarOutput);
            grammarOutput=null;
            foreach (var draft in grammarDrafts) if (draft!=null) DestroyImmediate(draft);
            grammarDrafts.Clear();
        }

        private void ReloadGrammarAssets()
        {
            grammarAssets.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:CreatureGrammarPreset"))
            {
                var preset=AssetDatabase.LoadAssetAtPath<CreatureGrammarPreset>(AssetDatabase.GUIDToAssetPath(guid));
                if (preset!=null) grammarAssets.Add(preset);
            }
            grammarAssets.Sort((a,b)=>string.Compare(GrammarLabel(a),GrammarLabel(b),StringComparison.OrdinalIgnoreCase));
            grammarEntities.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:EntityData"))
            {
                var entity=AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
                if (entity!=null) grammarEntities.Add(entity);
            }
            grammarEntities.Sort((a,b)=>string.Compare(a.name,b.name,StringComparison.OrdinalIgnoreCase));
            grammarEntityNames=new string[grammarEntities.Count+1]; grammarEntityNames[0]="None — manual channels";
            for (int i=0;i<grammarEntities.Count;i++) grammarEntityNames[i+1]=grammarEntities[i].name;
        }

        private static string GrammarLabel(CreatureGrammarPreset preset)=>string.IsNullOrWhiteSpace(preset.displayName) ? preset.name : preset.displayName;

        private void SwitchToParts(CreatureRecipe recipe)
        {
            grammarMode=false;
            partsSelection=recipe!=null ? recipe : drafts.Count>0 ? drafts[0] : null;
            Select(partsSelection);
        }

        private void SwitchToGrammar()
        {
            if (!grammarMode) partsSelection=selected;
            grammarMode=true;
            RegenerateGrammar();
        }

        private void SelectGrammar(CreatureGrammarPreset preset)
        {
            if (!grammarMode) partsSelection=selected;
            grammarSerialized?.ApplyModifiedProperties(); grammarSerialized?.Dispose();
            grammarSelected=preset;
            grammarSerialized=preset!=null ? new SerializedObject(preset) : null;
            previewGameOverride=false; grammarMode=true; inspectorScroll=Vector2.zero;
            RegenerateGrammar();
        }

        private void RegenerateGrammar()
        {
            if (grammarSelected==null) return;
            grammarSerialized?.Update();
            grammarWarnings=grammarSelected.Validate();
            grammarDiagnostics=grammarSelected.Diagnostics();
            grammarChannels=grammarSelected.Channels();
            CreatureRecipe output=grammarSelected.Compose();
            if (output!=null) { output.hideFlags=HideFlags.HideAndDontSave; output.name=GrammarLabel(grammarSelected)+" · generated"; }
            var old=grammarOutput;
            grammarOutput=output;
            CreatureRecipe authored=OverrideRecipe();
            if (previewGameOverride && authored==null) previewGameOverride=false;
            Select(previewGameOverride ? authored : grammarOutput);
            if (old!=null) DestroyImmediate(old);
            Repaint();
        }

        private GameObject SourceOverride()
        {
            if (grammarSelected==null || grammarSelected.sourceEntity==null || creatureLooks==null || creatureLooks.entities==null) return null;
            return creatureLooks.entities.TryGetValue(grammarSelected.sourceEntity,out var view) ? view : null;
        }

        private bool HasSourceOverride()=>grammarSelected!=null && grammarSelected.sourceEntity!=null && creatureLooks!=null && creatureLooks.entities!=null && creatureLooks.entities.ContainsKey(grammarSelected.sourceEntity);
        private CreatureRecipe OverrideRecipe()
        {
            var view=SourceOverride();
            if (view==null) return null;
            var builder=view.GetComponentInChildren<CreatureBuilder>(true);
            return builder!=null ? builder.recipe : null;
        }

        private void DrawGrammarLibrary(Rect rect)
        {
            EditorGUI.DrawRect(rect,Panel);
            GUILayout.BeginArea(new Rect(rect.x+10,rect.y+12,rect.width-20,rect.height-24));
            GUILayout.Label("GRAMMAR PRESETS",sectionStyle); GUILayout.Space(8);
            search=EditorGUILayout.TextField(search,EditorStyles.toolbarSearchField); GUILayout.Space(10);
            libraryScroll=EditorGUILayout.BeginScrollView(libraryScroll);
            GUILayout.Label("LOCAL CHANNEL DRAFTS",smallStyle);
            foreach (var preset in grammarDrafts) DrawGrammarCard(preset,false);
            GUILayout.Space(16); GUILayout.Label("SAVED PRESETS  ·  "+grammarAssets.Count,smallStyle);
            foreach (var preset in grammarAssets) DrawGrammarCard(preset,true);
            if (grammarAssets.Count==0) GUILayout.Label("Save a grammar preset to keep editable channels and vocabulary references.",smallStyle);
            GUILayout.Space(18); GUILayout.Label("NATIVE RENDER LIBRARY",sectionStyle);
            GUILayout.Label("Entity overrides: "+(creatureLooks?.entities?.Count ?? 0)+"\nCharacter overrides: "+(creatureLooks?.characters?.Count ?? 0),smallStyle);
            if (GUILayout.Button("Vocabulary & presets")) RenderGrammarLibraryWindow.OpenAsset(creatureLooks!=null ? (UnityEngine.Object)creatureLooks : grammarSelected?.vocabulary);
            GUILayout.Space(8); GUILayout.Label("Parts mode retains authored CreatureRecipe assets and manual drafts.",smallStyle);
            EditorGUILayout.EndScrollView();
            GUILayout.Label("Channels stay editable. Bake only when you want to change individual parts.",smallStyle);
            GUILayout.EndArea();
        }

        private void DrawGrammarCard(CreatureGrammarPreset preset,bool saved)
        {
            if (preset==null || (!string.IsNullOrEmpty(search) && GrammarLabel(preset).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)) return;
            Rect row=GUILayoutUtility.GetRect(10,33,GUILayout.ExpandWidth(true));
            if (preset==grammarSelected) { EditorGUI.DrawRect(row,new Color(.22f,.18f,.32f)); EditorGUI.DrawRect(new Rect(row.x,row.y,3,row.height),Accent); }
            if (GUI.Button(row,(saved ? "◇  " : "·  ")+GrammarLabel(preset),cardStyle)) { SelectGrammar(preset); GUIUtility.ExitGUI(); }
        }

        private void DrawGrammarInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect,Panel);
            GUILayout.BeginArea(new Rect(rect.x+12,rect.y+12,rect.width-24,rect.height-24));
            GUILayout.Label("GRAMMAR CREATOR",sectionStyle);
            if (grammarSelected==null) { GUILayout.Label("Choose a grammar preset."); GUILayout.EndArea(); return; }
            GUILayout.Label(AssetDatabase.Contains(grammarSelected) ? "Saved channel preset · Undo supported" : "Local channel draft · Undo supported",smallStyle);
            inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            grammarSerialized.Update(); EditorGUIUtility.labelWidth=112;
            EditorGUI.BeginChangeCheck();
            Section("PRESET & VOCABULARY"); GrammarField("displayName","Name"); GrammarField("description","Notes"); GrammarField("vocabulary","Vocabulary");
            Section("OPTIONAL GAME SOURCE");
            var source=grammarSerialized.FindProperty("sourceEntity");
            int entityIndex=grammarEntities.IndexOf(source.objectReferenceValue as EntityData)+1;
            int chosen=EditorGUILayout.Popup("Game entity",entityIndex,grammarEntityNames);
            if (chosen!=entityIndex) { source.objectReferenceValue=chosen>0 ? grammarEntities[chosen-1] : null; grammarSerialized.FindProperty("deriveFromEntity").boolValue=chosen>0; }
            GrammarField("sourceEntity","Entity asset"); GrammarField("sourceSide","Entity side"); GrammarField("deriveFromEntity","Derive channels");
            if (HasSourceOverride()) EditorGUILayout.HelpBox("The game has an authored override for this entity. This viewport auditions grammar unless you enable Preview game override below.",MessageType.Warning);
            Section("VISUAL CHANNELS");
            if (grammarSerialized.FindProperty("deriveFromEntity").boolValue)
            {
                UnitChannels channels=grammarChannels;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("Plant / stone",channels.side); EditorGUILayout.EnumPopup("Head",channels.head); EditorGUILayout.EnumPopup("Count",channels.count);
                    EditorGUILayout.EnumPopup("Stem / cadence",channels.stem); EditorGUILayout.EnumPopup("Mass",channels.mass); EditorGUILayout.EnumPopup("Reach",channels.reach);
                    EditorGUILayout.EnumPopup("Accessory",channels.accessory); EditorGUILayout.EnumPopup("Accessory head",channels.accessoryHead); EditorGUILayout.EnumPopup("Accent",channels.accent);
                }
            }
            else
            {
                GrammarField("side","Plant / stone"); GrammarField("head","Head"); GrammarField("count","Count");
                GrammarField("stem","Stem / cadence"); GrammarField("mass","Mass"); GrammarField("reach","Reach");
                GrammarField("accessory","Accessory"); GrammarField("accessoryHead","Accessory head"); GrammarField("accent","Accent");
            }
            bool changed=EditorGUI.EndChangeCheck();
            if (grammarSerialized.ApplyModifiedProperties() || changed) RegenerateGrammar();
            using (new EditorGUI.DisabledScope(grammarSelected.sourceEntity==null))
                if (GUILayout.Button("Copy derived channels into manual controls"))
                {
                    Undo.RecordObject(grammarSelected,"Copy derived creature channels");
                    if (grammarSelected.ReadFromEntity()) { EditorUtility.SetDirty(grammarSelected); grammarSerialized.Update(); RegenerateGrammar(); }
                }
            Section("OUTPUT & PROVENANCE");
            if (grammarWarnings.Length==0)
            {
                UnitChannels channels=grammarChannels;
                GUILayout.Label((grammarSelected.deriveFromEntity ? "Derived from game entity" : "Manual channel preset")+" → LookComposer",EditorStyles.boldLabel);
                GUILayout.Label(channels.side+" · "+channels.head+" · "+channels.count+"\n"+channels.stem+" stem · "+channels.mass+" mass · "+channels.reach+" reach\n"+channels.accessory+" · "+channels.accent+" accent",smallStyle);
                GUILayout.Label("Generated output: "+(grammarOutput?.parts?.Length ?? 0)+" parts, "+(grammarOutput?.arms?.Length ?? 0)+" arms",smallStyle);
            }
            foreach (string warning in grammarWarnings) EditorGUILayout.HelpBox(warning,MessageType.Warning);
            foreach (string diagnostic in grammarDiagnostics) EditorGUILayout.HelpBox(diagnostic,MessageType.Info);
            DrawSourceOverride();
            GUILayout.Space(14);
            using (new EditorGUI.DisabledScope(grammarOutput==null))
                if (GUILayout.Button("Bake to editable recipe",GUILayout.Height(30))) { BakeGrammar(); EndMutationGUI(); }
            GUILayout.Label("Baking makes an independent Parts draft. The grammar preset and source assets stay editable.",smallStyle);
            GUILayout.Space(8);
            if (GUILayout.Button("Duplicate grammar preset")) { DuplicateGrammar(); EndMutationGUI(); }
            if (GUILayout.Button("Edit vocabulary & native preset tables")) RenderGrammarLibraryWindow.OpenAsset(grammarSelected.vocabulary);
            EditorGUIUtility.labelWidth=0; EditorGUILayout.EndScrollView(); GUILayout.EndArea();
        }

        private void DrawSourceOverride()
        {
            if (grammarSelected.sourceEntity==null) return;
            Section("GAME VIEW RESOLUTION");
            EditorGUI.BeginChangeCheck(); creatureLooks=(CreatureLooks)EditorGUILayout.ObjectField("Creature looks",creatureLooks,typeof(CreatureLooks),false);
            if (EditorGUI.EndChangeCheck()) RegenerateGrammar();
            if (creatureLooks==null) { EditorGUILayout.HelpBox("Assign CreatureLooks to inspect the actual game override resolution.",MessageType.Info); return; }
            GameObject resolved=creatureLooks.entities!=null ? creatureLooks.GetView(grammarSelected.sourceEntity,grammarSelected.sourceSide) : null;
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Resolved view",resolved,typeof(GameObject),false);
            if (HasSourceOverride())
            {
                EditorGUILayout.HelpBox("An authored entity override wins in the game. The grammar output is an audition until the native override table is changed.",MessageType.Warning);
                var authored=OverrideRecipe();
                using (new EditorGUI.DisabledScope(authored==null))
                {
                    EditorGUI.BeginChangeCheck(); previewGameOverride=EditorGUILayout.Toggle("Preview game override",previewGameOverride);
                    if (EditorGUI.EndChangeCheck()) RegenerateGrammar();
                }
                if (authored==null) GUILayout.Label("This override does not expose a CreatureBuilder recipe; inspect its prefab in the native table.",smallStyle);
            }
            else GUILayout.Label("No authored entity override. The game uses the side’s derived host and its configured vocabulary.",smallStyle);
            if (creatureLooks.vocabulary!=grammarSelected.vocabulary) EditorGUILayout.HelpBox("This preset's vocabulary differs from the game CreatureLooks vocabulary.",MessageType.Warning);
            if (GUILayout.Button("Open source override table")) RenderGrammarLibraryWindow.OpenAsset(creatureLooks);
        }

        private void GrammarField(string name,string label) { var property=grammarSerialized.FindProperty(name); if (property!=null) EditorGUILayout.PropertyField(property,new GUIContent(label),true); }
        private void NewGrammarDraft() { var preset=CreateGrammarDraft(); grammarDrafts.Add(preset); SelectGrammar(preset); }
        private void DuplicateGrammar() { var copy=Instantiate(grammarSelected); copy.name=copy.displayName=GrammarLabel(grammarSelected)+" copy"; copy.hideFlags=HideFlags.HideAndDontSave; grammarDrafts.Add(copy); SelectGrammar(copy); }
        private void BakeGrammar()
        {
            if (grammarOutput==null) return;
            var recipe=CreatureStudioAuthoring.Clone(grammarOutput); recipe.name=GrammarLabel(grammarSelected)+" baked";
            recipe.hideFlags=HideFlags.HideAndDontSave; drafts.Add(recipe); SwitchToParts(recipe);
        }

        private void SaveGrammarAs()
        {
            if (grammarSelected==null) return;
            string path=EditorUtility.SaveFilePanelInProject("Save creature grammar preset",GrammarLabel(grammarSelected),"asset","Save the editable channels and vocabulary reference.","Assets/Render/Creatures/Data");
            if (string.IsNullOrEmpty(path)) return;
            var copy=Instantiate(grammarSelected); copy.hideFlags=HideFlags.None; copy.name=System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(path)); AssetDatabase.SaveAssets();
            ReloadGrammarAssets(); SelectGrammar(copy); EditorGUIUtility.PingObject(copy);
        }

        private void PersistGrammarDrafts()
        {
            var collection=new GrammarDraftCollection { grammarMode=grammarMode,selectedIndex=grammarDrafts.IndexOf(grammarSelected),selectedAsset=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(grammarSelected)) };
            foreach (var draft in grammarDrafts) if (draft!=null) collection.items.Add(new GrammarDraftRecord { json=JsonUtility.ToJson(draft),vocabulary=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(draft.vocabulary)),source=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(draft.sourceEntity)) });
            EditorPrefs.SetString(GrammarDraftKey,JsonUtility.ToJson(collection));
        }

        private void RestoreGrammarDrafts()
        {
            if (!EditorPrefs.HasKey(GrammarDraftKey)) return;
            try
            {
                var collection=JsonUtility.FromJson<GrammarDraftCollection>(EditorPrefs.GetString(GrammarDraftKey));
                if (collection?.items==null) return;
                foreach (var item in collection.items)
                {
                    var preset=CreateInstance<CreatureGrammarPreset>(); grammarDrafts.Add(preset);
                    JsonUtility.FromJsonOverwrite(item.json,preset); preset.hideFlags=HideFlags.HideAndDontSave; preset.name=GrammarLabel(preset);
                    preset.vocabulary=AssetDatabase.LoadAssetAtPath<LookVocabulary>(AssetDatabase.GUIDToAssetPath(item.vocabulary));
                    preset.sourceEntity=AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(item.source));
                }
                grammarSelected=AssetDatabase.LoadAssetAtPath<CreatureGrammarPreset>(AssetDatabase.GUIDToAssetPath(collection.selectedAsset));
                if (grammarSelected==null && collection.selectedIndex>=0 && collection.selectedIndex<grammarDrafts.Count) grammarSelected=grammarDrafts[collection.selectedIndex];
                grammarMode=collection.grammarMode;
            }
            catch (Exception exception)
            {
                foreach (var draft in grammarDrafts) if (draft!=null) DestroyImmediate(draft);
                grammarDrafts.Clear(); grammarSelected=null;
                Debug.LogWarning("Creature Studio could not restore grammar drafts: "+exception.Message);
            }
        }
    }
}
