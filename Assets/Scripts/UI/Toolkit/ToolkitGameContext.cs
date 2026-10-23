// State shared by the Toolkit panels: the game's managers and what the player has opened or selected
public class ToolkitGameContext
{
    public GameManager game;
    public PlayerBehaviour player;
    public EntityManager entities;
    public InteractionManager interaction;
    public AscensionGameType ascension;
    // The game data played, null without a DataManager (e.g. the menu)
    public GameData data;
    public bool isMenu = false;
    public bool isPaused = false;
    public bool isInventoryOpen = false;
    public bool isSheetOpen = false;
    public bool isInspecting = false;
    public Entity selectedEntity;
    public AItem selectedItem;
    public InventoryHandler selectedItemOwner;
    public bool hasInteraction
    {
        get { return interaction != null && interaction.GetInteraction() != null; }
    }

    public GameScreens screens
    {
        get { return game != null ? game.screens : null; }
    }

    public InventoryHandler stash
    {
        get { return player != null ? player.stash : null; }
    }

    public void Init()
    {
        game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
        entities = UnityEngine.Object.FindAnyObjectByType<EntityManager>();
        interaction = UnityEngine.Object.FindAnyObjectByType<InteractionManager>();
        ascension = UnityEngine.Object.FindAnyObjectByType<AscensionGameType>();
        DataManager dataManager = UnityEngine.Object.FindAnyObjectByType<DataManager>();
        data = dataManager != null ? dataManager.data : null;
        isMenu = game == null;
    }

    // The screen on top, None in the menu
    public GameScreen currentScreen
    {
        get { return !isMenu && screens != null ? screens.current : GameScreen.None; }
    }

    public bool IsCurrentView(GameScreen screen)
    {
        return currentScreen == screen;
    }

    public bool IsPreparing()
    {
        return ascension == null || ascension.isPreparing;
    }
}
