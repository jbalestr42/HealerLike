using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Editor.Toolkit
{
    /// <summary>Preview real data and modal layouts without starting or mutating gameplay.</summary>
    public sealed class ToolkitDesignPreview : EditorWindow
    {
        [MenuItem("HealerLike/UI Toolkit/Design Preview")]
        public static void Open()
        {
            var window = GetWindow<ToolkitDesignPreview>();
            window.titleContent = new GUIContent("Toolkit Design Preview");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            var toolbar = new Toolbar();
            var reload = new ToolbarButton(CreateGUI) { text = "Reload layout / theme" };
            toolbar.Add(reload);
            var state = new ToolbarMenu { text = "HUD" };
            toolbar.Add(state);
            var compact = new ToolbarToggle { text = "Compact" };
            toolbar.Add(compact);
            root.Add(toolbar);
            var tree = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
            if (tree == null) { root.Add(new Label("Import Resources/UI/Toolkit/GameUI.uxml first.")); return; }
            var preview = tree.CloneTree();
            preview.style.flexGrow = 1;
            preview.style.backgroundColor = new Color(.08f, .12f, .13f);
            root.Add(preview);
            var view = new ToolkitGameView(preview);
            view.OnInspect.AddListener(view.ShowDetail);
            foreach (string panel in new[] { "HUD", "inventory", "selection", "upgrade", "pause", "gameover" })
            {
                string selected = panel;
                state.menu.AppendAction(panel, _ =>
                {
                    state.text = selected;
                    foreach (string name in new[] { "inventory", "selection", "upgrade", "pause", "gameover" })
                        view.Show(name + "-panel", name == selected);
                });
            }
            compact.RegisterValueChangedCallback(evt => preview.Q("hud-root").EnableInClassList("is-compact", evt.newValue));
            view.SetText("currency-label", "125 gold");
            view.SetText("wave-label", "Wave 3");
            view.SetText("phase-label", "PREPARATION");
            view.SetResource("mana-bar", 72, 100);
            view.Show("start-button", false);
            view.AddClickListener("inventory-button", () => view.Show("inventory-panel", true));
            view.AddClickListener("inventory-close-button", () => view.Show("inventory-panel", false));
            view.AddClickListener("pause-button", () => view.Show("pause-panel", true));
            view.AddClickListener("resume-button", () => view.Show("pause-panel", false));
            var creatures = FindCards<EntityData>(data => data.title, data => data.description);
            var spells = FindCards<ACharacterSkillFactory>(data => data.name, data => "Inspect spell details in the game.");
            var items = FindCards<AItemFactory>(data => data.title, data => "Equipment reward");
            view.SetCards("party-list", creatures);
            view.SetCards("spell-list", spells);
            view.SetCards("inventory-list", items);
            view.SetCards("upgrade-list", items.Take(3).ToArray());
            view.SetCards("selection-list", FindCards<WavePatternData>(data => data.name, data => "Encounter choice"));
            if (creatures.Length > 0) view.ShowDetail(creatures[0]);
        }

        static ToolkitCardModel[] FindCards<T>(Func<T, string> title, Func<T, string> description) where T : ScriptableObject
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name).Take(8)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(data => data != null)
                .Select(data => new ToolkitCardModel { iconSource = data, title = title(data), description = description(data), status = "Ready" })
                .ToArray();
        }
    }
}
