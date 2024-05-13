using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PanelTower : APanel
{
    [SerializeField] TMP_Dropdown _targetBehaviourDropdown;
    [SerializeField] Button _sellButton;
    [SerializeField] SlotInventory _inventory;

    [Serializable]
    class AttributeUI
    {
        public string name;
        public AttributeType attributeType;
        public TMP_Text text;
        public Button addButton;
    }

    [SerializeField] List<AttributeUI> _attributeUI = new List<AttributeUI>();

    public override void OnShowUI(GameObject selectedObject)
    {
        _targetBehaviourDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string behaviourType in Enum.GetNames(typeof(TargetBehaviourType)))
        {
            options.Add(new TMP_Dropdown.OptionData(behaviourType));
        }
        _targetBehaviourDropdown.AddOptions(options);

        Tower tower = selectedObject.GetComponent<Tower>();
        _targetBehaviourDropdown.SetValueWithoutNotify((int)tower.GetComponent<ITargetProvider>().targetBehaviourType);
        _targetBehaviourDropdown.onValueChanged.AddListener((int index) => tower.GetComponent<ITargetProvider>().targetBehaviourType = (TargetBehaviourType)index);
        _sellButton.onClick.AddListener(tower.OnSold);
        _inventory.inventoryHandler = tower.inventoryHandler;
        _inventory.RefreshInventory();

        foreach (var attributeUI in _attributeUI)
        {
            attributeUI.addButton.onClick.AddListener(() => tower.UpgradeAttribute(attributeUI.attributeType));
        }
    }

    public override void UpdateUI(GameObject selectedObject)
    {
        if (selectedObject != null)
        {
            Tower tower = selectedObject.GetComponent<Tower>();
            AttributeManager attributeManager = tower.GetComponent<AttributeManager>();
            
            foreach (var attributeUI in _attributeUI)
            {
                attributeUI.text.text = $"{attributeUI.name}: {attributeManager.Get(attributeUI.attributeType).Value.ToString("F2")}";
                attributeUI.addButton.GetComponentInChildren<TMP_Text>().text = $"+ {tower.GetUpgradeAttribute(attributeUI.attributeType).cost}";
                attributeUI.addButton.interactable = tower.CanUpgradeAttribute(attributeUI.attributeType);
            }
        }
    }
    
    public override void OnHideUI(GameObject selectedObject)
    {
        if (selectedObject != null)
        {
            Tower tower = selectedObject.GetComponent<Tower>();
            _targetBehaviourDropdown.onValueChanged.RemoveAllListeners();
            _sellButton.onClick.RemoveAllListeners();
            
            foreach (var attributeUI in _attributeUI)
            {
                attributeUI.addButton.onClick.RemoveAllListeners();
            }
        }
    }
}