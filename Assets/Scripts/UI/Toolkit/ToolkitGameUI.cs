using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ToolkitGameUI : MonoBehaviour
{
    [SerializeField] string _gameplayScene = "Main";
    [SerializeField] string _menuScene = "MenuScene";
    [SerializeField] bool _hideLegacyCanvases = true;
    [SerializeField] VisualTreeAsset _layout;

    UIDocument _document;
    ToolkitGameView _view;
    PanelSettings _ownedPanel;
    float _nextRefresh;
    bool _interactionActive;
    ToolkitGameContext _context = new ToolkitGameContext();
    ToolkitLegacyCanvases _legacyCanvases = new ToolkitLegacyCanvases();
    ToolkitTimeControls _timeControls = new ToolkitTimeControls();
    ToolkitWaveBar _waveBar = new ToolkitWaveBar();
    ToolkitPartyPanel _partyPanel = new ToolkitPartyPanel();
    ToolkitSpellBar _spellBar = new ToolkitSpellBar();
    ToolkitRewardPanel _rewardPanel = new ToolkitRewardPanel();
    ToolkitWavePanel _wavePanel = new ToolkitWavePanel();
    ToolkitDetailPanel _detailPanel = new ToolkitDetailPanel();
    ToolkitInventoryPanel _inventoryPanel = new ToolkitInventoryPanel();
    ToolkitMobileLayout _mobileLayout = new ToolkitMobileLayout();

    public System.Func<string, bool> sceneLoader { get; set; }

    // Optional host insets, expressed as a normalized bottom-left screen rectangle.
    public System.Func<Rect> safeAreaProvider { get; set; }

    public Rect normalizedWorldViewport { get { return _mobileLayout.normalizedWorldViewport; } }

    public void SetBattleFocus(bool focused, System.Action toggle)
    {
        _mobileLayout.SetBattleFocus(focused, toggle);
    }

    public string gameplayScene { get { return _gameplayScene; } set { _gameplayScene = value; } }

    public string menuScene { get { return _menuScene; } set { _menuScene = value; } }

    void Start()
    {
        Init();
    }

    void OnEnable()
    {
        if (_view != null)
        {
            Init();
        }
    }

    void Init()
    {
        if (!LegacyUiReader.IsValid())
        {
            Debug.LogError("[ToolkitGameUI] The legacy UI contract changed, the Toolkit interface stays off");
            enabled = false;
            return;
        }

        _document = GetComponent<UIDocument>();
        if (_ownedPanel == null)
        {
            _ownedPanel = ToolkitMobileLayout.CreatePanelSettings(_document.panelSettings);
            _document.panelSettings = _ownedPanel;
        }

        VisualTreeAsset layout = _layout != null ? _layout : Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
        if (layout == null)
        {
            Debug.LogError("[ToolkitGameUI] Requires Resources/UI/Toolkit/GameUI.uxml", this);
            enabled = false;
            return;
        }

        if (_view != null)
        {
            _view.Release();
        }

        VisualElement root = _document.rootVisualElement;
        root.Clear();
        root.style.flexGrow = 1f;
        layout.CloneTree(root);
        // CloneTree may introduce full-screen TemplateContainers, keep the board pickable
        root.Query<TemplateContainer>().ForEach(IgnorePicking);
        _view = new ToolkitGameView(root);
        _context.Init();
        _view.OnInspect.AddListener(_detailPanel.OnInspect);
        _view.OnInspectEnded.AddListener(_detailPanel.OnInspectEnded);
        _mobileLayout.Init(_view, _context, _document);
        _view.AddClickListener("wave-button", StartOrAdvance);
        _view.AddClickListener("start-button", StartOrAdvance);
        _view.AddClickListener("inventory-button", OnInventoryClicked);
        _view.AddClickListener("inventory-close-button", OnInventoryCloseClicked);
        _timeControls.Init(this, _context, _view);
        _view.AddClickListener("restart-button", OnRestartClicked);
        _view.AddClickListener("menu-button", OnRestartClicked);
        Toggle markToggle = root.Q<Toggle>("mark-entity-toggle");
        if (markToggle != null)
        {
            markToggle.RegisterValueChangedCallback(OnMarkToggleChanged);
        }

        _waveBar.Init(_context, _view);
        _detailPanel.Init(_context, _view);
        _inventoryPanel.Init(this, _context, _view, _detailPanel);
        _partyPanel.Init(_context, _view, _detailPanel);
        _spellBar.Init(_context, _view);
        _rewardPanel.Init(this, _context, _view);
        _wavePanel.Init(_context, _view);
        if (_hideLegacyCanvases)
        {
            _legacyCanvases.Hide(_context);
        }

        Refresh();
    }

    void OnDestroy()
    {
        if (_view != null)
        {
            _view.Release();
        }

        if (_ownedPanel != null)
        {
            Destroy(_ownedPanel);
        }
    }

    void OnDisable()
    {
        _legacyCanvases.Restore();
        _timeControls.Resume();
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnEscape();
        }

        _interactionActive = _context.hasInteraction;
        _mobileLayout.Update(safeAreaProvider);
        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.1f;
            Refresh();
        }
    }

    public void Refresh()
    {
        _mobileLayout.Refresh();
        _view.Show("pause-panel", _context.isPaused && _context.IsCurrentView(ViewType.Game));
        _view.Show("inventory-panel", _context.isInventoryOpen && !_context.isPaused);
        _view.Show("selection-panel", _context.IsCurrentView(ViewType.Wave));
        _view.Show("upgrade-panel", _context.IsCurrentView(ViewType.Upgrade));
        _view.Show("gameover-panel", _context.IsCurrentView(ViewType.GameOver));
        if (_context.isMenu)
        {
            _waveBar.RefreshMenu();
            return;
        }

        if (_context.legacy == null || _context.game == null || _context.player == null)
        {
            return;
        }

        bool isStart = LegacyUiReader.GameState(_context.game) == GameManager.GameState.None;
        bool isPreparing = _context.IsPreparing();
        bool hasOverlay = LegacyUiReader.CurrentView(_context.ui) != ViewType.Game;
        _waveBar.Refresh(isStart, isPreparing, hasOverlay);
        if (!isPreparing || hasOverlay)
        {
            _context.isInventoryOpen = false;
            _view.Show("inventory-panel", false);
        }

        _waveBar.RefreshMana();
        _partyPanel.Refresh(isPreparing && !isStart && !hasOverlay);
        _spellBar.Refresh(!isStart && !hasOverlay && !_context.isPaused);
        _rewardPanel.Refresh();
        _wavePanel.Refresh();
        _detailPanel.Refresh();
        _inventoryPanel.RefreshEquipmentAction(isPreparing && !hasOverlay);
        if (_context.isInventoryOpen)
        {
            _inventoryPanel.Refresh();
        }
    }

    static void IgnorePicking(TemplateContainer container)
    {
        container.pickingMode = PickingMode.Ignore;
    }

    void StartOrAdvance()
    {
        if (_context.isMenu)
        {
            LoadScene(_gameplayScene);
            return;
        }

        if (_context.isPaused || _context.legacy == null)
        {
            return;
        }

        GameHUD hud = _context.legacy.gameHUD;
        bool isStart = LegacyUiReader.GameState(_context.game) == GameManager.GameState.None;
        if (isStart && hud.startGameButton.interactable)
        {
            hud.startGameButton.onClick.Invoke();
        }
        else if (hud.nextWaveButton.interactable)
        {
            hud.nextWaveButton.onClick.Invoke();
        }
    }

    void LoadScene(string scene)
    {
        _timeControls.ResetSpeed();
        bool loaded = sceneLoader != null ? sceneLoader.Invoke(scene) : ToolkitSceneNavigation.TryLoad(scene);
        if (!loaded)
        {
            _view.SetText("status-label", $"Scene '{scene}' is not included in this player build.");
        }
    }

    void OnEscape()
    {
        if (_mobileLayout.CloseDrawers())
        {
            return;
        }

        if (_context.isInventoryOpen)
        {
            _context.isInventoryOpen = false;
        }
        else if (_interactionActive || _context.hasInteraction)
        {
            if (_context.interaction != null)
            {
                _context.interaction.CancelInteraction();
            }
        }
        else
        {
            _timeControls.TogglePause();
        }
    }

    void OnInventoryClicked()
    {
        _context.isInventoryOpen = !_context.isInventoryOpen;
        Refresh();
    }

    void OnInventoryCloseClicked()
    {
        _context.isInventoryOpen = false;
        Refresh();
    }

    void OnRestartClicked()
    {
        LoadScene(_menuScene);
    }

    void OnMarkToggleChanged(ChangeEvent<bool> evt)
    {
        if (_context.legacy != null)
        {
            _context.legacy.gameHUD.markEntityToggle.isOn = evt.newValue;
        }
    }
}
