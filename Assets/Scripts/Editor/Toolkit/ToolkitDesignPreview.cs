using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

// The player templates, themes and responsive policy, with inert gameplay data.
public class ToolkitDesignPreview : EditorWindow
{
    static readonly string[] states =
    {
        "HUD",
        "party",
        "detail",
        "inventory",
        "map",
        "upgrade",
        "pause",
        "gameover",
        "menu",
    };
    static readonly string[] modalPanels = { "inventory", "map", "upgrade", "pause", "gameover" };
    static readonly string[] viewports = { "Desktop", "Phone portrait", "Phone landscape" };

    [SerializeField] string _state = "HUD";

    [SerializeField] string _viewport = "Desktop";

    [SerializeField] ThemeStyleSheet _theme;
    VisualElement _preview;
    ToolkitGameView _view;
    ToolkitMapGraph _mapGraph;
    RunState _mapRun;

    [MenuItem("Tools/UI Toolkit/Design Preview")]
    public static void Open()
    {
        ToolkitDesignPreview window = GetWindow<ToolkitDesignPreview>();
        window.titleContent = new GUIContent("Toolkit Design Preview");
        window.minSize = new Vector2(560f, 360f);
        window.Show();
    }

    public void CreateGUI()
    {
        Release();
        VisualElement root = rootVisualElement;
        root.Clear();
        Toolbar toolbar = new Toolbar();
        ToolbarButton reload = new ToolbarButton(CreateGUI);
        reload.text = "Reload assets";
        toolbar.Add(reload);
        ObjectField themeField = new ObjectField("Theme");
        themeField.objectType = typeof(ThemeStyleSheet);
        themeField.allowSceneObjects = false;
        themeField.value = _theme;
        themeField.tooltip = "Select any custom TSS. None uses RuntimeTheme (Forest).";
        themeField.RegisterValueChangedCallback(OnThemeChanged);
        toolbar.Add(themeField);
        AddMenu(toolbar, _viewport, viewports, OnViewportSelected);
        AddMenu(toolbar, _state, states, OnStateSelected);
        root.Add(toolbar);
        VisualTreeAsset tree = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
        if (tree == null)
        {
            root.Add(new Label("Import Resources/UI/Toolkit/GameUI.uxml first."));
            return;
        }

        ScrollView canvas = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        canvas.style.flexGrow = 1f;
        root.Add(canvas);
        _preview = tree.CloneTree();
        _preview.AddToClassList("toolkit-preview");
        if (!ToolkitTheme.Apply(_preview, _theme))
        {
            return;
        }

        canvas.Add(_preview);
        if (!ToolkitLayoutContract.Validate(_preview))
        {
            return;
        }

        _view = new ToolkitGameView(_preview);
        _view.OnInspect.AddListener(_view.ShowDetail);
        _view.OnInspectRequested.AddListener(OnInspectRequested);
        _preview.Q("hud-root").RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        ApplyViewport();
        _view.SetText("currency-label", "125 gold");
        _view.SetText("wave-label", "Room 3 · Combat");
        _view.SetText("phase-label", "PREPARATION");
        _view.SetResource("mana-bar", 72f, 100f);
        _view.Show("start-button", false);
        _view.AddClickListener("inventory-button", OnInventoryClicked);
        _view.AddClickListener("inventory-close-button", OnCloseClicked);
        _view.AddClickListener("pause-button", OnPauseClicked);
        _view.AddClickListener("resume-button", OnCloseClicked);
        _view.AddClickListener("party-button", OnPartyClicked);
        _view.AddClickListener("detail-button", OnDetailClicked);
        _view.AddClickListener("party-close-button", OnCloseClicked);
        _view.AddClickListener("detail-close-button", OnCloseClicked);
        ToolkitPreviewData.FillCards(_view);
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
            "Assets/Data/Run/MapGenerationSettings.asset"
        );
        if (settings != null)
        {
            _mapRun = new RunState(MapGenerator.Generate(settings, 65));
            _mapGraph = new ToolkitMapGraph(_preview.Q<ScrollView>("map-scroll"), null);
        }

        ShowState();
    }

    static void AddMenu(Toolbar toolbar, string selected, string[] choices, System.Action<DropdownMenuAction> callback)
    {
        ToolbarMenu menu = new ToolbarMenu();
        menu.text = selected;
        foreach (string choice in choices)
        {
            menu.menu.AppendAction(
                choice,
                callback,
                choice == selected ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
        }

        toolbar.Add(menu);
    }

    void ApplyViewport()
    {
        Vector2 size = new Vector2(1440f, 900f);
        if (_viewport == "Phone portrait")
        {
            size = new Vector2(390f, 844f);
        }
        else if (_viewport == "Phone landscape")
        {
            size = new Vector2(844f, 390f);
        }

        _preview.style.width = size.x;
        _preview.style.height = size.y;
        ToolkitResponsiveLayout.Apply(_view, size.x, size.y);
    }

    void OnStyleResolved(CustomStyleResolvedEvent evt)
    {
        ApplyViewport();
    }

    void OnThemeChanged(ChangeEvent<Object> evt)
    {
        _theme = evt.newValue as ThemeStyleSheet;
        CreateGUI();
    }

    void OnViewportSelected(DropdownMenuAction action)
    {
        _viewport = action.name;
        CreateGUI();
    }

    void OnStateSelected(DropdownMenuAction action)
    {
        _state = action.name;
        CreateGUI();
    }

    void ShowState()
    {
        if (_mapGraph != null)
        {
            if (_state == "map")
            {
                _mapGraph.Display(_mapRun, false);
            }
            else
            {
                _mapGraph.Hide();
            }
        }

        foreach (string panel in modalPanels)
        {
            _view.Show(panel + "-panel", panel == _state);
        }

        VisualElement hud = _preview.Q("hud-root");
        hud.EnableInClassList("party-open", _state == "party");
        hud.EnableInClassList("detail-open", _state == "detail");
        hud.EnableInClassList("is-menu", _state == "menu");
        _view.Show("menu-panel", _state == "menu");
        if (_state == "menu")
        {
            ToolkitEncounterBar encounter = new ToolkitEncounterBar();
            encounter.Init(new ToolkitGameContext(), _view);
            encounter.RefreshMenu();
        }
    }

    void OnDisable()
    {
        Release();
    }

    void Release()
    {
        if (_preview != null)
        {
            VisualElement hud = _preview.Q("hud-root");
            if (hud != null)
            {
                hud.UnregisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
            }
        }

        if (_mapGraph != null)
        {
            _mapGraph.Dispose();
            _mapGraph = null;
        }

        if (_view != null)
        {
            _view.Release();
            _view = null;
        }

        _mapRun = null;
    }

    void OnInspectRequested(ToolkitCardModel model)
    {
        _view.ShowDetail(model);
        _state = "detail";
        ShowState();
    }

    void OnInventoryClicked()
    {
        _state = "inventory";
        ShowState();
    }

    void OnPauseClicked()
    {
        _state = "pause";
        ShowState();
    }

    void OnPartyClicked()
    {
        _state = "party";
        ShowState();
    }

    void OnDetailClicked()
    {
        _state = "detail";
        ShowState();
    }

    void OnCloseClicked()
    {
        _state = "HUD";
        ShowState();
    }
}
