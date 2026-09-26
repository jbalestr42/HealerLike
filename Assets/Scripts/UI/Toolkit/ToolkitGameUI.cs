using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToolkitGameUI : MonoBehaviour
{
    [SerializeField] string _gameplayScene = "Main";

    [SerializeField] string _menuScene = "MenuScene";

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
    ToolkitTimeControls _timeControls = new ToolkitTimeControls();
    ToolkitEncounterBar _encounterBar = new ToolkitEncounterBar();
    ToolkitPartyPanel _partyPanel = new ToolkitPartyPanel();
    ToolkitSpellBar _spellBar = new ToolkitSpellBar();
    ToolkitRewardPanel _rewardPanel = new ToolkitRewardPanel();
    ToolkitMapPanel _mapPanel = new ToolkitMapPanel();
    ToolkitDetailPanel _detailPanel = new ToolkitDetailPanel();
    ToolkitInventoryPanel _inventoryPanel = new ToolkitInventoryPanel();
    ToolkitMobileLayout _mobileLayout = new ToolkitMobileLayout();
    IToolkitIconProvider _iconProvider;
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

    void Start()
    {
        _started = true;
        Init();
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
        _context.Init();
        _view.OnInspect.AddListener(_detailPanel.OnInspect);
        _view.OnInspectEnded.AddListener(_detailPanel.OnInspectEnded);
        _mobileLayout.Init(_view, _context, _document);
        _timeControls.Init(this, _context, _view);
        _actions = new ToolkitGameActions(this, _context, _view, _timeControls, _mobileLayout, _mapPanel);
        _encounterBar.Init(_context, _view);
        _detailPanel.Init(_context, _view);
        _inventoryPanel.Init(this, _context, _view, _detailPanel);
        _partyPanel.Init(_context, _view, _detailPanel);
        _spellBar.Init(_context, _view);
        _rewardPanel.Init(this, _context, _view);
        _mapPanel.Init(_context, _view);
        if (_hideLegacyCanvases)
        {
            _legacyCanvases.Hide(_context);
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
        _view.Show("pause-panel", _context.isPaused && _context.IsCurrentView(ViewType.Game));
        _view.Show("inventory-panel", _context.isInventoryOpen && !_context.isPaused);
        _view.Show("upgrade-panel", _context.IsCurrentView(ViewType.Upgrade));
        _view.Show("gameover-panel", _context.IsCurrentView(ViewType.GameOver));
        if (_context.isMenu)
        {
            _encounterBar.RefreshMenu();
            return;
        }

        if (_context.legacy == null || _context.game == null || _context.player == null)
        {
            return;
        }

        bool isStart = LegacyUiReader.GameState(_context.game) == GameManager.GameState.None;
        bool isPreparing = _context.IsPreparing();
        bool hasOverlay = LegacyUiReader.CurrentView(_context.ui) != ViewType.Game;
        _encounterBar.Refresh(isStart, isPreparing, hasOverlay);
        if (!isPreparing || hasOverlay)
        {
            _context.isInventoryOpen = false;
            _view.Show("inventory-panel", false);
        }

        _encounterBar.RefreshMana();
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
        _detailPanel.Dispose();
        _mapPanel.Dispose();
        if (_view != null)
        {
            _view.Release();
            _view = null;
        }
    }
}
