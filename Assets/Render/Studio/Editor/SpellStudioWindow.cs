using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    /// <summary>A non-destructive workbench for the renderer's shared spell vocabulary.</summary>
    public sealed class SpellStudioWindow : EditorWindow
    {
        private static readonly Color Background = new Color(.055f, .065f, .083f);
        private static readonly Color Panel = new Color(.085f, .098f, .12f);
        private static readonly Color Accent = new Color(.40f, .86f, .74f);
        private readonly List<SpellStudioPreset> drafts = new List<SpellStudioPreset>();
        private readonly List<ABuffHandlerFactory> gameplayHandlers = new List<ABuffHandlerFactory>();
        private readonly List<SpellStudioPreset> assets = new List<SpellStudioPreset>();
        private SpellStudioPreset selected;
        private SerializedObject serialized;
        private SpellStudioPreview preview;
        [SerializeField] private HealerLike.Render.Creatures.CreatureRecipe targetCreature;
        [SerializeField] private int targetSurface; // 0 auto, 1 plant, 2 stone; preview-only context.
        private Vector2 libraryScroll, inspectorScroll;
        private string search = "";
        private string[] validationWarnings = Array.Empty<string>();
        private bool validationDirty = true;
        private bool playing = true, loop = true;
        private float time, speed = 1;
        private double lastTick;
        private GUIStyle titleStyle, sectionStyle, smallStyle, cardStyle;
        [Serializable] private sealed class DraftRecord { public string json; public string vocabularyGuid; public string handlerGuid; public string looksGuid; public string projectileGuid; }
        [Serializable] private sealed class DraftCollection { public List<DraftRecord> items = new List<DraftRecord>(); public int selectedDraftIndex = -1; public string selectedAssetGuid; }
        private static string DraftKey => "HealerLike.SpellStudio.Drafts." + Application.dataPath;

        private void PersistDrafts()
        {
            var collection = new DraftCollection { selectedDraftIndex = drafts.IndexOf(selected),
                selectedAssetGuid = selected != null && AssetDatabase.Contains(selected) ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selected)) : "" };
            foreach (var draft in drafts)
            {
                if (draft == null) continue;
                collection.items.Add(new DraftRecord { json = JsonUtility.ToJson(draft),
                    vocabularyGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(draft.vocabulary)),
                    handlerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(draft.sourceHandler)),
                    looksGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(draft.spellLooks)),
                    projectileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(draft.sourceProjectile)) });
            }
            EditorPrefs.SetString(DraftKey, JsonUtility.ToJson(collection));
        }

        private bool RestoreDrafts()
        {
            if (!EditorPrefs.HasKey(DraftKey)) return false;
            try
            {
                var collection = JsonUtility.FromJson<DraftCollection>(EditorPrefs.GetString(DraftKey));
                if (collection == null || collection.items == null) return false;
                foreach (var record in collection.items)
                {
                    var draft = CreateInstance<SpellStudioPreset>();
                    drafts.Add(draft);
                    JsonUtility.FromJsonOverwrite(record.json,draft);
                    draft.hideFlags = HideFlags.HideAndDontSave;
                    draft.name = draft.displayName;
                    draft.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetDatabase.GUIDToAssetPath(record.vocabularyGuid));
                    draft.sourceHandler = LoadGuid<ABuffHandlerFactory>(record.handlerGuid);
                    draft.spellLooks = LoadGuid<SpellLooks>(record.looksGuid);
                    draft.sourceProjectile = LoadGuid<GameObject>(record.projectileGuid);
                }
                if (!string.IsNullOrEmpty(collection.selectedAssetGuid))
                    selected = AssetDatabase.LoadAssetAtPath<SpellStudioPreset>(AssetDatabase.GUIDToAssetPath(collection.selectedAssetGuid));
                if (selected == null && collection.selectedDraftIndex >= 0 && collection.selectedDraftIndex < drafts.Count)
                    selected = drafts[collection.selectedDraftIndex];
                return drafts.Count > 0;
            }
            catch (Exception exception)
            {
                foreach (var draft in drafts) if (draft != null) DestroyImmediate(draft);
                drafts.Clear();
                selected = null;
                Debug.LogWarning("Spell Studio could not restore local drafts: " + exception.Message);
                return false;
            }
        }

        private static T LoadGuid<T>(string guid) where T : UnityEngine.Object => string.IsNullOrEmpty(guid) ? null :
            AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

        [MenuItem("Tools/Render/Spell Studio", false, 110)]
        public static void Open()
        {
            bool alreadyOpen = HasOpenInstances<SpellStudioWindow>();
            var window = GetWindow<SpellStudioWindow>();
            if (!alreadyOpen) window.position = new Rect(80, 80, 1280, 800);
            window.titleContent = new GUIContent("Spell Studio");
            window.minSize = new Vector2(1040, 640);
            window.Show();
        }

        public static SpellStudioWindow OpenPreset(SpellStudioPreset preset)
        {
            Open();
            var window = GetWindow<SpellStudioWindow>();
            if (preset != null) window.Select(preset);
            window.Focus();
            return window;
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        private static bool OnOpenPresetAsset(EntityId entityId, int line)
        {
            var preset = EditorUtility.EntityIdToObject(entityId) as SpellStudioPreset;
            if (preset == null) return false;
            OpenPreset(preset);
            return true;
        }

        internal static void NotifyPresetChanged(SpellStudioPreset preset)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<SpellStudioWindow>())
            {
                if (window.selected != preset) continue;
                window.serialized?.Update();
                window.RefreshPreview();
                window.Repaint();
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Spell Studio");
            minSize = new Vector2(1040, 640);
            preview = new SpellStudioPreview();
            preview.ReferenceRecipe = targetCreature;
            if (targetSurface == 0) preview.AutomaticTargetSide = true;
            else preview.TargetSide = targetSurface == 2 ? LookSide.Stone : LookSide.Plant;
            CreateDrafts();
            ReloadAssets();
            Select(selected != null ? selected : drafts.Count > 0 ? drafts[0] : null);
            lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.projectChanged += ReloadAssets;
            RenderGrammarLibraryWindow.AssetChanged += OnGrammarAssetChanged;
        }

        private void OnDisable()
        {
            PersistDrafts();
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.projectChanged -= ReloadAssets;
            RenderGrammarLibraryWindow.AssetChanged -= OnGrammarAssetChanged;
            preview?.Dispose();
            preview = null;
            foreach (var draft in drafts) if (draft != null) DestroyImmediate(draft);
            drafts.Clear();
            serialized?.Dispose();
            serialized = null;
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing && selected != null)
            {
                float duration = Duration;
                time += (float)System.Math.Min(now - lastTick, .1) * speed;
                if (time >= duration)
                {
                    if (loop) time %= duration;
                    else { time = duration; playing = false; }
                }
                Repaint();
            }
            lastTick = now;
        }

        private float Duration => selected == null ? 2f : Mathf.Max(.01f, selected.PreviewDuration);
        private void OnUndo() { serialized?.Update(); RefreshPreview(); Repaint(); }

        private void CreateDrafts()
        {
            if (drafts.Count > 0 || RestoreDrafts()) return;
            var vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
            if (vocabulary == null)
            {
                var ids = AssetDatabase.FindAssets("t:EffectVocabulary");
                if (ids.Length > 0) vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetDatabase.GUIDToAssetPath(ids[0]));
            }
            foreach (EffectElement element in Enum.GetValues(typeof(EffectElement)))
            {
                var draft = CreateInstance<SpellStudioPreset>();
                draft.hideFlags = HideFlags.HideAndDontSave;
                draft.name = ObjectNames.NicifyVariableName(element.ToString());
                draft.displayName = draft.name;
                draft.vocabulary = vocabulary;
                draft.element = element;
                draft.family = Family(element);
                draft.durationSeconds = 2.4f;
                drafts.Add(draft);
            }
            for (int i = 6; i < SpellStudioSamples.Count; i++)
            {
                SpellStudioPreset grammar = SpellStudioSamples.Build(vocabulary, i);
                if (grammar == null) continue;
                grammar.hideFlags = HideFlags.HideAndDontSave;
                drafts.Add(grammar);
            }
        }

        private static EffectFamily Family(EffectElement element)
        {
            switch (element)
            {
                case EffectElement.Rise: return EffectFamily.Heal;
                case EffectElement.Stalks: return EffectFamily.Renew;
                case EffectElement.Drips: return EffectFamily.Rot;
                case EffectElement.Press:
                case EffectElement.Crack: return EffectFamily.Bane;
                case EffectElement.Orbit:
                case EffectElement.Plates:
                case EffectElement.Bud: return EffectFamily.Boon;
                default: return EffectFamily.Damage;
            }
        }

        private void OnGrammarAssetChanged(UnityEngine.Object asset)
        {
            serialized?.Update();
            RefreshPreview();
            Repaint();
        }

        private void RefreshPreview() { validationDirty = true; preview?.Refresh(); }

        private void ReloadAssets()
        {
            RefreshPreview();
            assets.Clear();
            gameplayHandlers.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ABuffHandlerFactory"))
            {
                var handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(AssetDatabase.GUIDToAssetPath(guid));
                if (handler != null) gameplayHandlers.Add(handler);
            }
            gameplayHandlers.Sort((a,b) => string.Compare(HandlerLabel(a), HandlerLabel(b), StringComparison.OrdinalIgnoreCase));
            foreach (string guid in AssetDatabase.FindAssets("t:SpellStudioPreset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<SpellStudioPreset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) assets.Add(asset);
            }
            assets.Sort((a,b) => string.Compare(Label(a), Label(b), StringComparison.OrdinalIgnoreCase));
            Repaint();
        }

        private void Select(SpellStudioPreset preset)
        {
            if (serialized != null && selected != null) serialized.ApplyModifiedProperties();
            selected = preset;
            serialized?.Dispose();
            serialized = preset == null ? null : new SerializedObject(preset);
            time = 0;
            inspectorScroll = Vector2.zero;
            RefreshPreview();
            Repaint();
        }

        private static string Label(SpellStudioPreset preset) => string.IsNullOrWhiteSpace(preset.displayName) ? preset.name : preset.displayName;

        private void InitStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 22, normal = { textColor = Color.white } };
            sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, normal = { textColor = Accent } };
            smallStyle = new GUIStyle(EditorStyles.label) { fontSize = 10, wordWrap = true, normal = { textColor = new Color(.60f,.67f,.73f) } };
            cardStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(12,8,6,6), fontSize = 12, normal = { textColor = new Color(.84f,.89f,.92f) } };
        }

        private void OnGUI()
        {
            InitStyles();
            HandleShortcuts();
            EditorGUI.DrawRect(new Rect(0,0,position.width,position.height), Background);
            DrawHeader();
            const float left = 210, right = 340, gap = 8;
            float height = position.height - 87;
            DrawLibrary(new Rect(10,77,left,height));
            DrawViewport(new Rect(left+10+gap,77,position.width-left-right-20-gap*2,height));
            DrawInspector(new Rect(position.width-right-10,77,right,height));
            if (GUI.changed) Repaint();
        }

        private void HandleShortcuts()
        {
            Event input = Event.current;
            if (input.type != EventType.KeyDown || EditorGUIUtility.editingTextField || input.alt || input.control || input.command) return;
            if (input.keyCode == KeyCode.Space)
            {
                if (!playing && time >= Duration) time = 0;
                playing = !playing;
                lastTick = EditorApplication.timeSinceStartup;
                input.Use();
                Repaint();
            }
            else if (input.keyCode == KeyCode.F)
            {
                preview?.ResetCamera();
                input.Use();
                Repaint();
            }
        }

        private void DrawHeader()
        {
            GUI.Label(new Rect(20,12,260,30), "Spell Studio", titleStyle);
            if (GUI.Button(new Rect(260,17,115,24), "Creatures →")) EditorApplication.ExecuteMenuItem("Tools/Render/Creature Studio");
            GUI.Label(new Rect(21,43,520,22), "RENDER LAB  /  Create, shape and rehearse your spell effects", smallStyle);
            var rect = new Rect(position.width-325,24,95,26);
            if (GUI.Button(rect, "New spell")) { NewDraft(); GUIUtility.ExitGUI(); }
            rect.x += 102;
            using (new EditorGUI.DisabledScope(selected == null))
            {
                if (GUI.Button(rect,"Save as…")) { SaveAs(); GUIUtility.ExitGUI(); }
                rect.x += 102;
                using (new EditorGUI.DisabledScope(selected == null || !AssetDatabase.Contains(selected)))
                    if (GUI.Button(rect,"Save")) { AssetDatabase.SaveAssetIfDirty(selected); ShowNotification(new GUIContent("Preset saved")); }
            }
            EditorGUI.DrawRect(new Rect(10,69,position.width-20,1), new Color(.17f,.20f,.24f));
        }

        private void DrawLibrary(Rect rect)
        {
            EditorGUI.DrawRect(rect, Panel);
            GUILayout.BeginArea(new Rect(rect.x+10,rect.y+12,rect.width-20,rect.height-24));
            GUILayout.Label("SPELL LIBRARY", sectionStyle);
            GUILayout.Space(8);
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            GUILayout.Space(6);
            if (GUILayout.Button("Grammar & native presets…")) RenderGrammarLibraryWindow.OpenSpells();
            if (GUILayout.Button("New grammar preset")) { NewGrammarDraft(); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("Add sample presets")) { SpellStudioSamples.Create(); ReloadAssets(); }
            GUILayout.Space(6);
            libraryScroll = EditorGUILayout.BeginScrollView(libraryScroll);
            GUILayout.Label("VOCABULARY & DRAFTS  ·  " + drafts.Count, smallStyle);
            foreach (var draft in drafts) DrawCard(draft, false);
            GUILayout.Space(16);
            GUILayout.Label("SAVED PRESETS  ·  " + assets.Count, smallStyle);
            foreach (var asset in assets) DrawCard(asset, true);
            if (assets.Count == 0) GUILayout.Label("Save a creation to build your own library.", smallStyle);
            GUILayout.Space(16);
            GUILayout.Label("GAMEPLAY HANDLERS  ·  " + gameplayHandlers.Count, smallStyle);
            foreach (var handler in gameplayHandlers)
            {
                string label = HandlerLabel(handler);
                if (!string.IsNullOrEmpty(search) && label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Button(new GUIContent("↳ " + label, AssetDatabase.GetAssetPath(handler)), EditorStyles.miniButton))
                { NewHandlerDraft(handler); GUIUtility.ExitGUI(); }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.Label("Vocabulary selections are local drafts. Drafts are kept locally. Save as a preset to share them.", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawCard(SpellStudioPreset preset, bool saved)
        {
            if (preset == null || (!string.IsNullOrEmpty(search) && Label(preset).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0 && preset.ResolvedElement.ToString().IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)) return;
            Rect rect = GUILayoutUtility.GetRect(10,33,GUILayout.ExpandWidth(true));
            if (selected == preset)
            {
                EditorGUI.DrawRect(rect,new Color(.14f,.25f,.25f));
                EditorGUI.DrawRect(new Rect(rect.x,rect.y,3,rect.height),Accent);
            }
            if (GUI.Button(rect,(saved ? "◇  " : "·  ")+Label(preset),cardStyle)) { Select(preset); GUIUtility.ExitGUI(); }
        }

        private void DrawViewport(Rect rect)
        {
            EditorGUI.DrawRect(rect, Panel);
            GUI.Label(new Rect(rect.x+14,rect.y+10,180,20),"LIVE PREVIEW",sectionStyle);
            if (selected == null) return;
            Rect render = new Rect(rect.x+1,rect.y+38,rect.width-2,rect.height-155);
            preview.Draw(render, selected, time);
            GUI.Label(new Rect(render.x+14,render.y+12,render.width-28,24),Label(selected),EditorStyles.boldLabel);
            GUI.Label(new Rect(render.x+14,render.y+34,render.width-28,20),selected.ResolvedElement+" / "+selected.ResolvedChannels.family+" / "+selected.ResolvedChannels.tempo,smallStyle);
            if (GUI.Button(new Rect(rect.xMax-180,rect.y+7,87,23),"Export PNG")) ExportPreview();
            if (GUI.Button(new Rect(rect.xMax-87,rect.y+7,75,23),"Reset view")) { preview.ResetCamera(); Repaint(); }
            GUILayout.BeginArea(new Rect(rect.x+12,render.yMax+10,rect.width-24,102));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(playing ? "Pause" : "Play",GUILayout.Width(65))) { if (!playing && time >= Duration) time = 0; playing = !playing; lastTick = EditorApplication.timeSinceStartup; }
            if (GUILayout.Button("Restart",GUILayout.Width(65))) { time = 0; RefreshPreview(); }
            loop = GUILayout.Toggle(loop,"Loop",GUILayout.Width(50));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Speed",smallStyle,GUILayout.Width(36));
            speed = EditorGUILayout.Slider(speed,.1f,3f,GUILayout.MinWidth(75));
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            time = EditorGUILayout.Slider(time,0,Duration);
            if (EditorGUI.EndChangeCheck()) { playing = false; Repaint(); }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(time.ToString("0.00")+" / "+Duration.ToString("0.00")+" s  ·  Space: play  ·  F: frame",smallStyle);
            GUILayout.FlexibleSpace();
            preview.ShowGround = GUILayout.Toggle(preview.ShowGround,"Ground");
            preview.ShowReference = GUILayout.Toggle(preview.ShowReference,"Target");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            targetCreature = (HealerLike.Render.Creatures.CreatureRecipe)EditorGUILayout.ObjectField(
                new GUIContent("Creature", "Reference recipe; None uses the built-in healer."), targetCreature,
                typeof(HealerLike.Render.Creatures.CreatureRecipe), false);
            if (EditorGUI.EndChangeCheck()) { preview.ReferenceRecipe = targetCreature; Repaint(); }
            EditorGUI.BeginChangeCheck();
            targetSurface = EditorGUILayout.Popup(targetSurface, new[] { "Auto", "Plant", "Stone" }, GUILayout.Width(65));
            if (EditorGUI.EndChangeCheck())
            {
                if (targetSurface == 0) preview.AutomaticTargetSide = true;
                else preview.TargetSide = targetSurface == 2 ? LookSide.Stone : LookSide.Plant;
                Repaint();
            }
            using (new EditorGUI.DisabledScope(targetCreature == null))
                if (GUILayout.Button("Edit", GUILayout.Width(42))) AssetDatabase.OpenAsset(targetCreature);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect,Panel);
            GUILayout.BeginArea(new Rect(rect.x+12,rect.y+12,rect.width-24,rect.height-24));
            GUILayout.Label("CREATOR",sectionStyle);
            if (selected == null) { GUILayout.Label("Choose a spell to begin."); GUILayout.EndArea(); return; }
            GUILayout.Space(6);
            GUILayout.Label(AssetDatabase.Contains(selected) ? "Saved preset · edits support Undo" : "Unsaved draft · edits support Undo",smallStyle);
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            serialized.Update();
            EditorGUIUtility.labelWidth = 112;
            EditorGUI.BeginChangeCheck();
            Section("IDENTITY");
            Field("displayName","Name");
            Field("description","Notes");
            Section("GRAMMAR & PRESETS");
            DrawGrammarFields();
            Section("PREVIEW TIMING");
            using (new EditorGUI.DisabledScope(selected.ResolvedChannels.tempo == EffectTempo.Once))
                Field("durationSeconds","Preview length");
            if (selected.ResolvedChannels.tempo == EffectTempo.Once) GUILayout.Label("One-shot length follows the resolved entry’s motion cycle.",smallStyle);
            Section("CAST CONTEXT");
            Field("stacks","Stacks"); Field("charges","Charges"); Field("amount","Amount");
            Field("critical","Critical"); Field("side","Side"); Field("scale","Scale");
            Section("COLOUR");
            Field("overrideColour","Custom colour");
            if (serialized.FindProperty("overrideColour").boolValue) Field("colour","Colour");
            Section("SHAPE & MOTION");
            Field("overrideEntry","Custom entry");
            bool changed = EditorGUI.EndChangeCheck();
            if (serialized.ApplyModifiedProperties() || changed) { RefreshPreview(); time = Mathf.Min(time, Duration); }
            if (GUILayout.Button("Copy vocabulary shape into preset"))
            {
                Undo.RecordObject(selected,"Copy spell vocabulary entry");
                if (selected.CaptureEntry()) { EditorUtility.SetDirty(selected); serialized.Update(); RefreshPreview(); }
                else ShowNotification(new GUIContent("Choose a vocabulary containing this element"));
            }
            if (selected.overrideEntry)
            {
                EditorGUI.BeginChangeCheck();
                Field("entry","Entry",true);
                if (EditorGUI.EndChangeCheck()) { serialized.ApplyModifiedProperties(); RefreshPreview(); }
            }
            else GUILayout.Label("Copy the source shape to edit parts, motion, socket and counts without changing the shared vocabulary.",smallStyle);
            using (new EditorGUI.DisabledScope(selected.vocabulary == null))
            {
                if (GUILayout.Button("Apply shape to vocabulary…"))
                {
                    if (EditorUtility.DisplayDialog("Update shared vocabulary?",
                        "Replace " + selected.ResolvedElement + " in " + selected.vocabulary.name + "?\n\nThis changes the shared shape, motion, socket and count used by game effects. Colour and cast context remain in this preset. You can undo this change.",
                        "Apply shape", "Cancel"))
                    {
                        SpellStudioPublishing.PublishEntry(selected);
                        RefreshPreview();
                    }
                }
            }
            if (validationDirty) { validationWarnings = selected.Validate(); validationDirty = false; }
            string[] warnings = validationWarnings;
            if (warnings.Length > 0)
            {
                Section("CHECKS");
                foreach (string warning in warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
            GUILayout.Space(16);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate")) { EditorGUIUtility.labelWidth = 0; Duplicate(); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(!AssetDatabase.Contains(selected)))
                if (GUILayout.Button("Locate asset")) EditorGUIUtility.PingObject(selected);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Ctrl / Cmd + Z to undo. Use Save as to create a reusable asset.",smallStyle);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Section(string label) { GUILayout.Space(16); GUILayout.Label(label,sectionStyle); GUILayout.Space(5); }
        private void Field(string name,string label,bool children=false)
        {
            var property = serialized.FindProperty(name);
            if (property != null) EditorGUILayout.PropertyField(property,new GUIContent(label),children);
        }

        private void NewDraft()
        {
            var draft = CreateInstance<SpellStudioPreset>();
            draft.hideFlags = HideFlags.HideAndDontSave;
            draft.name = draft.displayName = "Untitled spell";
            if (selected != null) draft.vocabulary = selected.vocabulary;
            if (draft.vocabulary == null) draft.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
            draft.spellLooks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
            drafts.Add(draft);
            Select(draft);
        }

        private static string HandlerLabel(ABuffHandlerFactory handler)
        {
            string path = AssetDatabase.GetAssetPath(handler);
            string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
            return ObjectNames.NicifyVariableName(folder) + " / " + handler.name;
        }

        private void NewGrammarDraft()
        {
            NewDraft();
            selected.mode = SpellStudioMode.GrammarChannels;
            selected.family = EffectFamily.Boon;
            selected.attributeGroup = AttributeGroup.Defence;
            selected.tempo = EffectTempo.ForDuration;
            selected.name = selected.displayName = "Defence boon";
            serialized.Update();
            RefreshPreview();
        }

        private void NewHandlerDraft(ABuffHandlerFactory handler)
        {
            NewDraft();
            selected.mode = SpellStudioMode.GameplayHandler;
            selected.sourceHandler = handler;
            selected.name = selected.displayName = HandlerLabel(handler);
            selected.isSameSide = true;
            serialized.Update();
            RefreshPreview();
        }

        private void DrawGrammarFields()
        {
            Field("mode", "Recipe source");
            var mode = (SpellStudioMode)serialized.FindProperty("mode").enumValueIndex;
            Field("vocabulary", "Vocabulary");
            using (new EditorGUI.DisabledScope(selected.vocabulary == null))
                if (GUILayout.Button("Edit effect vocabulary…")) RenderGrammarLibraryWindow.OpenAsset(selected.vocabulary);
            if (mode == SpellStudioMode.AuthoredElement)
            {
                Field("element", "Element"); Field("family", "Family"); Field("tempo", "Tempo"); Field("periodSeconds", "Period (s)");
                GUILayout.Label("Authored elements keep full manual control. Grammar mode derives the element from family and attribute group.", smallStyle);
            }
            else if (mode == SpellStudioMode.GrammarChannels)
            {
                Field("family", "Family"); Field("attributeGroup", "Attribute group"); Field("tempo", "Tempo"); Field("periodSeconds", "Period (s)");
                GUILayout.Label("Uses EffectComposer: boon + defence → plates; boon + prevention → bud; bane + offence → press.", smallStyle);
            }
            else
            {
                Field("sourceHandler", "Buff handler"); Field("isSameSide", "Same side");
                using (new EditorGUI.DisabledScope(selected.sourceHandler == null))
                    if (GUILayout.Button("Inspect gameplay handler")) { Selection.activeObject = selected.sourceHandler; EditorGUIUtility.PingObject(selected.sourceHandler); }
                GUILayout.Label("Reads consumer sign, modifier polarity, attribute group, duration and period from the actual gameplay asset.", smallStyle);
            }
            Field("spellLooks", "Native presets");
            Field("useGameplayOverrides", "Use native rows");
            using (new EditorGUI.DisabledScope(selected.spellLooks == null))
                if (GUILayout.Button("Edit native spell / projectile presets…")) RenderGrammarLibraryWindow.OpenAsset(selected.spellLooks);
            if (selected.TryResolve(out EffectChannels channels, out EffectElement resolved))
            {
                string priority = selected.UsesGameplayOverride ? "Native handler preset wins" :
                    mode == SpellStudioMode.AuthoredElement ? "Manual element" : "Derived by renderer grammar";
                EditorGUILayout.HelpBox(priority + "\n" + channels.family + " · " + channels.group + " · " + channels.tempo +
                    " → " + resolved + (channels.tempo == EffectTempo.PerPeriod ? "\nPeriod: " + channels.periodSeconds.ToString("0.###") + " s; zero uses entry cycle." : ""), MessageType.Info);
            }
            else EditorGUILayout.HelpBox("Choose a gameplay handler to resolve this preset.", MessageType.Info);
            Field("sourceProjectile", "Projectile lookup");
            if (selected.sourceProjectile != null)
            {
                GUILayout.Label(ProjectileReadout(selected), smallStyle);
            }
        }

        internal static string ProjectileReadout(SpellStudioPreset preset)
        {
            ProjectileLook projectile = preset.ResolveProjectile();
            if (projectile == null)
                return "The native projectile preset is empty. Edit the native table to fill or remove this row.";
            return "Delivery: " + projectile.style +
                (projectile.preserveContactPath ? " · preserves contact path" : "") +
                "\nLookup only; this viewport previews the effect element.";
        }

        private void Duplicate()
        {
            if (selected == null) return;
            var copy = Instantiate(selected);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.name = copy.displayName = Label(selected)+" copy";
            drafts.Add(copy);
            Select(copy);
        }

        private void ExportPreview()
        {
            if (selected == null) return;
            string path = EditorUtility.SaveFilePanel("Export spell preview", "", Label(selected) + ".png", "png");
            if (string.IsNullOrEmpty(path)) return;
            Texture2D image = null;
            try
            {
                image = preview.Capture(selected,time,1600,1000);
                System.IO.File.WriteAllBytes(path,image.EncodeToPNG());
                ShowNotification(new GUIContent("Preview exported at 1600 × 1000"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Could not export preview",exception.Message,"OK");
            }
            finally { if (image != null) DestroyImmediate(image); }
        }

        internal static SpellStudioPreset CreatePresetAsset(SpellStudioPreset source, string path)
        {
            var copy = Instantiate(source);
            copy.hideFlags = HideFlags.None;
            copy.name = System.IO.Path.GetFileNameWithoutExtension(path);
            try
            {
                AssetDatabase.CreateAsset(copy, path);
                AssetDatabase.SaveAssetIfDirty(copy);
                return copy;
            }
            catch
            {
                if (copy && !AssetDatabase.Contains(copy)) DestroyImmediate(copy);
                throw;
            }
        }

        private void SaveAs()
        {
            if (selected == null) return;
            string path = EditorUtility.SaveFilePanelInProject("Save spell preset",Label(selected),"asset","Choose where to store this spell preset.","Assets/Render/Studio/Data/Presets");
            if (string.IsNullOrEmpty(path)) return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var copy = CreatePresetAsset(selected, path);
            ReloadAssets();
            Select(copy);
            EditorGUIUtility.PingObject(copy);
            ShowNotification(new GUIContent("Spell preset saved"));
        }
    }
}
