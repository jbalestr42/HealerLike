using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// Previews real data and the modal layouts without starting or changing the gameplay
public class ToolkitDesignPreview : EditorWindow
{
    static readonly string[] states = { "HUD", "inventory", "map", "upgrade", "pause", "gameover" };
    static readonly string[] modalPanels = { "inventory", "map", "upgrade", "pause", "gameover" };
    static readonly int cardCount = 8;
    static readonly int rewardCount = 3;

    ToolbarMenu _stateMenu;
    VisualElement _preview;
    ToolkitGameView _view;
    ToolkitMapGraph _mapGraph;
    RunState _mapRun;

    [MenuItem("Tools/UI Toolkit/Design Preview")]
    public static void Open()
    {
        ToolkitDesignPreview window = GetWindow<ToolkitDesignPreview>();
        window.titleContent = new GUIContent("Toolkit Design Preview");
        window.minSize = new Vector2(800f, 600f);
        window.Show();
    }

    public void CreateGUI()
    {
        Release();
        VisualElement root = rootVisualElement;
        root.Clear();
        Toolbar toolbar = new Toolbar();
        ToolbarButton reload = new ToolbarButton(CreateGUI);
        reload.text = "Reload layout / theme";
        toolbar.Add(reload);
        _stateMenu = new ToolbarMenu();
        _stateMenu.text = "HUD";
        toolbar.Add(_stateMenu);
        ToolbarToggle compact = new ToolbarToggle();
        compact.text = "Compact";
        toolbar.Add(compact);
        root.Add(toolbar);
        VisualTreeAsset tree = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
        if (tree == null)
        {
            root.Add(new Label("Import Resources/UI/Toolkit/GameUI.uxml first."));
            return;
        }

        _preview = tree.CloneTree();
        _preview.style.flexGrow = 1f;
        _preview.style.backgroundColor = new Color(0.08f, 0.12f, 0.13f);
        root.Add(_preview);
        _view = new ToolkitGameView(_preview);
        _view.OnInspect.AddListener(_view.ShowDetail);
        foreach (string state in states)
        {
            _stateMenu.menu.AppendAction(state, OnStateSelected);
        }

        compact.RegisterValueChangedCallback(OnCompactChanged);
        _view.SetText("currency-label", "125 gold");
        _view.SetText("wave-label", "Room 3 · Combat");
        _view.SetText("phase-label", "PREPARATION");
        _view.SetResource("mana-bar", 72f, 100f);
        _view.Show("start-button", false);
        _view.AddClickListener("inventory-button", OnInventoryClicked);
        _view.AddClickListener("inventory-close-button", OnInventoryCloseClicked);
        _view.AddClickListener("pause-button", OnPauseClicked);
        _view.AddClickListener("resume-button", OnResumeClicked);
        FillCards();
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");
        if (settings != null)
        {
            _mapRun = new RunState(MapGenerator.Generate(settings, 65));
            _mapGraph = new ToolkitMapGraph(_preview.Q<ScrollView>("map-scroll"), null);
        }
    }

    void FillCards()
    {
        List<ToolkitCardModel> creatures = new List<ToolkitCardModel>();
        foreach (EntityData data in FindAssets<EntityData>())
        {
            creatures.Add(CreateCard(data, data.title, data.description));
        }

        List<ToolkitCardModel> spells = new List<ToolkitCardModel>();
        foreach (ACharacterSkillFactory data in FindAssets<ACharacterSkillFactory>())
        {
            spells.Add(CreateCard(data, data.name, "Inspect spell details in the game."));
        }

        List<ToolkitCardModel> items = new List<ToolkitCardModel>();
        foreach (AItemFactory data in FindAssets<AItemFactory>())
        {
            items.Add(CreateCard(data, data.title, "Equipment reward"));
        }

        _view.SetCards("party-list", creatures);
        _view.SetCards("spell-list", spells);
        _view.SetCards("inventory-list", items);
        _view.SetCards("upgrade-list", items.GetRange(0, Mathf.Min(rewardCount, items.Count)));
        if (creatures.Count > 0)
        {
            _view.ShowDetail(creatures[0]);
        }
    }

    static List<DataType> FindAssets<DataType>() where DataType : ScriptableObject
    {
        List<DataType> assets = new List<DataType>();
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(DataType).Name);
        for (int i = 0; i < guids.Length && i < cardCount; i++)
        {
            DataType data = AssetDatabase.LoadAssetAtPath<DataType>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (data != null)
            {
                assets.Add(data);
            }
        }

        return assets;
    }

    static ToolkitCardModel CreateCard(Object data, string title, string description)
    {
        ToolkitCardModel model = new ToolkitCardModel();
        model.iconSource = data;
        model.title = title;
        model.description = description;
        model.status = "Ready";
        return model;
    }

    void OnStateSelected(DropdownMenuAction action)
    {
        _stateMenu.text = action.name;
        if (action.name == "map") _mapGraph?.Display(_mapRun, false);
        else _mapGraph?.Hide();
        foreach (string panel in modalPanels)
        {
            _view.Show(panel + "-panel", panel == action.name);
        }
    }

    void OnDisable() { Release(); }

    void Release()
    {
        _mapGraph?.Dispose();
        _mapGraph = null;
        _view?.Release();
        _view = null;
    }

    void OnCompactChanged(ChangeEvent<bool> evt)
    {
        _preview.Q("hud-root").EnableInClassList("is-compact", evt.newValue);
    }

    void OnInventoryClicked()
    {
        _view.Show("inventory-panel", true);
    }

    void OnInventoryCloseClicked()
    {
        _view.Show("inventory-panel", false);
    }

    void OnPauseClicked()
    {
        _view.Show("pause-panel", true);
    }

    void OnResumeClicked()
    {
        _view.Show("pause-panel", false);
    }
}
