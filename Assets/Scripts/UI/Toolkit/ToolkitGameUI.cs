using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToolkitGameUI : MonoBehaviour
{
    [SerializeField] string _gameplayScene = "Main";

    [SerializeField] string _menuScene = "MenuScene";

    // The game's sandbox, offered beside Start on the menu
    [SerializeField] string _sandboxScene = ToolkitSceneNavigation.SandboxScene;

    // The classes the menu offers at Start: the game data Main's DataManager plays (Managers.prefab), so a pick
    // is honoured. Left empty, Start goes straight to the gameplay scene with a random class.
    [SerializeField] GameData _gameData;

    [SerializeField] bool _hideLegacyCanvases = true;

    [SerializeField] VisualTreeAsset _layout;

    [SerializeField] ThemeStyleSheet _theme;
    UIDocument _document;
    ToolkitGameView _view;
    PanelSettings _ownedPanel;
    PanelSettings _originalPanel;
    float _nextRefresh;
    ToolkitGameActions _actions;
    bool _started;
    ToolkitGameContext _context = new ToolkitGameContext();
    ToolkitLegacyCanvases _legacyCanvases = new ToolkitLegacyCanvases();
    ToolkitWorldFraming _worldFraming = new ToolkitWorldFraming();
    ToolkitTimeControls _timeControls = new ToolkitTimeControls();
    ToolkitEncounterBar _encounterBar = new ToolkitEncounterBar();
    ToolkitPartyPanel _partyPanel = new ToolkitPartyPanel();
    ToolkitSpellBar _spellBar = new ToolkitSpellBar();
    ToolkitRewardPanel _rewardPanel = new ToolkitRewardPanel();
    ToolkitEventPanel _eventPanel = new ToolkitEventPanel();
    ToolkitMapPanel _mapPanel = new ToolkitMapPanel();
    ToolkitDetailPanel _detailPanel = new ToolkitDetailPanel();
    ToolkitInventoryPanel _inventoryPanel = new ToolkitInventoryPanel();
    ToolkitSheetPanel _sheetPanel = new ToolkitSheetPanel();
    ToolkitSandboxPanel _sandboxPanel = new ToolkitSandboxPanel();
    ToolkitHudStatus _hudStatus = new ToolkitHudStatus();
    ToolkitItemBar _itemBar = new ToolkitItemBar();
#if UNITY_EDITOR
    ToolkitEditorTools _editorTools = new ToolkitEditorTools();
#endif
    ToolkitMobileLayout _mobileLayout = new ToolkitMobileLayout();
    IToolkitIconProvider _iconProvider;
    IToolkitRosterDrag _rosterDrag;
    System.Func<int, bool> _canBeginPointer;
    public void SetPointerGate(System.Func<int, bool> gate)
    { _canBeginPointer = gate; if (_view != null)
        {
            _view.canBeginPointer = gate;
        }
    }
    public void SetRosterDrag(IToolkitRosterDrag drag)
    { _rosterDrag = drag; if (_view != null)
        {
            _view.rosterDrag = drag;
        }
    }
    public bool acceptsWorldInput => _view != null && !_context.isPaused && !_context.isInventoryOpen
        && !_context.isSheetOpen && !_context.isSandboxOpen && _context.IsCurrentView(GameScreen.Game)
#if UNITY_EDITOR
        && !_editorTools.isOpen
#endif
        ;
    public void WorldPointerDown() { _view?.ClosePopover(); }
    public void CancelGestures() { _view?.CancelGestures(); }
    public void InspectEntity(Entity entity, Vector2 screenPoint)
    {
        if (_view == null || entity == null || _context.isPaused)
        {
            return;
        }

        // Without a panel (not displayed yet), the popover takes its default place
        _view.inspectAnchor = default;
        if (_view.root.panel != null)
        {
            Vector2 panel = RuntimePanelUtils.ScreenToPanel(_view.root.panel,
                new Vector2(screenPoint.x, Screen.height - screenPoint.y));
            _view.inspectAnchor = new Rect(panel.x - 1, panel.y - 28, 2, 2);
        }

        _view.OnInspectRequested.Invoke(new ToolkitCardModel { source = entity, iconSource = entity,
            title = entity.data.title, description = entity.data.description });
    }
    void OnApplicationFocus(bool focused) { if (!focused)
        {
            CancelGestures();
        }
    }
    void OnApplicationPause(bool paused) { if (paused)
        {
            CancelGestures();
        }
    }
    public IToolkitIconProvider iconProvider
    {
        get { return _iconProvider; }
    }

    public void SetIconProvider(IToolkitIconProvider provider)
    {
        _iconProvider = provider;
        if (_view != null && isActiveAndEnabled)
        {
            _view.SetIconProvider(provider);
        }
    }

    public System.Func<string, bool> sceneLoader { get; set; }

    // Optional host insets, expressed as a normalized bottom-left screen rectangle.
    public System.Func<Rect> safeAreaProvider { get; set; }

    public Rect normalizedWorldViewport
    {
        get { return _mobileLayout.normalizedWorldViewport; }
    }

    public void SetBattleFocus(bool focused, System.Action toggle)
    {
        _mobileLayout.SetBattleFocus(focused, toggle);
    }

    public string gameplayScene
    {
        get { return _gameplayScene; }
        set { _gameplayScene = value; }
    }

    public string menuScene
    {
        get { return _menuScene; }
        set { _menuScene = value; }
    }

    public GameData gameData
    {
        get { return _gameData; }
        set { _gameData = value; }
    }

    public ToolkitClassSelect classSelect
    {
        get { return _actions != null ? _actions.classSelect : null; }
    }

    public string sandboxScene
    {
        get { return _sandboxScene; }
        set { _sandboxScene = value; }
    }

    // Every Awake of a loaded scene runs before any Start, so the legacy menu never opens its screen
    void Awake()
    {
        if (_hideLegacyCanvases)
        {
            ToolkitLegacyCanvases.SilenceMenus();
        }
    }

    void Start()
    {
        _started = true;
        RegisterStatuses();
        AttachTouchInput();
        Init();
    }

    // A scene with a board gets the touch input: taps without hover, hold to inspect, and the party cards dragged
    // onto the board, the only way the Toolkit places a unit
    void AttachTouchInput()
    {
        InteractionManager interaction = FindAnyObjectByType<InteractionManager>();
        if (interaction == null)
        {
            return;
        }

        ToolkitTouchInput touch = GetComponent<ToolkitTouchInput>();
        if (touch == null)
        {
            touch = gameObject.AddComponent<ToolkitTouchInput>();
        }

        touch.Init(interaction, FindAnyObjectByType<Board>());
    }

    // The menu scene has no DataManager, so its texts ({status:X}) need the statuses of the game data from here
    void RegisterStatuses()
    {
        if (_gameData != null)
        {
            StatusKeywords.Register(_gameData.statuses);
        }
    }

    void OnEnable()
    {
        if (_started)
        {
            Init();
        }
    }

    void Init()
    {
        _document = GetComponent<UIDocument>();
        if (_ownedPanel == null)
        {
            _originalPanel = _document.panelSettings;
            _ownedPanel = ToolkitTheme.CreatePanelSettings(_originalPanel, _theme);
            _document.panelSettings = _ownedPanel;
        }

        VisualTreeAsset layout = _layout != null ? _layout : Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
        if (layout == null)
        {
            Debug.LogError("[ToolkitGameUI] Requires Resources/UI/Toolkit/GameUI.uxml", this);
            enabled = false;
            return;
        }

        ReleaseView();
        VisualElement root = _document.rootVisualElement;
        root.Clear();
        root.AddToClassList("toolkit-document");
        if (!ToolkitTheme.Apply(root, _theme))
        {
            enabled = false;
            return;
        }

        _ownedPanel.themeStyleSheet = ToolkitTheme.Resolve(_theme);
        layout.CloneTree(root);
        if (!ToolkitLayoutContract.Validate(root))
        {
            enabled = false;
            return;
        }

        ToolkitTemplates.PreparePicking(root);
        _view = new ToolkitGameView(root, _iconProvider);
        _view.rosterDrag = _rosterDrag;
        _view.canBeginPointer = _canBeginPointer;
        ToolkitHudGlyph.Attach(root);
        _context.Init();
        _view.OnInspect.AddListener(_detailPanel.OnInspect);
        _view.OnInspectEnded.AddListener(_detailPanel.OnInspectEnded);
        _mobileLayout.Init(_view, _context, _document);
        _timeControls.Init(this, _context, _view);
        _actions = new ToolkitGameActions(this, _context, _view, _timeControls, _mobileLayout, _mapPanel);
        _encounterBar.Init(_context, _view);
        _detailPanel.Init(_context, _view);
        _inventoryPanel.Init(this, _context, _view, _detailPanel);
        _sheetPanel.Init(_context, _view, Refresh);
        _partyPanel.Init(_context, _view, _detailPanel);
        _spellBar.Init(_context, _view);
        _rewardPanel.Init(this, _context, _view);
        _eventPanel.Init(this, _context, _view);
        _mapPanel.Init(_context, _view);
        _sandboxPanel.Init(_context, _view, Refresh);
        _hudStatus.Init(_context, _view);
        _itemBar.Init(_context, _view);
#if UNITY_EDITOR
        _editorTools.Init(_context, _view);
#endif
        if (_hideLegacyCanvases)
        {
            _legacyCanvases.Hide(_context, FindAnyObjectByType<UIManager>());
        }

        Refresh();
    }

    void OnDestroy()
    {
        ReleaseView();
        if (_ownedPanel != null)
        {
            if (_document != null && _document.panelSettings == _ownedPanel)
            {
                _document.panelSettings = _originalPanel;
            }

            Destroy(_ownedPanel);
            _ownedPanel = null;
        }
    }

    void OnDisable()
    {
        ReleaseView();
        _legacyCanvases.Restore();
        _worldFraming.Restore();
        if (_document != null && _document.rootVisualElement != null)
        {
            _document.rootVisualElement.Clear();
        }
    }

    void Update()
    {
        if (_view == null)
        {
            return;
        }

        _actions.Update();
        _mobileLayout.Update(safeAreaProvider);
        _worldFraming.Update(normalizedWorldViewport);
        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.1f;
            Refresh();
        }
    }

    public void Refresh()
    {
        if (_view == null || !isActiveAndEnabled)
        {
            return;
        }

        _mobileLayout.Refresh();
        _mapPanel.Refresh();
        _view.Show("pause-panel", _context.isPaused && _context.IsCurrentView(GameScreen.Game));
        _view.Show("inventory-panel", _context.isInventoryOpen && !_context.isPaused);
        _view.Show("upgrade-panel", _context.IsCurrentView(GameScreen.Reward));
        _view.Show("gameover-panel", _context.IsCurrentView(GameScreen.GameOver));
        if (_context.IsCurrentView(GameScreen.GameOver) && _context.ascension != null)
        {
            _view.SetText("gameover-title", GetResultTitle(_context.ascension.isVictory));
        }
        _sheetPanel.Refresh();
        _sandboxPanel.Refresh();
        _eventPanel.Refresh();
        _itemBar.Refresh(!_context.isMenu);
#if UNITY_EDITOR
        // Before the menu returns: it hides them there
        _editorTools.Refresh();
#endif
        if (_context.isMenu)
        {
            _encounterBar.RefreshMenu(_mobileLayout.isPopoverOpen);
            return;
        }

        if (_context.game == null || _context.player == null)
        {
            return;
        }

        bool isStart = _context.game.state == GameManager.GameState.None;
        bool isPreparing = _context.IsPreparing();
        bool hasOverlay = _context.currentScreen != GameScreen.Game;
        _encounterBar.Refresh(isStart, isPreparing, hasOverlay);
        if (!isPreparing || hasOverlay)
        {
            _context.isInventoryOpen = false;
            _view.Show("inventory-panel", false);
        }

        _encounterBar.RefreshMana();
        _hudStatus.Refresh();
        _partyPanel.Refresh(isPreparing && !isStart && !hasOverlay);
        _spellBar.Refresh(!isStart && !hasOverlay && !_context.isPaused);
        _rewardPanel.Refresh();
        _detailPanel.Refresh();
        _inventoryPanel.RefreshEquipmentAction(isPreparing && !hasOverlay);
        if (_context.isInventoryOpen)
        {
            _inventoryPanel.Refresh();
        }
    }

    public static string GetResultTitle(bool isVictory)
    {
        return isVictory ? "Victory!" : "Your party has fallen";
    }

    void ReleaseView()
    {
        if (_actions != null)
        {
            _actions.Dispose();
            _actions = null;
        }

        _timeControls.Dispose();
        _mobileLayout.Dispose();
        _inventoryPanel.Dispose();
        _sheetPanel.Dispose();
        _sandboxPanel.Dispose();
        _hudStatus.Dispose();
        _itemBar.Dispose();
#if UNITY_EDITOR
        _editorTools.Dispose();
#endif
        _detailPanel.Dispose();
        _mapPanel.Dispose();
        if (_view != null)
        {
            _view.Release();
            _view = null;
        }
    }
}
