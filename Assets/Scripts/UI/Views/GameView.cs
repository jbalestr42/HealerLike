using System.Collections.Generic;
using UnityEngine;

public enum PanelType
{
    None,
    Entity
}

public class GameView : AView
{
    [SerializeField] CharacterSkillInventory _characterSkillInventory;
    public CharacterSkillInventory characterSkillInventory { get { return _characterSkillInventory; } }

    [SerializeField] EntityInventory _entityInventory;
    public EntityInventory entityInventory { get { return _entityInventory; } }

    [SerializeField] PlayerInventory _playerInventory;
    public PlayerInventory playerInventory { get { return _playerInventory; } }

    [SerializeField] GameHUD _gameHUD;
    public GameHUD gameHUD { get { return _gameHUD; } }

    [SerializeField] ToolTip _toolTip;
    public ToolTip toolTip { get { return _toolTip; } }

    PanelType _selectedPanel = PanelType.None;
    GameObject _selectedObject = null;

    public PanelType selectedPanel { get { return _selectedPanel; } }
    public GameObject selectedObject { get { return _selectedObject; } }

    [SerializeField] Dictionary<PanelType, APanel> _panels;
    public IDictionary<PanelType, APanel> panels { get { return _panels; } }

    // The run shown, null outside of one (e.g. the sandbox)
    AscensionGameType _ascension;
    bool _isAscensionShown = false;

#if UNITY_EDITOR
    // Editor only, every item of the game to try them (Items button)
    ItemCatalogView _itemCatalog;
#endif

    void Start()
    {
        _entityInventory.Bind(PlayerBehaviour.instance.roster);
        _ascension = FindAnyObjectByType<AscensionGameType>();
        if (_ascension == null)
        {
            return;
        }

        _gameHUD.inventoryButton.enabled = true;
        _gameHUD.nextWaveButton.onClick.AddListener(_ascension.StartBattle);
        if (_gameHUD.mapButton != null)
        {
            _gameHUD.mapButton.onClick.AddListener(_ascension.LookAtMap);
        }
        _gameHUD.manaTooltip = () => AscensionGameType.GetManaTooltip(PlayerBehaviour.instance.character.mana);
#if UNITY_EDITOR
        _gameHUD.CreateEditorButton("Kill Wave").onClick.AddListener(_ascension.KillEnemyWave);
        _gameHUD.CreateEditorButton("Items").onClick.AddListener(ToggleItemCatalog);
#endif
        RefreshRun();
    }

    void OnEnable()
    {
        _gameHUD.inventoryButton.onClick.AddListener(ToggleInventory);
    }

    void OnDisable()
    {
        _gameHUD.inventoryButton.onClick.RemoveListener(ToggleInventory);
    }

    public void ToggleInventory()
    {
        if (_playerInventory.IsInventoryVisible())
        {
            _playerInventory.HideInventory();
        }
        else
        {
            _playerInventory.ShowInventory();
        }
    }

    void Update()
    {
        RefreshSelection();
        UpdatePanel(_selectedPanel);
        RefreshRun();
    }

    // The buttons of the run follow its state: the units are arranged and given items only before a fight
    void RefreshRun()
    {
        if (_ascension == null)
        {
            return;
        }

        bool isPreparing = _ascension.isPreparing;
        bool hasRun = _ascension.run != null;
        _gameHUD.inventoryButton.interactable = isPreparing;
        _gameHUD.nextWaveButton.interactable = _ascension.canStartBattle;
        if (_gameHUD.mapButton != null)
        {
            _gameHUD.mapButton.interactable = _ascension.canLookAtMap;
        }
        _gameHUD.ShowManaBar(hasRun);
        _characterSkillInventory.Show(_ascension.hasStartedARound);
        _entityInventory.Show(isPreparing);
        if (!isPreparing && _playerInventory.IsInventoryVisible())
        {
            _playerInventory.HideInventory();
        }

        if (hasRun && !_isAscensionShown && _gameHUD.ascensionIndicator != null)
        {
            _isAscensionShown = true;
            _gameHUD.ascensionIndicator.Show(DataManager.instance.data.ascension, _ascension.run.ascensionLevel);
        }
    }

#if UNITY_EDITOR
    // Editor only, to try the items: every item of the game, made the first time it is opened
    void ToggleItemCatalog()
    {
        if (_itemCatalog == null)
        {
            SandboxButton buttonPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<SandboxButton>("Assets/Prefabs/UI/SandboxButton.prefab");
            _itemCatalog = ItemCatalogView.Create(_playerInventory.inventory, buttonPrefab, DataManager.instance.items, _ascension.AddUnitItem, _ascension.AddPlayerItem);
            return;
        }
        _itemCatalog.Show(!_itemCatalog.gameObject.activeSelf);
    }
#endif

    // The panel of the unit selected on the board
    void RefreshSelection()
    {
        InteractionManager interaction = InteractionManager.existingInstance;
        Component selected = interaction != null ? interaction.selected as Component : null;
        GameObject selectedObject = selected != null ? selected.gameObject : null;
        if (selectedObject == _selectedObject && (selectedObject == null) == (_selectedPanel == PanelType.None))
        {
            return;
        }

        HidePanel(_selectedPanel);
        if (selectedObject != null)
        {
            ShowPanel(PanelType.Entity, selectedObject);
        }
    }

    #region Panels

    public void ShowPanel(PanelType type, GameObject target)
    {
        if (_selectedPanel != type && type != PanelType.None)
        {
            if (_selectedPanel != PanelType.None)
            {
                panels[_selectedPanel].HideUI(target);
            }
            _selectedPanel = type;
            _selectedObject = target;
            panels[type].ShowUI(target);
        }
    }

    public void UpdatePanel(PanelType type)
    {
        if (type != PanelType.None && panels[type].IsActive())
        {
            if (_selectedObject == null)
            {
                HidePanel(type);
            }
            else
            {
                panels[type].UpdateUI(_selectedObject);
            }
        }
    }

    public void HidePanel(PanelType type)
    {
        if (type != PanelType.None && panels[type] != null)
        {
            panels[type].HideUI(_selectedObject);
            _selectedPanel = PanelType.None;
            _selectedObject = null;
        }
    }

    #endregion

    #region AView

    public override void Show()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
    }

    public override void Hide()
    {
        GetComponent<CanvasGroup>().alpha = 0.1f;
    }

    #endregion
}
