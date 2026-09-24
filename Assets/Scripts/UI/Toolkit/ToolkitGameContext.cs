// State shared by the Toolkit panels: the game's managers and what the player has opened or selected
public class ToolkitGameContext
{
    public UIManager ui;
    public GameManager game;
    public PlayerBehaviour player;
    public EntityManager entities;
    public InteractionManager interaction;
    public GameView legacy;
    public AscensionGameType ascension;
    public bool isMenu = false;
    public bool isPaused = false;
    public bool isInventoryOpen = false;
    public bool isInspecting = false;
    public Entity selectedEntity;
    public AItem selectedItem;
    public InventoryHandler selectedItemOwner;

    public bool hasInteraction { get { return interaction != null && interaction.GetInteraction() != null; } }

    public InventoryHandler stash { get { return legacy.playerInventory.inventory.inventoryHandler; } }

    public void Init()
    {
        ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
        entities = UnityEngine.Object.FindAnyObjectByType<EntityManager>();
        interaction = UnityEngine.Object.FindAnyObjectByType<InteractionManager>();
        ascension = UnityEngine.Object.FindAnyObjectByType<AscensionGameType>();
        isMenu = game == null;
        if (ui != null)
        {
            legacy = ui.GetView<GameView>(ViewType.Game);
        }
    }

    public bool IsCurrentView(ViewType type)
    {
        return !isMenu && ui != null && LegacyUiReader.CurrentView(ui) == type;
    }

    public bool IsPreparing()
    {
        return ascension == null || LegacyUiReader.AscensionState(ascension) == AscensionGameType.State.WaitForRoundToStart;
    }
}
