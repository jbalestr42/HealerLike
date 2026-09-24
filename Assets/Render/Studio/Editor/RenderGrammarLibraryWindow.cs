using System;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    /// <summary>Hosts the native asset inspectors, including Odin's serialized vocabulary dictionaries.</summary>
    public sealed class RenderGrammarLibraryWindow : EditorWindow
    {
        public static event Action<Object> AssetChanged;
        public static void NotifyAssetChanged(Object value) => AssetChanged?.Invoke(value);
        public static readonly string[] AssetPaths =
        {
            "Assets/Render/Creatures/Data/LookVocabulary.asset",
            "Assets/Render/Creatures/Data/CreatureLooks.asset",
            "Assets/Render/Spells/Data/EffectVocabulary.asset",
            "Assets/Render/Spells/Data/SpellLooks.asset",
            "Assets/Render/Grammar/Data/LookPalette.asset",
            "Assets/Render/Deliveries/Data/DeliveryVocabulary.asset"
        };
        static readonly string[] Labels =
        {
            "Creature grammar", "Creature overrides", "Spell grammar", "Spell overrides", "Shared palette", "Delivery grammar"
        };
        static readonly string[] Descriptions =
        {
            "Head and accessory fragments, body / stem / reach bands, and proportions used by LookComposer.",
            "Entity and character prefab overrides take priority over generated looks. Plant, stone and character hosts are the fallbacks.",
            "Effect vocabulary defines the parts, motion, socket and count rule for every grammar element.",
            "Buff-handler rows override generated element / family / tempo. Projectile rows select delivery style and contact-path preservation.",
            "Shared family accents and material colour roles used by the creature and effect composers.",
            "The vocabulary for projectile tips, drops and delivery motion. These are shared renderer settings."
        };
        [SerializeField] Object asset;
        [SerializeField] int selectedTab;
        UnityEditor.Editor inspector;
        Vector2 scroll;
        public Object SelectedAsset => asset;

        [MenuItem("Tools/Render/Grammar & Presets", false, 112)]
        public static void OpenCreatures() => OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[0]));
        public static void OpenSpells() => OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[2]));

        public static void OpenAsset(Object value)
        {
            var window = GetWindow<RenderGrammarLibraryWindow>();
            window.SelectAsset(value);
            window.Show();
            window.Focus();
        }

        public void SelectAsset(Object value)
        {
            if (inspector) DestroyImmediate(inspector);
            inspector = null;
            asset = value;
            int index = Array.IndexOf(AssetPaths, AssetDatabase.GetAssetPath(value));
            if (index >= 0) selectedTab = index;
            if (value) inspector = CreateNativeInspector(value);
            scroll = Vector2.zero;
            Repaint();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Grammar & Presets");
            minSize = new Vector2(780, 600);
            Undo.undoRedoPerformed += OnUndo;
            if (!asset) asset = AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[Mathf.Clamp(selectedTab, 0, AssetPaths.Length - 1)]);
            SelectAsset(asset);
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            if (inspector) DestroyImmediate(inspector);
            inspector = null;
        }

        void OnUndo() { AssetChanged?.Invoke(asset); Repaint(); }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Grammar & Presets", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 });
            EditorGUILayout.HelpBox("Grammar presets remember inputs. Vocabulary supplies shapes and proportions. Authored overrides win over generated looks.", MessageType.Info);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(185));
            for (int i = 0; i < Labels.Length; i++)
            {
                bool active = selectedTab == i;
                if (GUILayout.Toggle(active, Labels[i], "Button", GUILayout.Height(30)) && !active)
                {
                    selectedTab = i;
                    SelectAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetPaths[i]));
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.Space(14);
            GUILayout.Label("Edit the shared asset here. Save commits these values to the renderer's vocabulary or override table.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label(Descriptions[Mathf.Clamp(selectedTab, 0, Descriptions.Length - 1)], EditorStyles.wordWrappedLabel);
            EditorGUI.BeginChangeCheck();
            Object next = EditorGUILayout.ObjectField("Asset", asset, typeof(ScriptableObject), false);
            if (EditorGUI.EndChangeCheck()) { SelectAsset(next); GUIUtility.ExitGUI(); }
            if (!asset)
            {
                EditorGUILayout.HelpBox("Select a vocabulary or override asset to edit.", MessageType.Warning);
            }
            else
            {
                GUILayout.Label(Summary(asset), EditorStyles.wordWrappedMiniLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Locate asset")) EditorGUIUtility.PingObject(asset);
                if (GUILayout.Button("Save changes")) { AssetDatabase.SaveAssetIfDirty(asset); AssetChanged?.Invoke(asset); }
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                if (!inspector) inspector = CreateNativeInspector(asset);
                EditorGUI.BeginChangeCheck();
                inspector.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck()) AssetChanged?.Invoke(asset);
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // Odin's automatic inspector replacement can be disabled in project preferences.
        // Request its editor explicitly so serialized dictionaries remain authorable.
        public static UnityEditor.Editor CreateNativeInspector(Object value) => value
            ? UnityEditor.Editor.CreateEditor(value, typeof(Sirenix.OdinInspector.Editor.OdinEditor))
            : null;

        public static string Summary(Object value)
        {
            if (value is LookVocabulary creature)
                return $"{creature.heads?.Count ?? 0} head presets · {creature.accessories?.Count ?? 0} accessory presets · {creature.bodies?.Count ?? 0} body bands · {creature.stems?.Count ?? 0} stem bands · {creature.roots?.Count ?? 0} reach bands";
            if (value is CreatureLooks creatures)
                return $"{creatures.entities?.Count ?? 0} entity overrides · {creatures.characters?.Count ?? 0} character overrides. Empty tables mean those looks are generated from gameplay data.";
            if (value is EffectVocabulary effects) return $"{effects.elements?.Count ?? 0} effect element presets";
            if (value is SpellLooks spells)
                return $"{spells.buffs?.Count ?? 0} buff overrides · {spells.projectiles?.Count ?? 0} projectile overrides. Empty tables mean the grammar supplies the look.";
            if (value is LookPalette) return "Family accents and shared plant / stone / mana colour roles";
            if (value is DeliveryVocabulary) return "Projectile delivery shapes and motion vocabulary";
            return value ? value.GetType().Name : "No asset selected";
        }
    }
}
