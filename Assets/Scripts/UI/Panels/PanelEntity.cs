using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PanelEntity : APanel
{
    [SerializeField] TMP_Dropdown _targetBehaviourDropdown;
    [SerializeField] SlotInventory _inventory;

    [Serializable]
    class AttributeUI
    {
        public string name;
        public AttributeType attributeType;
        public TMP_Text text;
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

        Entity entity = selectedObject.GetComponent<Entity>();
        _targetBehaviourDropdown.SetValueWithoutNotify((int)entity.GetComponent<ITargetProvider>().targetBehaviourType);
        _targetBehaviourDropdown.onValueChanged.AddListener((int index) => entity.GetComponent<ITargetProvider>().targetBehaviourType = (TargetBehaviourType)index);
        _inventory.inventoryHandler = entity.inventoryHandler;
        _inventory.RefreshInventory();
    }

    public override void UpdateUI(GameObject selectedObject)
    {
        if (selectedObject != null)
        {
            Entity entity = selectedObject.GetComponent<Entity>();
            AttributeManager attributeManager = entity.GetComponent<AttributeManager>();
            
            foreach (var attributeUI in _attributeUI)
            {
                attributeUI.text.text = $"{attributeUI.name}: {attributeManager.Get(attributeUI.attributeType).Value.ToString("F2")}";
            }
        }
    }
    
    public override void OnHideUI(GameObject selectedObject)
    {
        if (selectedObject != null)
        {
            Entity entity = selectedObject.GetComponent<Entity>();
            _targetBehaviourDropdown.onValueChanged.RemoveAllListeners();
        }
    }
}