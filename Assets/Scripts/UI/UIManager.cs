using System.Collections.Generic;
using UnityEngine;

// Values are serialized in the scenes, never change them
public enum ViewType
{
    None = 0,
    Game = 1,
    Upgrade = 2,
    GameOver = 4,
    Map = 5,
    Event = 6,
}

// Shows the view of each screen the game opens, filled from the run
public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    Dictionary<ViewType, AView> _views = new Dictionary<ViewType, AView>();
    ViewType _currentView = ViewType.None;

    public ViewType currentView { get { return _currentView; } }

    List<ViewType> _viewStack = new List<ViewType>();

    GameManager _game;
    AscensionGameType ascension => _game != null ? _game.gameType as AscensionGameType : null;

    // Before any Start: the game opens its first screen from its own
    void Awake()
    {
        _game = GameManager.existingInstance;
        if (_game != null)
        {
            _game.screens.OnOpened.AddListener(OnScreenOpened);
            _game.screens.OnClosed.AddListener(OnScreenClosed);
        }

        PlayerBehaviour player = PlayerBehaviour.existingInstance;
        if (player != null)
        {
            player.OnCharacterInit.AddListener(OnCharacterInit);
        }

        UpgradeView upgradeView = GetView<UpgradeView>(ViewType.Upgrade);
        if (upgradeView != null)
        {
            upgradeView.OnItemSelected.AddListener(item => SelectReward(choice => choice.item == item));
            upgradeView.OnPlayerItemSelected.AddListener(item => SelectReward(choice => choice.item == item));
            upgradeView.OnEntitySelected.AddListener(unit => SelectReward(choice => choice.unit == unit));
        }

        MapView mapView = GetView<MapView>(ViewType.Map);
        if (mapView != null)
        {
            mapView.OnNodeSelected.AddListener(node => ascension?.SelectRoom(node));
            mapView.OnCloseRequested.AddListener(() => ascension?.CloseMap());
        }
    }

    public static ViewType ToViewType(GameScreen screen)
    {
        switch (screen)
        {
            case GameScreen.Game:
                return ViewType.Game;
            case GameScreen.Reward:
                return ViewType.Upgrade;
            case GameScreen.GameOver:
                return ViewType.GameOver;
            case GameScreen.Map:
                return ViewType.Map;
            case GameScreen.Event:
                return ViewType.Event;
            default:
                return ViewType.None;
        }
    }

    void OnScreenOpened(GameScreen screen)
    {
        ViewType type = ToViewType(screen);
        if (!_views.ContainsKey(type))
        {
            return;
        }

        AddView(type);
        Fill(type);
    }

    void OnScreenClosed(GameScreen screen)
    {
        if (_views.ContainsKey(ToViewType(screen)))
        {
            PopCurrentView();
        }
    }

    // What the run gives the view to show
    void Fill(ViewType type)
    {
        AscensionGameType run = ascension;
        if (run == null)
        {
            return;
        }

        switch (type)
        {
            case ViewType.Map:
                GetView<MapView>(type).Display(run.run, run.canSelectRoom);
                break;
            case ViewType.Upgrade:
                GetView<UpgradeView>(type).FillChoices(run.rewardChoices);
                break;
            case ViewType.Event:
                EventPrompt prompt = run.currentEvent;
                if (prompt != null)
                {
                    GetView<EventView>(type).Display(prompt.title, prompt.description, prompt.choices);
                }
                break;
            case ViewType.GameOver:
                GetView<GameOverView>(type).ShowResult(run.isVictory);
                break;
        }
    }

    void SelectReward(System.Predicate<RewardChoice> match)
    {
        AscensionGameType run = ascension;
        if (run == null)
        {
            return;
        }

        foreach (RewardChoice choice in run.rewardChoices)
        {
            if (match(choice))
            {
                GetView<UpgradeView>(ViewType.Upgrade).ClearChoices();
                run.SelectReward(choice);
                return;
            }
        }
    }

    // A button per skill, and the resources of the character in the sandbox, whose HUD is hidden in the scene
    void OnCharacterInit(Character character)
    {
        GameView gameView = GetView<GameView>(ViewType.Game);
        if (gameView == null)
        {
            return;
        }

        foreach (CharacterSkillSlot slot in character.skillSlots)
        {
            gameView.characterSkillInventory.Create(slot, character);
        }

        if (_game != null && _game.gameType is SandboxGameType)
        {
            gameView.gameHUD.ShowResourcesOnly();
            gameView.gameHUD.ShowCharacter(character);
        }
    }

    #region Views

    public void AddView(ViewType type)
    {
        if (_currentView != ViewType.None)
        {
            _views[_currentView].Hide();
        }

        _currentView = type;
        _viewStack.Add(_currentView);
        _views[_currentView].gameObject.SetActive(true);
        _views[_currentView].Show();
    }

    public void PopCurrentView()
    {
        if (_currentView != ViewType.None)
        {
            _views[_currentView].gameObject.SetActive(false);
            _viewStack.Remove(_currentView);
        }

        _currentView = _viewStack[_viewStack.Count - 1];
        _views[_currentView].Show();
    }

    public T GetView<T>(ViewType type) where T : AView
    {
        return _views.TryGetValue(type, out AView view) ? view as T : null;
    }

    #endregion
}
