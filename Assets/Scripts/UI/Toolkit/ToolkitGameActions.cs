using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitGameActions : IDisposable
{
    readonly ToolkitGameUI _host;
    readonly ToolkitGameContext _context;
    readonly ToolkitGameView _view;
    readonly ToolkitTimeControls _timeControls;
    readonly ToolkitMobileLayout _mobileLayout;
    readonly ToolkitMapPanel _mapPanel;
    readonly Toggle _markToggle;
    readonly ToolkitClassSelect _classSelect = new ToolkitClassSelect();
    bool _interactionActive;

    // The class choice Start opens on the menu
    public ToolkitClassSelect classSelect
    {
        get { return _classSelect; }
    }

    public ToolkitGameActions(
        ToolkitGameUI host,
        ToolkitGameContext context,
        ToolkitGameView view,
        ToolkitTimeControls timeControls,
        ToolkitMobileLayout mobileLayout,
        ToolkitMapPanel mapPanel
    )
    {
        _host = host;
        _context = context;
        _view = view;
        _timeControls = timeControls;
        _mobileLayout = mobileLayout;
        _mapPanel = mapPanel;
        view.AddClickListener("wave-button", StartOrAdvance);
        view.AddClickListener("start-button", StartOrAdvance);
        view.AddClickListener("sandbox-button", OnSandboxClicked);
        view.AddClickListener("inventory-button", OnInventoryClicked);
        view.AddClickListener("inventory-close-button", OnInventoryCloseClicked);
        view.AddClickListener("restart-button", OnRestartClicked);
        view.AddClickListener("menu-button", OnRestartClicked);
        view.AddClickListener(ToolkitClassSelect.BackButtonName, OnClassBackClicked);
        _classSelect.Init(view, OnClassChosen);
        _markToggle = view.root.Q<Toggle>("mark-entity-toggle");
        _markToggle.RegisterValueChangedCallback(OnMarkToggleChanged);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnEscape();
        }

        _interactionActive = _context.hasInteraction;
    }

    public void Dispose()
    {
        _markToggle.UnregisterValueChangedCallback(OnMarkToggleChanged);
        _view.RemoveClickListener("wave-button", StartOrAdvance);
        _view.RemoveClickListener("start-button", StartOrAdvance);
        _view.RemoveClickListener("sandbox-button", OnSandboxClicked);
        _view.RemoveClickListener("inventory-button", OnInventoryClicked);
        _view.RemoveClickListener("inventory-close-button", OnInventoryCloseClicked);
        _view.RemoveClickListener("restart-button", OnRestartClicked);
        _view.RemoveClickListener("menu-button", OnRestartClicked);
        _view.RemoveClickListener(ToolkitClassSelect.BackButtonName, OnClassBackClicked);
        _classSelect.Init(null, null);
    }

    void StartOrAdvance()
    {
        if (_context.isMenu)
        {
            // The class choice comes first; a menu without game data keeps the direct start (a random class)
            if (!_classSelect.isOpen && !_classSelect.Open(_host.gameData))
            {
                LoadScene(_host.gameplayScene);
            }

            return;
        }

        if (_context.isPaused || _context.legacy == null || !_context.IsCurrentView(ViewType.Game))
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

    // The sandbox is a menu choice only, a running expedition never leaves through it
    void OnSandboxClicked()
    {
        if (_context.isMenu)
        {
            _classSelect.Close();
            LoadScene(_host.sandboxScene);
        }
    }

    // The pick is already in CharacterSelection; the expedition starts through the same route as before
    void OnClassChosen(CharacterData character)
    {
        LoadScene(_host.gameplayScene);
    }

    void OnClassBackClicked()
    {
        _classSelect.Close();
    }

    void LoadScene(string scene)
    {
        _timeControls.ResetSpeed();
        bool loaded =
            _host.sceneLoader != null ? _host.sceneLoader.Invoke(scene) : ToolkitSceneNavigation.TryLoad(scene);
        if (!loaded)
        {
            _view.SetText("status-label", $"Scene '{scene}' is not included in this player build.");
        }
    }

    void OnEscape()
    {
        if (_classSelect.isOpen)
        {
            _classSelect.Close();
            return;
        }

        if (_mapPanel.Close())
        {
            return;
        }

        if (_mobileLayout.CloseDrawers())
        {
            return;
        }

        if (_context.isSheetOpen)
        {
            _context.isSheetOpen = false;
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
        _host.Refresh();
    }

    void OnInventoryCloseClicked()
    {
        _context.isInventoryOpen = false;
        _host.Refresh();
    }

    void OnRestartClicked()
    {
        LoadScene(_host.menuScene);
    }

    void OnMarkToggleChanged(ChangeEvent<bool> evt)
    {
        if (_context.legacy != null)
        {
            _context.legacy.gameHUD.markEntityToggle.isOn = evt.newValue;
        }
    }
}
