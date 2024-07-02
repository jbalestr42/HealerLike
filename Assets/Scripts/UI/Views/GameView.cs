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

    [SerializeField] InventoryItemDescription _itemDescription;
    public InventoryItemDescription itemDescription { get { return _itemDescription; } }

    [SerializeField] GameHUD _gameHUD;
    public GameHUD gameHUD { get { return _gameHUD; } }

    [SerializeField] ToolTip _toolTip;
    public ToolTip toolTip { get { return _toolTip; } }

    PanelType _selectedPanel = PanelType.None;
    GameObject _selectedObject = null;

    [SerializeField] Dictionary<PanelType, APanel> _panels;
    public IDictionary<PanelType, APanel> panels { get { return _panels; } }

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
            _itemDescription.gameObject.SetActive(false);
        }
        else
        {
            _playerInventory.ShowInventory();
            _itemDescription.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        UpdatePanel(_selectedPanel);
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
