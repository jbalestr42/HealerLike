using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{
    // The renderer's shared vocabularies and override tables in one window, each drawn by its own Odin inspector
    // so the serialized dictionaries stay editable. The studios listen to OnAssetChanged to preview edits at once.
    public class RenderGrammarLibraryWindow : EditorWindow
    {
        public static readonly string[] AssetPaths =
        {
            "Assets/Render/Creatures/Data/LookVocabulary.asset",
            "Assets/Render/Creatures/Data/CreatureLooks.asset",
            "Assets/Render/Spells/Data/EffectVocabulary.asset",
            "Assets/Render/Spells/Data/SpellLooks.asset",
            "Assets/Render/Grammar/Data/LookPalette.asset",
            "Assets/Render/Deliveries/Data/DeliveryVocabulary.asset"
        };

        // Raised with the asset an editor changed, the unsaved creature drafts included
        public static readonly UnityEvent<Object> OnAssetChanged = new UnityEvent<Object>();

        static readonly string[] labels =
        {
            "Creature grammar", "Creature overrides", "Spell grammar", "Spell overrides", "Shared palette",
            "Delivery grammar"
        };
        static readonly string[] descriptions =
        {
            "Head and accessory fragments, body / stem / reach bands, and proportions used by LookComposer.",
            "Entity and character prefab overrides take priority over generated looks. Plant, stone and character "
                + "hosts are the fallbacks.",
            "Effect vocabulary defines the parts, motion, socket and count rule for every grammar element.",
            "Buff-handler rows override generated element / family / tempo. Projectile rows select delivery style "
                + "and contact-path preservation.",
            "Shared family accents and material colour roles used by the creature and effect composers.",
            "The vocabulary for projectile tips, drops and delivery motion. These are shared renderer settings."
        };

        [SerializeField] Object _asset;
        [SerializeField] int _selectedTab;
        UnityEditor.Editor _inspector;
        Vector2 _scroll;

        public Object selectedAsset { get { return _asset; } }

        [MenuItem("Tools/Render/Grammar & Presets", false, 112)]
        public static void OpenCreatures()
        {
            OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[0]));
        }

        public static void OpenSpells()
        {
            OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[2]));
        }

        public static void OpenAsset(Object value)
        {
            RenderGrammarLibraryWindow window = GetWindow<RenderGrammarLibraryWindow>();
            window.SelectAsset(value);
            window.Show();
            window.Focus();
        }

        // Odin's inspector replacement can be switched off in the project preferences, so its editor is asked for
        public static UnityEditor.Editor CreateNativeInspector(Object value)
        {
            if (!value)
            {
                return null;
            }
            return UnityEditor.Editor.CreateEditor(value, typeof(Sirenix.OdinInspector.Editor.OdinEditor));
        }

        public static string Summary(Object value)
        {
            if (value is LookVocabulary creature)
            {
                return RenderGrammarSummary.Describe(creature);
            }

            if (value is CreatureLooks creatures)
            {
                return RenderGrammarSummary.Describe(creatures);
            }

            if (value is EffectVocabulary effects)
            {
                return RenderGrammarSummary.Count(effects.elements) + " effect element presets";
            }

            if (value is SpellLooks spells)
            {
                return RenderGrammarSummary.Describe(spells);
            }

            if (value is LookPalette)
            {
                return "Family accents and shared plant / stone / mana colour roles";
            }

            if (value is DeliveryVocabulary)
            {
                return "Projectile delivery shapes and motion vocabulary";
            }

            if (value)
            {
                return value.GetType().Name;
            }
            return "No asset selected";
        }

        public void SelectAsset(Object value)
        {
            if (_inspector)
            {
                DestroyImmediate(_inspector);
            }

            _inspector = null;
            _asset = value;
            int index = Array.IndexOf(AssetPaths, AssetDatabase.GetAssetPath(value));
            if (index >= 0)
            {
                _selectedTab = index;
            }

            if (value)
            {
                _inspector = CreateNativeInspector(value);
            }

            _scroll = Vector2.zero;
            Repaint();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Grammar & Presets");
            minSize = new Vector2(780f, 600f);
            Undo.undoRedoPerformed += OnUndoRedo;
            if (!_asset)
            {
                int tab = Mathf.Clamp(_selectedTab, 0, AssetPaths.Length - 1);
                _asset = AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[tab]);
            }
            SelectAsset(_asset);
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (_inspector)
            {
                DestroyImmediate(_inspector);
            }
            _inspector = null;
        }

        void OnUndoRedo()
        {
            OnAssetChanged.Invoke(_asset);
            Repaint();
        }

        void OnGUI()
        {
            GUILayout.Space(10f);
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel);
            title.fontSize = 20;
            GUILayout.Label("Grammar & Presets", title);
            EditorGUILayout.HelpBox("Grammar presets remember inputs. Vocabulary supplies shapes and proportions. "
                + "Authored overrides win over generated looks.", MessageType.Info);
            GUILayout.BeginHorizontal();
            DrawTabs();
            GUILayout.BeginVertical();
            GUILayout.Label(descriptions[Mathf.Clamp(_selectedTab, 0, descriptions.Length - 1)],
                EditorStyles.wordWrappedLabel);
            EditorGUI.BeginChangeCheck();
            Object next = EditorGUILayout.ObjectField("Asset", _asset, typeof(ScriptableObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                SelectAsset(next);
                GUIUtility.ExitGUI();
            }

            if (!_asset)
            {
                EditorGUILayout.HelpBox("Select a vocabulary or override asset to edit.", MessageType.Warning);
            }
            else
            {
                DrawAsset();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        void DrawTabs()
        {
            GUILayout.BeginVertical(GUILayout.Width(185f));
            for (int i = 0; i < labels.Length; i++)
            {
                bool isActive = _selectedTab == i;
                if (GUILayout.Toggle(isActive, labels[i], "Button", GUILayout.Height(30f)) && !isActive)
                {
                    _selectedTab = i;
                    SelectAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[i]));
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.Space(14f);
            GUILayout.Label("Edit the shared asset here. Save commits these values to the renderer's vocabulary "
                + "or override table.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }

        void DrawAsset()
        {
            GUILayout.Label(Summary(_asset), EditorStyles.wordWrappedMiniLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Locate asset"))
            {
                EditorGUIUtility.PingObject(_asset);
            }

            if (GUILayout.Button("Save changes"))
            {
                AssetDatabase.SaveAssetIfDirty(_asset);
                OnAssetChanged.Invoke(_asset);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (!_inspector)
            {
                _inspector = CreateNativeInspector(_asset);
            }

            EditorGUI.BeginChangeCheck();
            _inspector.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                OnAssetChanged.Invoke(_asset);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
